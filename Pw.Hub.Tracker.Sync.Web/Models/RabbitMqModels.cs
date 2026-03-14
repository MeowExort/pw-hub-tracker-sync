using System.Text.Json.Serialization;

namespace Pw.Hub.Tracker.Sync.Web.Models;

public record RelicLotMessage
{
    public required string Server { get; init; }
    public required List<RelicLotItem> Lots { get; init; }
}

public record RelicLotItem
{
    public required SellIdDto SellId { get; init; }
    public long ArriveTime { get; init; }
    public long Price { get; init; }
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
