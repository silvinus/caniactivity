using Bogus.Platform;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Caniactivity.Models;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Caniactivity.Sqlite;

public abstract class Marker {}

public class SqliteMigrationContext : IDesignTimeDbContextFactory<CaniActivityContext>
{
    public CaniActivityContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CaniActivityContext>();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        optionsBuilder
            .UseSqlite(
                configurationRoot.GetConnectionString("Sqlite"),
                x => x.MigrationsAssembly("Caniactivity.Sqlite")
            );

        return new CaniActivityContext(optionsBuilder.Options);
    }
}