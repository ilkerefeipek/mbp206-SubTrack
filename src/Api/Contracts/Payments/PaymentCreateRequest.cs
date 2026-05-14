namespace SubTrack.Api.Contracts.Payments;

public sealed record PaymentCreateRequest(
    decimal Amount,
    string Method,
    DateOnly PaymentDate,
    string? Currency,
    string? TransactionId);
