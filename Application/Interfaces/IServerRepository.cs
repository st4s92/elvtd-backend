using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IServerRepository : IRepository<Server>
{
    Task<Server?> GetFirstAvailableServer(int maxAccountPerServer);
}