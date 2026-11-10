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

## 4. Enable lingering for tam-deploy

Required so the user-level systemd service keeps running (and starts at boot)
without an active SSH session:

```sh
loginctl enable-linger tam-deploy
```

## 5. Install the systemd user service

As `tam-deploy`:

```sh
mkdir -p ~/.config/systemd/user
cp deploy/systemd/trento-amodo-api.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable trento-amodo-api
```

The service will keep restarting (`Restart=always`) until the first deploy
creates the `current` symlink.

## 6. Install and configure nginx

```sh
apt install nginx
cp deploy/nginx/trento-amodo-api.conf /etc/nginx/sites-available/trento-amodo-api.conf
ln -s /etc/nginx/sites-available/trento-amodo-api.conf /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

## 7. (Later) Enable TLS

```sh
certbot --nginx -d trento-amodo-api.briansivieri.com
```

## GitHub secrets required

Configured as organization secrets:

- `T55_STAGING_DEPLOY_HOST` — staging server host/IP
- `T55_STAGING_DEPLOY_KEY` — private key matching the public key installed in step 2
