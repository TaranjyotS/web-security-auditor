using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebSecurityAuditor.Core;

namespace WebSecurityAuditor.Api;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditEntity> Audits => Set<AuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntity>().HasKey(audit => audit.Id);
        modelBuilder.Entity<AuditEntity>().Property(audit => audit.Target).HasMaxLength(253).IsRequired();
        modelBuilder.Entity<AuditEntity>().Property(audit => audit.Status).HasConversion<string>().HasMaxLength(32);
        modelBuilder.Entity<AuditEntity>().HasIndex(audit => audit.CreatedAtUtc);
        modelBuilder.Entity<AuditEntity>().HasIndex(audit => audit.Target);
    }
}

public sealed class AuditEntity
{
    public Guid Id { get; set; }
    public string Target { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public AuditStatus Status { get; set; }
    public string ReportJson { get; set; } = string.Empty;

    public static AuditEntity FromReport(AuditReport report)
    {
        return new AuditEntity
        {
            Id = report.Id,
            Target = report.Target,
            CreatedAtUtc = report.CreatedAtUtc,
            Status = report.Status,
            ReportJson = JsonSerializer.Serialize(report, JsonOptions.Default)
        };
    }

    public AuditReport ToReport()
    {
        return JsonSerializer.Deserialize<AuditReport>(ReportJson, JsonOptions.Default)
            ?? throw new InvalidOperationException("Stored report could not be deserialized.");
    }

    public AuditSummary ToSummary()
    {
        try
        {
            var report = ToReport();
            return new AuditSummary(
                report.Id,
                report.Target,
                report.CreatedAtUtc,
                report.Status,
                report.Ports.Where(port => port.IsOpen).Select(port => port.Port).ToArray(),
                report.Recommendations.Count);
        }
        catch (JsonException)
        {
            return new AuditSummary(Id, Target, CreatedAtUtc, Status, Array.Empty<int>(), 0);
        }
        catch (NotSupportedException)
        {
            return new AuditSummary(Id, Target, CreatedAtUtc, Status, Array.Empty<int>(), 0);
        }
        catch (InvalidOperationException)
        {
            return new AuditSummary(Id, Target, CreatedAtUtc, Status, Array.Empty<int>(), 0);
        }
    }
}
