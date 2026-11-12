# Staging server setup (one-time)

These steps prepare the staging server so that `.github/workflows/deploy-staging.yml`
can deploy to it. They only need to be run once (or again after provisioning a new server).

Run as root unless stated otherwise.

## 1. Install the ASP.NET Core 10 runtime

Follow the official Microsoft instructions for the distro in use, for example on
Debian/Ubuntu via the `dotnet-install` script or the Microsoft package feed:

```sh
curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
sh ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
```

## 2. Create the deploy user

```sh
useradd -m -s /bin/bash tam-deploy
mkdir -p /home/tam-deploy/.ssh
echo "<public key matching the T55_STAGING_DEPLOY_KEY secret>" >> /home/tam-deploy/.ssh/authorized_keys
chown -R tam-deploy:tam-deploy /home/tam-deploy/.ssh
chmod 700 /home/tam-deploy/.ssh
chmod 600 /home/tam-deploy/.ssh/authorized_keys
```

## 3. Create the deploy directory

```sh
mkdir -p /var/www/trento-amodo-api/releases
chown -R tam-deploy:tam-deploy /var/www/trento-amodo-api
```

## 4. Install the systemd service

This is a system-level unit (`/etc/systemd/system/...`), not a user-level one:
in LXC/Docker containers `systemctl --user` / lingering / the logind D-Bus
session are often broken or disabled ("Failed to connect to bus: No medium
found"), while the system-level systemd instance (PID 1) works normally. The
unit itself runs the process unprivileged, as `tam-deploy` (`User=tam-deploy`
in the unit file).

```sh
cp deploy/systemd/trento-amodo-api.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable trento-amodo-api
```

The service will keep restarting (`Restart=always`) until the first deploy
creates the `current` symlink.

## 5. Allow tam-deploy to restart the service without a password

The deploy workflow restarts the service over SSH as `tam-deploy`, which has
no root access otherwise. Grant only this one command, via `visudo`:

```sh
echo 'tam-deploy ALL=(root) NOPASSWD: /usr/bin/systemctl restart trento-amodo-api' \
  | tee /etc/sudoers.d/tam-deploy-trento-amodo-api
chmod 440 /etc/sudoers.d/tam-deploy-trento-amodo-api
visudo -c
```

## 6. Open inbound ports in the cloud firewall

This server runs on Oracle Cloud (OCI): besides the local `iptables` rules
below, the VCN's Security List (or NSG, if the instance uses one instead)
must also allow the traffic, or it never reaches the instance at all.

In the OCI console, under the instance's VCN → Security Lists (or the
attached NSG), add ingress rules for TCP `80` and `443`, source `0.0.0.0/0`.

**Set `Source Port Range` to `All`, not to `80`/`443`.** That field filters
the *client's* source port, which is always an ephemeral port, never 80/443
— setting it to match the destination port silently blocks all real
traffic while looking correctly configured.

Then allow the same ports in the instance's local firewall (these images
preconfigure `iptables` to only allow SSH):

```sh
iptables -I INPUT 5 -p tcp --dport 80 -m state --state NEW -j ACCEPT
iptables -I INPUT 6 -p tcp --dport 443 -m state --state NEW -j ACCEPT
iptables -L INPUT -n --line-numbers   # confirm both ACCEPT rules sit before the REJECT catch-all
netfilter-persistent save
```

## 7. Install and configure nginx

