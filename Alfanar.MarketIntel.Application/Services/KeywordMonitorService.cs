using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Application.Interfaces;
using Alfanar.MarketIntel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Service for managing keyword monitors - CRUD operations and active monitor retrieval
/// </summary>
public class KeywordMonitorService : IKeywordMonitorService
{
    private readonly IKeywordMonitorRepository _repository;
    private readonly ILogger<KeywordMonitorService> _logger;
    private readonly MarketIntelDbContext _context;

    public KeywordMonitorService(
        IKeywordMonitorRepository repository,
        ILogger<KeywordMonitorService> logger,
        MarketIntelDbContext context)
    {
        _repository = repository;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<KeywordMonitorDto>> CreateMonitorAsync(CreateKeywordMonitorDto dto)
    {
        try
        {
            // Validate keyword
            if (string.IsNullOrWhiteSpace(dto.Keyword))
                return Result<KeywordMonitorDto>.Failure("Keyword cannot be empty");

            // Check for duplicates (case-insensitive)
            var normalizedKeyword = dto.Keyword.Trim().ToLower();
            var existing = await _repository.GetByKeywordAsync(normalizedKeyword);
            if (existing != null)
                return Result<KeywordMonitorDto>.Failure($"Monitor already exists for keyword '{dto.Keyword}'");

            // Validate interval
            if (dto.CheckIntervalMinutes < 5 || dto.CheckIntervalMinutes > 10080) // 5 min to 7 days
                return Result<KeywordMonitorDto>.Failure("Check interval must be between 5 and 10080 minutes");

            var entity = new Domain.Entities.KeywordMonitor
            {
                Keyword = dto.Keyword.Trim(),
                CheckIntervalMinutes = dto.CheckIntervalMinutes,
                MaxResultsPerCheck = Math.Min(dto.MaxResultsPerCheck, 100), // Cap at 100
                Tags = dto.Tags.Any() ? System.Text.Json.JsonSerializer.Serialize(dto.Tags) : null,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Keyword monitor created for: {Keyword}", entity.Keyword);

            return Result<KeywordMonitorDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating keyword monitor: {Keyword}", dto.Keyword);
            return Result<KeywordMonitorDto>.Failure($"Error creating monitor: {ex.Message}");
        }
    }

    public async Task<Result<KeywordMonitorDto>> UpdateMonitorAsync(Guid id, CreateKeywordMonitorDto dto)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<KeywordMonitorDto>.Failure("Monitor not found");

            // Don't allow changing keyword to an existing one (case-insensitive check)
            if (entity.Keyword.ToLower() != dto.Keyword.Trim().ToLower())
            {
                var existing = await _repository.GetByKeywordAsync(dto.Keyword.Trim().ToLower());
                if (existing != null)
                    return Result<KeywordMonitorDto>.Failure($"Monitor already exists for keyword '{dto.Keyword}'");
            }

            entity.Keyword = dto.Keyword.Trim();
            entity.CheckIntervalMinutes = dto.CheckIntervalMinutes;
            entity.MaxResultsPerCheck = Math.Min(dto.MaxResultsPerCheck, 100);
            entity.Tags = dto.Tags.Any() ? System.Text.Json.JsonSerializer.Serialize(dto.Tags) : null;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Keyword monitor updated: {Keyword}", entity.Keyword);

            return Result<KeywordMonitorDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating keyword monitor: {Id}", id);
            return Result<KeywordMonitorDto>.Failure($"Error updating monitor: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteMonitorAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<bool>.Failure("Monitor not found");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Keyword monitor deleted: {Keyword}", entity.Keyword);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting keyword monitor: {Id}", id);
            return Result<bool>.Failure($"Error deleting monitor: {ex.Message}");
        }
    }

    public async Task<Result<List<KeywordMonitorDto>>> GetAllMonitorsAsync()
    {
        try
        {
            var entities = await _repository.GetAllAsync();
            var dtos = entities.Select(MapToDto).ToList();
            return Result<List<KeywordMonitorDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all monitors");
            return Result<List<KeywordMonitorDto>>.Failure($"Error retrieving monitors: {ex.Message}");
        }
    }

    public async Task<Result<KeywordMonitorDto>> GetMonitorByIdAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<KeywordMonitorDto>.Failure("Monitor not found");

            return Result<KeywordMonitorDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitor: {Id}", id);
            return Result<KeywordMonitorDto>.Failure($"Error retrieving monitor: {ex.Message}");
        }
    }

    public async Task<Result<List<KeywordMonitorDto>>> GetActiveMonitorsAsync()
    {
        try
        {
            var entities = await _repository.GetActiveMonitorsAsync();
            var dtos = entities.Select(MapToDto).ToList();
            return Result<List<KeywordMonitorDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active monitors");
            return Result<List<KeywordMonitorDto>>.Failure($"Error retrieving monitors: {ex.Message}");
        }
    }

    public async Task<Result<KeywordMonitorDto>> ToggleMonitorAsync(Guid id, bool isActive)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<KeywordMonitorDto>.Failure("Monitor not found");

            entity.IsActive = isActive;
            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Monitor toggled to {Active}: {Keyword}", isActive, entity.Keyword);

            return Result<KeywordMonitorDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling monitor: {Id}", id);
            return Result<KeywordMonitorDto>.Failure($"Error toggling monitor: {ex.Message}");
        }
    }

    public async Task<Result<List<KeywordMonitorDto>>> GetMonitorsDueForCheckAsync(int intervalMinutes)
    {
        try
        {
            var entities = await _repository.GetMonitorsDueForCheckAsync(intervalMinutes);
            var dtos = entities.Select(MapToDto).ToList();
            return Result<List<KeywordMonitorDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitors due for check");
            return Result<List<KeywordMonitorDto>>.Failure($"Error retrieving monitors: {ex.Message}");
        }
    }

    private KeywordMonitorDto MapToDto(Domain.Entities.KeywordMonitor entity)
    {
        var tags = new List<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            try
            {
                tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.Tags) ?? new();
            }
            catch
            {
                // Log and ignore JSON parsing errors
            }
        }

        return new KeywordMonitorDto
        {
            Id = entity.Id,
            Keyword = entity.Keyword,
            IsActive = entity.IsActive,
            CheckIntervalMinutes = entity.CheckIntervalMinutes,
            LastCheckedUtc = entity.LastCheckedUtc,
            Tags = tags,
            MaxResultsPerCheck = entity.MaxResultsPerCheck
        };
    }
}
