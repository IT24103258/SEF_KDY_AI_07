using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FixFlow.Api.Interfaces;

namespace FixFlow.Api.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    public CloudinaryFileStorageService(IConfiguration configuration, ILogger<CloudinaryFileStorageService> logger)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = "fixflow-requests",
            PublicId = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}"
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var result = await _cloudinary.UploadAsync(uploadParams, cts.Token);
            if (result.Error != null)
            {
                _logger.LogWarning("Cloudinary upload failed: {Error}", result.Error.Message);
                throw new Exception($"Upload failed: {result.Error.Message}");
            }
            // Return secure_url — the caller stores this on RequestAttachment.
            return result.SecureUrl.ToString();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Cloudinary upload timed out for {FileName}", fileName);
            throw new Exception("Photo upload timed out. Please try again.");
        }
    }

    public Task<Stream> GetFileAsync(string fileKey)
    {
        // Not used for Cloudinary-hosted files — the secure_url is served directly.
        throw new NotSupportedException("Use the stored SecureUrl directly for Cloudinary-hosted files.");
    }
}