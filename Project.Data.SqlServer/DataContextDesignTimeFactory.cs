using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Core.Services;
using System.Diagnostics;
using System.IO;

namespace Data.SqlServer
{
    public class DataContextDesignTimeFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            Trace.WriteLine($"BasePath: {basePath}");
            var configuration = new ConfigurationBuilder()
               .SetBasePath(basePath)
               .AddJsonFile("appsettings.json")
               .AddJsonFile($"appsettings.{environment}.json")
               .Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var builder = new DbContextOptionsBuilder<DataContext>();
            builder.UseSqlServer(connectionString);

            return new DataContext(builder.Options, new DbServices());
        }
    }
}