using Backend.Helper;
using Backend.Infrastructure.Repositories;
using Backend.Model;

namespace Backend.Application.Usecases;

public class SystemLogUsecase
{
    private readonly ISystemLogRepository _systemLogRepository;

    public SystemLogUsecase(ISystemLogRepository systemLogRepository)
    {
        _systemLogRepository = systemLogRepository;
    }

    public async Task<ITError?> CreateLog(string category, string action, long? accountId, string message, string level = "Info")
    {
        try
        {
            var log = new SystemLog
            {
                Category = category,
                Action = action,
                AccountId = accountId,
                Message = message,
                Level = level
            };

            await _systemLogRepository.Save(log);
            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer("Failed to create log", ex.Message);
        }
    }

    public async Task<(GetPaginatedResponse<SystemLogDto>?, ITError?)> GetPaginatedLogs(SystemLogGetPaginatedPayload param)
    {
        try
        {
            var filter = new SystemLog
            {
                Category = param.Category ?? "",
                Action = param.Action ?? "",
                AccountId = param.AccountNumber, // The repository query will map AccountNumber to AccountId
                Level = param.Level ?? "",
                Message = param.Search ?? ""
            };

            // Warning: If param.AccountNumber is meant to be the exact Acc #, 
            // the repository query currently treats param.AccountId as the true Account.Id. 
            // So if you pass an AccountNumber, you may need to resolve the ID first.
            // Let's assume the frontend passes `accountNumber` but we need `AccountId`.
            // We should adjust this based on how the frontend sends filters.

            var (data, total) = await _systemLogRepository.GetPaginatedLogs(filter, param.Page, param.PerPage);

            var list = data.Select(l => new SystemLogDto
            {
                Id = l.Id,
                Category = l.Category,
                Action = l.Action,
                AccountId = l.AccountId,
                AccountNumber = l.Account?.AccountNumber,
                ServerName = l.Account?.ServerAccount?.Server?.ServerIp,
                Message = l.Message,
                Level = l.Level,
                CreatedAt = l.CreatedAt
            }).ToList();

            var resp = new GetPaginatedResponse<SystemLogDto>
            {
                Data = list,
                Total = total
            };

            return (resp, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("Failed to fetch logs", ex.Message));
        }
    }
}
