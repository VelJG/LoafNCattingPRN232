using System.IdentityModel.Tokens.Jwt;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.Extensions.Options;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class JwtTokenServiceTests
{
    [TestMethod]
    public void CreateToken_UsesConfiguredIdentityClaimsAndThirtyMinuteLifetime()
    {
        var before = DateTime.UtcNow;
        var service = new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "LoafNCatting.Api",
            Audience = "LoafNCatting.Client",
            SigningKey = "local-test-signing-key-at-least-32-characters-long",
            AccessTokenMinutes = 30
        }));
        var user = new User
        {
            UserId = 42,
            Name = "Cat Lover",
            Email = "cat@example.com"
        };

        var result = service.CreateToken(user, "Customer");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.AreEqual("LoafNCatting.Api", token.Issuer);
        CollectionAssert.Contains(token.Audiences.ToList(), "LoafNCatting.Client");
        Assert.AreEqual("42", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.AreEqual("cat@example.com", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.AreEqual("Cat Lover", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.AreEqual("Customer", token.Claims.Single(claim => claim.Type == AuthClaimTypes.Role).Value);
        Assert.IsTrue(token.Claims.Any(claim => claim.Type == JwtRegisteredClaimNames.Jti));
        Assert.IsTrue(result.ExpiresAtUtc >= before.AddMinutes(29).AddSeconds(55));
        Assert.IsTrue(result.ExpiresAtUtc <= DateTime.UtcNow.AddMinutes(30).AddSeconds(5));
    }
}
