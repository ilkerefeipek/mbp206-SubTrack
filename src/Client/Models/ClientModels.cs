namespace SubTrack.Client.Models;

// --- Enums (mirror backend Domain.Enums) ---
public enum BillingCycle { Weekly = 1, Monthly = 2, Quarterly = 3, Yearly = 4 }
public enum SubscriptionStatus { Active = 1, Paused = 2, Cancelled = 3 }
public enum PaymentStatus { Pending = 1, Success = 2, Failed = 3, Refunded = 4 }
public enum NotificationType { RenewalReminder = 1, UnusedAlert = 2, System = 3, Welcome = 4 }
public enum InsightType { UnusedSubscription = 1, HighSpending = 2, UpcomingRenewal = 3, DuplicateService = 4 }
public enum InsightSeverity { Info = 1, Warning = 2, Critical = 3 }

// --- Auth ---
public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
public sealed record UserDto(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    int ThresholdDays,
    string PreferredCurrency,
    bool IsVerified,
    DateTime CreatedAt);

// --- Categories ---
public sealed record CategoryDto(
    long Id,
    string Name,
    string Icon,
    string Color,
    bool IsDefault,
    int SortOrder);

// --- Subscriptions ---
public sealed record SubscriptionCreateRequest(
    string Name,
    long CategoryId,
    decimal Amount,
    string? Currency,
    BillingCycle BillingCycle,
    DateOnly NextBilling);

public sealed record SubscriptionUpdateRequest(
    string? Name,
    long? CategoryId,
    decimal? Amount,
    BillingCycle? BillingCycle,
    DateOnly? NextBilling,
    SubscriptionStatus? Status);

public sealed record SubscriptionDto(
    long Id,
    string Name,
    long CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly NextBilling,
    DateOnly? LastUsedDate,
    SubscriptionStatus Status,
    DateTime CreatedAt);

public sealed record SubscriptionListItemDto(
    long Id,
    string Name,
    long CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly NextBilling,
    DateOnly? LastUsedDate,
    SubscriptionStatus Status);

public sealed class SubscriptionFilters
{
    public long? CategoryId { get; set; }
    public SubscriptionStatus? Status { get; set; }
    public string? Search { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

// --- Payments ---
public sealed record PaymentCreateRequest(
    decimal Amount,
    string Method,
    DateOnly PaymentDate,
    string? Currency,
    string? TransactionId);

public sealed record PaymentDto(
    long Id,
    long SubscriptionId,
    decimal Amount,
    string Currency,
    string Method,
    DateOnly PaymentDate,
    PaymentStatus Status,
    string? TransactionId,
    DateTime CreatedAt);

// --- Notifications ---
public sealed record NotificationDto(
    long Id,
    NotificationType Type,
    string Message,
    DateTime SentAt,
    bool IsRead,
    long? SubscriptionId,
    string? Channel,
    string? Priority);

// --- Analytics ---
public sealed record DashboardSummaryDto(
    int ActiveCount,
    decimal MonthlyTotal,
    string Currency,
    int UpcomingCount,
    int UnusedCount);

public sealed record CategoryBreakdownItemDto(
    long CategoryId,
    string CategoryName,
    decimal MonthlyAmount,
    decimal Percentage,
    int SubscriptionCount);

public sealed record MonthlyTrendItemDto(
    int Year,
    int Month,
    decimal Amount,
    string Currency);

public sealed record UnusedSubscriptionDto(
    long SubscriptionId,
    string Name,
    string CategoryName,
    decimal Amount,
    decimal MonthlyAmount,
    int DaysSinceLastUse,
    DateOnly? LastUsedDate);

public sealed record InsightDto(
    InsightType Type,
    string Title,
    string Description,
    InsightSeverity Severity,
    long? RelatedSubscriptionId,
    decimal? PotentialSavings);
