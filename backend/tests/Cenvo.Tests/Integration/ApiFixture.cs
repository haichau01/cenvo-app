using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Cenvo.Api.Auth;
using Cenvo.Infrastructure.Persistence;
using Cenvo.SharedKernel;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Cenvo.Tests.Integration;

/// <summary>One Postgres (Testcontainers) + one API host shared by the whole collection, seeded with sample data.</summary>
public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", _pg.GetConnectionString());
            b.UseSetting("Database:Migrate", "true");
            b.UseSetting("Database:Seed", "true");
            b.UseSetting("Storage:Photos:Root", Path.Combine(Path.GetTempPath(), "cenvo-test-photos"));
            b.UseSetting("RateLimits:CredentialAttemptsPerMinute", "100000"); // every test logs in many times from one address
        });
        _factory.CreateClient(); // starts the host → migrate + seed
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _pg.DisposeAsync();
    }

    /// <summary>Direct database access (as the system, every branch) — only to set up states the API cannot reach, e.g. the past.</summary>
    public async Task WithDb(Func<AppDbContext, Task> work)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentUser>().RunAs(SystemUser.AllBranches);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await work(db);
        await db.SaveChangesAsync();
    }

    /// <summary>Signed-in client (default seed password).</summary>
    public async Task<HttpClient> LoginAs(string userName)
    {
        var client = _factory.CreateClient();
        var password = userName == "admin" ? "Admin@123" : "123456";
        var res = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        res.EnsureSuccessStatusCode();
        var body = (await res.Content.ReadFromJsonAsync<JsonNode>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body["accessToken"]!.GetValue<string>());
        return client;
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
