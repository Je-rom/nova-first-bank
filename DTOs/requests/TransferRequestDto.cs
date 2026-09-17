using System.ComponentModel.DataAnnotations;

namespace NovaWallet.Dtos.Transfer
{
    public class TransferRequestDto
    {
        [Required]
        public Guid FromWalletId { get; set; }

        [Required]
        public Guid ToWalletId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "AmountKobo must be a positive integer.")]
        public long AmountKobo { get; set; }
    }
}