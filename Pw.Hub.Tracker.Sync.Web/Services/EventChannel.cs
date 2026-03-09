using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Pw.Hub.Tracker.Sync.Web.Models;

namespace Pw.Hub.Tracker.Sync.Web.Services;

public class EventChannelOptions
{
    public int ChannelCapacity { get; set; } = 10000;
}

public class EventChannel
{
    private readonly Channel<EventDto> _channel;

    public EventChannel(IOptions<EventChannelOptions> options)
    {
        var channelOptions = new BoundedChannelOptions(options.Value.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<EventDto>(channelOptions);
    }

    public ChannelReader<EventDto> Reader => _channel.Reader;
    public ChannelWriter<EventDto> Writer => _channel.Writer;

    public async ValueTask AddEventAsync(EventDto eventDto, CancellationToken ct = default)
    {
        await Writer.WriteAsync(eventDto, ct);
    }
}
