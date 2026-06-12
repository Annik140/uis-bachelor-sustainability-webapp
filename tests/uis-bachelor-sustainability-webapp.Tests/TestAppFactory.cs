using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace uis_bachelor_sustainability_webapp.Tests;

internal sealed class TestAppFactory : WebApplicationFactory<Program>
{
    public const string BootstrapUser = "testadmin";
    public const string BootstrapPassword = "test-password-1234";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=ignored;Username=ignored;Password=ignored",
                ["ADMIN_BOOTSTRAP_USER"] = BootstrapUser,
                ["ADMIN_BOOTSTRAP_PASSWORD"] = BootstrapPassword,
            };

            configBuilder.AddInMemoryCollection(values);
        });
    }
}

internal static class TestHttpHelpers
{
    public static async Task<HttpResponseMessage> LoginAsync(HttpClient client)
    {
        return await client.PostAsJsonAsync("/admin/login", new
        {
            username = TestAppFactory.BootstrapUser,
            password = TestAppFactory.BootstrapPassword,
        });
    }

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var csrfResponse = await client.GetAsync("/admin/csrf-token");
        if (csrfResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await csrfResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to get CSRF token. Status: {csrfResponse.StatusCode}, Body: {body}");
        }

        var payload = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenPayload>();
        if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("CSRF token payload was empty.");
        }

        return payload.Token;
    }

    private sealed record CsrfTokenPayload(string Token);
}
