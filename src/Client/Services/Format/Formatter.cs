using System.Globalization;

namespace SubTrack.Client.Services.Format;

/// <summary>tr-TR culture helpers for money + date display.</summary>
public static class Formatter
{
    private static readonly CultureInfo _tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string FormatCurrency(decimal amount, string currency = "TRY")
    {
        var symbol = currency switch
        {
            "TRY" => "TL",
            "USD" => "$",
            "EUR" => "EUR",
            _ => currency
        };
        return $"{amount.ToString("N2", _tr)} {symbol}";
    }

    public static string FormatDate(DateOnly date) =>
        date.ToString("dd MMM yyyy", _tr);

    public static string FormatDate(DateTime dt) =>
        dt.ToString("dd MMM yyyy", _tr);

    public static string FormatDateTime(DateTime dt) =>
        dt.ToString("dd MMM yyyy HH:mm", _tr);

    /// <summary>Bugün / Yarın / N gün önce/sonra; uzak tarihlerde tam tarih.</summary>
    public static string FormatRelativeDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var diff = date.DayNumber - today.DayNumber;

        return diff switch
        {
            0 => "Bugun",
            1 => "Yarin",
            -1 => "Dun",
            > 1 and <= 7 => $"{diff} gun sonra",
            < -1 and >= -7 => $"{-diff} gun once",
            _ => FormatDate(date)
        };
    }

    /// <summary>"5 dakika once" / "2 saat once" / tam tarih.</summary>
    public static string FormatTimeAgo(DateTime dt)
    {
        var delta = DateTime.UtcNow - dt;
        if (delta.TotalSeconds < 60)
        {
            return "az once";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{(int)delta.TotalMinutes} dakika once";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours} saat once";
        }

        if (delta.TotalDays < 7)
        {
            return $"{(int)delta.TotalDays} gun once";
        }

        return FormatDate(dt);
    }
}
