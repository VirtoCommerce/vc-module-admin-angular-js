using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.AdminAngularJs.Data.Repositories;

namespace VirtoCommerce.AdminAngularJs.Data.SqlServer;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdminAngularJsDbContext>
{
    public AdminAngularJsDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AdminAngularJsDbContext>();
        var connectionString = args.Length != 0 ? args[0] : "Server=(local);User=virto;Password=virto;Database=VirtoCommerce3;";

        builder.UseSqlServer(
            connectionString,
            options => options.MigrationsAssembly(typeof(SqlServerDataAssemblyMarker).Assembly.GetName().Name));

        return new AdminAngularJsDbContext(builder.Options);
    }
}
