using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class ShopCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();
    
    private static readonly Dictionary<string, ShopCategory> _shopCategories = new()
    {
        { "tools", new ShopCategory("Tools", "🛠️", new Dictionary<string, ShopItem>
        {
            { "fishingrod", new ShopItem("Fishing Rod", "🎣", 500, "Basic fishing rod", new[] { "rod" }) },
            { "prorod", new ShopItem("Pro Rod", "🎣", 2000, "Better catch rates", new[] { "professional" }) },
            { "goldrod", new ShopItem("Golden Rod", "🎣", 5000, "Best fishing rod", new[] { "gold" }) },
            { "pickaxe", new ShopItem("Pickaxe", "⛏️", 750, "Required for mining") },
            { "steelpickaxe", new ShopItem("Steel Pickaxe", "⛏️", 2500, "Better mining rates") },
            { "diamondpickaxe", new ShopItem("Diamond Pickaxe", "⛏️", 7500, "Best mining rates") },
            { "axe", new ShopItem("Axe", "🪓", 500, "Required for woodcutting") },
            { "steelaxe", new ShopItem("Steel Axe", "🪓", 2000, "Better woodcutting rates") },
            { "sword", new ShopItem("Sword", "⚔️", 1000, "Required for hunting") },
            { "bow", new ShopItem("Bow", "🏹", 1500, "Alternative for hunting") }
        })},
        { "supplies", new ShopCategory("Supplies", "🎒", new Dictionary<string, ShopItem>
        {
            { "bait", new ShopItem("Fishing Bait", "🪱", 50, "Basic fishing bait (10)", new[] { "worm" }) },
            { "advancedbait", new ShopItem("Advanced Bait", "🦐", 150, "Better bait (10)") },
            { "premiumbait", new ShopItem("Premium Bait", "🦑", 300, "Best bait (10)") },
            { "torch", new ShopItem("Torch", "🔦", 100, "Required for cave exploration") },
            { "net", new ShopItem("Fishing Net", "🕸️", 200, "Catch multiple fish") },
            { "arrows", new ShopItem("Arrows", "🏹", 100, "Required for bow (20)") },
            { "healingpotion", new ShopItem("Healing Potion", "🧪", 300, "Recover health") },
            { "fishingboat", new ShopItem("Fishing Boat", "🚤", 5000, "Access deep sea fishing") }
        })},
        { "collectibles", new ShopCategory("Collectibles", "🏆", new Dictionary<string, ShopItem>
        {
            { "trophy", new ShopItem("Trophy", "🏆", 10000, "Show off your wealth") },
            { "crown", new ShopItem("Crown", "👑", 50000, "Royal status symbol") },
            { "ring", new ShopItem("Ring", "💍", 25000, "Precious jewelry") },
            { "necklace", new ShopItem("Necklace", "📿", 15000, "Elegant necklace") },
            { "gem", new ShopItem("Rare Gem", "💎", 20000, "Valuable gemstone") }
        })},
        { "pets", new ShopCategory("Pet Items", "🐾", new Dictionary<string, ShopItem>
        {
            { "petfood", new ShopItem("Pet Food", "🥫", 100, "Basic pet food (5)") },
            { "premiumfood", new ShopItem("Premium Pet Food", "🍖", 300, "Premium pet food (5)") },
            { "pettoy", new ShopItem("Pet Toy", "🧸", 200, "Keep your pet happy") },
            { "petbed", new ShopItem("Pet Bed", "🛏️", 500, "Comfortable bed for your pet") },
            { "petcollar", new ShopItem("Pet Collar", "🔔", 1000, "Fancy collar for your pet") }
        })},
        { "farming", new ShopCategory("Farming", "🌱", new Dictionary<string, ShopItem>
        {
            { "seeds", new ShopItem("Basic Seeds", "🌱", 50, "Basic crop seeds (5)") },
            { "raroseeds", new ShopItem("Rare Seeds", "🌺", 200, "Rare crop seeds (3)") },
            { "wateringcan", new ShopItem("Watering Can", "💧", 300, "Water your plants") },
            { "fertilizer", new ShopItem("Fertilizer", "💪", 150, "Boost crop growth (5)") },
            { "hoe", new ShopItem("Hoe", "🌾", 400, "Required for farming") }
        })},
        { "special", new ShopCategory("Special", "✨", new Dictionary<string, ShopItem>
        {
            { "luckycharm", new ShopItem("Lucky Charm", "🍀", 5000, "Increases rare find chances") },
            { "xpboost", new ShopItem("XP Boost", "📈", 2000, "2x XP for 1 hour") },
            { "treasuremap", new ShopItem("Treasure Map", "🗺️", 1000, "Find hidden treasure") },
            { "mysterybox", new ShopItem("Mystery Box", "📦", 3000, "Contains random valuable items") }
        })}
    };

    private static readonly Dictionary<string, Dictionary<string, CatchableItem>> _catchableItems = new()
    {
        { "fish", new Dictionary<string, CatchableItem>
        {
            { "common_fish", new CatchableItem("Common Fish", "🐟", 10, 45) },
            { "rare_fish", new CatchableItem("Rare Fish", "🐠", 50, 25) },
            { "epic_fish", new CatchableItem("Epic Fish", "🐡", 150, 15) },
            { "legendary_fish", new CatchableItem("Legendary Fish", "🐋", 500, 5) },
            { "shark", new CatchableItem("Shark", "🦈", 1000, 2) },
            { "tropical_fish", new CatchableItem("Tropical Fish", "🐠", 75, 20) },
            { "octopus", new CatchableItem("Octopus", "🐙", 200, 10) },
            { "trash", new CatchableItem("Trash", "🗑️", 1, 10) }
        }},
        { "ore", new Dictionary<string, CatchableItem>
        {
            { "stone", new CatchableItem("Stone", "🪨", 5, 40) },
            { "iron", new CatchableItem("Iron Ore", "⛰️", 30, 25) },
            { "gold", new CatchableItem("Gold Ore", "💎", 100, 15) },
            { "diamond", new CatchableItem("Diamond", "💎", 500, 5) }
        }},
        { "wood", new Dictionary<string, CatchableItem>
        {
            { "oak", new CatchableItem("Oak Wood", "🌳", 20, 40) },
            { "maple", new CatchableItem("Maple Wood", "🌲", 40, 30) },
            { "pine", new CatchableItem("Pine Wood", "🌲", 30, 30) }
        }}
    };

    public ShopCommands(DatabaseService db)
    {
        _db = db;
    }

    // Add these static methods to access the collections
    public static Dictionary<string, ShopCategory> GetShopCategories() => _shopCategories;
    public static Dictionary<string, Dictionary<string, CatchableItem>> GetCatchableItems() => _catchableItems;

    [Command("shop")]
    [Summary("View shop categories or items. Usage: !shop [category]")]
    public async Task ShowShop(string? category = null)
    {
        if (category == null)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏪 Shop Categories")
                .WithDescription("Use `!shop <category>` to view items")
                .WithColor(Color.Gold);

            foreach (var cat in _shopCategories)
            {
                embed.AddField(
                    $"{cat.Value.Emoji} {cat.Value.Name}",
                    $"Items: {cat.Value.Items.Count}",
                    true
                );
            }

            await ReplyAsync(embed: embed.Build());
            return;
        }

        if (!_shopCategories.TryGetValue(category.ToLower(), out var shopCategory))
        {
            await ReplyAsync("❌ Category not found!");
            return;
        }

        var categoryEmbed = new EmbedBuilder()
            .WithTitle($"{shopCategory.Emoji} {shopCategory.Name}")
            .WithDescription("Buy items using `!buy <item>`")
            .WithColor(Color.Gold);

        foreach (var item in shopCategory.Items.Values)
        {
            categoryEmbed.AddField(
                $"{item.Emoji} {item.Name}",
                $"Price: ${item.Price}\n{item.Description}",
                true
            );
        }

        await ReplyAsync(embed: categoryEmbed.Build());
    }

    [Command("buy")]
    [Summary("Buy an item from the shop. Usage: !buy <item name>")]
    public async Task BuyItem([Remainder]string itemName)
    {
        var normalizedInput = itemName.ToLower().Trim();
        
        // Search through all categories
        ShopItem item = null;
        string itemKey = null;

        foreach (var category in _shopCategories)
        {
            foreach (var shopItem in category.Value.Items)
            {
                if (shopItem.Key.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                    shopItem.Value.Name.Replace(" ", "").Equals(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                    (shopItem.Value.Aliases != null && shopItem.Value.Aliases.Any(a => a.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))))
                {
                    item = shopItem.Value;
                    itemKey = shopItem.Key;
                    break;
                }
            }
            if (item != null) break;
        }

        if (item == null)
        {
            await ReplyAsync("❌ Item not found! Use !shop to see available items.");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < item.Price)
        {
            await ReplyAsync($"❌ You don't have enough money! You need ${item.Price}");
            return;
        }

        // Handle special quantities for certain items
        int quantity = GetItemQuantity(itemKey);

        user.Balance -= item.Price;
        user.Inventory[itemKey] = user.Inventory.GetValueOrDefault(itemKey) + quantity;
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ You bought {item.Emoji} {item.Name} x{quantity} for ${item.Price}!");
    }

    private int GetItemQuantity(string itemKey)
    {
        if (itemKey.Contains("bait")) return 10;
        if (itemKey.Contains("food")) return 5;
        if (itemKey == "arrows") return 20;
        if (itemKey == "seeds") return 5;
        if (itemKey == "fertilizer") return 5;
        return 1;
    }

    [Command("fish")]
    [Summary("Go fishing with your rod")]
    public async Task Fish()
    {
        var user = _db.GetAccount(Context.User.Id);
        var rodType = GetBestTool(user.Inventory, new[] { "goldrod", "prorod", "fishingrod" });
        
        if (rodType == null)
        {
            await ReplyAsync("❌ You need a fishing rod! Buy one from the shop.");
            return;
        }

        if (!user.Inventory.ContainsKey("bait") && 
            !user.Inventory.ContainsKey("advancedbait") && 
            !user.Inventory.ContainsKey("premiumbait"))
        {
            await ReplyAsync("❌ You need bait! Buy some from the shop.");
            return;
        }

        if (DateTime.UtcNow - user.LastFished < TimeSpan.FromMinutes(1))
        {
            var timeLeft = TimeSpan.FromMinutes(1) - (DateTime.UtcNow - user.LastFished);
            await ReplyAsync($"⏰ Wait {timeLeft.Seconds} seconds before fishing again!");
            return;
        }

        user.Inventory["bait"]--;
        user.LastFished = DateTime.UtcNow;

        var fish = GetRandomFish();
        user.Inventory[fish.Id] = user.Inventory.GetValueOrDefault(fish.Id) + 1;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🎣 Fishing Results")
            .WithDescription($"You caught: {fish.Emoji} {fish.Name}!")
            .WithColor(Color.Blue)
            .AddField("Value", $"${fish.Value}", true)
            .AddField("Bait Remaining", user.Inventory["bait"], true)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("inventory")]
    [Alias("inv")]
    [Summary("View your inventory")]
    public async Task ShowInventory()
    {
        var user = _db.GetAccount(Context.User.Id);
        var embed = new EmbedBuilder()
            .WithTitle("🎒 Inventory")
            .WithColor(Color.Blue);

        foreach (var (itemId, count) in user.Inventory.Where(x => x.Value > 0))
        {
            var shopItem = _shopCategories.SelectMany(cat => cat.Value.Items).FirstOrDefault(x => x.Key == itemId).Value;
            var fishItem = _catchableItems.SelectMany(cat => cat.Value).FirstOrDefault(x => x.Key == itemId).Value;
            var item = shopItem ?? (IItem)fishItem;

            if (item != null)
            {
                embed.AddField(
                    $"{item.Emoji} {item.Name}",
                    $"Quantity: {count}",
                    true
                );
            }
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("sell")]
    [Summary("Sell items. Usage: !sell <item> [amount]")]
    public async Task SellFish(string itemName, int amount = 1)
    {
        var normalizedName = itemName.ToLower();
        if (!_catchableItems["fish"].ContainsKey(normalizedName))
        {
            await ReplyAsync("❌ You can only sell fish!");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (!user.Inventory.ContainsKey(normalizedName) || user.Inventory[normalizedName] < amount)
        {
            await ReplyAsync("❌ You don't have enough of this item!");
            return;
        }

        var fish = _catchableItems["fish"][normalizedName];
        int totalValue = fish.Value * amount;

        user.Inventory[normalizedName] -= amount;
        user.Balance += totalValue;
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ Sold {amount}x {fish.Emoji} {fish.Name} for ${totalValue}!");
    }

    private CatchableItem GetRandomFish()
    {
        int roll = _random.Next(1, 101);
        var user = _db.GetAccount(Context.User.Id);
        
        // Apply luck bonus if luck charm is active
        if (DateTime.UtcNow < user.LuckCharmExpiry)
            roll += 20; // 20% better chance for rare items

        int cumulative = 0;
        foreach (var fish in _catchableItems["fish"].Values)
        {
            cumulative += fish.Chance;
            if (roll <= cumulative)
                return fish;
        }

        return _catchableItems["fish"]["trash"];
    }

    // Add new methods for other activities
    [Command("mine")]
    [Summary("Go mining for ores. Requires: pickaxe")]
    public async Task Mine()
    {
        var user = _db.GetAccount(Context.User.Id);
        var pickaxeType = GetBestTool(user.Inventory, new[] { "diamondpickaxe", "steelpickaxe", "pickaxe" });
        
        if (pickaxeType == null)
        {
            await ReplyAsync("❌ You need a pickaxe! Buy one from the shop.");
            return;
        }

        var activity = _catchableItems["ore"];
        var result = activity.ElementAt(_random.Next(activity.Count));
        user.Inventory[result.Key] = user.Inventory.GetValueOrDefault(result.Key) + 1;
        _db.UpdateAccount(user);

        await ReplyAsync($"⛏️ You mined: {result.Value.Emoji} {result.Value.Name}!");
    }

    // Helper method to get the best tool from inventory
    private string? GetBestTool(Dictionary<string, int> inventory, string[] toolTiers)
    {
        return toolTiers.FirstOrDefault(tool => inventory.ContainsKey(tool) && inventory[tool] > 0);
    }

    [Command("use")]
    [Summary("Use a special item. Usage: !use <item>")]
    public async Task UseItem([Remainder] string itemName)
    {
        var user = _db.GetAccount(Context.User.Id);
        var normalizedName = itemName.ToLower().Replace(" ", "");

        if (!user.Inventory.ContainsKey(normalizedName))
        {
            await ReplyAsync("❌ You don't have this item!");
            return;
        }

        switch (normalizedName)
        {
            case "luckycharm":
                if (DateTime.UtcNow < user.LuckCharmExpiry)
                {
                    var timeLeft = user.LuckCharmExpiry - DateTime.UtcNow;
                    await ReplyAsync($"❌ Your luck charm is still active for {timeLeft.Hours}h {timeLeft.Minutes}m!");
                    return;
                }
                user.LuckCharmExpiry = DateTime.UtcNow.AddHours(1);
                user.Inventory[normalizedName]--;
                await ReplyAsync("🍀 Luck charm activated for 1 hour! Better drops incoming!");
                break;

            case "xpboost":
                if (DateTime.UtcNow < user.XPBoostExpiry)
                {
                    var timeLeft = user.XPBoostExpiry - DateTime.UtcNow;
                    await ReplyAsync($"❌ Your XP boost is still active for {timeLeft.Hours}h {timeLeft.Minutes}m!");
                    return;
                }
                user.XPBoostExpiry = DateTime.UtcNow.AddHours(1);
                user.Inventory[normalizedName]--;
                await ReplyAsync("📈 XP boost activated for 1 hour! Double XP incoming!");
                break;

            case "mysterybox":
                await OpenMysteryBox(user);
                user.Inventory[normalizedName]--;
                break;

            case "treasuremap":
                await UseTreasureMap(user);
                user.Inventory[normalizedName]--;
                break;

            default:
                await ReplyAsync("❌ This item cannot be used!");
                return;
        }

        _db.UpdateAccount(user);
    }

    private async Task OpenMysteryBox(UserAccount user)
    {
        string[] possibleItems = { "rare_fish", "diamond", "gold", "epic_fish", "dragon_scale" };
        int[] amounts = { 1, 2, 3 };
        
        int numItems = _random.Next(1, 4);
        var rewards = new List<(string item, int amount)>();

        for (int i = 0; i < numItems; i++)
        {
            string item = possibleItems[_random.Next(possibleItems.Length)];
            int amount = amounts[_random.Next(amounts.Length)];
            user.Inventory[item] = user.Inventory.GetValueOrDefault(item) + amount;
            rewards.Add((item, amount));
        }

        var embed = new EmbedBuilder()
            .WithTitle("📦 Mystery Box Opened!")
            .WithDescription("You received:")
            .WithColor(Color.Gold);

        foreach (var (item, amount) in rewards)
        {
            embed.AddField(item, $"x{amount}", true);
        }

        await ReplyAsync(embed: embed.Build());
    }

    private async Task UseTreasureMap(UserAccount user)
    {
        int goldAmount = _random.Next(1000, 5001);
        string[] possibleItems = { "diamond", "crown", "rare_gem" };
        string rareItem = possibleItems[_random.Next(possibleItems.Length)];

        user.Balance += goldAmount;
        user.Inventory[rareItem] = user.Inventory.GetValueOrDefault(rareItem) + 1;

        await ReplyAsync($"🗺️ Your treasure hunt was successful!\nFound: ${goldAmount} and a {rareItem}!");
    }
}

public interface IItem
{
    string Name { get; }
    string Emoji { get; }
    int Price { get; }
}

public class ShopItem : IItem
{
    public string Name { get; }
    public string Emoji { get; }
    public int Price { get; }
    public string Description { get; }
    public string[] Aliases { get; }

    public ShopItem(string name, string emoji, int price, string description, string[] aliases = null)
    {
        Name = name;
        Emoji = emoji;
        Price = price;
        Description = description;
        Aliases = aliases ?? Array.Empty<string>();
    }
}

public class CatchableItem : IItem
{
    public string Id { get; }
    public string Name { get; }
    public string Emoji { get; }
    public int Price { get; }
    public int Value => Price;
    public int Chance { get; }

    public CatchableItem(string name, string emoji, int price, int chance)
    {
        Id = name.ToLower().Replace(" ", "_");
        Name = name;
        Emoji = emoji;
        Price = price;
        Chance = chance;
    }
}

public class ShopCategory
{
    public string Name { get; }
    public string Emoji { get; }
    public Dictionary<string, ShopItem> Items { get; }

    public ShopCategory(string name, string emoji, Dictionary<string, ShopItem> items)
    {
        Name = name;
        Emoji = emoji;
        Items = items;
    }
}
