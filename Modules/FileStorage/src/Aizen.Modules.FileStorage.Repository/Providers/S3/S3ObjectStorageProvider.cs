using Amazon;
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
    private readonly AmazonS3Client _presignClient;

    /// <summary>
    /// Scheme used when signing presigned URLs. <see cref="GetPreSignedUrlRequest.Protocol"/> defaults to
    /// <see cref="Protocol.HTTPS"/> and OVERRIDES the scheme of the client's ServiceURL — so a client configured
    /// with "http://localhost:9000" still emits "https://localhost:9000" unless this is set explicitly. MinIO
    /// listens on plain HTTP locally, so the browser then fails the PUT with a TLS/network error. Derive it from
    /// the endpoint we actually sign against.
    /// </summary>
    private readonly Protocol _presignProtocol;

    private readonly S3ObjectStorageOptions _options;
    private readonly ILogger<S3ObjectStorageProvider> _logger;

    static S3ObjectStorageProvider()
    {
        // AWSSDK.S3 still emits SigV2 presigned URLs (?AWSAccessKeyId=…&Signature=…) unless this global flag is
        // set — AuthenticationRegion alone does not switch it. MinIO rejects SigV2 presigned PUTs with 403.
        // With this on, GetPreSignedURL produces AWS4-HMAC-SHA256 URLs, which MinIO accepts.
        AWSConfigsS3.UseSignatureVersion4 = true;
    }

    public S3ObjectStorageProvider(IOptions<S3ObjectStorageOptions> options, ILogger<S3ObjectStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.Provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
        {
            // AuthenticationRegion is required for SigV4: the credential scope embeds a region, and without one
            // the SDK silently falls back to SigV2 presigned URLs (?AWSAccessKeyId=…&Signature=…), which MinIO
            // rejects with 403. With it, we get proper AWS4-HMAC-SHA256 URLs.
            var config = new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                UseHttp = _options.UseHttp,
                AuthenticationRegion = _options.Region
            };
            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);

            // Presign client: uses PublicServiceUrl (browser-reachable) if set, otherwise same as internal
            var publicUrl = _options.PublicServiceUrl ?? _options.ServiceUrl;
            var presignConfig = new AmazonS3Config
            {
                ServiceURL = publicUrl,
                ForcePathStyle = true,
                UseHttp = _options.UseHttp,
                AuthenticationRegion = _options.Region
            };
            _presignClient = new AmazonS3Client(_options.AccessKey, _options.SecretKey, presignConfig);

            _presignProtocol = publicUrl is not null
                && publicUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                    ? Protocol.HTTPS
                    : Protocol.HTTP;
        }
        else
        {
            var endpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, endpoint);
            _presignClient = _client; // AWS S3: same endpoint for both
            _presignProtocol = Protocol.HTTPS;
        }
    }

    public Task<string> GenerateUploadUrlAsync(string bucketName, string objectKey, string contentType, TimeSpan expiresIn, bool useInternalEndpoint = false, CancellationToken cancellationToken = default)
    {
        // useInternalEndpoint: sign with ServiceUrl (container-internal, e.g. http://minio:9000)
        // instead of PublicServiceUrl (browser-facing, e.g. http://localhost:9000).
        // Used for server-side uploads where the browser is never involved.
        var client = useInternalEndpoint ? _client : _presignClient;
        var protocol = useInternalEndpoint
            ? (_options.ServiceUrl?.StartsWith("https", StringComparison.OrdinalIgnoreCase) == true ? Protocol.HTTPS : Protocol.HTTP)
            : _presignProtocol;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiresIn),
            ContentType = contentType,
            Protocol = protocol
        };
        var url = client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public Task<string> GenerateReadUrlAsync(string bucketName, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Protocol = _presignProtocol
        };
        var url = _presignClient.GetPreSignedURL(request);
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

    public async Task<byte[]> ReadFirstBytesAsync(string bucketName, string objectKey, int byteCount, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ByteRange = new ByteRange(0, byteCount - 1)
        };
        using var response = await _client.GetObjectAsync(request, cancellationToken);
        using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    public async Task PutObjectAsync(string bucketName, string objectKey, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType
        };
        await _client.PutObjectAsync(request, cancellationToken);
    }
}
