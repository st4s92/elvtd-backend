using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace Backend.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<OrderRepository> _logger;

    public OrderRepository(AppDbContext context, AppLogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<Order>, ITError?)> GetOrders(Order filter)
    {
        List<Order> res = new();
        ITError? terr = null;

        try
        {
            var query = ApplyFilters(_context.Orders.Where(o => o.DeletedAt == null), filter);
            res = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    public async Task<(List<Order>, ITError?)> GetActiveOrders(Order filter)
    {
        List<Order> res = new();
        ITError? terr = null;

        try
        {
            var query = ApplyFilters(_context.Orders.Where(o => o.DeletedAt == null), filter);
            res = await query.Where(o => o.OrderCloseAt == null).ToListAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    public async Task<(Order?, ITError?)> GetOrder(Order filter)
    {
        Order? res = new();
        ITError? terr = null;

        try
        {
            var query = ApplyFilters(_context.Orders.Where(o => o.DeletedAt == null), filter);
            res = await query.FirstOrDefaultAsync();
            if(res == null)
            {
                terr = TError.NewNotFound("order not found");
            }
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    private static IQueryable<Order> ApplyFilters(IQueryable<Order> query, Order filter)
    {
        if (filter.Id != 0)
            query = query.Where(o => o.Id == filter.Id);

        if (filter.AccountId != 0)
            query = query.Where(o => o.AccountId == filter.AccountId);

        if (filter.MasterOrderId == 0 || filter.MasterOrderId == null){ }
        else query = query.Where(o => o.MasterOrderId == filter.MasterOrderId);

        if (filter.OrderTicket != 0)
            query = query.Where(o => o.OrderTicket == filter.OrderTicket);

        if (!string.IsNullOrWhiteSpace(filter.OrderSymbol))
            query = query.Where(o => o.OrderSymbol != null && o.OrderSymbol.Contains(filter.OrderSymbol));

        if (!string.IsNullOrWhiteSpace(filter.OrderType))
            query = query.Where(o => o.OrderType != null && o.OrderType.Contains(filter.OrderType));

        if (filter.OrderLot != 0)
            query = query.Where(o => o.OrderLot == filter.OrderLot);

        if (filter.OrderPrice != 0)
            query = query.Where(o => o.OrderPrice == filter.OrderPrice);

        if (filter.ActualPrice != 0 && filter.ActualPrice != null)
            query = query.Where(o => o.ActualPrice == filter.ActualPrice);

        if (!string.IsNullOrWhiteSpace(filter.OrderComment))
            query = query.Where(o => o.OrderComment != null && o.OrderComment == filter.OrderType);

        if (filter.Status != 0)
            query = query.Where(o => o.Status == filter.Status);

        return query;
    }

    public async Task<(Order?, ITError?)> SaveOrder(Order order)
    {
        Order? res = null;
        ITError? terr = null;

        try
        {
            var existing = await _context.Orders
                .FirstOrDefaultAsync(t =>
                    t.AccountId == order.AccountId &&
                    t.OrderTicket == order.OrderTicket &&
                    t.DeletedAt == null);

            if (existing is not null)
            {
                existing = order;
                _context.Orders.Update(existing);
                res = existing;
            }
            else
            {
                order.CreatedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Add(order);
                res = order;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }
}