using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;

namespace Alfanar.MarketIntel.Application.Interfaces;

public interface ITenderMonitoringService
{
    Task<Result<TenderIngestResponseDto>> IngestAsync(TenderIngestRequestDto request);
    Task<Result<List<TenderNoticeDto>>> GetSaudiNoticesAsync(int pageNumber = 1, int pageSize = 50);
    Task<Result<List<TenderNoticeDto>>> GetMiddleEastNoticesAsync(int pageNumber = 1, int pageSize = 50);
}
