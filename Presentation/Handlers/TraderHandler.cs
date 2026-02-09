using Backend.Application.Usecases;
using Backend.Helper;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    private readonly TraderUsecase _usecase;
    private readonly AppLogger<TraderHandler> _logger;

    public TraderHandler(TraderUsecase usecase, AppLogger<TraderHandler> logger)
    {
        _usecase = usecase;
        _logger = logger;
    }
}
