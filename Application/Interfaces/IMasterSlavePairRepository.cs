using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IMasterSlavePairRepository : IRepository<MasterSlavePair>
{
    Task DeleteByMasterSlaveId(long masterSlaveId);
}