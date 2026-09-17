using System.ComponentModel.DataAnnotations;

namespace NovaWallet.Dtos.Wallet
{
    public class CreateWalletRequestDto
    {
        [Required]
        public string CustomerId { get; set; } = default!;

        public string Currency { get; set; } = "NGN";
    }
}