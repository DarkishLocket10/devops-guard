using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevOpsGuard.Infrastructure.Data;

public class DesignTimeFactory : IDesignTimeDbContextFactory<DevOpsGuardDbContext>
{
    public DevOpsGuardDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DevOpsGuardDbContext>();
        // Match your dev connection string (same as appsettings.Development.json)
        var cs = "Server=localhost,14333;Database=DevOpsGuard;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
        optionsBuilder.UseSqlServer(cs);
        return new DevOpsGuardDbContext(optionsBuilder.Options);
    }
}
