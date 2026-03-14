using System.Text.Json.Serialization;

namespace Pw.Hub.Tracker.Sync.Web.Models;

public record RelicLotMessage
{
    public required string Server { get; init; }
    public required List<RelicLotItem> Lots { get; init; }
}

public record RelicLotItem
{
    [JsonPropertyName("sell_id")]
    public required SellIdDto SellId { get; init; }
    [JsonPropertyName("arrive_time")]
    public long ArriveTime { get; init; }
    public long Price { get; init; }
    [JsonPropertyName("relic_item")]
    public required RelicItemDto RelicItem { get; init; }
}

public record SellIdDto
{
    [JsonPropertyName("player_id")]
    public long PlayerId { get; init; }

    [JsonPropertyName("pos_in_shop")]
    public int PosInShop { get; init; }
}

public record RelicItemDto
{
    public int Id { get; init; }
    public int Exp { get; init; }

    [JsonPropertyName("main_addon")]
    public int MainAddon { get; init; }

    [JsonPropertyName("lock_")]
    public int Lock { get; init; }

    public int Reserve { get; init; }
    public List<AddonDto> Addons { get; init; } = [];
}

public record AddonDto
{
    public int Type { get; init; }
    public int Val { get; init; }
}
