namespace Aizen.Modules.Payment.Abstraction.Enum
{
    public enum WalletTransactionType
    {
        Deposit = 1,   // Sisteme kazanç olarak para girer
        Withdraw = 2,  // Banka aktarımı ya da sistem dışı çıkış
        Refund = 3     // Geri ödeme
    }

}