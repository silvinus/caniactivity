using Microsoft.EntityFrameworkCore;
using Caniactivity.Models;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Caniactivity.Mysql
{
    public abstract class Marker { }

    public class MysqlMigrationContext : IDesignTimeDbContextFactory<CaniActivityContext>
    {
        public CaniActivityContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CaniActivityContext>();
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            optionsBuilder
                .UseMySQL(
                    configurationRoot.GetConnectionString("Mysql"),
                    x => x.MigrationsAssembly("Caniactivity.Mysql")
                );

            return new CaniActivityContext(optionsBuilder.Options, configurationRoot);
        }
    }
}
