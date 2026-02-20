using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Infrastructure.Repositories;

public class SymbolMapRepository : BaseRepository<SymbolMap>, ISymbolMapRepository
{
    public SymbolMapRepository(AppDbContext context) : base(context) { }
}
