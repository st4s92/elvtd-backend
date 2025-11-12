using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface ITradingRepository
{
    public Task<(AppToken?, ITError?)> SaveToken(AppToken token);
}