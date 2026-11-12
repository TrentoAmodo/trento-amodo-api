using Microsoft.EntityFrameworkCore;

namespace TrentoAmodo.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
