using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class NotificationApiTests
{
    [TestMethod]
    public async Task Endpoints_WithoutToken_ReturnUnauthorized()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var listResponse = await client.GetAsync("/api/notifications");
        var countResponse = await client.GetAsync("/api/notifications/unread-count");
        var readResponse = await client.PatchAsync("/api/notifications/1/read", null);
        var readAllResponse = await client.PatchAsync("/api/notifications/read-all", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, listResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, countResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, readResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, readAllResponse.StatusCode);
    }

    [TestMethod]
    public async Task Customer_ListFilterAndUnreadCount_ReturnOnlyOwnedNotifications()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        var client = factory.CreateClient();
        var customer = await RegisterCustomerAsync(
            client,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        var otherCustomer = await RegisterCustomerAsync(
            factory.CreateClient(),
            "Alice",
            "alice@gmail.com",
            "0987000000");
        await SeedNotificationsAsync(factory, customer.UserId, otherCustomer.UserId);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var listResponse = await client.GetAsync("/api/notifications");
        var unreadResponse = await client.GetAsync("/api/notifications?isRead=false");
        var countResponse = await client.GetAsync("/api/notifications/unread-count");

        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        var all = await listResponse.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.IsNotNull(all);
        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            all.Select(notification => notification.NotificationId).ToArray());
        var unread = await unreadResponse.Content
            .ReadFromJsonAsync<List<NotificationDto>>();
        Assert.IsNotNull(unread);
        Assert.HasCount(1, unread);
        Assert.AreEqual(1, unread[0].NotificationId);
        var count = await countResponse.Content
            .ReadFromJsonAsync<UnreadNotificationCountDto>();
        Assert.IsNotNull(count);
        Assert.AreEqual(1, count.Count);
    }

    [TestMethod]
    public async Task Customer_MarkReadAndReadAll_UpdateOnlyOwnedNotifications()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        var client = factory.CreateClient();
        var customer = await RegisterCustomerAsync(
            client,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        var otherCustomer = await RegisterCustomerAsync(
            factory.CreateClient(),
            "Alice",
            "alice@gmail.com",
            "0987000000");
        await SeedNotificationsAsync(factory, customer.UserId, otherCustomer.UserId);
        await SetNotificationUnreadAsync(factory, notificationId: 2);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var readResponse = await client.PatchAsync("/api/notifications/1/read", null);
        var readAllResponse = await client.PatchAsync("/api/notifications/read-all", null);

        Assert.AreEqual(HttpStatusCode.OK, readResponse.StatusCode);
        var read = await readResponse.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.IsNotNull(read);
        Assert.IsTrue(read.IsRead);
        Assert.AreEqual(HttpStatusCode.OK, readAllResponse.StatusCode);
        var result = await readAllResponse.Content
            .ReadFromJsonAsync<MarkNotificationsReadResultDto>();
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.UpdatedCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        Assert.IsTrue(await context.Notifications
            .Where(notification => notification.NotificationId == 1)
            .Select(notification => notification.IsRead)
            .SingleAsync());
        Assert.IsFalse(await context.Notifications
            .Where(notification => notification.NotificationId == 3)
            .Select(notification => notification.IsRead)
            .SingleAsync());
    }

    [TestMethod]
    public async Task MarkRead_AnotherUsersNotification_ReturnsNotFound()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        var client = factory.CreateClient();
        var customer = await RegisterCustomerAsync(
            client,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        var otherCustomer = await RegisterCustomerAsync(
            factory.CreateClient(),
            "Alice",
            "alice@gmail.com",
            "0987000000");
        await SeedNotificationsAsync(factory, customer.UserId, otherCustomer.UserId);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var response = await client.PatchAsync("/api/notifications/3/read", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Staff_CanReadNotificationsAddressedToStoreAccount()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();
        await AuthenticateAsync(client, "staff@example.com", "Password1");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
            var staffUserId = await context.Users
                .Where(user => user.Email == "staff@example.com")
                .Select(user => user.UserId)
                .SingleAsync();
            context.Notifications.Add(new Notification
            {
                UserId = staffUserId,
                Title = "New reservation",
                Content = "A customer created a reservation.",
                Type = NotificationTypes.ReservationCreated,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content
            .ReadFromJsonAsync<List<NotificationDto>>();
        Assert.IsNotNull(notifications);
        Assert.HasCount(1, notifications);
        Assert.AreEqual(NotificationTypes.ReservationCreated, notifications[0].Type);
    }

    private static async Task<UserDto> RegisterCustomerAsync(
        HttpClient client,
        string name,
        string email,
        string phoneNumber)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                name,
                email,
                "Password1",
                phoneNumber,
                "Ho Chi Minh City"));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(customer);
        return customer;
    }

    private static async Task SeedNotificationsAsync(
        AuthApiFactory factory,
        int customerUserId,
        int otherCustomerUserId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        context.Notifications.AddRange(
            CreateNotification(1, customerUserId, isRead: false, minute: 1),
            CreateNotification(2, customerUserId, isRead: true, minute: 2),
            CreateNotification(3, otherCustomerUserId, isRead: false, minute: 3));
        await context.SaveChangesAsync();
    }

    private static Notification CreateNotification(
        int notificationId,
        int userId,
        bool isRead,
        int minute)
        => new()
        {
            NotificationId = notificationId,
            UserId = userId,
            Title = $"Notification {notificationId}",
            Content = $"Content {notificationId}",
            Type = NotificationTypes.ReservationConfirmed,
            IsRead = isRead,
            CreatedAt = new DateTime(2026, 7, 22, 3, minute, 0, DateTimeKind.Utc)
        };

    private static async Task SetNotificationUnreadAsync(
        AuthApiFactory factory,
        int notificationId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        var notification = await context.Notifications.SingleAsync(
            current => current.NotificationId == notificationId);
        notification.IsRead = false;
        await context.SaveChangesAsync();
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
