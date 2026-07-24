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
        Assert.AreEqual(120, reservation.DurationMinutes);
        Assert.IsFalse(
            (await response.Content.ReadAsStringAsync()).Contains(
                "\"table\"",
                StringComparison.OrdinalIgnoreCase));

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

    [TestMethod]
    public async Task MineAndDetail_WithCustomerToken_ReturnOnlyOwnedReservation()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            CustomerRegistration());
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");
        var createResponse = await client.PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(created);

        var mineResponse = await client.GetAsync("/api/reservations/mine");
        var detailResponse = await client.GetAsync(
            $"/api/reservations/{created.ReservationId}");

        Assert.AreEqual(HttpStatusCode.OK, mineResponse.StatusCode);
        var mine = await mineResponse.Content
            .ReadFromJsonAsync<List<ReservationDto>>();
        Assert.IsNotNull(mine);
        Assert.HasCount(1, mine);
        Assert.AreEqual(created.ReservationId, mine[0].ReservationId);
        Assert.AreEqual(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(detail);
        Assert.AreEqual(created.CustomerUserId, detail.CustomerUserId);
    }

    [TestMethod]
    public async Task Detail_WhenOwnedByAnotherCustomer_ReturnsNotFound()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var billClient = factory.CreateClient();
        await billClient.PostAsJsonAsync(
            "/api/auth/register",
            CustomerRegistration());
        await AuthenticateAsync(billClient, "bill@gmail.com", "Password1");
        var createResponse = await billClient.PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(created);

        var aliceClient = factory.CreateClient();
        await aliceClient.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Alice",
                "alice@gmail.com",
                "Password1",
                "0987000000",
                "Ho Chi Minh City"));
        await AuthenticateAsync(aliceClient, "alice@gmail.com", "Password1");

        var response = await aliceClient.GetAsync(
            $"/api/reservations/{created.ReservationId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Cancel_ExactlyTwoHoursBeforeStart_CancelsAndNotifiesStaff()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            CustomerRegistration());
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");
        var createResponse = await client.PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest(hour: 9, minute: 0));
        var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(created);

        var response = await client.PatchAsync(
            $"/api/reservations/{created.ReservationId}/cancel",
            content: null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var cancelled = await response.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(cancelled);
        Assert.AreEqual("Đã hủy", cancelled.Status);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        Assert.AreEqual(
            ReservationTestData.CancelledStatusId,
            await context.Reservations
                .Where(reservation => reservation.ReservationId == created.ReservationId)
                .Select(reservation => reservation.StatusId)
                .SingleAsync());
        Assert.AreEqual(
            2,
            await context.Notifications.CountAsync());
        CollectionAssert.AreEquivalent(
            new[]
            {
                NotificationTypes.ReservationCreated,
                NotificationTypes.ReservationCancelled
            },
            await context.Notifications
                .Select(notification => notification.Type!)
                .ToArrayAsync());
    }

    [TestMethod]
    public async Task Cancel_LessThanTwoHoursBeforeStart_ReturnsConflict()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            CustomerRegistration());
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");
        var createResponse = await client.PostAsJsonAsync(
            "/api/reservations",
            ReservationRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(created);

        var response = await client.PatchAsync(
            $"/api/reservations/{created.ReservationId}/cancel",
            content: null);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static RegisterRequest CustomerRegistration() => new(
        "Bill",
        "bill@gmail.com",
        "Password1",
        "0987654321",
        "Ho Chi Minh City");

    private static CreateReservationRequest ReservationRequest(
        int hour = 8,
        int minute = 30) => new()
    {
        Date = new DateOnly(2026, 7, 22),
        Time = new TimeOnly(hour, minute),
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
