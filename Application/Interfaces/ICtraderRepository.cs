using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface ICtraderRepository
{
    public Task<(CtraderUser?, ITError?)> GetUserByTokenAsync(AppToken token);
    public Task<(AppToken?, ITError?)> GetTokenAsync(string code);
    public Task<(AppToken?, ITError?)> RefreshTokenAsync(string refreshToken);
}