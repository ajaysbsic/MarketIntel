using Alfanar.MarketIntel.Application.Common;
using Alfanar.MarketIntel.Application.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alfanar.MarketIntel.Application.Services;

/// <summary>
/// Azure Blob Storage implementation of the file storage service.
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly HashSet<string> _allowedExtensions;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"] ?? "pdf-reports";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
        }

        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.None);

        _maxFileSizeBytes = long.TryParse(configuration["FileStorage:MaxFileSizeBytes"], out var max)
            ? max
            : 500 * 1024 * 1024;

        var allowed = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                      ?? new[] { ".pdf" };

        _allowedExtensions = allowed
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : $".{x.ToLowerInvariant()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Result<string>> SaveFileAsync(Stream fileStream, string fileName, string? subfolder = null)
    {
        try
        {
            await EnsureContainerExistsAsync();

            var safeFileName = SanitizeFileName(fileName);
            var extension = Path.GetExtension(safeFileName);

            if (_allowedExtensions.Any() && !_allowedExtensions.Contains(extension))
            {
                return Result<string>.Failure($"File extension '{extension}' is not allowed");
            }

            var seekableStream = await ToSeekableStreamAsync(fileStream);
            if (seekableStream.Length > _maxFileSizeBytes)
            {
                return Result<string>.Failure($"File size exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB");
            }

            seekableStream.Position = 0;

            var blobName = BuildBlobName(safeFileName, subfolder);
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                blobName = BuildBlobName(AppendTimestamp(safeFileName), subfolder);
                blobClient = _containerClient.GetBlobClient(blobName);
            }

            await blobClient.UploadAsync(seekableStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(safeFileName)
                }
            });

            _logger.LogInformation("Blob saved: {BlobName} ({Size} bytes)", blobName, seekableStream.Length);

            return Result<string>.Success(blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving blob {FileName}", fileName);
            return Result<string>.Failure($"Failed to save file: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> GetFileAsync(string filePath)
    {
        try
        {
            await EnsureContainerExistsAsync();

            var blobClient = _containerClient.GetBlobClient(NormalizeBlobPath(filePath));
            if (!await blobClient.ExistsAsync())
            {
                return Result<byte[]>.Failure("File not found");
            }

            var downloadResult = await blobClient.DownloadContentAsync();
            var data = downloadResult.Value.Content.ToArray();

            _logger.LogInformation("Blob retrieved: {BlobName} ({Size} bytes)", blobClient.Name, data.Length);

            return Result<byte[]>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blob {Path}", filePath);
            return Result<byte[]>.Failure($"Failed to retrieve file: {ex.Message}");
        }
    }

    public async Task<Result> DeleteFileAsync(string filePath)
    {
        try
        {
            await EnsureContainerExistsAsync();

            var blobClient = _containerClient.GetBlobClient(NormalizeBlobPath(filePath));
            var response = await blobClient.DeleteIfExistsAsync();

            if (!response.Value)
            {
                return Result.Failure("File not found");
            }

            _logger.LogInformation("Blob deleted: {BlobName}", blobClient.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob {Path}", filePath);
            return Result.Failure($"Failed to delete file: {ex.Message}");
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        await EnsureContainerExistsAsync();
        var blobClient = _containerClient.GetBlobClient(NormalizeBlobPath(filePath));
        return await blobClient.ExistsAsync();
    }

    public async Task<Result<FileInfo>> GetFileInfoAsync(string filePath)
    {
        try
        {
            await EnsureContainerExistsAsync();
            var blobClient = _containerClient.GetBlobClient(NormalizeBlobPath(filePath));

            if (!await blobClient.ExistsAsync())
            {
                return Result<FileInfo>.Failure("File not found");
            }

            return Result<FileInfo>.Failure("Blob metadata retrieval is not supported as FileInfo. Use GetFileAsync or GetFileStreamAsync instead.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blob info for {Path}", filePath);
            return Result<FileInfo>.Failure($"Failed to get file info: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> GetFileStreamAsync(string filePath)
    {
        try
        {
            await EnsureContainerExistsAsync();
            var blobClient = _containerClient.GetBlobClient(NormalizeBlobPath(filePath));

            if (!await blobClient.ExistsAsync())
            {
                return Result<Stream>.Failure("File not found");
            }

            var download = await blobClient.DownloadStreamingAsync();
            return Result<Stream>.Success(download.Value.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening blob stream for {Path}", filePath);
            return Result<Stream>.Failure($"Failed to open file stream: {ex.Message}");
        }
    }

    private async Task EnsureContainerExistsAsync()
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
    }

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

    private string BuildBlobName(string fileName, string? subfolder)
    {
        if (string.IsNullOrWhiteSpace(subfolder))
        {
            return NormalizeBlobPath(fileName);
        }

        var segments = subfolder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(SanitizePathSegment)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();

        segments.Add(fileName);
        return NormalizeBlobPath(string.Join('/', segments));
    }

    private string NormalizeBlobPath(string path)
    {
        return path.Replace("\\", "/").TrimStart('/');
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"report_{Guid.NewGuid():N}.pdf";
        }

        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt[..Math.Min(nameWithoutExt.Length, 200 - extension.Length)] + extension;
        }

        return sanitized;
    }

    private string SanitizePathSegment(string segment)
    {
        var invalidChars = Path.GetInvalidPathChars();
        var sanitized = string.Join("_", segment.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        sanitized = sanitized.Replace("/", "_").Replace("\\", "_");
        return sanitized;
    }

    private string AppendTimestamp(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return $"{nameWithoutExt}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
}
