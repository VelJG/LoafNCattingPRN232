using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class MessageApiTests
{
    [TestMethod]
    public async Task CustomerEndpoints_WithoutToken_ReturnUnauthorized()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var conversation = await client.GetAsync("/api/conversations/mine");
        var history = await client.GetAsync("/api/conversations/mine/messages");
        var send = await client.PostAsJsonAsync(
            "/api/conversations/mine/messages",
            new SendMessageRequest { Content = "Hello" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, conversation.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, history.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, send.StatusCode);
    }

    [TestMethod]
    public async Task CustomerEndpoints_WithStaffToken_ReturnForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await AuthenticateAsync(client, "staff@example.com", "Password1");

        var conversation = await client.GetAsync("/api/conversations/mine");
        var history = await client.GetAsync("/api/conversations/mine/messages");
        var send = await client.PostAsJsonAsync(
            "/api/conversations/mine/messages",
            new SendMessageRequest { Content = "Hello" });

        Assert.AreEqual(HttpStatusCode.Forbidden, conversation.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, history.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, send.StatusCode);
    }

    [TestMethod]
    public async Task CustomerFlow_FirstMessageCreatesOneConversationAndHistory()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        var customer = await RegisterCustomerAsync(client);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var emptyConversationResponse = await client.GetAsync(
            "/api/conversations/mine");
        var emptyHistoryResponse = await client.GetAsync(
            "/api/conversations/mine/messages");
        var sendResponse = await client.PostAsJsonAsync(
            "/api/conversations/mine/messages",
            new SendMessageRequest { Content = "  I need help  " });
        var conversationResponse = await client.GetAsync(
            "/api/conversations/mine");
        var historyResponse = await client.GetAsync(
            "/api/conversations/mine/messages");

        Assert.AreEqual(HttpStatusCode.OK, emptyConversationResponse.StatusCode);
        var emptyConversation = await emptyConversationResponse.Content
            .ReadFromJsonAsync<CustomerConversationDto>();
        Assert.IsNotNull(emptyConversation);
        Assert.IsNull(emptyConversation.Conversation);
        Assert.AreEqual(HttpStatusCode.OK, emptyHistoryResponse.StatusCode);
        var emptyHistory = await emptyHistoryResponse.Content
            .ReadFromJsonAsync<List<MessageDto>>();
        Assert.IsNotNull(emptyHistory);
        Assert.HasCount(0, emptyHistory);

        Assert.AreEqual(HttpStatusCode.Created, sendResponse.StatusCode);
        var sent = await sendResponse.Content.ReadFromJsonAsync<MessageDto>();
        Assert.IsNotNull(sent);
        Assert.AreEqual(customer.UserId, sent.SenderUserId);
        Assert.AreEqual("I need help", sent.Content);

        Assert.AreEqual(HttpStatusCode.OK, conversationResponse.StatusCode);
        var conversation = await conversationResponse.Content
            .ReadFromJsonAsync<CustomerConversationDto>();
        Assert.IsNotNull(conversation?.Conversation);
        Assert.AreEqual(customer.UserId, conversation.Conversation.CustomerUserId);
        Assert.AreEqual(sent.ConversationId, conversation.Conversation.ConversationId);

        Assert.AreEqual(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content
            .ReadFromJsonAsync<List<MessageDto>>();
        Assert.IsNotNull(history);
        Assert.HasCount(1, history);
        Assert.AreEqual(sent.MessageId, history[0].MessageId);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<LoafNcattingPrn232Context>();
        var storedConversation = await context.Conversations
            .AsNoTracking()
            .SingleAsync();
        Assert.AreEqual(customer.UserId, storedConversation.CustomerUserId);
        Assert.IsNull(storedConversation.StaffUserId);
        Assert.AreEqual(1, await context.Messages.CountAsync());
        Assert.AreEqual(
            1,
            await context.Notifications.CountAsync(notification =>
                notification.Type == NotificationTypes.NewCustomerMessage));
    }

    [TestMethod]
    public async Task Send_BlankContent_ReturnsBadRequestWithoutCreatingConversation()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await RegisterCustomerAsync(client);
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var response = await client.PostAsJsonAsync(
            "/api/conversations/mine/messages",
            new SendMessageRequest { Content = "   " });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<LoafNcattingPrn232Context>();
        Assert.AreEqual(0, await context.Conversations.CountAsync());
        Assert.AreEqual(0, await context.Messages.CountAsync());
    }

    private static async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
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
