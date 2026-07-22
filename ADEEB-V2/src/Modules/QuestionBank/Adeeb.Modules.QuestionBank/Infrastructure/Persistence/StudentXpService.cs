using System.Data;
using System.Text.Json;
using Adeeb.Application.Abstractions.Progression;
using Adeeb.Application.Abstractions.Time;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Progression;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Adeeb.Modules.QuestionBank.Infrastructure.Persistence;

internal sealed class StudentXpService(
    QuestionBankDbContext db,
    IDateTimeProvider clock,
    ILogger<StudentXpService> logger) : IStudentXpService
{
    private const int MaximumSourceLength = 128;
    private const int MaximumIdempotencyKeyLength = 160;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<XpGrantResult>> GrantAsync(XpGrantRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return Result<XpGrantResult>.Failure(validation);

        logger.LogInformation(
            "XP grant started. UserId={UserId} SourceType={SourceType} SourceId={SourceId} EntryType={EntryType} AmountUnits={AmountUnits} IdempotencyKey={IdempotencyKey}",
            request.UserId, request.SourceType, request.SourceId, request.EntryType, request.AmountUnits,
            request.IdempotencyKey);

        if (!db.Database.IsNpgsql())
            return await GrantWithTrackedEntitiesAsync(request, cancellationToken);

        IDbContextTransaction? ownedTransaction = null;
        try
        {
            if (db.Database.CurrentTransaction is null)
                ownedTransaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            var result = await GrantPostgresAsync(request, cancellationToken);
            if (result.IsSuccess && ownedTransaction is not null)
                await ownedTransaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.NumericValueOutOfRange)
        {
            logger.LogWarning(exception,
                "XP balance overflow prevented. UserId={UserId} SourceType={SourceType} SourceId={SourceId} AmountUnits={AmountUnits}",
                request.UserId, request.SourceType, request.SourceId, request.AmountUnits);
            return Result<XpGrantResult>.Failure(XpErrors.BalanceOverflow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "XP grant rolled back. UserId={UserId} SourceType={SourceType} SourceId={SourceId} EntryType={EntryType} AmountUnits={AmountUnits}",
                request.UserId, request.SourceType, request.SourceId, request.EntryType, request.AmountUnits);
            return Result<XpGrantResult>.Failure(XpErrors.PersistenceFailed);
        }
        finally
        {
            if (ownedTransaction is not null) await ownedTransaction.DisposeAsync();
        }
    }

    private async Task<Result<XpGrantResult>> GrantPostgresAsync(XpGrantRequest request, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        var ledgerEntryId = Guid.NewGuid();
        var now = clock.UtcNow;
        var metadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata, Json);

        await using var insert = new NpgsqlCommand("""
            INSERT INTO question_bank.xp_ledger_entries
                (id, user_id, source_type, source_id, entry_type, amount_units, idempotency_key,
                 balance_before_units, balance_after_units, metadata_json, created_at_utc)
            VALUES
                (@id, @user_id, @source_type, @source_id, @entry_type, @amount_units, @idempotency_key,
                 0, 0, @metadata_json, @created_at_utc)
            ON CONFLICT DO NOTHING
            RETURNING id;
            """, connection, transaction);
        AddRequestParameters(insert, request, ledgerEntryId, metadataJson, now);
        var inserted = await insert.ExecuteScalarAsync(ct);
        if (inserted is null)
            return await ExistingResultAsync(connection, transaction, request, ct);

        long previousBalance;
        long newBalance;
        if (request.EntryType == XpEntryType.Credit)
        {
            await using var balance = new NpgsqlCommand("""
                INSERT INTO question_bank.student_xp_balances (user_id, total_xp_units, updated_at_utc)
                VALUES (@user_id, @amount_units, @updated_at_utc)
                ON CONFLICT (user_id) DO UPDATE
                SET total_xp_units = question_bank.student_xp_balances.total_xp_units + EXCLUDED.total_xp_units,
                    updated_at_utc = EXCLUDED.updated_at_utc
                RETURNING total_xp_units;
                """, connection, transaction);
            balance.Parameters.AddWithValue("user_id", request.UserId);
            balance.Parameters.AddWithValue("amount_units", request.AmountUnits);
            balance.Parameters.AddWithValue("updated_at_utc", now);
            newBalance = Convert.ToInt64(await balance.ExecuteScalarAsync(ct));
            previousBalance = checked(newBalance - request.AmountUnits);
        }
        else
        {
            await using var current = new NpgsqlCommand(
                "SELECT COALESCE((SELECT total_xp_units FROM question_bank.student_xp_balances WHERE user_id = @user_id), 0);",
                connection, transaction);
            current.Parameters.AddWithValue("user_id", request.UserId);
            previousBalance = newBalance = Convert.ToInt64(await current.ExecuteScalarAsync(ct));
        }

        await using var updateLedger = new NpgsqlCommand("""
            UPDATE question_bank.xp_ledger_entries
            SET balance_before_units = @previous_balance, balance_after_units = @new_balance
            WHERE id = @id;
            """, connection, transaction);
        updateLedger.Parameters.AddWithValue("previous_balance", previousBalance);
        updateLedger.Parameters.AddWithValue("new_balance", newBalance);
        updateLedger.Parameters.AddWithValue("id", ledgerEntryId);
        if (await updateLedger.ExecuteNonQueryAsync(ct) != 1)
            return Result<XpGrantResult>.Failure(XpErrors.PersistenceFailed);

        await using var outbox = new NpgsqlCommand("""
            INSERT INTO question_bank.xp_grant_outbox
                (ledger_entry_id, user_id, source_type, entry_type, amount_units, new_balance_units,
                 created_at_utc, processed_at_utc, attempts, last_error)
            VALUES (@ledger_entry_id, @user_id, @source_type, @entry_type, @amount_units,
                    @new_balance_units, @created_at_utc, NULL, 0, NULL)
            ON CONFLICT (ledger_entry_id) DO NOTHING;
            """, connection, transaction);
        outbox.Parameters.AddWithValue("ledger_entry_id", ledgerEntryId);
        outbox.Parameters.AddWithValue("user_id", request.UserId);
        outbox.Parameters.AddWithValue("source_type", request.SourceType.ToString());
        outbox.Parameters.AddWithValue("entry_type", request.EntryType.ToString());
        outbox.Parameters.AddWithValue("amount_units", request.AmountUnits);
        outbox.Parameters.AddWithValue("new_balance_units", newBalance);
        outbox.Parameters.AddWithValue("created_at_utc", now);
        if (await outbox.ExecuteNonQueryAsync(ct) != 1)
            return Result<XpGrantResult>.Failure(XpErrors.PersistenceFailed);

        logger.LogInformation(
            "XP ledger entry created and balance updated. UserId={UserId} SourceType={SourceType} SourceId={SourceId} EntryType={EntryType} AmountUnits={AmountUnits} PreviousBalanceUnits={PreviousBalanceUnits} NewBalanceUnits={NewBalanceUnits} LedgerEntryId={LedgerEntryId}",
            request.UserId, request.SourceType, request.SourceId, request.EntryType, request.AmountUnits,
            previousBalance, newBalance, ledgerEntryId);
        return Result<XpGrantResult>.Success(new(
            ledgerEntryId, previousBalance, newBalance, request.AmountUnits, WasAlreadyProcessed: false));
    }

    private async Task<Result<XpGrantResult>> ExistingResultAsync(NpgsqlConnection connection,
        NpgsqlTransaction? transaction, XpGrantRequest request, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, user_id, source_type, source_id, entry_type, amount_units, idempotency_key,
                   balance_before_units, balance_after_units
            FROM question_bank.xp_ledger_entries
            WHERE idempotency_key = @idempotency_key
               OR (user_id = @user_id AND source_type = @source_type AND source_id = @source_id)
            ORDER BY CASE WHEN idempotency_key = @idempotency_key THEN 0 ELSE 1 END
            LIMIT 1;
            """, connection, transaction);
        command.Parameters.AddWithValue("idempotency_key", request.IdempotencyKey);
        command.Parameters.AddWithValue("user_id", request.UserId);
        command.Parameters.AddWithValue("source_type", request.SourceType.ToString());
        command.Parameters.AddWithValue("source_id", request.SourceId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return Result<XpGrantResult>.Failure(XpErrors.PersistenceFailed);

        var exactMatch = reader.GetGuid(1) == request.UserId
            && string.Equals(reader.GetString(2), request.SourceType.ToString(), StringComparison.Ordinal)
            && string.Equals(reader.GetString(3), request.SourceId, StringComparison.Ordinal)
            && string.Equals(reader.GetString(4), request.EntryType.ToString(), StringComparison.Ordinal)
            && reader.GetInt32(5) == request.AmountUnits
            && string.Equals(reader.GetString(6), request.IdempotencyKey, StringComparison.Ordinal);
        if (!exactMatch)
        {
            logger.LogWarning(
                "XP source conflict detected. UserId={UserId} SourceType={SourceType} SourceId={SourceId} IdempotencyKey={IdempotencyKey}",
                request.UserId, request.SourceType, request.SourceId, request.IdempotencyKey);
            return Result<XpGrantResult>.Failure(XpErrors.SourceConflict);
        }

        var result = new XpGrantResult(reader.GetGuid(0), reader.GetInt64(7), reader.GetInt64(8),
            reader.GetInt32(5), WasAlreadyProcessed: true);
        logger.LogInformation(
            "XP settlement already exists. UserId={UserId} SourceType={SourceType} SourceId={SourceId} LedgerEntryId={LedgerEntryId}",
            request.UserId, request.SourceType, request.SourceId, result.LedgerEntryId);
        return Result<XpGrantResult>.Success(result);
    }

    private async Task<Result<XpGrantResult>> GrantWithTrackedEntitiesAsync(XpGrantRequest request, CancellationToken ct)
    {
        var existing = await db.XpLedgerEntries.SingleOrDefaultAsync(x =>
            x.IdempotencyKey == request.IdempotencyKey
            || (x.UserId == request.UserId && x.SourceType == request.SourceType && x.SourceId == request.SourceId), ct);
        if (existing is not null)
        {
            var exact = existing.UserId == request.UserId && existing.SourceType == request.SourceType
                && existing.SourceId == request.SourceId && existing.EntryType == request.EntryType
                && existing.AmountUnits == request.AmountUnits && existing.IdempotencyKey == request.IdempotencyKey;
            return exact
                ? Result<XpGrantResult>.Success(new(existing.Id, existing.BalanceBeforeUnits,
                    existing.BalanceAfterUnits, existing.AmountUnits, WasAlreadyProcessed: true))
                : Result<XpGrantResult>.Failure(XpErrors.SourceConflict);
        }

        var now = clock.UtcNow;
        var balance = await db.StudentXpBalances.SingleOrDefaultAsync(x => x.UserId == request.UserId, ct);
        var previous = balance?.TotalXpUnits ?? 0;
        var current = previous;
        if (request.EntryType == XpEntryType.Credit)
        {
            balance ??= new StudentXpBalance(request.UserId, now);
            if (db.Entry(balance).State == EntityState.Detached) db.StudentXpBalances.Add(balance);
            try { (_, current) = balance.Credit(request.AmountUnits, now); }
            catch (OverflowException) { return Result<XpGrantResult>.Failure(XpErrors.BalanceOverflow); }
        }

        var id = Guid.NewGuid();
        db.XpLedgerEntries.Add(new(id, request.UserId, request.SourceType, request.SourceId, request.EntryType,
            request.AmountUnits, request.IdempotencyKey, previous, current,
            request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata, Json), now));
        db.XpGrantOutbox.Add(new(id, request.UserId, request.SourceType, request.EntryType, request.AmountUnits,
            current, now));
        await db.SaveChangesAsync(ct);
        return Result<XpGrantResult>.Success(new(id, previous, current, request.AmountUnits, WasAlreadyProcessed: false));
    }

    private static Error? Validate(XpGrantRequest request)
    {
        if (request.UserId == Guid.Empty || !Enum.IsDefined(request.SourceType)
            || string.IsNullOrWhiteSpace(request.SourceId) || request.SourceId.Length > MaximumSourceLength
            || string.IsNullOrWhiteSpace(request.IdempotencyKey)
            || request.IdempotencyKey.Length > MaximumIdempotencyKeyLength)
            return XpErrors.InvalidSource;
        if (!Enum.IsDefined(request.EntryType) || request.AmountUnits < 0)
            return XpErrors.InvalidAmount;
        if (request.EntryType == XpEntryType.Credit && request.AmountUnits == 0)
            return XpErrors.InvalidAmount;
        if (request.EntryType == XpEntryType.Settlement && request.AmountUnits != 0)
            return XpErrors.InvalidAmount;
        if (request.EntryType is not (XpEntryType.Credit or XpEntryType.Settlement))
            return XpErrors.InvalidAmount;
        return null;
    }

    private static void AddRequestParameters(NpgsqlCommand command, XpGrantRequest request, Guid id,
        string? metadataJson, DateTimeOffset now)
    {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("user_id", request.UserId);
        command.Parameters.AddWithValue("source_type", request.SourceType.ToString());
        command.Parameters.AddWithValue("source_id", request.SourceId);
        command.Parameters.AddWithValue("entry_type", request.EntryType.ToString());
        command.Parameters.AddWithValue("amount_units", request.AmountUnits);
        command.Parameters.AddWithValue("idempotency_key", request.IdempotencyKey);
        command.Parameters.Add(new("metadata_json", NpgsqlDbType.Jsonb) { Value = (object?)metadataJson ?? DBNull.Value });
        command.Parameters.AddWithValue("created_at_utc", now);
    }
}
