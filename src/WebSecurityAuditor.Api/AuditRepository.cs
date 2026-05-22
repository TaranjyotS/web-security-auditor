using Microsoft.EntityFrameworkCore;
using WebSecurityAuditor.Core;

namespace WebSecurityAuditor.Api;

public interface IAuditRepository
{
    Task SaveAsync(AuditReport report, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditSummary>> ListSummariesAsync(CancellationToken cancellationToken);
    Task<AuditReport?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record AuditSummary(
    Guid Id,
    string Target,
    DateTimeOffset CreatedAtUtc,
    AuditStatus Status,
    int[] OpenPorts,
    int RecommendationCount);

public sealed class AuditRepository(IDbContextFactory<AuditDbContext> dbFactory) : IAuditRepository
{
    public async Task SaveAsync(AuditReport report, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Audits.FindAsync([report.Id], cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            db.Audits.Add(AuditEntity.FromReport(report));
        }
        else
        {
            existing.Target = report.Target;
            existing.CreatedAtUtc = report.CreatedAtUtc;
            existing.Status = report.Status;
            existing.ReportJson = System.Text.Json.JsonSerializer.Serialize(report, JsonOptions.Default);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditSummary>> ListSummariesAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        // SQLite cannot translate ORDER BY for DateTimeOffset values.
        // Load the small audit-history table first, then sort in .NET.
        // For larger production workloads, store CreatedAtUtc as DateTime or Unix epoch.
        var entities = await db.Audits
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .OrderByDescending(audit => audit.CreatedAtUtc)
            .Take(50)
            .Select(entity => entity.ToSummary())
            .ToArray();
    }

    public async Task<AuditReport?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await db.Audits.AsNoTracking().FirstOrDefaultAsync(audit => audit.Id == id, cancellationToken).ConfigureAwait(false);
        return entity?.ToReport();
    }
}
