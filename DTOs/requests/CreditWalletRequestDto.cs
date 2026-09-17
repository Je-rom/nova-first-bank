using System.ComponentModel.DataAnnotations;

namespace NovaWallet.Dtos.Wallet
{
    public class CreditWalletRequestDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "AmountKobo must be a positive integer.")]
        public long AmountKobo { get; set; }
    }
}