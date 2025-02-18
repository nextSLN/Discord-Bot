using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class PetsAndAchievementsCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();

    private static readonly Dictionary<string, PetInfo> _pets = new()
    {
        { "cat", new PetInfo("Cat", "🐱", 1000, new[] {"fish"}) },
        { "dog", new PetInfo("Dog", "🐕", 1000, new[] {"meat"}) },
        { "dragon", new PetInfo("Dragon", "🐲", 10000, new[] {"gold", "diamond"}) },
        { "penguin", new PetInfo("Penguin", "🐧", 2000, new[] {"fish"}) },
        { "monkey", new PetInfo("Monkey", "🐒", 1500, new[] {"banana"}) }
    };

    private static readonly Dictionary<string, Achievement> _achievements = new()
    {
        { "rich", new Achievement("Millionaire", "💰", "Have 1,000,000 coins", 1000) },
        { "fisher", new Achievement("Master Fisher", "🎣", "Catch 100 fish", 500) },
        { "collector", new Achievement("Collector", "📚", "Own 10 different items", 200) },
        { "craftsman", new Achievement("Master Craftsman", "⚒️", "Craft 50 items", 300) },
        { "hunter", new Achievement("Expert Hunter", "🏹", "Hunt 50 animals", 400) }
    };

    public PetsAndAchievementsCommands(DatabaseService db)
    {
        _db = db;
    }

    [Command("pets")]
    [Summary("View available pets")]
    public async Task ShowPets()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🐾 Available Pets")
            .WithColor(Color.Gold);

        foreach (var pet in _pets)
        {
            embed.AddField(
                $"{pet.Value.Emoji} {pet.Value.Name}",
                $"Price: ${pet.Value.Price}\nFood: {string.Join(", ", pet.Value.FoodTypes)}",
                true
            );
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("buypet")]
    [Summary("Buy a pet")]
    public async Task BuyPet(string petName)
    {
        var user = _db.GetAccount(Context.User.Id);
        var normalizedName = petName.ToLower();

        if (!_pets.ContainsKey(normalizedName))
        {
            await ReplyAsync("❌ Pet not found! Use !pets to see available pets.");
            return;
        }

        var pet = _pets[normalizedName];
        if (user.Balance < pet.Price)
        {
            await ReplyAsync("❌ You don't have enough money!");
            return;
        }

        user.Balance -= pet.Price;
        user.Inventory[$"pet_{normalizedName}"] = 1;
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ You bought a {pet.Emoji} {pet.Name}!");
    }

    [Command("feedpet")]
    [Summary("Feed your pet (Usage: !feedpet <food>)")]
    public async Task FeedPet([Remainder]string foodType)
    {
        var user = _db.GetAccount(Context.User.Id);
        var normalizedFoodType = foodType.ToLower();

        // Find the first pet the user owns
        var ownedPet = user.Inventory
            .FirstOrDefault(x => x.Key.StartsWith("pet_") && x.Value > 0);

        if (ownedPet.Key == null)
        {
            await ReplyAsync("❌ You don't own any pets! Use !pets to see available pets.");
            return;
        }

        string petName = ownedPet.Key.Replace("pet_", "");
        var pet = _pets[petName];

        if (!pet.FoodTypes.Contains(normalizedFoodType))
        {
            await ReplyAsync($"❌ {pet.Name} doesn't eat that!\nAccepted foods: {string.Join(", ", pet.FoodTypes)}");
            return;
        }

        if (!user.Inventory.ContainsKey(normalizedFoodType) || user.Inventory[normalizedFoodType] < 1)
        {
            await ReplyAsync($"❌ You don't have any {foodType}!");
            return;
        }

        user.Inventory[normalizedFoodType]--;
        _db.UpdateAccount(user);

        await ReplyAsync($"🍖 You fed your {pet.Emoji} {pet.Name} some {foodType}!");
    }

    [Command("achievements")]
    [Alias("ach")]
    [Summary("View your achievements")]
    public async Task ShowAchievements()
    {
        var user = _db.GetAccount(Context.User.Id);
        var embed = new EmbedBuilder()
            .WithTitle("🏆 Achievements")
            .WithColor(Color.Gold);

        foreach (var achievement in _achievements)
        {
            bool completed = user.Inventory.GetValueOrDefault($"ach_{achievement.Key}", 0) > 0;
            embed.AddField(
                $"{achievement.Value.Emoji} {achievement.Value.Name}",
                $"{achievement.Value.Description}\nReward: ${achievement.Value.Reward}\n{(completed ? "✅ Completed" : "❌ Incomplete")}",
                false
            );
        }

        await ReplyAsync(embed: embed.Build());
    }

    // Add this to your periodically running code or after relevant actions
    private async Task CheckAchievements(UserAccount user)
    {
        if (user.Balance >= 1000000 && !HasAchievement(user, "rich"))
            await AwardAchievement(user, "rich");

        if (GetTotalFishCaught(user) >= 100 && !HasAchievement(user, "fisher"))
            await AwardAchievement(user, "fisher");

        // Add more achievement checks
    }

    private bool HasAchievement(UserAccount user, string achievementId)
    {
        return user.Inventory.GetValueOrDefault($"ach_{achievementId}", 0) > 0;
    }

    private async Task AwardAchievement(UserAccount user, string achievementId)
    {
        var achievement = _achievements[achievementId];
        user.Inventory[$"ach_{achievementId}"] = 1;
        user.Balance += achievement.Reward;
        _db.UpdateAccount(user);

        await ReplyAsync($"🎉 Achievement Unlocked: {achievement.Emoji} {achievement.Name}\nYou earned ${achievement.Reward}!");
    }

    private int GetTotalFishCaught(UserAccount user)
    {
        return user.Inventory.Where(x => x.Key.Contains("fish")).Sum(x => x.Value);
    }
}

public class PetInfo
{
    public string Name { get; }
    public string Emoji { get; }
    public int Price { get; }
    public string[] FoodTypes { get; }

    public PetInfo(string name, string emoji, int price, string[] foodTypes)
    {
        Name = name;
        Emoji = emoji;
        Price = price;
        FoodTypes = foodTypes;
    }
}

public class Achievement
{
    public string Name { get; }
    public string Emoji { get; }
    public string Description { get; }
    public int Reward { get; }

    public Achievement(string name, string emoji, string description, int reward)
    {
        Name = name;
        Emoji = emoji;
        Description = description;
        Reward = reward;
    }
}
