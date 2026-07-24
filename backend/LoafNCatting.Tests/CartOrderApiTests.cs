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
public sealed class CartOrderApiTests
{
    [TestMethod]
    public async Task CustomerEndpoints_WithoutBearerToken_ReturnUnauthorized()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var cartResponse = await client.GetAsync("/api/cart");
        var checkoutResponse = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            CheckoutRequest());

        Assert.AreEqual(HttpStatusCode.Unauthorized, cartResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, checkoutResponse.StatusCode);
    }

    [TestMethod]
    public async Task RoleProtectedEndpoints_RejectTheWrongRole()
    {
        await using var factory = await CreateFactoryAsync();
        var staffClient = factory.CreateClient();
        await AuthenticateAsync(staffClient, "staff@example.com", "Password1");
        var customerClient = factory.CreateClient();
        await RegisterCustomerAsync(
            customerClient,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        await AuthenticateAsync(customerClient, "bill@gmail.com", "Password1");

        var staffCartResponse = await staffClient.GetAsync("/api/cart");
        var customerStoreResponse = await customerClient.GetAsync("/api/store/orders");

        Assert.AreEqual(HttpStatusCode.Forbidden, staffCartResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, customerStoreResponse.StatusCode);
    }

    [TestMethod]
    public async Task CartAndCheckout_UseAuthenticatedCustomerSubject()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient();
        var customer = await RegisterCustomerAsync(
            client,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        await AuthenticateAsync(client, "bill@gmail.com", "Password1");

        var addResponse = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(ProductId: 1, Quantity: 2));
        var checkoutResponse = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            CheckoutRequest());
        var cartResponse = await client.GetAsync("/api/cart");

        Assert.AreEqual(HttpStatusCode.OK, addResponse.StatusCode);
        var cartAfterAdd = await addResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.IsNotNull(cartAfterAdd);
        Assert.AreEqual(customer.UserId, cartAfterAdd.UserId);
        Assert.AreEqual(100_000m, cartAfterAdd.Total);

        Assert.AreEqual(
            HttpStatusCode.Created,
            checkoutResponse.StatusCode,
            await checkoutResponse.Content.ReadAsStringAsync());
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.IsNotNull(order);
        Assert.AreEqual(customer.UserId, order.CustomerUserId);
        Assert.AreEqual(100_000m, order.TotalPrice);
        Assert.AreEqual("Pending", order.OrderStatusName);

        Assert.AreEqual(HttpStatusCode.OK, cartResponse.StatusCode);
        var emptyCart = await cartResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.IsNotNull(emptyCart);
        Assert.IsEmpty(emptyCart.Items);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        var storedOrder = await context.Orders.AsNoTracking().SingleAsync();
        Assert.AreEqual(customer.UserId, storedOrder.CustomerUserId);
        Assert.AreEqual(8, await context.Products
            .Where(product => product.ProductId == 1)
            .Select(product => product.UnitInStock)
            .SingleAsync());
        Assert.AreEqual(
            1,
            await context.Notifications.CountAsync(notification =>
                notification.UserId == customer.UserId &&
                notification.Type == NotificationTypes.OrderCreated));
    }

    [TestMethod]
    public async Task CustomerDetail_WhenOrderBelongsToAnotherCustomer_ReturnsNotFound()
    {
        await using var factory = await CreateFactoryAsync();
        var billClient = factory.CreateClient();
        await RegisterCustomerAsync(
            billClient,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        await AuthenticateAsync(billClient, "bill@gmail.com", "Password1");
        await billClient.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(ProductId: 1, Quantity: 1));
        var checkoutResponse = await billClient.PostAsJsonAsync(
            "/api/orders/checkout",
            CheckoutRequest());
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.IsNotNull(order);

        var aliceClient = factory.CreateClient();
        await RegisterCustomerAsync(
            aliceClient,
            "Alice",
            "alice@gmail.com",
            "0987000000");
        await AuthenticateAsync(aliceClient, "alice@gmail.com", "Password1");

        var response = await aliceClient.GetAsync($"/api/orders/{order.OrderId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Staff_CanListAndAdvanceOrder_AndBecomesAssignedOperator()
    {
        await using var factory = await CreateFactoryAsync();
        var customerClient = factory.CreateClient();
        var customer = await RegisterCustomerAsync(
            customerClient,
            "Bill",
            "bill@gmail.com",
            "0987654321");
        await AuthenticateAsync(customerClient, "bill@gmail.com", "Password1");
        await customerClient.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(ProductId: 2, Quantity: 1));
        var checkoutResponse = await customerClient.PostAsJsonAsync(
            "/api/orders/checkout",
            CheckoutRequest());
        var created = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.IsNotNull(created);

        var staffClient = factory.CreateClient();
        await AuthenticateAsync(staffClient, "staff@example.com", "Password1");
        var listResponse = await staffClient.GetAsync("/api/store/orders");
        var updateResponse = await staffClient.PatchAsJsonAsync(
            $"/api/store/orders/{created.OrderId}/status",
            new OrderStatusUpdateRequest(OrderStatusId: 2));

        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
        var orders = await listResponse.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.IsNotNull(orders);
        Assert.HasCount(1, orders);
        Assert.AreEqual(customer.UserId, orders[0].CustomerUserId);
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.IsNotNull(updated);
        Assert.AreEqual("Processing", updated.OrderStatusName);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
        var staffUserId = await context.Users
            .Where(user => user.Email == "staff@example.com")
            .Select(user => user.UserId)
            .SingleAsync();
        var stored = await context.Orders.AsNoTracking().SingleAsync();
        Assert.AreEqual(staffUserId, stored.StaffUserId);
        Assert.AreEqual(2, stored.OrderStatusId);
        Assert.AreEqual(
            1,
            await context.Notifications.CountAsync(notification =>
                notification.UserId == customer.UserId &&
                notification.Type == NotificationTypes.OrderStatusChanged));
    }

    private static CheckoutRequest CheckoutRequest()
        => new(
            OrderType: "Takeaway",
            TableId: null,
            ReservationId: null,
            PaymentMethodId: 1,
            Note: "Less ice");

    private static async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory();
        await factory.SeedRolesAsync();
        await factory.SeedOrderingDataAsync();
        await factory.CreateStaffAsync();
        return factory;
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
