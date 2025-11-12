using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IOrderRepository
{
    public Task<(Order?, ITError?)> SaveOrder(Order order);
    public Task<(List<Order>, ITError?)> GetOrders(Order filter);
    public Task<(List<Order>, ITError?)> GetActiveOrders(Order filter);
    public Task<(Order?, ITError?)> GetOrder(Order filter);
}