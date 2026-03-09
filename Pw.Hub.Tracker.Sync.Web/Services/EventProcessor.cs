using Microsoft.Extensions.Options;
using Pw.Hub.Tracker.Sync.Web.Data;
using Pw.Hub.Tracker.Sync.Web.Models;

namespace Pw.Hub.Tracker.Sync.Web.Services;

public class EventProcessorOptions
{
    public int BatchSize { get; set; } = 1000;
    public int FlushIntervalSeconds { get; set; } = 5;
}

public class EventProcessor(
    EventChannel channel,
    IEventRepository repository,
    IOptions<EventProcessorOptions> options,
    ILogger<EventProcessor> logger) : BackgroundService
{
    private readonly EventProcessorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventProcessor started with BatchSize={BatchSize}, FlushInterval={FlushInterval}s", 
            _options.BatchSize, _options.FlushIntervalSeconds);

        var batch = new List<EventDto>(_options.BatchSize);
        
        // Используем таймер для периодического сброса батча
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.FlushIntervalSeconds));

        // Основной цикл чтения из канала
        // В .NET 10 можно использовать более современные способы работы с каналами, 
        // но классический WaitToReadAsync/TryRead остается надежным для батчинга.
        
        var readTask = channel.Reader.WaitToReadAsync(stoppingToken).AsTask();
        var timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var completedTask = await Task.WhenAny(readTask, timerTask);

                if (completedTask == readTask)
                {
                    if (await readTask)
                    {
                        while (channel.Reader.TryRead(out var @event))
                        {
                            batch.Add(@event);
                            if (batch.Count >= _options.BatchSize)
                            {
                                await FlushBatchAsync(batch, stoppingToken);
                            }
                        }
                        readTask = channel.Reader.WaitToReadAsync(stoppingToken).AsTask();
                    }
                    else
                    {
                        // Канал закрыт
                        break;
                    }
                }
                else if (completedTask == timerTask)
                {
                    if (batch.Count > 0)
                    {
                        await FlushBatchAsync(batch, stoppingToken);
                    }
                    timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("EventProcessor is stopping...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in EventProcessor");
        }
        finally
        {
            // Graceful shutdown: сбрасываем остатки
            if (batch.Count > 0)
            {
                try 
                {
                    // Используем CancellationToken.None или отдельный таймаут для финального сброса
                    await FlushBatchAsync(batch, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during final flush");
                }
            }
        }
    }

    private async Task FlushBatchAsync(List<EventDto> batch, CancellationToken ct)
    {
        try
        {
            int count = batch.Count;
            logger.LogDebug("Flushing batch of {Count} events", count);
            await repository.BulkInsertAsync(batch, ct);
            batch.Clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to flush batch to database");
            // В реальной системе здесь могла бы быть логика повторов или сохранения в Dead Letter Queue
            // Для ТЗ просто очищаем батч или оставляем (но тогда он может бесконечно фейлить цикл)
            batch.Clear(); 
        }
    }
}
