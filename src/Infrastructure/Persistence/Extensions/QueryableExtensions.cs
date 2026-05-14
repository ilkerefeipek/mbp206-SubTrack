using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Common;

namespace SubTrack.Infrastructure.Persistence.Extensions;

public static class QueryableExtensions
{
    private const int _defaultPageSize = 20;
    private const int _maxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = _defaultPageSize;
        }

        if (pageSize > _maxPageSize)
        {
            pageSize = _maxPageSize;
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedResult<T>(items, total, page, pageSize);
    }

    /// <summary>
    /// Reflection-based dynamic ordering. <paramref name="sortBy"/> MUST be in
    /// <paramref name="allowedFields"/> (case-insensitive); otherwise the supplied
    /// fallback ordering is applied. This whitelist guards against runtime errors
    /// from invalid property names — OWASP A03 (Injection) defensive measure.
    /// </summary>
    public static IOrderedQueryable<T> ApplyOrdering<T>(
        this IQueryable<T> query,
        string? sortBy,
        IReadOnlySet<string> allowedFields,
        Expression<Func<T, object>> fallback,
        bool descending = false)
    {
        if (string.IsNullOrWhiteSpace(sortBy) || !allowedFields.Contains(sortBy))
        {
            return descending ? query.OrderByDescending(fallback) : query.OrderBy(fallback);
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.PropertyOrField(parameter, sortBy);
        var typedLambda = Expression.Lambda<Func<T, object>>(
            Expression.Convert(property, typeof(object)),
            parameter);

        return descending ? query.OrderByDescending(typedLambda) : query.OrderBy(typedLambda);
    }
}
