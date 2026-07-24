using System.Linq.Expressions;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class AdminTableService : IAdminTableService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminTableService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AdminTableDto>> GetAllAsync(
        string? search,
        string? status,
        string? area,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<RestaurantTable>().Entities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(table =>
                table.TableName.Contains(term) ||
                table.Area != null && table.Area.Contains(term) ||
                table.Description != null && table.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusName = status.Trim();
            query = query.Where(table => table.TableStatus.StatusName == statusName);
        }

        if (!string.IsNullOrWhiteSpace(area))
        {
            var areaName = area.Trim();
            query = query.Where(table => table.Area == areaName);
        }

        return await query
            .OrderBy(table => table.Area)
            .ThenBy(table => table.TableName)
            .Select(Map)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminTableDto?> GetByIdAsync(
        int tableId,
        CancellationToken cancellationToken = default)
        => await _unitOfWork.Repository<RestaurantTable>().Entities
            .AsNoTracking()
            .Where(table => table.TableId == tableId)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AdminTableOptionsDto> GetOptionsAsync(
        CancellationToken cancellationToken = default)
        => new()
        {
            Statuses = await _unitOfWork.Repository<TableStatus>().Entities
                .AsNoTracking()
                .OrderBy(status => status.StatusName)
                .Select(status => status.StatusName)
                .ToListAsync(cancellationToken)
        };

    public async Task<AdminTableDto> CreateAsync(
        AdminTableUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var statusId = await ValidateAndResolveStatusAsync(
            request,
            tableId: null,
            cancellationToken);

        var table = new RestaurantTable
        {
            TableName = request.TableName.Trim(),
            Capacity = request.Capacity,
            Area = Clean(request.Area),
            Description = Clean(request.Description),
            TableStatusId = statusId
        };

        await _unitOfWork.Repository<RestaurantTable>().InsertAsync(table, saveChanges: false);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(table.TableId, cancellationToken))!;
    }

    public async Task<AdminTableDto?> UpdateAsync(
        int tableId,
        AdminTableUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var statusId = await ValidateAndResolveStatusAsync(
            request,
            tableId,
            cancellationToken);

        var table = await _unitOfWork.Repository<RestaurantTable>().FindAsync(tableId);
        if (table is null)
        {
            return null;
        }

        table.TableName = request.TableName.Trim();
        table.Capacity = request.Capacity;
        table.Area = Clean(request.Area);
        table.Description = Clean(request.Description);
        table.TableStatusId = statusId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(tableId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int tableId,
        CancellationToken cancellationToken = default)
    {
        var table = await _unitOfWork.Repository<RestaurantTable>().FindAsync(tableId);
        if (table is null)
        {
            return false;
        }

        if (await HasTableDependenciesAsync(tableId, cancellationToken))
        {
            throw new InvalidOperationException(
                "Cannot delete this table because orders or reservations already reference it.");
        }

        await _unitOfWork.Repository<RestaurantTable>().DeleteAsync(table, saveChanges: false);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<int> ValidateAndResolveStatusAsync(
        AdminTableUpsertRequest request,
        int? tableId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
        {
            throw new ArgumentException("Table name is required.");
        }

        if (request.Capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new ArgumentException("Table status is required.");
        }

        var tableName = request.TableName.Trim();
        var duplicateExists = await _unitOfWork.Repository<RestaurantTable>().Entities
            .AsNoTracking()
            .AnyAsync(table =>
                table.TableName == tableName &&
                table.TableId != tableId,
                cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException("Table name already exists.");
        }

        var statusName = request.Status.Trim();
        var statusId = await _unitOfWork.Repository<TableStatus>().Entities
            .AsNoTracking()
            .Where(status => status.StatusName == statusName)
            .Select(status => (int?)status.TableStatusId)
            .FirstOrDefaultAsync(cancellationToken);

        if (statusId is null)
        {
            throw new ArgumentException("Table status does not exist.");
        }

        return statusId.Value;
    }

    private async Task<bool> HasTableDependenciesAsync(
        int tableId,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<Order>().Entities
            .AsNoTracking()
            .AnyAsync(order => order.TableId == tableId, cancellationToken) ||
        await _unitOfWork.Repository<Reservation>().Entities
            .AsNoTracking()
            .AnyAsync(reservation => reservation.TableId == tableId, cancellationToken);

    private static readonly Expression<Func<RestaurantTable, AdminTableDto>> Map = table => new()
    {
        TableId = table.TableId,
        TableName = table.TableName,
        Capacity = table.Capacity,
        Area = table.Area,
        Description = table.Description,
        Status = table.TableStatus.StatusName
    };

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
