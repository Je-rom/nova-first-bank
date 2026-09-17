namespace NovaWallet.Models;

public enum TransactionType
{
    Credit = 0,          // inbound funds, e.g. simulated NIP transfer
    DebitTransfer = 1,   // outbound leg of a wallet-to-wallet transfer
    CreditTransfer = 2   // inbound leg of a wallet-to-wallet transfer
}