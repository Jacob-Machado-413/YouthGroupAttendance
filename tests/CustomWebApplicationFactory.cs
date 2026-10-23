using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YouthGroupAttendance.Api.Data;

namespace YouthGroupAttendance.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = $"TestDb_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("YOUTH_GROUP_API_KEY", "test-api-key");
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<YouthGroupContext>>();
            services.AddDbContext<YouthGroupContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    public Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        DatabaseInitializer.Initialize(scope.ServiceProvider.GetRequiredService<YouthGroupContext>());
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            File.Delete(_dbPath + suffix);
    }
}
