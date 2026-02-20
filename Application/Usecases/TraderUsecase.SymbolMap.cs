using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    // =============================================
    // SYMBOL MAP CRUD
    // =============================================

    public async Task<(List<SymbolMap>, ITError?)> GetSymbolMaps()
    {
        try
        {
            var items = await _symbolMapRepository.GetMany(x => x.DeletedAt == null);
            return (items.ToList(), null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer(ex.Message));
        }
    }

    public async Task<(List<string>, ITError?)> GetCanonicalSymbols()
    {
        try
        {
            var items = await _symbolMapRepository.GetMany(x => x.DeletedAt == null);
            var canonical = items
                .Select(x => x.CanonicalSymbol.ToUpper())
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            return (canonical, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer(ex.Message));
        }
    }

    public async Task<(SymbolMap?, ITError?)> CreateSymbolMap(SymbolMapPayload payload)
    {
        try
        {
            var entity = new SymbolMap
            {
                BrokerName = payload.BrokerName,
                ServerName = payload.ServerName,
                BrokerSymbol = payload.BrokerSymbol,
                CanonicalSymbol = payload.CanonicalSymbol,
            };
            var result = await _symbolMapRepository.Add(entity);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<(SymbolMap?, ITError?)> UpdateSymbolMap(long id, SymbolMapPayload payload)
    {
        try
        {
            var existing = await _symbolMapRepository.Get(x => x.Id == id);
            if (existing == null)
                return (null, TError.NewNotFound("SymbolMap not found"));

            existing.BrokerName = payload.BrokerName;
            existing.ServerName = payload.ServerName;
            existing.BrokerSymbol = payload.BrokerSymbol;
            existing.CanonicalSymbol = payload.CanonicalSymbol;

            await _symbolMapRepository.Update(existing);
            return (existing, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<ITError?> DeleteSymbolMap(long id)
    {
        try
        {
            var existing = await _symbolMapRepository.Get(x => x.Id == id);
            if (existing == null)
                return TError.NewNotFound("SymbolMap not found");

            existing.DeletedAt = DateTime.UtcNow;
            await _symbolMapRepository.Update(existing);
            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    private string CleanSymbol(string sym)
    {
        if (string.IsNullOrEmpty(sym)) return sym;
        string s = sym.ToUpper();
        // Strip common suffixes
        string[] suffixes = { ".CASH", ".PRO", ".M", ".ECN", ".I", ".SB", ".PLUS", ".MINI", ".MICRO", "++", "+" };
        foreach (var suffix in suffixes)
        {
            if (s.EndsWith(suffix))
            {
                s = s.Substring(0, s.Length - suffix.Length);
                break;
            }
        }
        // Also strip any trailing non-alphanumeric (like . or _ or +)
        return System.Text.RegularExpressions.Regex.Replace(s, @"[^A-Z0-9]+$", "");
    }
}
