using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class StoreReservationApiTests
{
    [TestMethod]
    public async Task StoreEndpoints_WithCustomerToken_ReturnForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        await RegisterCustomerAsync(customerClient);
        await AuthenticateAsync(customerClient, "bill@gmail.com", "Password1");

        var response = await customerClient.GetAsync("/api/store/reservations");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Staff_CanListViewConfirmCheckInAndCompleteReservation()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        var customer = await RegisterCustomerAsync(customerClient);
        await AuthenticateAsync(customerClient, "bill@gmail.com", "Password1");
        var created = await CreateReservationAsync(customerClient);
        var staffClient = factory.CreateClient();
        await AuthenticateAsync(staffClient, "staff@example.com", "Password1");

        var listResponse = await staffClient.GetAsync("/api/store/reservations");
        var detailResponse = await staffClient.GetAsync(
            $"/api/store/reservations/{created.ReservationId}");
        var confirmResponse = await staffClient.PatchAsync(
            $"/api/store/reservations/{created.ReservationId}/confirm",
            content: null);
        factory.SetVietnamTime(hour: 9);
        var checkInResponse = await staffClient.PatchAsync(
            $"/api/store/reservations/{created.ReservationId}/check-in",
            content: null);
        var completeResponse = await staffClient.PatchAsync(
            $"/api/store/reservations/{created.ReservationId}/complete",
            content: null);

        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content
            .ReadFromJsonAsync<List<StoreReservationDto>>();
        Assert.IsNotNull(list);
        Assert.HasCount(1, list);
        Assert.AreEqual(customer.UserId, list[0].CustomerUserId);
        Assert.AreEqual(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, checkInResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content
            .ReadFromJsonAsync<StoreReservationDto>();
        Assert.IsNotNull(completed);
        Assert.AreEqual("Hoàn thành", completed.Status);
        Assert.AreEqual("Trống", completed.TableStatus);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        var stored = await context.Reservations.AsNoTracking().SingleAsync();
        var table = await context.RestaurantTables.AsNoTracking().SingleAsync(
            current => current.TableId == stored.TableId);
        Assert.AreEqual(ReservationTestData.CompletedStatusId, stored.StatusId);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        CollectionAssert.AreEquivalent(
            new[]
            {
                NotificationTypes.ReservationConfirmed,
                NotificationTypes.ReservationCheckedIn,
                NotificationTypes.ReservationCompleted
            },
            await context.Notifications
                .Where(notification => notification.UserId == customer.UserId)
                .Select(notification => notification.Type!)
                .ToArrayAsync());
    }

    [TestMethod]
    public async Task Staff_CanCancelPendingReservation()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        var customer = await RegisterCustomerAsync(customerClient);
        await AuthenticateAsync(customerClient, "bill@gmail.com", "Password1");
        var created = await CreateReservationAsync(customerClient);
        var staffClient = factory.CreateClient();
        await AuthenticateAsync(staffClient, "staff@example.com", "Password1");

        var response = await staffClient.PatchAsync(
            $"/api/store/reservations/{created.ReservationId}/cancel",
            content: null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var cancelled = await response.Content.ReadFromJsonAsync<StoreReservationDto>();
        Assert.IsNotNull(cancelled);
        Assert.AreEqual("Đã hủy", cancelled.Status);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        Assert.AreEqual(
            1,
            await context.Notifications.CountAsync(notification =>
                notification.UserId == customer.UserId &&
                notification.Type == NotificationTypes.ReservationCancelled));
    }

    private static async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        return factory;
    }

    private static async Task<UserDto> RegisterCustomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Bill",
                "bill@gmail.com",
                "Password1",
                "0987654321",
                "Ho Chi Minh City"));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(customer);
        return customer;
    }

    private static async Task<ReservationDto> CreateReservationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/reservations",
            new CreateReservationRequest
            {
                Date = new DateOnly(2026, 7, 22),
                Time = new TimeOnly(9, 0),
                NumberOfGuests = 2,
                GuestName = "Bill",
                GuestPhoneNumber = "0987654321"
            });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(reservation);
        return reservation;
    }

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
