using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<MasterSlaveConfig, bool>> FilterMasterSlaveConfig(MasterSlaveConfig param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (param.MasterSlaveId == 0 || a.MasterSlaveId == param.MasterSlaveId) &&
                (param.Multiplier == 0 || a.Multiplier == param.Multiplier)
        );
    }
    public async Task<(MasterSlaveConfig?, ITError?)> GetMasterSlaveConfig(MasterSlaveConfig param)
    {
        try
        {
            var data = await _masterSlaveConfigRepository.Get(FilterMasterSlaveConfig(param));
            if (data == null)
                return (null, TError.NewNotFound("masterSlavePair not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

     public async Task<(List<MasterSlaveConfig>, ITError?)> GetMasterSlaveConfigs(MasterSlaveConfig param)
    {
        try
        {
            var data = await _masterSlaveConfigRepository.GetMany(FilterMasterSlaveConfig(param));
            if (data == null)
                return ([], TError.NewNotFound("master slave not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<MasterSlaveConfig>, long total, ITError?)> GetPaginatedMasterSlaveConfigs(MasterSlaveConfig param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _masterSlaveConfigRepository.GetPaginated(FilterMasterSlaveConfig(param), page, pageSize);
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(MasterSlaveConfig?, ITError?)> AddMasterSlaveConfig(MasterSlaveConfig masterSlavePair)
    {
        var existingMasterSlaveConfig = new MasterSlaveConfig
        {
            MasterSlaveId = masterSlavePair.MasterSlaveId,
            Multiplier = masterSlavePair.Multiplier,
        };

        var (_, terr) = await GetMasterSlaveConfig(existingMasterSlaveConfig);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("failed. masterSlave config already exist"));
        }

        try
        {
            var data = await _masterSlaveConfigRepository.Save(masterSlavePair);
            if (data == null)
                return (null, TError.NewServer("cannot create new masterSlavePair"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(MasterSlaveConfig?, ITError?)> UpdateMasterSlaveConfigById(long id, MasterSlaveConfig param)
    {   
        try
        {
            var (existing, terr) = await GetMasterSlaveConfig(new MasterSlaveConfig {Id = id});
            if(terr != null)
                return (null, terr);
            
            var data = await _masterSlaveConfigRepository.Save(param, a => a.Id == id);
            if(data == null)
                return (null, TError.NewServer("cannot save masterSlavePair"));

            return (data, null);
        }

        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }
}