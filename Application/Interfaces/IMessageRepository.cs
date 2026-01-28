using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IJobPublisher
{
    Task PublishCreateJob(TradePlatformCreateJob job);
    Task PublishDeleteJob(TradePlatformCreateJob job);
}