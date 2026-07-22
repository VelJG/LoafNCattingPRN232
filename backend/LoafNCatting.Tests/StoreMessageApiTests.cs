using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class StoreMessageApiTests
{
    [TestMethod]
    public async Task StoreEndpoints_WithoutToken_ReturnUnauthorized()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var inbox = await client.GetAsync("/api/store/conversations");
        var history = await client.GetAsync(
            "/api/store/conversations/1/messages");
        var send = await client.PostAsJsonAsync(
            "/api/store/conversations/1/messages",
            new SendMessageRequest { Content = "Reply" });
        var read = await client.PatchAsync(
            "/api/store/conversations/1/messages/read",
            null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, inbox.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, history.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, send.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, read.StatusCode);
    }

    [TestMethod]
    public async Task StoreEndpoints_WithCustomerToken_ReturnForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        await RegisterAndAuthenticateCustomerAsync(customerClient);

        var inbox = await customerClient.GetAsync("/api/store/conversations");
        var send = await customerClient.PostAsJsonAsync(
            "/api/store/conversations/1/messages",
            new SendMessageRequest { Content = "Reply" });

        Assert.AreEqual(HttpStatusCode.Forbidden, inbox.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, send.StatusCode);
    }

    [TestMethod]
    public async Task CustomerAndStaff_FullRestFlow_ExchangesMessagesAndReadState()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        var customer = await RegisterAndAuthenticateCustomerAsync(customerClient);
        var staffClient = factory.CreateClient();
        await AuthenticateAsync(staffClient, "staff@example.com", "Password1");

        var customerSend = await customerClient.PostAsJsonAsync(
            "/api/conversations/mine/messages",
            new SendMessageRequest { Content = "Do you have a quiet table?" });
        Assert.AreEqual(HttpStatusCode.Created, customerSend.StatusCode);
        var customerMessage = await customerSend.Content
            .ReadFromJsonAsync<MessageDto>();
        Assert.IsNotNull(customerMessage);

        var inboxResponse = await staffClient.GetAsync(
            "/api/store/conversations");
        Assert.AreEqual(HttpStatusCode.OK, inboxResponse.StatusCode);
        var inbox = await inboxResponse.Content
            .ReadFromJsonAsync<List<StoreConversationDto>>();
        Assert.IsNotNull(inbox);
        Assert.HasCount(1, inbox);
        Assert.AreEqual(customer.UserId, inbox[0].CustomerUserId);
        Assert.AreEqual(1, inbox[0].UnreadCustomerMessageCount);
        Assert.AreEqual(
            "Do you have a quiet table?",
            inbox[0].LastMessageContent);

        var conversationId = inbox[0].ConversationId;
        var staffHistoryResponse = await staffClient.GetAsync(
            $"/api/store/conversations/{conversationId}/messages");
        Assert.AreEqual(HttpStatusCode.OK, staffHistoryResponse.StatusCode);
        var staffHistory = await staffHistoryResponse.Content
            .ReadFromJsonAsync<List<MessageDto>>();
        Assert.IsNotNull(staffHistory);
        Assert.HasCount(1, staffHistory);

        var staffReadResponse = await staffClient.PatchAsync(
            $"/api/store/conversations/{conversationId}/messages/read",
            null);
        Assert.AreEqual(HttpStatusCode.OK, staffReadResponse.StatusCode);
        var staffRead = await staffReadResponse.Content
            .ReadFromJsonAsync<MarkMessagesReadResultDto>();
        Assert.IsNotNull(staffRead);
        Assert.AreEqual(1, staffRead.UpdatedCount);

        var staffSendResponse = await staffClient.PostAsJsonAsync(
            $"/api/store/conversations/{conversationId}/messages",
            new SendMessageRequest { Content = "Yes, we do." });
        Assert.AreEqual(HttpStatusCode.Created, staffSendResponse.StatusCode);
        var staffMessage = await staffSendResponse.Content
            .ReadFromJsonAsync<MessageDto>();
        Assert.IsNotNull(staffMessage);
        Assert.AreEqual("Staff", staffMessage.SenderRole);

        var customerHistoryResponse = await customerClient.GetAsync(
            "/api/conversations/mine/messages");
        Assert.AreEqual(HttpStatusCode.OK, customerHistoryResponse.StatusCode);
        var customerHistory = await customerHistoryResponse.Content
            .ReadFromJsonAsync<List<MessageDto>>();
        Assert.IsNotNull(customerHistory);
        Assert.HasCount(2, customerHistory);
        Assert.IsTrue(customerHistory[0].IsRead);
        Assert.IsFalse(customerHistory[1].IsRead);

        var customerReadResponse = await customerClient.PatchAsync(
            "/api/conversations/mine/messages/read",
            null);
        Assert.AreEqual(HttpStatusCode.OK, customerReadResponse.StatusCode);
        var customerRead = await customerReadResponse.Content
            .ReadFromJsonAsync<MarkMessagesReadResultDto>();
        Assert.IsNotNull(customerRead);
        Assert.AreEqual(1, customerRead.UpdatedCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<LoafNcattingPrn232Context>();
        Assert.AreEqual(2, await context.Messages.CountAsync());
        Assert.IsTrue(await context.Messages.AllAsync(message => message.IsRead));
        Assert.AreEqual(
            1,
            await context.Notifications.CountAsync(notification =>
                notification.UserId == customer.UserId &&
                notification.Type == NotificationTypes.NewStaffReply));
    }

    [TestMethod]
    public async Task StoreConversation_NotFound_ReturnsNotFound()
    {
        await using var factory = await CreateFactoryAsync();
        var staffClient = factory.CreateClient();
        await AuthenticateAsync(staffClient, "staff@example.com", "Password1");

        var history = await staffClient.GetAsync(
            "/api/store/conversations/999/messages");
        var send = await staffClient.PostAsJsonAsync(
            "/api/store/conversations/999/messages",
            new SendMessageRequest { Content = "Reply" });

        Assert.AreEqual(HttpStatusCode.NotFound, history.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, send.StatusCode);
    }

    private static async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.CreateStaffAsync();
        return factory;
    }

    private static async Task<UserDto> RegisterAndAuthenticateCustomerAsync(
        HttpClient client)
    {
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Bill",
                "bill@gmail.com",
                "Password1",
                "0987654321",
                "Ho Chi Minh City"));
        Assert.AreEqual(HttpStatusCode.Created, registerResponse.StatusCode);
        var customer = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(customer);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");
        return customer;
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
