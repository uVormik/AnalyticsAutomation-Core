using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App.Api.IntegrationTests;

public sealed class AuthenticatedEndpointTests(AppApiFactory factory) : IClassFixture<AppApiFactory>
{
    [Fact]
    public async Task GroupTreeNodesAllowsAuthenticated()
    {
        using HttpClient client = factory.CreateClient();

        string login = $"s2-06-user-{Guid.NewGuid():N}";
        const string password = "S2-06-test-password!";
        Guid deviceId = Guid.NewGuid();

        await SeedUserAsync(login, password);

        using HttpResponseMessage signInResponse = await client.PostAsJsonAsync(
            "/api/auth/sign-in",
            new
            {
                login,
                password,
                deviceId
            });

        await AssertStatusCodeAsync("sign in", HttpStatusCode.OK, signInResponse);

        string signInJson = await signInResponse.Content.ReadAsStringAsync();
        using JsonDocument signInDocument = JsonDocument.Parse(signInJson);

        string accessToken = ReadRequiredStringProperty(
            signInDocument.RootElement,
            "accessToken",
            "AccessToken");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/group-tree/nodes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await client.SendAsync(request);

        await AssertStatusCodeAsync("group tree nodes", HttpStatusCode.OK, response);
    }

    private async Task SeedUserAsync(string login, string password)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AuthUser>>();

        string normalizedLogin = login.Trim().ToUpperInvariant();

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = login,
            NormalizedLogin = normalizedLogin,
            DisplayName = "S2-06 Test User",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.AuthUsers.Add(user);
        await dbContext.SaveChangesAsync();
    }

    private static string ReadRequiredStringProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property)
                && property.ValueKind == JsonValueKind.String)
            {
                string? value = property.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        throw new InvalidOperationException(
            $"None of the required string properties were present: {string.Join(", ", propertyNames)}.");
    }

    private static async Task AssertStatusCodeAsync(
        string stepName,
        HttpStatusCode expectedStatus,
        HttpResponseMessage response)
    {
        if (response.StatusCode == expectedStatus)
        {
            return;
        }

        string responseBody = await response.Content.ReadAsStringAsync();
        string redactedResponseBody = RedactSensitiveTokenValues(responseBody);

        Assert.Fail(
            $"""
            Step: {stepName}
            Expected status: {expectedStatus} ({(int)expectedStatus})
            Actual status: {response.StatusCode} ({(int)response.StatusCode})
            Response body:
            {redactedResponseBody}
            """);
    }

    private static string RedactSensitiveTokenValues(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return responseBody;
        }

        try
        {
            JsonNode? responseJson = JsonNode.Parse(responseBody);

            if (responseJson is not null)
            {
                RedactSensitiveTokenProperties(responseJson);

                return responseJson.ToJsonString(new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
        }
        catch (JsonException)
        {
        }

        return Regex.Replace(
            responseBody,
            @"(?i)(\b(?:accessToken|refreshToken)\b\s*[:=]\s*[""']?)[^""'\s,}\]]+([""']?)",
            "$1<redacted>$2");
    }

    private static void RedactSensitiveTokenProperties(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (string propertyName in jsonObject.Select(property => property.Key).ToArray())
            {
                if (IsSensitiveTokenProperty(propertyName))
                {
                    jsonObject[propertyName] = "<redacted>";
                    continue;
                }

                JsonNode? propertyValue = jsonObject[propertyName];

                if (propertyValue is not null)
                {
                    RedactSensitiveTokenProperties(propertyValue);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? item in jsonArray)
            {
                if (item is not null)
                {
                    RedactSensitiveTokenProperties(item);
                }
            }
        }
    }

    private static bool IsSensitiveTokenProperty(string propertyName)
    {
        return string.Equals(propertyName, "accessToken", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "refreshToken", StringComparison.OrdinalIgnoreCase);
    }
}