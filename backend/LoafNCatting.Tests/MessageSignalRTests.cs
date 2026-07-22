using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.WebApi.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class MessageSignalRTests
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
    public async Task CustomerAndStaff_ReceiveEachOthersMessagesInRealtime()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.CreateStaffAsync();
        var customerHttpClient = factory.CreateClient();
        await RegisterCustomerAsync(customerHttpClient);
        var customerToken = await LoginAsync(
            customerHttpClient,
            "bill@gmail.com",
            "Password1");
        var staffToken = await LoginAsync(
            factory.CreateClient(),
            "staff@example.com",
            "Password1");
        await using var customerConnection = CreateConnection(factory, customerToken);
        await using var staffConnection = CreateConnection(factory, staffToken);
        var staffReceivedCustomerMessage = CompletionSource();
        var customerReceivedStaffReply = CompletionSource();
        var customerReceivedStaffRead = ReadCompletionSource();
        var staffReceivedCustomerRead = ReadCompletionSource();
        staffConnection.On<MessageCreatedRealtimeDto>(
            MessageRealtimeEvents.MessageCreated,
            messageEvent =>
            {
                if (messageEvent.Message.SenderRole == "Customer")
                {
                    staffReceivedCustomerMessage.TrySetResult(messageEvent);
                }
            });
        customerConnection.On<MessageCreatedRealtimeDto>(
            MessageRealtimeEvents.MessageCreated,
            messageEvent =>
            {
                if (messageEvent.Message.SenderRole == "Staff")
                {
                    customerReceivedStaffReply.TrySetResult(messageEvent);
                }
            });
        customerConnection.On<MessagesReadRealtimeDto>(
            MessageRealtimeEvents.MessagesRead,
            readEvent =>
            {
                if (readEvent.ReaderRole == "Staff")
                {
                    customerReceivedStaffRead.TrySetResult(readEvent);
                }
            });
        staffConnection.On<MessagesReadRealtimeDto>(
            MessageRealtimeEvents.MessagesRead,
            readEvent =>
            {
                if (readEvent.ReaderRole == "Customer")
                {
                    staffReceivedCustomerRead.TrySetResult(readEvent);
                }
            });
        await staffConnection.StartAsync();
        await customerConnection.StartAsync();

        var customerMessage = await customerConnection.InvokeAsync<MessageDto>(
            "SendCustomerMessage",
            new SendMessageRequest { Content = "Realtime hello" });
        var eventAtStaff = await staffReceivedCustomerMessage.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        Assert.AreEqual(customerMessage.MessageId, eventAtStaff.Message.MessageId);
        Assert.AreEqual("Realtime hello", eventAtStaff.Message.Content);

        var staffRead = await staffConnection
            .InvokeAsync<MarkMessagesReadResultDto>(
                "MarkStoreMessagesRead",
                customerMessage.ConversationId);
        var staffReadEvent = await customerReceivedStaffRead.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, staffRead.UpdatedCount);
        Assert.AreEqual(1, staffReadEvent.UpdatedCount);

        var staffReply = await staffConnection.InvokeAsync<MessageDto>(
            "SendStoreMessage",
            customerMessage.ConversationId,
            new SendMessageRequest { Content = "Realtime reply" });
        var eventAtCustomer = await customerReceivedStaffReply.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        Assert.AreEqual(staffReply.MessageId, eventAtCustomer.Message.MessageId);
        Assert.AreEqual("Realtime reply", eventAtCustomer.Message.Content);
        Assert.AreEqual(customerMessage.ConversationId, staffReply.ConversationId);

        var customerRead = await customerConnection
            .InvokeAsync<MarkMessagesReadResultDto>(
                "MarkCustomerMessagesRead");
        var customerReadEvent = await staffReceivedCustomerRead.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, customerRead.UpdatedCount);
        Assert.AreEqual(1, customerReadEvent.UpdatedCount);

        await Assert.ThrowsExactlyAsync<HubException>(() =>
            customerConnection.InvokeAsync<MessageDto>(
                "SendStoreMessage",
                customerMessage.ConversationId,
                new SendMessageRequest { Content = "Impersonation attempt" }));
    }

    [TestMethod]
    public async Task Hub_AcceptsJwtFromAccessTokenQueryForSignalRTransport()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        var client = factory.CreateClient();
        await RegisterCustomerAsync(client);
        var token = await LoginAsync(client, "bill@gmail.com", "Password1");
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PostAsync(
            $"{MessageHub.Route}/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token)}",
            content: null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var negotiate = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var transports = negotiate.RootElement
            .GetProperty("availableTransports")
            .EnumerateArray()
            .Select(transport => transport.GetProperty("transport").GetString())
            .ToArray();
        CollectionAssert.Contains(transports, "WebSockets");
        CollectionAssert.Contains(transports, "LongPolling");
    }

    private static HubConnection CreateConnection(
        AuthApiFactory factory,
        string? accessToken)
        => new HubConnectionBuilder()
            .WithUrl(
                new Uri(factory.Server.BaseAddress, MessageHub.Route),
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

    private static TaskCompletionSource<MessageCreatedRealtimeDto> CompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<MessagesReadRealtimeDto> ReadCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

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
