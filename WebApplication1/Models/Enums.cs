namespace WebApplication1.Models;

public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

public enum PropertyType
{
    House,
    Apartment,
    Villa,
    Cabin,
    Cottage,
    Room
}
[Flags]
public enum RequiredDocument
{
    None = 0,
    IdOrPassport = 1,
    ProofOfAddress = 2,
    DriversLicense = 4
}
public enum BookingStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Completed
}
public enum DocumentStatus
{
    Pending,
    Verified,
    Rejected
}