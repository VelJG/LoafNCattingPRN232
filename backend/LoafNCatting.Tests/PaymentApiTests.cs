using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.DTOs.Carts;
using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class PaymentApiTests
{
    [TestMethod]
    public async Task OnlineOrder_CreatesPayOsLink_AndMarksPaymentPaid()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        var customer = await RegisterAndAuthenticateAsync(
            client,
            "Bill",
            "bill-pay@example.com");
        var order = await CreateOnlineOrderAsync(client);

        var linkResponse = await client.PostAsJsonAsync(
            "/api/payments/links",
            new CreatePaymentLinkRequest(order.OrderId));

        Assert.AreEqual(
            HttpStatusCode.OK,
            linkResponse.StatusCode,
            await linkResponse.Content.ReadAsStringAsync());
        var link = await linkResponse.Content
            .ReadFromJsonAsync<PaymentLinkDto>();
        Assert.IsNotNull(link);
        Assert.AreEqual(order.OrderId, link.OrderId);
        Assert.AreEqual(50_000, link.Amount);
        Assert.AreEqual(
            50_000,
            factory.PaymentGateway.LastRequest?.Amount);
        Assert.AreEqual(
            "Cat Latte",
            factory.PaymentGateway.LastRequest?.Items.Single().Name);

        factory.PaymentGateway.Status = "PAID";
        var statusResponse = await client.GetAsync(
            $"/api/payments/{order.OrderId}/status");
        var status = await statusResponse.Content
            .ReadFromJsonAsync<PaymentStatusDto>();

        Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.IsNotNull(status);
        Assert.IsTrue(status.IsPaid);
        Assert.AreEqual("Paid", status.PaymentStatus);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<LoafNcattingPrn232Context>();
        var storedPayment = await context.Payments
            .AsNoTracking()
            .SingleAsync();
        Assert.AreEqual(
            link.OrderCode.ToString(),
            storedPayment.TransactionCode);
        Assert.IsNotNull(storedPayment.PaidAt);
        Assert.AreEqual(
            1,
            await context.Notifications.CountAsync(notification =>
                notification.UserId == customer.UserId &&
                notification.Type ==
                    NotificationTypes.PaymentSucceeded));
    }

    [TestMethod]
    public async Task PaymentLink_ForAnotherCustomersOrder_ReturnsNotFound()
    {
        await using var factory = await CreateFactoryAsync();
        var ownerClient = factory.CreateClient();
        await RegisterAndAuthenticateAsync(
            ownerClient,
            "Owner",
            "owner-pay@example.com");
        var order = await CreateOnlineOrderAsync(ownerClient);

        var otherClient = factory.CreateClient();
        await RegisterAndAuthenticateAsync(
            otherClient,
            "Other",
            "other-pay@example.com",
            "0900000003");
        var response = await otherClient.PostAsJsonAsync(
            "/api/payments/links",
            new CreatePaymentLinkRequest(order.OrderId));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.IsNull(factory.PaymentGateway.LastRequest);
    }

    [TestMethod]
    public async Task Staff_CannotProcessAnOrderAwaitingOnlinePayment()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        await RegisterAndAuthenticateAsync(
            customerClient,
            "Bill",
            "pending-pay@example.com");
        var order = await CreateOnlineOrderAsync(customerClient);

        var staffClient = factory.CreateClient();
        await AuthenticateAsync(
            staffClient,
            "staff@example.com",
            "Password1");
        var response = await staffClient.PatchAsJsonAsync(
            $"/api/store/orders/{order.OrderId}/status",
            new OrderStatusUpdateRequest(OrderStatusId: 2));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task ExpiredPendingPayment_CancelsOrder_AndRestoresStock()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(
            client,
            "Bill",
            "expired-pay@example.com");
        var order = await CreateOnlineOrderAsync(client);
        factory.SetVietnamTime(7, 16);

        var response = await client.GetAsync(
            $"/api/payments/{order.OrderId}/status");
        var status = await response.Content
            .ReadFromJsonAsync<PaymentStatusDto>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(status);
        Assert.IsFalse(status.IsPaid);
        Assert.AreEqual("Cancelled", status.PaymentStatus);
        Assert.AreEqual("Cancelled", status.OrderStatus);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<LoafNcattingPrn232Context>();
        Assert.AreEqual(
            10,
            await context.Products
                .Where(product => product.ProductId == 1)
                .Select(product => product.UnitInStock)
                .SingleAsync());
    }


    [TestMethod]
    public async Task Customer_CannotCreateAnotherOrder_WithPendingOnlinePayment()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(
            client,
            "Bill",
            "single-pending@example.com");
        await CreateOnlineOrderAsync(client);
        var addResponse = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(ProductId: 2, Quantity: 1));
        Assert.AreEqual(HttpStatusCode.OK, addResponse.StatusCode);

        var checkoutResponse = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            new CheckoutRequest(
                OrderType: "Takeaway",
                TableId: null,
                ReservationId: null,
                PaymentMethodId: 2,
                Note: null));

        Assert.AreEqual(
            HttpStatusCode.Conflict,
            checkoutResponse.StatusCode);
    }

    private static async Task<OrderDto> CreateOnlineOrderAsync(
        HttpClient client)
    {
        var addResponse = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(ProductId: 1, Quantity: 1));
        Assert.AreEqual(HttpStatusCode.OK, addResponse.StatusCode);

        var checkoutResponse = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            new CheckoutRequest(
                OrderType: "Takeaway",
                TableId: null,
                ReservationId: null,
                PaymentMethodId: 2,
                Note: null));
        Assert.AreEqual(
            HttpStatusCode.Created,
            checkoutResponse.StatusCode,
            await checkoutResponse.Content.ReadAsStringAsync());
        var order = await checkoutResponse.Content
            .ReadFromJsonAsync<OrderDto>();
        Assert.IsNotNull(order);
        Assert.AreEqual("Pending", order.Payments.Single().PaymentStatus);
        return order;
    }

    private static async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedOrderingDataAsync();
        await factory.CreateStaffAsync();
        return factory;
    }

    private static async Task<UserDto> RegisterAndAuthenticateAsync(
        HttpClient client,
        string name,
        string email,
        string phoneNumber = "0900000001")
    {
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                name,
                email,
                "Password1",
                phoneNumber,
                "Ho Chi Minh City"));
        Assert.AreEqual(
            HttpStatusCode.Created,
            registerResponse.StatusCode);
        var user = await registerResponse.Content
            .ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(user);
        await AuthenticateAsync(client, email, "Password1");
        return user;
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
        var login = await response.Content
            .ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);
    }
}
