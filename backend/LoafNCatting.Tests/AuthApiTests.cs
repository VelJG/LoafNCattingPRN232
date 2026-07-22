using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using LoafNCatting.Application.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class AuthApiTests
{
    [TestMethod]
    public async Task RegisterLoginVerifyAndLogout_CompletesCustomerFlow()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CustomerRegistration());
        Assert.AreEqual(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(registered);
        Assert.AreEqual("Customer", registered.Role);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("customer@example.com", "Password1"));
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(login);
        Assert.AreEqual("Bearer", login.TokenType);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var verifyResponse = await client.GetAsync("/api/auth/verify");
        Assert.AreEqual(HttpStatusCode.OK, verifyResponse.StatusCode);
        var verification = await verifyResponse.Content
            .ReadFromJsonAsync<TokenVerificationResponse>();
        Assert.IsNotNull(verification);
        Assert.AreEqual(registered.UserId, verification.User.UserId);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        Assert.AreEqual(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [TestMethod]
    public async Task Verify_WithoutBearerToken_ReturnsUnauthorized()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().GetAsync("/api/auth/verify");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CustomerToken_CannotCreateStaff()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", CustomerRegistration());
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("customer@example.com", "Password1"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/admin/users/staff",
            StaffRegistration());

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task AdminToken_CreatesStaffAccount()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.CreateAdminAsync();
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@gmail.com", "12345"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/admin/users/staff",
            StaffRegistration());

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(staff);
        Assert.AreEqual("Staff", staff.Role);
    }

    [TestMethod]
    [DataRow("wrong-issuer")]
    [DataRow("wrong-audience")]
    [DataRow("forged-signature")]
    [DataRow("expired")]
    public async Task Verify_WithInvalidToken_ReturnsUnauthorized(string invalidity)
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateInvalidToken(invalidity));

        var response = await client.GetAsync("/api/auth/verify");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static RegisterRequest CustomerRegistration() => new(
        "Customer",
        "customer@example.com",
        "Password1",
        "0900000001",
        null);

    private static CreateStaffRequest StaffRegistration() => new(
        "Staff Member",
        "staff@example.com",
        "Password1",
        "0900000002",
        null);

    private static string CreateInvalidToken(string invalidity)
    {
        var now = DateTime.UtcNow;
        var issuer = invalidity == "wrong-issuer"
            ? "SomeoneElse"
            : AuthApiFactory.Issuer;
        var audience = invalidity == "wrong-audience"
            ? "SomeoneElse"
            : AuthApiFactory.Audience;
        var key = invalidity == "forged-signature"
            ? "forged-integration-test-key-at-least-32-characters"
            : AuthApiFactory.SigningKey;
        var expires = invalidity == "expired"
            ? now.AddMinutes(-1)
            : now.AddMinutes(5);
        var notBefore = invalidity == "expired"
            ? now.AddMinutes(-10)
            : now.AddMinutes(-1);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, "999"),
                new Claim(AuthClaimTypes.Role, "Admin")
            ],
            notBefore,
            expires,
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
