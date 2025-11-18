using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<MasterSlavePair, bool>> FilterMasterSlavePair(MasterSlavePair param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (param.MasterSlaveId == 0 || a.MasterSlaveId == param.MasterSlaveId) &&
                (string.IsNullOrEmpty(param.MasterPair) || a.MasterPair == param.MasterPair) &&
                (string.IsNullOrEmpty(param.SlavePair) || a.SlavePair == param.SlavePair) 
        );
    }
    public async Task<(MasterSlavePair?, ITError?)> GetMasterSlavePair(MasterSlavePair param)
    {
        try
        {
            var data = await _masterSlavePairRepository.Get(FilterMasterSlavePair(param));
            if (data == null)
                return (null, TError.NewNotFound("masterSlavePair not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

     public async Task<(List<MasterSlavePair>, ITError?)> GetMasterSlavePairs(MasterSlavePair param)
    {
        try
        {
            var data = await _masterSlavePairRepository.GetMany(FilterMasterSlavePair(param));
            if (data == null)
                return ([], TError.NewNotFound("master slave not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<MasterSlavePair>, long total, ITError?)> GetPaginatedMasterSlavePairs(MasterSlavePair param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _masterSlavePairRepository.GetPaginated(FilterMasterSlavePair(param), page, pageSize);
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(MasterSlavePair?, ITError?)> AddMasterSlavePair(MasterSlavePair masterSlavePair)
    {
        var existingMasterSlavePair = new MasterSlavePair
        {
            MasterSlaveId = masterSlavePair.Id,
            SlavePair = masterSlavePair.SlavePair,
            MasterPair = masterSlavePair.MasterPair
        };

        var (_, terr) = await GetMasterSlavePair(existingMasterSlavePair);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("failed. masterSlavePair already exist"));
        }

        try
        {
            var data = await _masterSlavePairRepository.Save(masterSlavePair);
            if (data == null)
                return (null, TError.NewServer("cannot create new masterSlavePair"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(MasterSlavePair?, ITError?)> UpdateMasterSlavePairById(long id, MasterSlavePair param)
    {   
        try
        {
            var (existing, terr) = await GetMasterSlavePair(new MasterSlavePair {Id = id});
            if(terr != null)
                return (null, terr);
            
            var data = await _masterSlavePairRepository.Save(param, a => a.Id == id);
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