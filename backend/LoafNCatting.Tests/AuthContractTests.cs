using System.ComponentModel.DataAnnotations;
using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class AuthContractTests
{
    [TestMethod]
    public void RegisterRequest_RejectsPasswordShorterThanEightCharacters()
    {
        var request = new RegisterRequest(
            "Customer",
            "customer@example.com",
            "12345",
            "0900000001",
            null);

        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.IsFalse(valid);
        Assert.IsTrue(results.Any(result =>
            result.MemberNames.Contains(nameof(RegisterRequest.Password))));
    }
}
