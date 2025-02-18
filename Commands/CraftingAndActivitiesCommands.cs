using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class CraftingAndActivitiesCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();

    private static readonly Dictionary<string, CraftingRecipe> _recipes = new()
    {
        { "superrod", new CraftingRecipe("Super Rod", "🎣", 
            new Dictionary<string, int> { {"fishingrod", 1}, {"gold", 5}, {"wood", 10} }) },
        { "goldpickaxe", new CraftingRecipe("Gold Pickaxe", "⛏️", 
            new Dictionary<string, int> { {"pickaxe", 1}, {"gold", 10} }) },
        { "magicbait", new CraftingRecipe("Magic Bait", "✨", 
            new Dictionary<string, int> { {"bait", 5}, {"rare_fish", 1} }) }
    };

    private static readonly Dictionary<string, ActivityInfo> _activities = new()
    {
        { "woodcutting", new ActivityInfo("Woodcutting", "🌳", new Dictionary<string, int>
        {
            {"oak_log", 40}, {"maple_log", 30}, {"pine_log", 20}, {"magic_log", 10}
        })},
        { "mining", new ActivityInfo("Mining", "⛏️", new Dictionary<string, int>
        {
            {"copper", 40}, {"iron", 30}, {"gold", 20}, {"diamond", 10}
        })},
        { "hunting", new ActivityInfo("Hunting", "🏹", new Dictionary<string, int>
        {
            {"rabbit", 40}, {"deer", 30}, {"wolf", 20}, {"dragon", 10}
        })}
    };

    public CraftingAndActivitiesCommands(DatabaseService db)
    {
        _db = db;
    }

    [Command("recipes")]
    [Summary("View available crafting recipes")]
    public async Task ShowRecipes()
    {
        var embed = new EmbedBuilder()
            .WithTitle("📚 Crafting Recipes")
            .WithColor(Color.Gold);

        foreach (var recipe in _recipes)
        {
            string requirements = string.Join("\n", recipe.Value.Requirements
                .Select(r => $"{r.Key}: {r.Value}x"));
            embed.AddField($"{recipe.Value.Emoji} {recipe.Value.Name}", requirements, true);
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("craft")]
    [Summary("Craft an item using materials")]
    public async Task Craft(string itemName)
    {
        var user = _db.GetAccount(Context.User.Id);
        var normalizedName = itemName.ToLower();

        if (!_recipes.ContainsKey(normalizedName))
        {
            await ReplyAsync("❌ Recipe not found! Use !recipes to see available recipes.");
            return;
        }

        var recipe = _recipes[normalizedName];
        foreach (var requirement in recipe.Requirements)
        {
            if (!user.Inventory.ContainsKey(requirement.Key) || 
                user.Inventory[requirement.Key] < requirement.Value)
            {
                await ReplyAsync($"❌ You need {requirement.Value}x {requirement.Key}!");
                return;
            }
        }

        // Remove materials
        foreach (var requirement in recipe.Requirements)
        {
            user.Inventory[requirement.Key] -= requirement.Value;
        }

        // Add crafted item
        user.Inventory[normalizedName] = user.Inventory.GetValueOrDefault(normalizedName) + 1;
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ Successfully crafted {recipe.Emoji} {recipe.Name}!");
    }

    [Command("woodcut")]
    [Alias("chop")]
    [Summary("Cut trees for wood")]
    public async Task Woodcut()
    {
        var user = _db.GetAccount(Context.User.Id);
        if (!user.Inventory.ContainsKey("axe"))
        {
            await ReplyAsync("❌ You need an axe! Buy one from the shop.");
            return;
        }

        var activity = _activities["woodcutting"];
        var result = GetRandomDrop(activity.Drops);
        user.Inventory[result.Key] = user.Inventory.GetValueOrDefault(result.Key) + 1;
        _db.UpdateAccount(user);

        await ReplyAsync($"🌳 You got: {result.Key} x1");
    }

    [Command("mine")]
    [Summary("Mine for ores")]
    public async Task Mine()
    {
        var user = _db.GetAccount(Context.User.Id);
        if (!user.Inventory.ContainsKey("pickaxe"))
        {
            await ReplyAsync("❌ You need a pickaxe! Buy one from the shop.");
            return;
        }

        var activity = _activities["mining"];
        var result = GetRandomDrop(activity.Drops);
        user.Inventory[result.Key] = user.Inventory.GetValueOrDefault(result.Key) + 1;
        _db.UpdateAccount(user);

        await ReplyAsync($"⛏️ You got: {result.Key} x1");
    }

    [Command("hunt")]
    [Summary("Hunt for animals")]
    public async Task Hunt()
    {
        var user = _db.GetAccount(Context.User.Id);
        if (!user.Inventory.ContainsKey("sword"))
        {
            await ReplyAsync("❌ You need a sword! Buy one from the shop.");
            return;
        }

        var activity = _activities["hunting"];
        var result = GetRandomDrop(activity.Drops);
        user.Inventory[result.Key] = user.Inventory.GetValueOrDefault(result.Key) + 1;
        _db.UpdateAccount(user);

        await ReplyAsync($"🏹 You got: {result.Key} x1");
    }

    private KeyValuePair<string, int> GetRandomDrop(Dictionary<string, int> drops)
    {
        int roll = _random.Next(1, 101);
        int cumulative = 0;

        foreach (var drop in drops)
        {
            cumulative += drop.Value;
            if (roll <= cumulative)
                return drop;
        }

        return drops.First();
    }
}

public class CraftingRecipe
{
    public string Name { get; }
    public string Emoji { get; }
    public Dictionary<string, int> Requirements { get; }

    public CraftingRecipe(string name, string emoji, Dictionary<string, int> requirements)
    {
        Name = name;
        Emoji = emoji;
        Requirements = requirements;
    }
}

public class ActivityInfo
{
    public string Name { get; }
    public string Emoji { get; }
    public Dictionary<string, int> Drops { get; }

    public ActivityInfo(string name, string emoji, Dictionary<string, int> drops)
    {
        Name = name;
        Emoji = emoji;
        Drops = drops;
    }
}
