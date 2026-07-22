using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.WebApi.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class NotificationSignalRTests
{
    [TestMethod]
    public async Task Hub_WithoutToken_RejectsConnection()
    {
        await using var factory = new AuthApiFactory();
        await using var connection = CreateConnection(factory, accessToken: null);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            connection.StartAsync());
    }

    [TestMethod]
    public async Task MessageNotifications_ArriveRealtimeForCorrectRecipientsAndReadState()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.CreateStaffAsync();
        var customerClient = factory.CreateClient();
        await RegisterCustomerAsync(customerClient);
        var customerToken = await LoginAsync(
            customerClient,
            "bill@gmail.com",
            "Password1");
        var staffClient = factory.CreateClient();
        var staffToken = await LoginAsync(
            staffClient,
            "staff@example.com",
            "Password1");
        SetBearer(customerClient, customerToken);
        SetBearer(staffClient, staffToken);
        await using var customerConnection = CreateConnection(factory, customerToken);
        await using var staffConnection = CreateConnection(factory, staffToken);
        var staffCreated = CompletionSource();
        var customerCreated = CompletionSource();
        var customerRead = CompletionSource();
        staffConnection.On<NotificationChangedRealtimeDto>(
            NotificationRealtimeEvents.Changed,
            notificationEvent =>
            {
                if (notificationEvent.ChangeType == NotificationRealtimeChangeTypes.Created &&
                    notificationEvent.Notification?.Type ==
                        NotificationTypes.NewCustomerMessage)
                {
                    staffCreated.TrySetResult(notificationEvent);
                }
            });
        customerConnection.On<NotificationChangedRealtimeDto>(
            NotificationRealtimeEvents.Changed,
            notificationEvent =>
            {
                if (notificationEvent.ChangeType == NotificationRealtimeChangeTypes.Created &&
                    notificationEvent.Notification?.Type ==
                        NotificationTypes.NewStaffReply)
                {
                    customerCreated.TrySetResult(notificationEvent);
                }
                else if (notificationEvent.ChangeType ==
                    NotificationRealtimeChangeTypes.Read)
                {
                    customerRead.TrySetResult(notificationEvent);
                }
            });
        await staffConnection.StartAsync();
        await customerConnection.StartAsync();

        var customerSendResponse = await customerClient.PostAsJsonAsync(
            "/api/conversations/mine/messages",
            new SendMessageRequest { Content = "Please help" });
        Assert.AreEqual(HttpStatusCode.Created, customerSendResponse.StatusCode);
        var customerMessage = await customerSendResponse.Content
            .ReadFromJsonAsync<MessageDto>();
        Assert.IsNotNull(customerMessage);
        var staffNotification = await staffCreated.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, staffNotification.UnreadCount);
        Assert.IsTrue(staffNotification.Notification!.NotificationId > 0);

        var staffSendResponse = await staffClient.PostAsJsonAsync(
            $"/api/store/conversations/{customerMessage.ConversationId}/messages",
            new SendMessageRequest { Content = "The store can help" });
        Assert.AreEqual(HttpStatusCode.Created, staffSendResponse.StatusCode);
        var customerNotification = await customerCreated.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, customerNotification.UnreadCount);
        Assert.IsNotNull(customerNotification.Notification);

        var readResponse = await customerClient.PatchAsync(
            $"/api/notifications/{customerNotification.Notification.NotificationId}/read",
            content: null);
        Assert.AreEqual(HttpStatusCode.OK, readResponse.StatusCode);
        var readEvent = await customerRead.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, readEvent.UnreadCount);
        Assert.IsTrue(readEvent.Notification!.IsRead);
    }

    [TestMethod]
    public async Task ReservationNotifications_ArriveRealtimeAfterEachCommittedTransition()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedReservationDataAsync();
        await factory.CreateStaffAsync();
        var customerClient = factory.CreateClient();
        await RegisterCustomerAsync(customerClient);
        var customerToken = await LoginAsync(
            customerClient,
            "bill@gmail.com",
            "Password1");
        var staffClient = factory.CreateClient();
        var staffToken = await LoginAsync(
            staffClient,
            "staff@example.com",
            "Password1");
        SetBearer(customerClient, customerToken);
        SetBearer(staffClient, staffToken);
        await using var customerConnection = CreateConnection(factory, customerToken);
        await using var staffConnection = CreateConnection(factory, staffToken);
        var staffReservationCreated = CompletionSource();
        var customerReservationConfirmed = CompletionSource();
        staffConnection.On<NotificationChangedRealtimeDto>(
            NotificationRealtimeEvents.Changed,
            notificationEvent =>
            {
                if (notificationEvent.Notification?.Type ==
                    NotificationTypes.ReservationCreated)
                {
                    staffReservationCreated.TrySetResult(notificationEvent);
                }
            });
        customerConnection.On<NotificationChangedRealtimeDto>(
            NotificationRealtimeEvents.Changed,
            notificationEvent =>
            {
                if (notificationEvent.Notification?.Type ==
                    NotificationTypes.ReservationConfirmed)
                {
                    customerReservationConfirmed.TrySetResult(notificationEvent);
                }
            });
        await staffConnection.StartAsync();
        await customerConnection.StartAsync();

        var createResponse = await customerClient.PostAsJsonAsync(
            "/api/reservations",
            new CreateReservationRequest
            {
                Date = new DateOnly(2026, 7, 22),
                Time = new TimeOnly(9, 0),
                NumberOfGuests = 2,
                GuestName = "Bill",
                GuestPhoneNumber = "0987654321"
            });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var reservation = await createResponse.Content
            .ReadFromJsonAsync<ReservationDto>();
        Assert.IsNotNull(reservation);
        var createdEvent = await staffReservationCreated.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(NotificationTypes.ReservationCreated, createdEvent.Notification!.Type);

        var confirmResponse = await staffClient.PatchAsync(
            $"/api/store/reservations/{reservation.ReservationId}/confirm",
            content: null);
        Assert.AreEqual(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmedEvent = await customerReservationConfirmed.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(
            NotificationTypes.ReservationConfirmed,
            confirmedEvent.Notification!.Type);
    }

    private static HubConnection CreateConnection(
        AuthApiFactory factory,
        string? accessToken)
        => new HubConnectionBuilder()
            .WithUrl(
                new Uri(factory.Server.BaseAddress, NotificationHub.Route),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ =>
                        factory.Server.CreateHandler();
                    if (accessToken is not null)
                    {
                        options.AccessTokenProvider = () =>
                            Task.FromResult<string?>(accessToken);
                    }
                })
            .Build();

    private static TaskCompletionSource<NotificationChangedRealtimeDto>
        CompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void SetBearer(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

    private static async Task RegisterCustomerAsync(HttpClient client)
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
    }

    private static async Task<string> LoginAsync(
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
        return login.AccessToken;
    }
}
