using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IdentityService.Infrastructure.Data;

namespace IdentityService.Tests.Fixtures;

public class IdentityServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove all EF Core related services that use PostgreSQL
            var dbContextDescriptor = services.FirstOrDefault(d => 
                d.ServiceType == typeof(IdentityDbContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dbContextOptionsDescriptor = services.FirstOrDefault(d => 
                d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (dbContextOptionsDescriptor != null)
                services.Remove(dbContextOptionsDescriptor);

            // Remove any factory options
            var dbContextOptionsFactoryDescriptor = services.FirstOrDefault(d => 
                d.ServiceType.IsGenericType && 
                d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                d.ServiceType.GetGenericArguments()[0] == typeof(IdentityDbContext));
            if (dbContextOptionsFactoryDescriptor != null)
                services.Remove(dbContextOptionsFactoryDescriptor);

            // Add in-memory database for testing
            var dbName = $"IdentityServiceTestDb_{Guid.NewGuid()}";
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(dbName), 
                ServiceLifetime.Scoped, 
                ServiceLifetime.Scoped);

            // Build temporary service provider to seed database
            using (var sp = services.BuildServiceProvider())
            {
                using (var scope = sp.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    dbContext.Database.EnsureCreated();
                }
            }
        });
    }
}
