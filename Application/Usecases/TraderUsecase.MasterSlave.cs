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

    public async Task<(MasterSlave, ITError?)> EditMasterSlaveFullonfig(MasterSlaveFullConfigPayload payload)
    {
        // role mapping
        long masterId;
        long slaveId;

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

        // validation master account
        var masterAccount = await _accountRepository.Get(a =>
            a.Id == masterId && a.DeletedAt == null
        );
        if (masterAccount == null)
        {
            return (null!, TError.NewClient("Master account not found"));
        }

        // validation slave account
        var slaveAccount = await _accountRepository.Get(a =>
            a.Id == slaveId && a.DeletedAt == null
        );
        if (slaveAccount == null)
        {
            return (null!, TError.NewClient("Slave account not found"));
        }

        // 1️⃣ master_slave
        var masterSlave = new MasterSlave
        {
            Name = payload.ConnectionName,
            MasterId = masterId,
            SlaveId = slaveId
        };

        var (msRes, msErr) = await AddMasterSlave(masterSlave);
        if (msErr != null)
        {
            return (null!, msErr);
        }

        // 2️⃣ master_slave_config
        var config = new MasterSlaveConfig
        {
            MasterSlaveId = msRes.Id,
            Multiplier = payload.Multiplier
        };

        var (_, cfgErr) = await AddMasterSlaveConfig(config);
        if (cfgErr != null)
        {
            return (null!, cfgErr);
        }

        // 3️⃣ master_slave_pair (optional)
        if (payload.SymbolPairs != null && payload.SymbolPairs.Any())
        {
            foreach (var pair in payload.SymbolPairs)
            {
                var pairEntity = new MasterSlavePair
                {
                    MasterSlaveId = msRes.Id,
                    MasterPair = pair.MasterSymbol,
                    SlavePair = pair.SlaveSymbol
                };

                var (_, pairErr) = await AddMasterSlavePair(pairEntity);
                if (pairErr != null)
                {
                    return (null!, pairErr);
                }
            }
        }

        return (msRes, null);
    }

}