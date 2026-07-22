using System.Net;
using System.Text.Json;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class SwaggerUiTests
{
    [TestMethod]
    public async Task Development_ExposesSwaggerUiForExistingOpenApiDocument()
    {
        await using var factory = new AuthApiFactory("Development");
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var configuration = await client.GetStringAsync("/swagger/index.js");
        StringAssert.Contains(configuration, "/openapi/v1.json");
    }

    [TestMethod]
    public async Task NonDevelopment_DoesNotExposeSwaggerUiOrOpenApiDocument()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var swaggerResponse = await client.GetAsync("/swagger/index.html");
        var documentResponse = await client.GetAsync("/openapi/v1.json");

        Assert.AreEqual(HttpStatusCode.NotFound, swaggerResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, documentResponse.StatusCode);
    }

    [TestMethod]
    public async Task OpenApiDocument_RequiresBearerTokenOnlyForAuthorizedOperations()
    {
        await using var factory = new AuthApiFactory("Development");
        var client = factory.CreateClient();
        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;

        var bearer = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.AreEqual("http", bearer.GetProperty("type").GetString());
        Assert.AreEqual("bearer", bearer.GetProperty("scheme").GetString());
        Assert.AreEqual("JWT", bearer.GetProperty("bearerFormat").GetString());

        var paths = root.GetProperty("paths");
        var verify = paths
            .GetProperty("/api/auth/verify")
            .GetProperty("get");
        Assert.IsTrue(RequiresBearer(verify));

        var login = paths
            .GetProperty("/api/auth/login")
            .GetProperty("post");
        Assert.IsFalse(login.TryGetProperty("security", out _));
    }

    private static bool RequiresBearer(JsonElement operation)
    {
        if (!operation.TryGetProperty("security", out var requirements))
        {
            return false;
        }

        return requirements.EnumerateArray().Any(requirement =>
            requirement.TryGetProperty("Bearer", out _));
    }
}
