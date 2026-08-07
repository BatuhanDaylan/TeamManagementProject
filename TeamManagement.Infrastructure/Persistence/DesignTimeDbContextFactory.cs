using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TeamManagement.Infrastructure.Persistence
{
    // EF Tools'un migration sırasında DbContext oluşturabilmesi için gerekli factory
    public class AppDbContextFactory : IDesignTimeDbContextFactory<TeamManagementAppDbContext>
    {
        public TeamManagementAppDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "TeamManagement.Api"))
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<TeamManagementAppDbContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("AppConnection"));

            return new TeamManagementAppDbContext(optionsBuilder.Options);
        }
    }

    public class IdentityDbContextFactory : IDesignTimeDbContextFactory<TeamManagementIdentityDbContext>
    {
        public TeamManagementIdentityDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "TeamManagement.Api"))
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<TeamManagementIdentityDbContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("IdentityConnection"));

            return new TeamManagementIdentityDbContext(optionsBuilder.Options);
        }
    }
}
