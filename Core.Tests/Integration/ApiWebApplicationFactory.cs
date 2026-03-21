using Infrastructure.DAL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Tests.Integration;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MyFeaturesDbContext>>();
            services.RemoveAll<MyFeaturesDbContext>();

            services.AddDbContext<MyFeaturesDbContext>(options =>
            {
                options.UseInMemoryDatabase($"MyFeaturesIntegrationTests-{Guid.NewGuid()}");
            });
        });
    }
}