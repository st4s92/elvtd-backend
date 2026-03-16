using Backend.Application.Interfaces;
using Backend.Application.Usecases;
using Backend.Helper;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    private readonly TraderUsecase _usecase;
    private readonly AppLogger<TraderHandler> _logger;
    private readonly ITradingRepository _tradingRepository;

    public TraderHandler(TraderUsecase usecase, AppLogger<TraderHandler> logger, ITradingRepository tradingRepository)
    {
        _usecase = usecase;
        _logger = logger;
        _tradingRepository = tradingRepository;
    }
}
