using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Local file system storage service
/// Can be replaced with Azure Blob Storage implementation later
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly HashSet<string> _allowedExtensions;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        
        // Get storage path from configuration
        _basePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage", "reports");
        _maxFileSizeBytes = long.Parse(configuration["FileStorage:MaxFileSizeBytes"] ?? $"{500 * 1024 * 1024}"); // 500MB default

        var allowed = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                      ?? new[] { ".pdf" };
        _allowedExtensions = allowed
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : $".{x.ToLowerInvariant()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Ensure base directory exists
        Directory.CreateDirectory(_basePath);
        
        _logger.LogInformation("File storage initialized at: {Path}", _basePath);
    }

    public async Task<Result<string>> SaveFileAsync(Stream fileStream, string fileName, string? subfolder = null)
    {
        try
        {
            var streamToSave = await ToSeekableStreamAsync(fileStream);

            // Validate file size
            if (streamToSave.Length > _maxFileSizeBytes)
            {
                return Result<string>.Failure($"File size exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB");
            }

            // Sanitize file name
            var sanitizedFileName = SanitizeFileName(fileName);
            var extension = Path.GetExtension(sanitizedFileName);
            if (_allowedExtensions.Any() && !_allowedExtensions.Contains(extension))
            {
                return Result<string>.Failure($"File extension '{extension}' is not allowed");
            }
            
            // Build full path
            var targetDirectory = string.IsNullOrWhiteSpace(subfolder)
                ? _basePath
                : Path.Combine(_basePath, subfolder);

            Directory.CreateDirectory(targetDirectory);

            var filePath = Path.Combine(targetDirectory, sanitizedFileName);

            // If file exists, append timestamp to make it unique
            if (File.Exists(filePath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedFileName);
                var fileExtension = Path.GetExtension(sanitizedFileName);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                sanitizedFileName = $"{nameWithoutExt}_{timestamp}{fileExtension}";
                filePath = Path.Combine(targetDirectory, sanitizedFileName);
            }

            // Save file
            streamToSave.Position = 0;

            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await streamToSave.CopyToAsync(fileStreamOutput);
            }

            _logger.LogInformation("File saved: {Path} ({Size} bytes)", filePath, streamToSave.Length);

            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", fileName);
            return Result<string>.Failure($"Failed to save file: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> GetFileAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Attempting to retrieve file: {Path}", filePath);

            // Validate path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogError("File path is null or empty");
                return Result<byte[]>.Failure("File path is null or empty");
            }

            // Check if file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found at path: {Path}", filePath);
                
                // Try to resolve as relative path from base path
                var potentialPath = Path.Combine(_basePath, filePath);
                if (File.Exists(potentialPath))
                {
                    _logger.LogInformation("File found at resolved path: {ResolvedPath}", potentialPath);
                    filePath = potentialPath;
                }
                else
                {
                    _logger.LogError("File not found at absolute path or relative to base path. Absolute: {AbsolutePath}, Relative attempt: {RelativePath}", filePath, potentialPath);
                    return Result<byte[]>.Failure($"File not found: {filePath}");
                }
            }

            // Verify file is within base path (security check)
            var fullPath = Path.GetFullPath(filePath);
            var basePath = Path.GetFullPath(_basePath);
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("File path is outside base directory. Full path: {FullPath}, Base: {BasePath}", fullPath, basePath);
                return Result<byte[]>.Failure("Access denied: file path is outside allowed directory");
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            
            _logger.LogInformation("File retrieved successfully: {Path} ({Size} bytes)", filePath, fileBytes.Length);

            return Result<byte[]>.Success(fileBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file: {Path}", filePath);
            return Result<byte[]>.Failure($"Failed to retrieve file: {ex.Message}");
        }
    }

    public async Task<Result> DeleteFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result.Failure("File not found");
            }

            await Task.Run(() => File.Delete(filePath));

            _logger.LogInformation("File deleted: {Path}", filePath);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", filePath);
            return Result.Failure($"Failed to delete file: {ex.Message}");
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.FromResult(File.Exists(filePath));
    }

    public async Task<Result<FileInfo>> GetFileInfoAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result<FileInfo>.Failure("File not found");
            }

            var fileInfo = await Task.FromResult(new FileInfo(filePath));

            return Result<FileInfo>.Success(fileInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info: {Path}", filePath);
            return Result<FileInfo>.Failure($"Failed to get file info: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> GetFileStreamAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result<Stream>.Failure("File not found");
            }

            var stream = await Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));

            _logger.LogDebug("File stream opened: {Path}", filePath);

            return Result<Stream>.Success(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening file stream: {Path}", filePath);
            return Result<Stream>.Failure($"Failed to open file stream: {ex.Message}");
        }
    }

    // Private helper methods

    private async Task<Stream> ToSeekableStreamAsync(Stream source)
    {
        if (source.CanSeek)
        {
            return source;
        }

        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        buffer.Position = 0;
        return buffer;
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt.Substring(0, 200 - extension.Length) + extension;
        }

        return sanitized;
    }
}
