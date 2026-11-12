using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrentoAmodo.Infrastructure.Persistence;

// Lets EF Core design-time tools build an AppDbContext without running Program.cs, which needs a real connection string unavailable in CI.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=design_time");

        return new AppDbContext(optionsBuilder.Options);
    }
}
