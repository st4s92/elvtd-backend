using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IAccountRepository
{
    public Task<(List<Account>, ITError?)> GetAccounts(Account filter);
    public Task<(Account?, ITError?)> SaveAccount(Account account);
    public Task<(Account?, ITError?)> GetAccount(Account filter);
    public Task<(List<MasterSlave>, ITError?)> GetMasterSlaves(MasterSlave filter);
    public Task<(MasterSlavePair?, ITError?)> GetMasterSlavePair(MasterSlavePair filter);
    public Task<(MasterSlaveConfig?, ITError?)> GetMasterSlaveConfig(MasterSlaveConfig filter);
}