using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class StoreWalkInApiTests
{
    [TestMethod]
    public async Task Create_WithStaffToken_CreatesCheckedInReservationAndOccupiesTable()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await AuthenticateAsync(client, "staff@example.com", "Password1");

        var response = await client.PostAsJsonAsync(
            "/api/store/walk-ins",
            WalkInRequest(numberOfGuests: 2));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var walkIn = await response.Content.ReadFromJsonAsync<StoreReservationDto>();
        Assert.IsNotNull(walkIn);
        Assert.IsNull(walkIn.CustomerUserId);
        Assert.AreEqual(new TimeOnly(17, 10), walkIn.Time);
        Assert.AreEqual("Đã đến", walkIn.Status);
        Assert.AreEqual("Đang sử dụng", walkIn.TableStatus);
        Assert.AreEqual(1, walkIn.Table.TableId);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        var stored = await context.Reservations.AsNoTracking().SingleAsync();
        Assert.IsNull(stored.UserId);
        Assert.AreEqual(ReservationTestData.CheckedInStatusId, stored.StatusId);
        Assert.AreEqual(
            ReservationTestData.OccupiedTableStatusId,
            await context.RestaurantTables
                .Where(table => table.TableId == stored.TableId)
                .Select(table => table.TableStatusId)
                .SingleAsync());
    }

    [TestMethod]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = new AuthApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/store/walk-ins",
            WalkInRequest(numberOfGuests: 2));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Create_WithCustomerToken_ReturnsForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Bill",
                "bill@gmail.com",
                "Password1",
                "0987654321",
                "Ho Chi Minh City"));
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var response = await client.PostAsJsonAsync(
            "/api/store/walk-ins",
            WalkInRequest(numberOfGuests: 2));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Create_WhenNoTableHasEnoughCapacity_ReturnsConflict()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await AuthenticateAsync(client, "staff@example.com", "Password1");

        var response = await client.PostAsJsonAsync(
            "/api/store/walk-ins",
            WalkInRequest(numberOfGuests: 100));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        Assert.AreEqual(0, await context.Reservations.CountAsync());
    }

    private static async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        factory.SetVietnamTime(hour: 17, minute: 10);
        return factory;
    }

    private static CreateWalkInRequest WalkInRequest(int numberOfGuests)
        => new()
        {
            NumberOfGuests = numberOfGuests,
            GuestName = "Walk-in Guest",
            GuestPhoneNumber = "0987654321",
            Note = "Near the window"
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
