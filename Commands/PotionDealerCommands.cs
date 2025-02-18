using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class PotionDealerCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();

    private static readonly Dictionary<string, PotionInfo> _potions = new()
    {
        { "health", new PotionInfo("Health Potion", "❤️", 50, 100) },
        { "mana", new PotionInfo("Mana Potion", "💙", 75, 150) },
        { "strength", new PotionInfo("Strength Potion", "💪", 100, 200) },
        { "speed", new PotionInfo("Speed Potion", "⚡", 125, 250) },
        { "invisibility", new PotionInfo("Invisibility Potion", "👻", 200, 400) }
    };

    public PotionDealerCommands(DatabaseService db)
    {
        _db = db;
    }

    [Command("brew")]
    public async Task BrewPotion(string potionType, int amount = 1)
    {
        var user = _db.GetAccount(Context.User.Id);
        var normalizedType = potionType.ToLower();

        if (!_potions.ContainsKey(normalizedType))  // Fixed casing here
        {
            await ReplyAsync("❌ Unknown potion type! Available types: " + string.Join(", ", _potions.Keys));
            return;
        }

        var potion = _potions[normalizedType];
        int totalCost = potion.Cost * amount;

        if (user.Balance < totalCost)
        {
            await ReplyAsync($"❌ You need ${totalCost} to brew {amount}x {potion.Name}!");
            return;
        }

        user.Balance -= totalCost;
        user.Inventory[normalizedType] = user.Inventory.GetValueOrDefault(normalizedType) + amount;
        _db.UpdateAccount(user);

        await ReplyAsync($"✨ You brewed {amount}x {potion.Emoji} {potion.Name} for ${totalCost}!");
    }

    [Command("sell")]
    public async Task SellPotion(string potionType, int amount = 1)
    {
        var user = _db.GetAccount(Context.User.Id);
        var normalizedType = potionType.ToLower();

        if (!_potions.ContainsKey(normalizedType))
        {
            await ReplyAsync("❌ Unknown potion type!");
            return;
        }

        if (!user.Inventory.ContainsKey(normalizedType) || user.Inventory[normalizedType] < amount)
        {
            await ReplyAsync("❌ You don't have enough potions!");
            return;
        }

        var potion = _potions[normalizedType];
        int totalValue = potion.Value * amount;

        user.Inventory[normalizedType] -= amount;
        user.Balance += totalValue;
        _db.UpdateAccount(user);

        await ReplyAsync($"💰 Sold {amount}x {potion.Emoji} {potion.Name} for ${totalValue}!");
    }

    [Command("potions")]
    public async Task ShowPotions()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🧪 Potion Shop")
            .WithColor(Color.Purple)
            .WithDescription("Use !brew <type> [amount] to brew potions\nUse !sell <type> [amount] to sell potions");

        foreach (var potion in _potions.Values)
        {
            embed.AddField(
                $"{potion.Emoji} {potion.Name}",
                $"Brew Cost: ${potion.Cost}\nSell Value: ${potion.Value}",
                true
            );
        }

        await ReplyAsync(embed: embed.Build());
    }
}

public class PotionInfo
{
    public string Name { get; }
    public string Emoji { get; }
    public int Cost { get; }
    public int Value { get; }

    public PotionInfo(string name, string emoji, int cost, int value)
    {
        Name = name;
        Emoji = emoji;
        Cost = cost;
        Value = value;
    }
}
