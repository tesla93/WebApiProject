using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Project.Data;
using Microsoft.EntityFrameworkCore;
using Core.Services;
using Microsoft.Extensions.Configuration;

namespace Project.Data.SqlServer
{
    public class DataContext: DataContextBase
    {
        private readonly IConfiguration _config;

        public DataContext(DbContextOptions<DataContext> options, IDbServices dbServices, IConfiguration config) : base(options, dbServices)
        {
            _config = config;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _config.GetValue<string>("ConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}
