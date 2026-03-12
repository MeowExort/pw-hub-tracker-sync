using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Pw.Hub.Tracker.Sync.Web.Models;

namespace Pw.Hub.Tracker.Sync.Web.Data;

public interface IEventRepository
{
    Task EnsureTableExistsAsync(CancellationToken ct);
    Task BulkInsertAsync(IEnumerable<EventDto> events, CancellationToken ct);
}

public class EventRepository(string connectionString) : IEventRepository
{
    public async Task EnsureTableExistsAsync(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        const string sql = """
            CREATE TABLE IF NOT EXISTS "Events" (
                "Id" BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                "Server" TEXT NOT NULL,
                "EventType" TEXT NOT NULL,
                "Payload" JSONB NOT NULL,
                "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Events' AND column_name='Server') THEN
                    ALTER TABLE "Events" ADD COLUMN "Server" TEXT;
                    UPDATE "Events" SET "Server" = 'Unknown' WHERE "Server" IS NULL;
                    ALTER TABLE "Events" ALTER COLUMN "Server" SET NOT NULL;
                END IF;
            END $$;

            CREATE INDEX IF NOT EXISTS "IX_Events_Server" ON "Events" ("Server");
            CREATE INDEX IF NOT EXISTS "IX_Events_EventType" ON "Events" ("EventType");
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task BulkInsertAsync(IEnumerable<EventDto> events, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // Используем Binary COPY для максимальной производительности в PostgreSQL
        await using var writer = await connection.BeginBinaryImportAsync(
            "COPY \"Events\" (\"Server\", \"EventType\", \"Payload\", \"CreatedAt\") FROM STDIN (FORMAT BINARY)", 
            ct);

        foreach (var @event in events)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(@event.Server, ct);
            await writer.WriteAsync(@event.EventType, ct);
            
            // Записываем JSON как строку, Npgsql интерпретирует это как jsonb при наличии соответствующей команды в COPY
            // Или используем WriteAsync с типом NpgsqlDbType.Jsonb
            var jsonString = JsonSerializer.Serialize(@event.Json);
            await writer.WriteAsync(jsonString, NpgsqlDbType.Jsonb, ct);
            
            await writer.WriteAsync(DateTime.UtcNow, NpgsqlDbType.TimestampTz, ct);
        }

        await writer.CompleteAsync(ct);
    }
}