```sh
apt install nginx
cp deploy/nginx/trento-amodo-api.conf /etc/nginx/sites-available/trento-amodo-api.conf
ln -s /etc/nginx/sites-available/trento-amodo-api.conf /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

## 8. Enable TLS

```sh
apt install -y certbot python3-certbot-nginx
certbot --nginx -d trento-amodo-api.briansivieri.com
```

Certbot rewrites `/etc/nginx/sites-available/trento-amodo-api.conf` in place
(adds the `443 ssl` server block and an HTTP→HTTPS redirect) — keep
`deploy/nginx/trento-amodo-api.conf` in the repo in sync with whatever it
produces. Renewal is handled by a systemd timer already installed with the
package; confirm it with:

```sh
systemctl list-timers | grep certbot
certbot renew --dry-run
```

Traffic for this domain is proxied through Cloudflare. Validation uses the
HTTP-01 challenge over plain port 80, which Cloudflare forwards to the
origin unmodified regardless of the SSL/TLS encryption mode — so renewals
keep working automatically as long as port 80 stays reachable from
Cloudflare to the origin (steps 6–7 above).

If Cloudflare's SSL/TLS encryption mode (dashboard → SSL/TLS → Overview) is
**Full (strict)**, this step is required for HTTPS visitors to reach the
origin at all: Cloudflare only accepts a publicly trusted certificate on the
origin, and Let's Encrypt qualifies.

## 9. Install Postgres

```sh
apt install -y postgresql
```

By default Postgres listens on `localhost` only (`listen_addresses = 'localhost'`
in `postgresql.conf`) — unlike step 6, **no firewall changes are needed**: the
database is never exposed to the Internet, only reachable from the app on the
same host (or via an SSH tunnel, see below).

Create the database and a dedicated role:

```sh
sudo -u postgres psql -c "CREATE ROLE trento_amodo WITH LOGIN PASSWORD '<strong-password>';"
sudo -u postgres createdb -O trento_amodo trento_amodo
```

Store the connection string outside the repo, in a file that systemd reads
as an `EnvironmentFile` (the unit file itself, `EnvironmentFile=` line
included, stays tracked in git — only this file holds the secret):

```sh
mkdir -p /etc/trento-amodo-api
cat > /etc/trento-amodo-api/environment <<'EOF'
ConnectionStrings__Default=Host=localhost;Port=5432;Database=trento_amodo;Username=trento_amodo;Password=<strong-password>
EOF
chown tam-deploy:tam-deploy /etc/trento-amodo-api/environment
chmod 600 /etc/trento-amodo-api/environment
systemctl daemon-reload
systemctl restart trento-amodo-api
```

(The double underscore in `ConnectionStrings__Default` is how ASP.NET Core
maps a nested config key from an environment variable. The unit file's
`EnvironmentFile=-/etc/trento-amodo-api/environment` has a leading `-`, so
systemd doesn't fail to load it if this step hasn't run yet — matching the
"keep restarting" behavior from step 4.)

The file is owned by `tam-deploy`, not `root`: the deploy workflow (running
as `tam-deploy` over SSH) reads it to apply migrations before each release
(see below). This doesn't widen the secret's exposure — `tam-deploy` already
controls the systemd service that reads this same value on every restart.

## Migrations run automatically on deploy

`.github/workflows/deploy-staging.yml` builds a self-contained EF Core
migrations bundle (no .NET SDK needed on the server) and runs it against
`/etc/trento-amodo-api/environment`'s connection string before swapping in
the new release. It's safe on every push to `main`, migration or not — EF
Core skips what's already applied.

## Accessing the staging DB from your IDE

The database is not exposed publicly, so use an SSH tunnel instead of opening
a port. From your machine (replace `<ssh-user>` and `<staging-host>`; pick a
free local port, e.g. `5434`, if you already have a local Postgres on 5432):

```sh
ssh -L 5434:localhost:5432 <ssh-user>@<staging-host>
```

Keep that session open, then point your IDE's database tool at
`localhost:5434` with the same `trento_amodo`/`<strong-password>` credentials
used above. Migrations themselves no longer need to be run this way — see
above — but the tunnel is still useful to inspect data directly.

## GitHub secrets required

Configured as organization secrets:

- `T55_STAGING_DEPLOY_HOST` — staging server host/IP
- `T55_STAGING_DEPLOY_KEY` — private key matching the public key installed in step 2
