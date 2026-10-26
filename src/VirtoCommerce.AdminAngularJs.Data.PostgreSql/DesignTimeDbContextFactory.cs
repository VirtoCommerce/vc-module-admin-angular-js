using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.AdminAngularJs.Data.Repositories;

namespace VirtoCommerce.AdminAngularJs.Data.PostgreSql;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdminAngularJsDbContext>
{
    public AdminAngularJsDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AdminAngularJsDbContext>();
        var connectionString = args.Length != 0 ? args[0] : "Server=localhost;Username=virto;Password=virto;Database=VirtoCommerce3;";

        builder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(typeof(PostgreSqlDataAssemblyMarker).Assembly.GetName().Name));

        return new AdminAngularJsDbContext(builder.Options);
    }
}
