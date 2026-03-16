using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IJobPublisher
{
    Task PublishCreateJob(TradePlatformCreateJob job);
    Task PublishDeleteJob(TradePlatformCreateJob job);
    Task PublishRestartJob(TradePlatformCreateJob job);
    Task PublishMt5Packet(
        string server,
        long account,
        string type,
        object payload
    );
    Task PublishMt5PacketBatch(
        string server,
        long account,
        IEnumerable<object> payloads
    );
    Task PublishCtraderPacket(
        long ctraderId,
        string type,
        object payload
    );
    Task PublishCtraderPacketBatch(
        long ctraderId,
        IEnumerable<object> payloads
    );
    Task PublishCtraderManageAccount(Account account);
}