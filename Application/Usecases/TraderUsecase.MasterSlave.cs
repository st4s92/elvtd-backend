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

    public async Task<(MasterSlave?, ITError?)> EditMasterSlaveFullonfig(
    MasterSlaveFullConfigPayload payload
)
    {
        using var tx = await _masterSlaveRepository.BeginTransactionAsync();

        try
        {
            // role mapping
            long masterId, slaveId;

            if (payload.Role == CopyTradeRole.SLAVE)
            {
                masterId = payload.DestinationId;
                slaveId = payload.AccountId;
            }
            else
            {
                masterId = payload.AccountId;
                slaveId = payload.DestinationId;
            }

            // validate accounts
            if (await _accountRepository.Get(a => a.Id == masterId && a.DeletedAt == null) == null)
                return (null, TError.NewClient("Master account not found"));

            if (await _accountRepository.Get(a => a.Id == slaveId && a.DeletedAt == null) == null)
                return (null, TError.NewClient("Slave account not found"));

            // 1️⃣ find or create master_slave
            var masterSlave = await _masterSlaveRepository.Get(m =>
                m.MasterId == masterId &&
                m.SlaveId == slaveId &&
                m.DeletedAt == null
            );

            if (masterSlave == null)
            {
                var (created, err) = await AddMasterSlave(new MasterSlave
                {
                    Name = payload.ConnectionName,
                    MasterId = masterId,
                    SlaveId = slaveId
                });

                if (err != null)
                    return (null, err);

                masterSlave = created!;
            }
            else
            {
                await _masterSlaveRepository.Update(
                    m => m.Id == masterSlave.Id,
                    m => m.Name = payload.ConnectionName
                );
            }

            // update config
            var existingConfig = await _masterSlaveConfigRepository.Get(c =>
                c.MasterSlaveId == masterSlave.Id &&
                c.DeletedAt == null
            );

            if (existingConfig == null)
            {
                // CREATE
                await _masterSlaveConfigRepository.Save(new MasterSlaveConfig
                {
                    MasterSlaveId = masterSlave.Id,
                    Multiplier = payload.Multiplier
                });
            }
            else
            {
                // UPDATE
                await _masterSlaveConfigRepository.Update(
                    c => c.Id == existingConfig.Id,
                    c => c.Multiplier = payload.Multiplier
                );
            }

            // HARD DELETE PAIRS
            await _masterSlavePairRepository.DeleteByMasterSlaveId(masterSlave.Id);

            // INSERT NEW PAIRS
            foreach (var pair in payload.SymbolPairs ?? [])
            {
                await _masterSlavePairRepository.Save(new MasterSlavePair
                {
                    MasterSlaveId = masterSlave.Id,
                    MasterPair = pair.MasterSymbol.Trim().ToUpper(),
                    SlavePair = pair.SlaveSymbol.Trim().ToUpper()
                });
            }

            // COMMIT
            await _masterSlaveRepository.CommitAsync();

            return (masterSlave, null);
        }
        catch (Exception ex)
        {
            await _masterSlaveRepository.RollbackAsync();
            return (null, TError.NewServer(ex.Message));
        }
    }


    public async Task<(MasterSlaveFullConfigPayload?, ITError?)> GetMasterSlaveFullConfig(long accountId)
    {
        try
        {
            var masterSlave = await _masterSlaveRepository.Get(ms =>
                ms.DeletedAt == null &&
                (ms.MasterId == accountId || ms.SlaveId == accountId)
            );

            if (masterSlave == null)
                return (null, null);

            var isMaster = masterSlave.MasterId == accountId;
            var role = isMaster ? CopyTradeRole.MASTER : CopyTradeRole.SLAVE;

            var destinationId = isMaster
                ? masterSlave.SlaveId
                : masterSlave.MasterId;

            var config = await _masterSlaveConfigRepository.Get(c =>
                c.DeletedAt == null &&
                c.MasterSlaveId == masterSlave.Id
            );

            var pairs = await _masterSlavePairRepository.GetMany(p =>
                p.DeletedAt == null &&
                p.MasterSlaveId == masterSlave.Id
            );

            var dto = new MasterSlaveFullConfigPayload
            {
                ConnectionName = masterSlave.Name,
                Role = role,
                AccountId = accountId,
                DestinationId = destinationId,
                Multiplier = config?.Multiplier ?? 1.0m,
                SymbolPairs = [.. pairs.Select(p => new MasterSlaveSymbolPairPayload
                {
                    MasterSymbol = p.MasterPair,
                    SlaveSymbol = p.SlavePair
                })]
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }
}