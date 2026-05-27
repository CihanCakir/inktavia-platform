namespace Aizen.Modules.Identity.Abstraction.Enum
{
    public enum UserValidationType
    {
        Login = 1,
        Register = 2,
        PasswordReset = 3,
        EmailConfirmation = 4,
        PhoneNumberUpdate = 5
    }
    public enum UserValidationMethodType
    {
        Sms = 1,
        Email = 2,
        MobileAppOtp = 3
    }
}
