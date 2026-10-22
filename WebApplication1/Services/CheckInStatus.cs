using WebApplication1.Models;

namespace WebApplication1.Services;

public static class CheckInStatus
{
    private static readonly RequiredDocument[] AllTypes =
    {
        RequiredDocument.IdOrPassport,
        RequiredDocument.ProofOfAddress,
        RequiredDocument.DriversLicense
    };

    // No check-in setup saved yet means ID or Passport only (the platform minimum)
    public static IReadOnlyList<RequiredDocument> RequiredList(CheckInProcess? process)
    {
        var required = (process?.RequiredDocuments ?? RequiredDocument.None) | RequiredDocument.IdOrPassport;
        return AllTypes.Where(t => required.HasFlag(t)).ToList();
    }

    // The newest upload of a given type (older rejected ones stay in the database as history)
    public static GuestDocument? Latest(Booking booking, RequiredDocument type) =>
        booking.Documents
            .Where(d => d.DocumentType == type)
            .OrderByDescending(d => d.UploadedAt)
            .ThenByDescending(d => d.Id)
            .FirstOrDefault();

    public static bool IsComplete(Booking booking, CheckInProcess? process) =>
        booking.RulesAcceptedAt != null &&
        RequiredList(process).All(t => Latest(booking, t)?.Status == DocumentStatus.Verified);

    public static string Name(RequiredDocument type) => type switch
    {
        RequiredDocument.IdOrPassport => "Government ID or Passport",
        RequiredDocument.ProofOfAddress => "Proof of address",
        RequiredDocument.DriversLicense => "Driver's licence",
        _ => type.ToString()
    };
}