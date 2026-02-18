using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<MasterSlave, bool>> FilterMasterSlave(MasterSlave param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (param.MasterId == 0 || a.MasterId == param.MasterId) &&
                (param.SlaveId == 0 || a.SlaveId == param.SlaveId)
        );
    }
    public async Task<(MasterSlave?, ITError?)> GetMasterSlave(MasterSlave param)
    {
        try
        {
            var data = await _masterSlaveRepository.Get(FilterMasterSlave(param));
            if (data == null)
                return (null, TError.NewNotFound("masterSlave not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<MasterSlave>, ITError?)> GetMasterSlaves(MasterSlave param)
    {
        try
        {
            var data = await _masterSlaveRepository.GetMany(FilterMasterSlave(param));
            if (data == null)
                return ([], TError.NewNotFound("master slave not found"));

            foreach (var item in data)
            {
                item.MasterAccount = await _accountRepository.Get(FilterAccount(new Account
                {
                    Id = item.MasterId
                }));

                item.SlaveAccount = await _accountRepository.Get(FilterAccount(new Account
                {
                    Id = item.SlaveId
                }));
            }
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<MasterSlave>, long total, ITError?)> GetPaginatedMasterSlaves(MasterSlave param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _masterSlaveRepository.GetPaginated(FilterMasterSlave(param), page, pageSize, q => q.OrderByDescending(o => o.CreatedAt));
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    private async Task<bool> IsMasterSlaveCrossing(MasterSlave masterSlave)
    {
        try
        {
            var data = await _masterSlaveRepository.Get(a =>
                a.MasterId == masterSlave.SlaveId &&
                (masterSlave.Id <= 0 || a.Id != masterSlave.Id)
            );
            if (data != null)
            {
                return true;
            }

            data = await _masterSlaveRepository.Get(a =>
                a.SlaveId == masterSlave.MasterId &&
                (masterSlave.Id <= 0 || a.Id != masterSlave.Id)
            );
            if (data != null)
            {
                return true;
            }
            return false;
        }
        catch
        {
            return true;
        }
    }

    public async Task<(MasterSlave?, ITError?)> AddMasterSlave(MasterSlave masterSlave)
    {
        var existingMasterSlave = new MasterSlave
        {
            MasterId = masterSlave.MasterId,
            SlaveId = masterSlave.SlaveId
        };

        var (_, terr) = await GetMasterSlave(existingMasterSlave);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("failed. masterSlave already exist"));
        }

        if (await IsMasterSlaveCrossing(masterSlave))
        {
            return (null, TError.NewClient("failed. check whether master id is not registered as slave before and vice versa for slave id"));
        }

        try
        {
            var data = await _masterSlaveRepository.Save(masterSlave);
            if (data == null)
                return (null, TError.NewServer("cannot create new masterSlave"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(MasterSlave?, ITError?)> UpdateMasterSlaveById(long id, MasterSlave param)
    {
        try
        {
            var (existing, terr) = await GetMasterSlave(new MasterSlave { Id = id });
            if (terr != null)
                return (null, terr);

            param.Id = id;

            if (await IsMasterSlaveCrossing(param))
            {
                return (null, TError.NewClient("failed. check whether master id is not registered as slave before and vice versa for slave id"));
            }

            var data = await _masterSlaveRepository.Save(param, a => a.Id == id);
            if (data == null)
                return (null, TError.NewServer("cannot save masterSlave"));

            return (data, null);
        }

        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<(MasterSlave?, ITError?)> EditMasterSlaveFullConfig(
        MasterSlaveFullConfigPayload payload
    )
    {
        using var tx = await _masterSlaveRepository.BeginTransactionAsync();

        try
        {
            if (payload.AccountId == 0 || payload.DestinationId == 0)
                return (null, TError.NewClient("AccountId and DestinationId must be filled"));

            if (payload.AccountId == payload.DestinationId)
                return (null, TError.NewClient("Master and Slave cannot be the same account"));

            // GET SOURCE ACCOUNT
            var account = await _accountRepository.Get(a =>
                a.Id == payload.AccountId &&
                a.DeletedAt == null
            );

            if (account == null)
                return (null, TError.NewClient("Account not found"));

            long masterId;
            long slaveId;

            // DETERMINE ROLE FROM DB
            if (account.Role == "SLAVE")
            {
                // berarti accountId ini slave
                masterId = payload.DestinationId;
                slaveId = account.Id;
            }
            else if (account.Role == "MASTER")
            {
                masterId = account.Id;
                slaveId = payload.DestinationId;
            }
            else
            {
                return (null, TError.NewClient("Invalid account role"));
            }

            // VALIDATE DESTINATION
            var destinationAccount = await _accountRepository.Get(a =>
                a.Id == payload.DestinationId &&
                a.DeletedAt == null
            );

            if (destinationAccount == null)
                return (null, TError.NewClient("Destination account not found"));

            if (account.Role == "SLAVE" && destinationAccount.Role != "MASTER" ||
            account.Role == "MASTER" && destinationAccount.Role != "SLAVE")
                return (null, TError.NewClient("Should be master - slave connection, vice versa"));

            // SOFT DELETE OLD RELATIONS
            MasterSlave masterSlave;
            var existingRelation = await _masterSlaveRepository.Get(m =>
                m.MasterId == masterId &&
                m.SlaveId == slaveId &&
                m.DeletedAt == null
            );

            if (existingRelation != null)
            {
                // UPDATE ONLY
                await _masterSlaveRepository.Update(
                    m => m.Id == existingRelation.Id,
                    m => m.Name = payload.ConnectionName
                );

                masterSlave = existingRelation;
            }
            else
            {
                // DELETE OLD RELATION
                await _masterSlaveRepository.SoftDeleteByAccountId(
                    account.Id,
                    account.Role
                );

                masterSlave = new MasterSlave
                {
                    Name = payload.ConnectionName,
                    MasterId = masterId,
                    SlaveId = slaveId
                };
                // CREATE
                await _masterSlaveRepository.Save(masterSlave);
            }

            // UPSERT CONFIG
            await _masterSlaveConfigRepository.Save(new MasterSlaveConfig
            {
                MasterSlaveId = masterSlave.Id,
                Multiplier = payload.Multiplier
            });

            // INSERT SYMBOL PAIRS
            if (payload.SymbolPairs != null)
            {
                foreach (var pair in payload.SymbolPairs)
                {
                    if (string.IsNullOrWhiteSpace(pair.MasterSymbol) ||
                        string.IsNullOrWhiteSpace(pair.SlaveSymbol))
                        continue;

                    await _masterSlavePairRepository.Save(new MasterSlavePair
                    {
                        MasterSlaveId = masterSlave.Id,
                        MasterPair = pair.MasterSymbol.Trim().ToUpper(),
                        SlavePair = pair.SlaveSymbol.Trim().ToUpper()
                    });
                }
            }

            await _masterSlaveRepository.CommitAsync();

            return (masterSlave, null);
        }
        catch (Exception ex)
        {
            await _masterSlaveRepository.RollbackAsync();
            return (null, TError.NewServer("Edit master slave failed", ex.Message));
        }
    }


    public async Task<(MasterSlaveFullConfigPayload?, ITError?)> GetMasterSlaveFullConfig(long accountId)
    {
        try
        {
            // get account first
            var account = await _accountRepository.Get(a =>
                a.Id == accountId &&
                a.DeletedAt == null
            );

            if (account == null)
                return (null, TError.NewNotFound("account not found"));

            // make sure role
            var role = account.Role;

            // query based on role
            MasterSlave? masterSlave = null;

            if (role == "MASTER")
            {
                masterSlave = await _masterSlaveRepository.Get(ms =>
                    ms.DeletedAt == null &&
                    ms.MasterId == accountId
                );
            }
            else if (role == "SLAVE")
            {
                masterSlave = await _masterSlaveRepository.Get(ms =>
                    ms.DeletedAt == null &&
                    ms.SlaveId == accountId
                );
            }

            if (masterSlave == null)
                return (null, null);

            var destinationId = role == "MASTER"
                ? masterSlave.SlaveId
                : masterSlave.MasterId;

            // take config
            var config = await _masterSlaveConfigRepository.Get(c =>
                c.DeletedAt == null &&
                c.MasterSlaveId == masterSlave.Id
            );

            // take pairs
            var pairs = await _masterSlavePairRepository.GetMany(p =>
                p.DeletedAt == null &&
                p.MasterSlaveId == masterSlave.Id
            );

            var dto = new MasterSlaveFullConfigPayload
            {
                ConnectionName = masterSlave.Name,
                AccountId = accountId,
                DestinationId = destinationId,
                Multiplier = config?.Multiplier ?? 1.0m,
                SymbolPairs = pairs.Select(p => new MasterSlaveSymbolPairPayload
                {
                    MasterSymbol = p.MasterPair,
                    SlaveSymbol = p.SlavePair
                }).ToList()
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

}