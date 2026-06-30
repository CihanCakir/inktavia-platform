using Microsoft.Extensions.Options;

namespace Aizen.Modules.FileStorage.Repository.Providers.S3;

[DocumentationInfo("S3 object storage options validator", "Validates S3ObjectStorageOptions at startup to catch misconfiguration early.")]
public sealed class S3ObjectStorageOptionsValidator : IValidateOptions<S3ObjectStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, S3ObjectStorageOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BucketName))
            errors.Add("S3ObjectStorage:BucketName must not be empty.");

        if (string.IsNullOrWhiteSpace(options.AccessKey))
            errors.Add("S3ObjectStorage:AccessKey must not be empty.");

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            errors.Add("S3ObjectStorage:SecretKey must not be empty.");

        if (options.Provider.Equals("AWS", StringComparison.OrdinalIgnoreCase))
        {
            if (options.AccessKey?.Equals("minioadmin", StringComparison.OrdinalIgnoreCase) == true)
                errors.Add("S3ObjectStorage:AccessKey cannot be 'minioadmin' when Provider is AWS (production credentials required).");

            if (options.UseHttp)
                errors.Add("S3ObjectStorage:UseHttp must be false when Provider is AWS.");
        }

        if (options.Provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(options.ServiceUrl))
            errors.Add("S3ObjectStorage:ServiceUrl must be set when Provider is MinIO.");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
