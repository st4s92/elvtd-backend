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
    private readonly AppLogger<TraderUsecase> _logger;
    private readonly WebSocketServer _wsServer;
    public TraderUsecase(
        ITradingRepository tradingRepository,
        IAccountRepository accountRepository,
        IOrderRepository orderRepository,
        IMasterSlaveRepository masterSlaveRepository,
        IMasterSlavePairRepository masterSlavePairRepository,
        IMasterSlaveConfigRepository masterSlaveConfigRepository,
        AppLogger<TraderUsecase> logger,
        WebSocketServer wsServer
    )
    {
        _tradingRepository = tradingRepository;
        _accountRepository = accountRepository;
        _orderRepository = orderRepository;
        _masterSlaveRepository = masterSlaveRepository;
        _masterSlavePairRepository = masterSlavePairRepository;
        _masterSlaveConfigRepository = masterSlaveConfigRepository;
        _logger = logger;
        _wsServer = wsServer;
    }
}