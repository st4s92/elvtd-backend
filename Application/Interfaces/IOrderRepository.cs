using System.Linq.Expressions;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<List<Order>> GetOrdersWithMaster(Expression<Func<Order, bool>> predicate);
}