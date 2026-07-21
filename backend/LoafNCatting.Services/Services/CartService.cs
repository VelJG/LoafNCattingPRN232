using LoafNCatting.Application.DTOs.Carts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;

    public CartService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CartDto> GetCartAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var cart = await FindCartAsync(userId, trackChanges: false, cancellationToken);
        return cart is null ? EmptyCart(userId) : ToCartDto(cart);
    }

    public async Task<CartDto> AddItemAsync(
        int userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be greater than zero.");
        }

        await EnsureUserExistsAsync(userId, cancellationToken);
        var product = await Set<Product>()
            .FirstOrDefaultAsync(item => item.ProductId == request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        EnsureProductCanBeOrdered(product);

        var cart = await FindCartAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            Set<Cart>().Add(cart);
        }

        var item = cart.CartItems.FirstOrDefault(current => current.ProductId == request.ProductId);
        var quantity = Math.Min((item?.Quantity ?? 0) + request.Quantity, product.UnitInStock);
        if (item is null)
        {
            cart.CartItems.Add(CreateCartItem(product, quantity));
        }
        else
        {
            UpdateCartItem(item, product, quantity);
        }

        Touch(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToCartDto(cart);
    }

    public async Task<CartDto> UpdateItemAsync(
        int userId,
        int productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await EnsureUserExistsAsync(userId, cancellationToken);

        var cart = await FindCartAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
        {
            return request.Quantity <= 0
                ? EmptyCart(userId)
                : await AddItemAsync(
                    userId,
                    new AddCartItemRequest(productId, request.Quantity),
                    cancellationToken);
        }

        var item = cart.CartItems.FirstOrDefault(current => current.ProductId == productId);
        if (request.Quantity <= 0)
        {
            if (item is not null)
            {
                RemoveItem(cart, item);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ToCartDto(cart);
        }

        var product = await Set<Product>()
            .FirstOrDefaultAsync(current => current.ProductId == productId, cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        EnsureProductCanBeOrdered(product);
        var quantity = Math.Min(request.Quantity, product.UnitInStock);
        if (item is null)
        {
            cart.CartItems.Add(CreateCartItem(product, quantity));
        }
        else
        {
            UpdateCartItem(item, product, quantity);
        }

        Touch(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToCartDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(
        int userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await EnsureUserExistsAsync(userId, cancellationToken);

        var cart = await FindCartAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
        {
            return EmptyCart(userId);
        }

        var item = cart.CartItems.FirstOrDefault(current => current.ProductId == productId);
        if (item is not null)
        {
            RemoveItem(cart, item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToCartDto(cart);
    }

    public async Task<CartDto> ClearAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await EnsureUserExistsAsync(userId, cancellationToken);

        var cart = await FindCartAsync(userId, trackChanges: true, cancellationToken);
        if (cart is null)
        {
            return EmptyCart(userId);
        }

        Set<CartItem>().RemoveRange(cart.CartItems.ToList());
        cart.CartItems.Clear();
        Touch(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToCartDto(cart);
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

        return query.FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
    }

    private async Task EnsureUserExistsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        if (!await Set<User>().AsNoTracking().AnyAsync(user => user.UserId == userId, cancellationToken))
        {
            throw new KeyNotFoundException("User not found.");
        }
    }

    private void RemoveItem(Cart cart, CartItem item)
    {
        Set<CartItem>().Remove(item);
        cart.CartItems.Remove(item);
        Touch(cart);
    }

    private static CartItem CreateCartItem(Product product, int quantity)
        => new()
        {
            ProductId = product.ProductId,
            Product = product,
            Quantity = quantity,
            UnitPrice = CurrentPrice(product),
            CreatedAt = DateTime.UtcNow
        };

    private static void UpdateCartItem(CartItem item, Product product, int quantity)
    {
        item.Product = product;
        item.Quantity = quantity;
        item.UnitPrice = CurrentPrice(product);
        item.UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureProductCanBeOrdered(Product product)
    {
        if (!product.IsAvailable || product.UnitInStock <= 0)
        {
            throw new InvalidOperationException("Product is unavailable or out of stock.");
        }
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User id must be greater than zero.");
        }
    }

    private static void Touch(Cart cart)
        => cart.UpdatedAt = DateTime.UtcNow;

    private static decimal CurrentPrice(Product product)
        => product.DiscountPrice ?? product.Price;

    private static CartDto EmptyCart(int userId)
        => new(0, userId, Array.Empty<CartItemDto>(), 0m);

    private static CartDto ToCartDto(Cart cart)
    {
        var items = cart.CartItems
            .OrderBy(item => item.CreatedAt)
            .Select(item => new CartItemDto(
                item.CartItemId,
                item.ProductId,
                item.Product.Name,
                item.Product.Picture,
                item.UnitPrice,
                item.Quantity,
                item.UnitPrice * item.Quantity))
            .ToList();

        return new CartDto(cart.CartId, cart.UserId, items, items.Sum(item => item.LineTotal));
    }
}
