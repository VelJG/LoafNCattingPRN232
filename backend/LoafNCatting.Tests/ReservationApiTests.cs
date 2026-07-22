using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationApiTests
{
    [TestMethod]
    public async Task Create_WithCustomerToken_UsesJwtSubjectAndCreatesPending()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            CustomerRegistration());
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(registered);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var response = await client.PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest());

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(reservation);
        Assert.AreEqual(registered.UserId, reservation.CustomerUserId);
        Assert.AreEqual("Đang chờ", reservation.Status);
        Assert.AreEqual(1, reservation.Table.TableId);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        var stored = await context.Reservations.AsNoTracking().SingleAsync();
        Assert.AreEqual(registered.UserId, stored.UserId);
        Assert.AreEqual(1, await context.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest());

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Create_WithStaffToken_ReturnsForbidden()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();
        await AuthenticateAsync(client, "staff@example.com", "Password1");

        var response = await client.PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest());

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static RegisterRequest CustomerRegistration() => new(
        "Bill",
        "bill@gmail.com",
        "Password1",
        "0987654321",
        "Ho Chi Minh City");

    private static CreateReservationRequest ReservationRequest() => new()
    {
        Date = new DateOnly(2026, 7, 22),
        Time = new TimeOnly(8, 30),
        NumberOfGuests = 2,
        GuestName = "Bill",
        GuestPhoneNumber = "0987654321",
        Note = "Near a quiet area"
    };

    private static async Task AuthenticateAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
}
