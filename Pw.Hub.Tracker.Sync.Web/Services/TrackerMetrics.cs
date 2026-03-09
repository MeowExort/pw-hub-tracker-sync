using Prometheus;

namespace Pw.Hub.Tracker.Sync.Web.Services;

public class TrackerMetrics
{
    // Очередь (Gauge)
    public static readonly Gauge QueueSize = Metrics.CreateGauge(
        "tracker_sync_queue_size", 
        "Количество событий в очереди (Channel)");

    // Пропускная способность (Counter)
    public static readonly Counter EventsProcessedTotal = Metrics.CreateCounter(
        "tracker_sync_events_processed_total", 
        "Общее количество обработанных событий");

    // Размер батчей (Histogram)
    public static readonly Histogram BatchSize = Metrics.CreateHistogram(
        "tracker_sync_batch_size", 
        "Размер батча при вставке в БД",
        new HistogramConfiguration
        {
            Buckets = new[] { 1.0, 10, 50, 100, 250, 500, 1000, 2500 }
        });

    // Время обработки батчей (Histogram/Summary)
    public static readonly Histogram BatchProcessingTime = Metrics.CreateHistogram(
        "tracker_sync_batch_processing_seconds", 
        "Время вставки батча в БД (в секундах)",
        new HistogramConfiguration
        {
            Buckets = new[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1.0, 5.0 }
        });
}
