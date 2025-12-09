using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<Account, bool>> FilterAccount(Account param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (string.IsNullOrEmpty(param.PlatformName) || a.PlatformName == param.PlatformName) &&
                (string.IsNullOrEmpty(param.PlatformPath) || (a.PlatformPath != null && a.PlatformPath.Contains(param.PlatformPath))) &&
                (param.AccountNumber == 0 || a.AccountNumber == param.AccountNumber) &&
                (string.IsNullOrEmpty(param.BrokerName) || a.BrokerName.Contains(param.BrokerName)) &&
                (string.IsNullOrEmpty(param.ServerName) || a.ServerName.Contains(param.ServerName)) &&
                (param.UserId == 0 || a.UserId == param.UserId)
        );
    }
    public async Task<(Account?, ITError?)> GetAccount(Account param)
    {
        try
        {
            var data = await _accountRepository.Get(FilterAccount(param));
            if (data == null)
                return (null, TError.NewNotFound("account not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Account>, ITError?)> GetAccounts(Account param)
    {
        try
        {
            var data = await _accountRepository.GetMany(FilterAccount(param));
            if (data == null)
                return ([], TError.NewNotFound("account not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Account>, long total, ITError?)> GetPaginatedAccounts(Account param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _accountRepository.GetPaginated(FilterAccount(param), page, pageSize, q => q.OrderByDescending(o => o.CreatedAt));
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(Account?, ITError?)> AddAccount(Account account)
    {
        var existingAccount = new Account
        {
            AccountNumber = account.AccountNumber,
            ServerName = account.ServerName
        };

        var (_, terr) = await GetAccount(existingAccount);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("account with the server name and account number already exist"));
        }

        try
        {
            var data = await _accountRepository.Save(account);
            if (data == null)
                return (null, TError.NewServer("cannot create new account"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(Account?, ITError?)> UpdateAccountById(long id, Account param)
    {   
        try
        {
            var (_, terr) = await GetAccount(new Account {Id = id});
            if(terr != null)
                return (null, terr);
            
            var data = await _accountRepository.Save(param, a => a.Id == id);
            if(data == null)
                return (null, TError.NewServer("cannot save account"));

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }
}