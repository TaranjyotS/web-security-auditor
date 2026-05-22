using Microsoft.EntityFrameworkCore;
using WebSecurityAuditor.Api;
using WebSecurityAuditor.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    foreach (var converter in JsonOptions.Default.Converters)
    {
        options.SerializerOptions.Converters.Add(converter);
    }
});

builder.Services.AddHttpClient("auditor", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddSingleton<DnsInspector>();
builder.Services.AddSingleton<PortScanner>();
builder.Services.AddSingleton<RecommendationEngine>();
builder.Services.AddScoped<HttpInspector>();
builder.Services.AddScoped<AuditOrchestrator>();

var connectionString = builder.Configuration.GetConnectionString("AuditDb") ?? "Data Source=audits.db";
builder.Services.AddDbContextFactory<AuditDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync().ConfigureAwait(false);
    await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "web-security-auditor" }));

app.MapGet("/api/audits", async (IAuditRepository repository, CancellationToken cancellationToken) =>
{
    var summaries = await repository.ListSummariesAsync(cancellationToken).ConfigureAwait(false);
    return Results.Ok(summaries);
});


app.MapDelete("/api/audits", async (IAuditRepository repository, CancellationToken cancellationToken) =>
{
    var deletedCount = await repository.ClearAsync(cancellationToken).ConfigureAwait(false);
    return Results.Ok(new { deletedCount });
});

app.MapGet("/api/audits/{id:guid}", async (Guid id, IAuditRepository repository, CancellationToken cancellationToken) =>
{
    var report = await repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
    return report is null ? Results.NotFound() : Results.Ok(report);
});

app.MapPost("/api/audits", async (
    CreateAuditRequest input,
    AuditOrchestrator orchestrator,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
{
    AuditRequest request;
    try
    {
        request = TargetValidator.Validate(
            input.Target,
            input.StartPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            input.EndPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            input.TimeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
            input.Authorized);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var report = await orchestrator.RunAsync(Guid.NewGuid(), request, cancellationToken).ConfigureAwait(false);
    await repository.SaveAsync(report, cancellationToken).ConfigureAwait(false);
    return Results.Created($"/api/audits/{report.Id}", report);
});

app.MapGet("/api/reports/{id:guid}/download", async (Guid id, IAuditRepository repository, CancellationToken cancellationToken) =>
{
    var report = await repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
    if (report is null)
    {
        return Results.NotFound();
    }

    var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions.Default);
    return Results.File(bytes, "application/json", $"audit-{report.Target}-{report.Id}.json");
});

app.Run();
