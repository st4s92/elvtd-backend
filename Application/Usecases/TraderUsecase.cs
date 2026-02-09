using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private readonly ITradingRepository _tradingRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IMasterSlaveRepository _masterSlaveRepository;
    private readonly IMasterSlavePairRepository _masterSlavePairRepository;
    private readonly IMasterSlaveConfigRepository _masterSlaveConfigRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IServerAccountRepository _serverAccountRepository;
    private readonly AppLogger<TraderUsecase> _logger;
    private readonly IActiveOrderRepository _activeOrderRepository;
    private readonly WebSocketServer _wsServer;
    private readonly IJobPublisher _jobPublisher;
    public TraderUsecase(
        ITradingRepository tradingRepository,
        IAccountRepository accountRepository,
        IOrderRepository orderRepository,
        IMasterSlaveRepository masterSlaveRepository,
        IMasterSlavePairRepository masterSlavePairRepository,
        IMasterSlaveConfigRepository masterSlaveConfigRepository,
        IServerRepository serverRepository,
        IServerAccountRepository serverAccountRepository,
        IActiveOrderRepository activeOrderRepository,
        AppLogger<TraderUsecase> logger,
        WebSocketServer wsServer,
        IJobPublisher jobPublisher
    )
    {
        _tradingRepository = tradingRepository;
        _accountRepository = accountRepository;
        _orderRepository = orderRepository;
        _serverRepository = serverRepository;
        _serverAccountRepository = serverAccountRepository;
        _masterSlaveRepository = masterSlaveRepository;
        _masterSlavePairRepository = masterSlavePairRepository;
        _masterSlaveConfigRepository = masterSlaveConfigRepository;
        _activeOrderRepository = activeOrderRepository;
        _logger = logger;
        _wsServer = wsServer;
        _jobPublisher = jobPublisher;
    }
}