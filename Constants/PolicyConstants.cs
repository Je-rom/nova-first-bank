namespace NovaWallet.Constants
{
    public static class PolicyConstants
    {
        // ₦500,000/day per wallet, expressed in kobo (₦1 = 100 kobo).
        public const long DailyOutboundLimitKobo = 500_000_00L;

        // IANA timezone id for WAT (West Africa Time, UTC+1, no DST).
        // Use this id, not "West Central Africa Standard Time" (Windows id) —
        // the API runs in a Linux container.
        public const string WestAfricaTimeZoneId = "Africa/Lagos";
    }
}