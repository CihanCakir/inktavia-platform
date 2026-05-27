namespace Aizen.Modules.Payment.Abstraction.Enum
{
    public enum TransferStatusEnum { Pending, Success, Failed }
    public static class TransferStatusEnumExtension
    {
        public static string ToDisplayString(this TransferStatusEnum status)
        {
            return status switch
            {
                TransferStatusEnum.Pending => "Beklemede",
                TransferStatusEnum.Success => "Başarılı",
                TransferStatusEnum.Failed => "Başarısız",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}