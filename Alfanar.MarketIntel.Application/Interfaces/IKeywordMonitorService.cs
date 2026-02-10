using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface IKeywordMonitorService
{
    Task<Result<KeywordMonitorDto>> CreateMonitorAsync(CreateKeywordMonitorDto dto);

    Task<Result<KeywordMonitorDto>> UpdateMonitorAsync(Guid id, CreateKeywordMonitorDto dto);

    Task<Result<bool>> DeleteMonitorAsync(Guid id);

    Task<Result<List<KeywordMonitorDto>>> GetAllMonitorsAsync();

    Task<Result<KeywordMonitorDto>> GetMonitorByIdAsync(Guid id);

    Task<Result<List<KeywordMonitorDto>>> GetActiveMonitorsAsync();

    Task<Result<KeywordMonitorDto>> ToggleMonitorAsync(Guid id, bool isActive);

    Task<Result<List<KeywordMonitorDto>>> GetMonitorsDueForCheckAsync(int intervalMinutes);
}
