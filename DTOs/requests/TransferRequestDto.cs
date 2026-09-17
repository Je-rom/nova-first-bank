using System.ComponentModel.DataAnnotations;

namespace NovaWallet.Dtos.Transfer
{
    public class TransferRequestDto
    {
        [Required]
        public Guid FromWalletId { get; set; }

        [Required]
        public Guid ToWalletId { get; set; }

        // Kobo, always. Never accept a decimal Naira amount here — that's
        // exactly the seam where float/rounding bugs creep into money paths.
        [Range(1, long.MaxValue, ErrorMessage = "AmountKobo must be a positive integer.")]
        public long AmountKobo { get; set; }
    }
}