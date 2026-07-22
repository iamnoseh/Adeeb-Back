using Adeeb.Application.Abstractions.Progression;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

public sealed class XpGrantOutboxMessage
{
    private XpGrantOutboxMessage() { }
    public XpGrantOutboxMessage(Guid ledgerEntryId, Guid userId, XpSourceType sourceType, XpEntryType entryType,
        int amountUnits, long newBalanceUnits, DateTimeOffset createdAtUtc)
    { LedgerEntryId = ledgerEntryId; UserId = userId; SourceType = sourceType; EntryType = entryType; AmountUnits = amountUnits; NewBalanceUnits = newBalanceUnits; CreatedAtUtc = createdAtUtc; }
    public Guid LedgerEntryId { get; private set; }
    public Guid UserId { get; private set; }
    public XpSourceType SourceType { get; private set; }
    public XpEntryType EntryType { get; private set; }
    public int AmountUnits { get; private set; }
    public long NewBalanceUnits { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public void Processed(DateTimeOffset now) { ProcessedAtUtc = now; LastError = null; }
    public void Failed(string error) { Attempts++; LastError = error.Length <= 500 ? error : error[..500]; }
    public XpGrantedIntegrationEvent ToEvent() => new(LedgerEntryId, UserId, SourceType, EntryType, AmountUnits, NewBalanceUnits, CreatedAtUtc);
}

internal sealed class XpGrantOutboxMap : IEntityTypeConfiguration<XpGrantOutboxMessage>
{
    public void Configure(EntityTypeBuilder<XpGrantOutboxMessage> b)
    {
        b.ToTable("xp_grant_outbox"); b.HasKey(x => x.LedgerEntryId);
        b.Property(x => x.LedgerEntryId).HasColumnName("ledger_entry_id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.SourceType).HasColumnName("source_type").HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.EntryType).HasColumnName("entry_type").HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.AmountUnits).HasColumnName("amount_units"); b.Property(x => x.NewBalanceUnits).HasColumnName("new_balance_units");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
        b.Property(x => x.Attempts).HasColumnName("attempts"); b.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(500);
        b.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc }).HasDatabaseName("ix_question_bank_xp_outbox_pending");
    }
}

internal sealed class XpGrantOutboxDispatcher(IServiceScopeFactory scopes,
    ILogger<XpGrantOutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            try { await DispatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "XP outbox dispatch failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    private async Task DispatchAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<QuestionBankDbContext>();
        var handlers = scope.ServiceProvider.GetServices<IXpGrantedIntegrationHandler>().ToList();
        if (handlers.Count == 0) return;
        var messages = await db.XpGrantOutbox.Where(x => x.ProcessedAtUtc == null).OrderBy(x => x.CreatedAtUtc).Take(100).ToListAsync(ct);
        foreach (var message in messages)
        {
            try { foreach (var handler in handlers) await handler.HandleAsync(message.ToEvent(), ct); message.Processed(DateTimeOffset.UtcNow); }
            catch (Exception exception) { message.Failed(exception.Message); logger.LogWarning(exception, "XP outbox message failed. LedgerEntryId={LedgerEntryId}", message.LedgerEntryId); }
        }
        if (messages.Count > 0) await db.SaveChangesAsync(ct);
    }
}
