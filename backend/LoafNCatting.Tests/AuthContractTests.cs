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

        var passwordParameter = typeof(RegisterRequest)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Single(parameter => parameter.Name == "Password");
        var minimumLength = passwordParameter
            .GetCustomAttributes(typeof(MinLengthAttribute), inherit: false)
            .Cast<MinLengthAttribute>()
            .Single();

        Assert.IsFalse(minimumLength.IsValid(request.Password));
    }
}
