using System.Data;
using LoafNCatting.Application.DTOs.Carts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class CartService : ICartService
{
    private const string CustomerRoleName = "Customer";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorageService _mediaStorage;
    private readonly TimeProvider _timeProvider;

    public CartService(
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IMediaStorageService? mediaStorage = null)
    {
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _mediaStorage = mediaStorage ?? PassThroughMediaStorageService.Instance;
    }

    public async Task<CartDto> GetCartAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveCustomerAsync(userId, cancellationToken);
        var cart = await FindCartAsync(userId, trackChanges: false, cancellationToken);
        return cart is null ? EmptyCart(userId) : ToCartDto(cart, _mediaStorage);
    }

    public async Task<CartDto> AddItemAsync(
        int userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ValidateProductId(request.ProductId);
        ValidatePositiveQuantity(request.Quantity);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveCustomerAsync(userId, cancellationToken);
            var product = await GetOrderableProductAsync(
                request.ProductId,
                cancellationToken);
            var cart = await FindCartAsync(
                userId,
                trackChanges: true,
                cancellationToken);

            if (cart is null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = UtcNow()
                };
                Set<Cart>().Add(cart);
            }

            var item = cart.CartItems.FirstOrDefault(
                current => current.ProductId == request.ProductId);
            var requestedQuantity = (item?.Quantity ?? 0) + request.Quantity;
            EnsureQuantityAvailable(product, requestedQuantity);

            if (item is null)
            {
                cart.CartItems.Add(CreateCartItem(product, requestedQuantity));
            }
            else
            {
                UpdateCartItem(item, product, requestedQuantity);
            }

            Touch(cart);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ToCartDto(cart, _mediaStorage);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CartDto> UpdateItemAsync(
        int userId,
        int productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ValidateProductId(productId);
        if (request.Quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Quantity cannot be negative.");
        }

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveCustomerAsync(userId, cancellationToken);
            var cart = await FindCartAsync(
                userId,
                trackChanges: true,
                cancellationToken);

            if (cart is null)
            {
                if (request.Quantity == 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return EmptyCart(userId);
                }

                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = UtcNow()
                };
                Set<Cart>().Add(cart);
            }

            var item = cart.CartItems.FirstOrDefault(
                current => current.ProductId == productId);
            if (request.Quantity == 0)
            {
                if (item is not null)
                {
                    RemoveItem(cart, item);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return ToCartDto(cart, _mediaStorage);
            }

            var product = await GetOrderableProductAsync(productId, cancellationToken);
            EnsureQuantityAvailable(product, request.Quantity);
            if (item is null)
            {
                cart.CartItems.Add(CreateCartItem(product, request.Quantity));
            }
            else
            {
                UpdateCartItem(item, product, request.Quantity);
            }

            Touch(cart);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ToCartDto(cart, _mediaStorage);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CartDto> RemoveItemAsync(
        int userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        ValidateProductId(productId);
        await EnsureActiveCustomerAsync(userId, cancellationToken);

        var cart = await FindCartAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
        {
            return EmptyCart(userId);
        }

        var item = cart.CartItems.FirstOrDefault(
            current => current.ProductId == productId);
        if (item is not null)
        {
            RemoveItem(cart, item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToCartDto(cart, _mediaStorage);
    }

    public async Task<CartDto> ClearAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveCustomerAsync(userId, cancellationToken);

        var cart = await FindCartAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
        {
            return EmptyCart(userId);
        }

        Set<CartItem>().RemoveRange(cart.CartItems.ToList());
        cart.CartItems.Clear();
        Touch(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToCartDto(cart, _mediaStorage);
    }

    private DbSet<T> Set<T>() where T : class
        => _unitOfWork.Repository<T>().Entities;

    private Task<Cart?> FindCartAsync(
        int userId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        IQueryable<Cart> query = Set<Cart>()
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Product);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(
            cart => cart.UserId == userId,
            cancellationToken);
    }

    private async Task EnsureActiveCustomerAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        var isActiveCustomer = await Set<User>()
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.UserId == userId &&
                    user.IsActive &&
                    user.Role.RoleName == CustomerRoleName,
                cancellationToken);
        if (!isActiveCustomer)
        {
            throw new UnauthorizedAccessException(
                "The authenticated customer account is not active or valid.");
        }
    }

    private async Task<Product> GetOrderableProductAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var product = await Set<Product>()
            .FirstOrDefaultAsync(
                current => current.ProductId == productId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        if (!product.IsAvailable || product.UnitInStock <= 0)
        {
            throw new InvalidOperationException(
                "Product is unavailable or out of stock.");
        }

        return product;
    }

    private void RemoveItem(Cart cart, CartItem item)
    {
        Set<CartItem>().Remove(item);
        cart.CartItems.Remove(item);
        Touch(cart);
    }

    private CartItem CreateCartItem(Product product, int quantity)
        => new()
        {
            ProductId = product.ProductId,
            Product = product,
            Quantity = quantity,
            UnitPrice = CurrentPrice(product),
            CreatedAt = UtcNow()
        };

    private void UpdateCartItem(CartItem item, Product product, int quantity)
    {
        item.Product = product;
        item.Quantity = quantity;
        item.UnitPrice = CurrentPrice(product);
        item.UpdatedAt = UtcNow();
    }

    private static void EnsureQuantityAvailable(Product product, int quantity)
    {
        if (quantity > product.UnitInStock)
        {
            throw new InvalidOperationException(
                $"Only {product.UnitInStock} unit(s) of '{product.Name}' are available.");
        }
    }

    private static void ValidatePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }
    }

    private static void ValidateProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId),
                "Product id must be greater than zero.");
        }
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "User id must be greater than zero.");
        }
    }

    private void Touch(Cart cart)
        => cart.UpdatedAt = UtcNow();

    private DateTime UtcNow()
        => _timeProvider.GetUtcNow().UtcDateTime;

    private static decimal CurrentPrice(Product product)
        => product.DiscountPrice ?? product.Price;

    private static CartDto EmptyCart(int userId)
        => new(0, userId, Array.Empty<CartItemDto>(), 0m);

    private static CartDto ToCartDto(
        Cart cart,
        IMediaStorageService mediaStorage)
    {
        var items = cart.CartItems
            .OrderBy(item => item.CreatedAt)
            .Select(item => new CartItemDto(
                item.CartItemId,
                item.ProductId,
                item.Product.Name,
                mediaStorage.ResolveDisplayUrl(item.Product.Picture),
                item.UnitPrice,
                item.Quantity,
                item.UnitPrice * item.Quantity,
                item.Product.UnitInStock,
                item.Product.IsAvailable))
            .ToList();

        return new CartDto(
            cart.CartId,
            cart.UserId,
            items,
            items.Sum(item => item.LineTotal));
    }
}
