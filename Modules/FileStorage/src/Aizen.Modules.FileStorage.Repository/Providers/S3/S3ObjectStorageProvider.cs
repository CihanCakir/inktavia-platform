using Amazon.S3;
using Amazon.S3.Model;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.FileStorage.Repository.Providers.S3;

[DocumentationInfo("S3 object storage provider", "AWS S3 implementation of IObjectStorageProvider.")]
public sealed class S3ObjectStorageProvider : IObjectStorageProvider
{
    private readonly AmazonS3Client _client;
    private readonly S3ObjectStorageOptions _options;
    private readonly ILogger<S3ObjectStorageProvider> _logger;

    public S3ObjectStorageProvider(IOptions<S3ObjectStorageOptions> options, ILogger<S3ObjectStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.Provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
        {
            var config = new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                UseHttp = _options.UseHttp
            };
            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }
        else
        {
            _client = new AmazonS3Client(
                _options.AccessKey,
                _options.SecretKey,
                Amazon.RegionEndpoint.GetBySystemName(_options.Region));
        }
    }

    public Task<string> GenerateUploadUrlAsync(string bucketName, string objectKey, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiresIn),
            ContentType = contentType
        };
        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public Task<string> GenerateReadUrlAsync(string bucketName, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        };
        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task<bool> ObjectExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(bucketName, objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<ObjectMetadataResult> GetObjectMetadataAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectMetadataAsync(bucketName, objectKey, cancellationToken);
        return new ObjectMetadataResult
        {
            ContentLength = response.ContentLength,
            ContentType = response.Headers.ContentType,
            ETag = response.ETag,
            LastModified = response.LastModified
        };
    }

    public Task DeleteObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest { BucketName = bucketName, Key = objectKey };
        return _client.DeleteObjectAsync(request, cancellationToken);
    }
}
