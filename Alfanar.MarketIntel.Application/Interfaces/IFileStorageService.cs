using Alfanar.MarketIntel.Application.Common;

namespace Alfanar.MarketIntel.Application.Interfaces;

/// <summary>
/// Service for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save a file to storage
    /// </summary>
    Task<Result<string>> SaveFileAsync(Stream fileStream, string fileName, string? subfolder = null);

    /// <summary>
    /// Get a file from storage
    /// </summary>
    Task<Result<byte[]>> GetFileAsync(string filePath);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<Result> DeleteFileAsync(string filePath);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// Get file info (size, modified date, etc.)
    /// </summary>
    Task<Result<FileInfo>> GetFileInfoAsync(string filePath);

    /// <summary>
    /// Get file stream for reading
    /// </summary>
    Task<Result<Stream>> GetFileStreamAsync(string filePath);
}
