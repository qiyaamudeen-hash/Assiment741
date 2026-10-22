namespace WebApplication1.Services;

public record PriceBreakdown(
    int Nights,
    decimal NightlyRate,
    decimal Subtotal,
    decimal CleaningFee,
    decimal ServiceFee,
    decimal Total);

public static class BookingPricing
{
    // 10% of the nightly subtotal. Change this one number to change the service fee everywhere.
    public const decimal ServiceFeeRate = 0.10m;

    public static PriceBreakdown Calculate(decimal pricePerNight, decimal cleaningFee, int nights)
    {
        var subtotal = pricePerNight * nights;
        var serviceFee = Math.Round(subtotal * ServiceFeeRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + cleaningFee + serviceFee;   // (PricePerNight x Nights) + CleaningFee + ServiceFee

        return new PriceBreakdown(nights, pricePerNight, subtotal, cleaningFee, serviceFee, total);
    }
}