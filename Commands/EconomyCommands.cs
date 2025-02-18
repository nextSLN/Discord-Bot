using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Models;

public class EconomyCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private readonly ShopCommands _shop;
    private static readonly Random _random = new Random();

    private static readonly Dictionary<string, JobInfo> _jobs = new()
    {
        { "intern", new JobInfo("Intern", 100, 300, null) },
        { "cashier", new JobInfo("Cashier", 150, 400, null) },
        { "programmer", new JobInfo("Programmer", 500, 1500, "diploma_cs") },
        { "doctor", new JobInfo("Doctor", 1000, 3000, "degree_medical") },
        { "lawyer", new JobInfo("Lawyer", 800, 2500, "degree_law") },
        { "ceo", new JobInfo("CEO", 2000, 5000, "degree_business") }
    };

    private static readonly Dictionary<string, EducationInfo> _education = new()
    {
        { "diploma_cs", new EducationInfo("Computer Science Diploma", 5000, 70) },
        { "diploma_business", new EducationInfo("Business Diploma", 5000, 75) },
        { "degree_medical", new EducationInfo("Medical Degree", 20000, 60) },
        { "degree_law", new EducationInfo("Law Degree", 20000, 65) },
        { "degree_business", new EducationInfo("MBA", 20000, 70) }
    };

    public EconomyCommands(DatabaseService db, ShopCommands shop)
    {
        _db = db;
        _shop = shop;
    }

    [Command("balance")]
    [Alias("bal")]
    [Summary("Check your wallet and bank balance")]
    public async Task Balance()
    {
        var user = _db.GetAccount(Context.User.Id);
        var embed = new EmbedBuilder()
            .WithTitle("💰 Balance")
            .WithColor(Color.Green)
            .AddField("Wallet", $"${user.Balance:N0}", true)
            .AddField("Bank", $"${user.BankBalance:N0}", true)
            .Build();
        await ReplyAsync(embed: embed);
    }

    [Command("daily")]
    [Summary("Claim your daily reward (Every 24 hours)")]
    public async Task Daily()
    {
        var user = _db.GetAccount(Context.User.Id);
        if (DateTime.UtcNow - user.LastDaily < TimeSpan.FromDays(1))
        {
            var timeLeft = TimeSpan.FromDays(1) - (DateTime.UtcNow - user.LastDaily);
            await ReplyAsync($"⏰ Wait {timeLeft.Hours}h {timeLeft.Minutes}m before claiming daily reward again!");
            return;
        }

        int amount = 100;
        user.Balance += amount;
        user.LastDaily = DateTime.UtcNow;
        _db.UpdateAccount(user);
        await ReplyAsync($"💵 You claimed ${amount:N0} daily reward!");
    }

    [Command("work")]
    [Summary("Work to earn money (Every 1 hour)")]
    public new async Task Work()
    {
        var user = _db.GetAccount(Context.User.Id);
        
        if (string.IsNullOrEmpty(user.CurrentJob))
        {
            await ReplyAsync("❌ You need to get a job first! Use !jobs to see available positions.");
            return;
        }

        if (DateTime.UtcNow - user.LastWorked < TimeSpan.FromHours(1))
        {
            var timeLeft = TimeSpan.FromHours(1) - (DateTime.UtcNow - user.LastWorked);
            await ReplyAsync($"⏰ You need to rest for {timeLeft.Minutes} minutes!");
            return;
        }

        var job = _jobs[user.CurrentJob];
        int pay = _random.Next(job.MinPay, job.MaxPay + 1);
        user.Balance += pay;
        user.LastWorked = DateTime.UtcNow;
        _db.UpdateAccount(user);

        await ReplyAsync($"💼 You worked as a {job.Name} and earned ${pay:N0}!");
    }

    [Command("rob")]
    [Summary("Attempt to rob another user")]
    public async Task Rob([Remainder] string targetUser)
    {
        // Try to find user by mention or username
        var target = Context.Message.MentionedUsers.FirstOrDefault() as SocketUser ??
                    Context.Guild.Users.FirstOrDefault(x => 
                        x.Username.Equals(targetUser, StringComparison.OrdinalIgnoreCase) ||
                        (x.Username + "#" + x.Discriminator).Equals(targetUser, StringComparison.OrdinalIgnoreCase)) as SocketUser;

        if (target == null)
        {
            await ReplyAsync("❌ User not found! Make sure to @mention them or use their exact username.");
            return;
        }

        if (target.Id == Context.User.Id)
        {
            await ReplyAsync("❌ You can't rob yourself!");
            return;
        }

        var robber = _db.GetAccount(Context.User.Id);
        var victim = _db.GetAccount(target.Id);

        if (victim.HasPassive)
        {
            await ReplyAsync("❌ This user is in passive mode!");
            return;
        }

        if (DateTime.UtcNow - robber.LastRobbed < TimeSpan.FromHours(1))
        {
            var timeLeft = TimeSpan.FromHours(1) - (DateTime.UtcNow - robber.LastRobbed);
            await ReplyAsync($"⏰ Wait {timeLeft.Minutes}m {timeLeft.Seconds}s before robbing again!");
            return;
        }

        if (victim.Balance < 100)
        {
            await ReplyAsync("❌ This user is too poor to rob!");
            return;
        }

        bool success = _random.Next(100) < 40; // 40% success rate
        if (success)
        {
            int amount = _random.Next(100, Math.Min(1000, victim.Balance));
            robber.Balance += amount;
            victim.Balance -= amount;
            await ReplyAsync($"💰 You successfully robbed ${amount:N0} from {target.Username}!");
        }
        else
        {
            int fine = _random.Next(100, 500);
            robber.Balance -= fine;
            await ReplyAsync($"🚔 You got caught and paid ${fine:N0} in fines!");
        }

        robber.LastRobbed = DateTime.UtcNow;
        _db.UpdateAccount(robber);
        _db.UpdateAccount(victim);
    }

    [Command("give")]
    [Summary("Give money to another user")]
    public async Task Give(IUser target, int amount)
    {
        if (amount <= 0)
        {
            await ReplyAsync("❌ Amount must be positive!");
            return;
        }

        var giver = _db.GetAccount(Context.User.Id);
        var receiver = _db.GetAccount(target.Id);

        if (giver.Balance < amount)
        {
            await ReplyAsync("❌ You don't have enough money!");
            return;
        }

        giver.Balance -= amount;
        receiver.Balance += amount;
        _db.UpdateAccount(giver);
        _db.UpdateAccount(receiver);

        await ReplyAsync($"💸 You gave ${amount} to {target.Username}!");
    }

    [Command("deposit")]
    [Alias("dep")]
    [Summary("Deposit money into your bank. Usage: !deposit <amount/all>")]
    public async Task Deposit([Remainder] string amount)
    {
        var user = _db.GetAccount(Context.User.Id);
        
        if (DateTime.UtcNow - user.LastDeposit < TimeSpan.FromMinutes(1))
        {
            await ReplyAsync("⏰ Wait 1 minute between deposits!");
            return;
        }

        int depositAmount;
        if (amount.ToLower() == "all")
            depositAmount = user.Balance;
        else if (!int.TryParse(amount, out depositAmount) || depositAmount <= 0)
        {
            await ReplyAsync("❌ Please enter a valid amount!");
            return;
        }

        if (user.Balance < depositAmount)
        {
            await ReplyAsync("❌ You don't have that much money!");
            return;
        }

        if (user.BankBalance + depositAmount > user.MaxBankBalance)
        {
            await ReplyAsync($"❌ Your bank can only hold ${user.MaxBankBalance:N0}! Upgrade it with !upgradebank");
            return;
        }

        user.Balance -= depositAmount;
        user.BankBalance += depositAmount;
        user.LastDeposit = DateTime.UtcNow;
        _db.UpdateAccount(user);

        await ReplyAsync($"💰 Deposited ${depositAmount:N0} to your bank!");
    }

    [Command("withdraw")]
    [Alias("with")]
    [Summary("Withdraw money from your bank. Usage: !withdraw <amount/all>")]
    public async Task Withdraw([Remainder] string amount)
    {
        var user = _db.GetAccount(Context.User.Id);

        if (DateTime.UtcNow - user.LastWithdraw < TimeSpan.FromMinutes(1))
        {
            await ReplyAsync("⏰ Wait 1 minute between withdrawals!");
            return;
        }

        int withdrawAmount;
        if (amount.ToLower() == "all")
            withdrawAmount = user.BankBalance;
        else if (!int.TryParse(amount, out withdrawAmount) || withdrawAmount <= 0)
        {
            await ReplyAsync("❌ Please enter a valid amount!");
            return;
        }

        if (user.BankBalance < withdrawAmount)
        {
            await ReplyAsync("❌ You don't have that much money in the bank!");
            return;
        }

        user.BankBalance -= withdrawAmount;
        user.Balance += withdrawAmount;
        user.LastWithdraw = DateTime.UtcNow;
        _db.UpdateAccount(user);

        await ReplyAsync($"💰 Withdrew ${withdrawAmount:N0} from your bank!");
    }

    [Command("upgradebank")]
    [Summary("Upgrade your bank capacity")]
    public async Task UpgradeBank()
    {
        var user = _db.GetAccount(Context.User.Id);
        int upgradeCost = user.BankLevel * 5000;

        if (user.Balance < upgradeCost)
        {
            await ReplyAsync($"❌ Bank upgrade costs ${upgradeCost:N0}!");
            return;
        }

        user.Balance -= upgradeCost;
        user.BankLevel++;
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ Bank upgraded! New capacity: ${user.MaxBankBalance:N0}");
    }

    [Command("passive")]
    [Summary("Toggle passive mode to prevent robberies (2 hour cooldown)")]
    public async Task TogglePassive()
    {
        var user = _db.GetAccount(Context.User.Id);

        if (user.HasPassive)
        {
            if (DateTime.UtcNow < user.PassiveUntil)
            {
                var timeLeft = user.PassiveUntil - DateTime.UtcNow;
                await ReplyAsync($"⏰ Passive mode ends in {timeLeft.Hours}h {timeLeft.Minutes}m");
                return;
            }
            user.HasPassive = false;
            await ReplyAsync("🛡️ Passive mode disabled!");
        }
        else
        {
            user.HasPassive = true;
            user.PassiveUntil = DateTime.UtcNow.AddHours(2);
            await ReplyAsync("🛡️ Passive mode enabled for 2 hours!");
        }

        _db.UpdateAccount(user);
    }

    [Command("education")]
    [Summary("View or enroll in education programs")]
    public async Task Education([Remainder] string program = null)
    {
        if (string.IsNullOrEmpty(program))
        {
            // Show education programs list
            var embed = new EmbedBuilder()
                .WithTitle("🎓 Education Programs")
                .WithColor(Color.Blue);

            foreach (var eduProgram in _education)
            {
                embed.AddField(
                    eduProgram.Value.Name,
                    $"Cost: ${eduProgram.Value.Cost:N0}\n" +
                    $"Success Rate: {eduProgram.Value.SuccessRate}%",
                    false
                );
            }

            await ReplyAsync(embed: embed.Build());
            return;
        }

        // Handle enrollment
        string programKey = program.ToLower() switch
        {
            "cs" or "computer science" => "diploma_cs",
            "business" => "diploma_business",
            "medical" => "degree_medical",
            "law" => "degree_law",
            "mba" => "degree_business",
            _ => null
        };

        if (programKey == null)
        {
            await ReplyAsync("❌ Invalid program! Use !education to see available programs.");
            return;
        }

        var selectedProgram = _education[programKey];
        var user = _db.GetAccount(Context.User.Id);

        if (user.Balance < selectedProgram.Cost)
        {
            await ReplyAsync($"❌ You need ${selectedProgram.Cost:N0} to enroll!");
            return;
        }

        user.Balance -= selectedProgram.Cost;

        // Check if study was successful
        bool success = _random.Next(100) < selectedProgram.SuccessRate;
        if (success)
        {
            user.Education = programKey;
            await ReplyAsync($"🎓 Congratulations! You've enrolled in {selectedProgram.Name}!");
        }
        else
        {
            user.Balance += selectedProgram.Cost / 2; // Refund half the cost
            await ReplyAsync($"📚 Failed to enroll in {selectedProgram.Name}. Half of your tuition was refunded.");
        }

        _db.UpdateAccount(user);
    }

    [Command("study")]
    [Summary("Study your enrolled program")]
    public async Task Study()
    {
        var user = _db.GetAccount(Context.User.Id);

        if (string.IsNullOrEmpty(user.Education))
        {
            await ReplyAsync("❌ You need to enroll in a program first! Use !education to see available programs.");
            return;
        }

        if (DateTime.UtcNow - user.LastStudied < TimeSpan.FromHours(1))
        {
            var timeLeft = TimeSpan.FromHours(1) - (DateTime.UtcNow - user.LastStudied);
            await ReplyAsync($"⏰ You need to rest for {timeLeft.Minutes} minutes before studying again!");
            return;
        }

        // Study progress
        user.StudyProgress += _random.Next(15, 26); // 15-25% progress
        user.LastStudied = DateTime.UtcNow;

        if (user.StudyProgress >= 100)
        {
            user.Inventory[user.Education] = 1;
            user.Education = null;
            user.StudyProgress = 0;
            await ReplyAsync("🎓 Congratulations! You've completed your education program!");
        }
        else
        {
            await ReplyAsync($"📚 You studied hard! Progress: {user.StudyProgress}%");
        }

        _db.UpdateAccount(user);
    }

    [Command("jobs")]
    [Summary("View available jobs")]
    public async Task ShowJobs()
    {
        var embed = new EmbedBuilder()
            .WithTitle("💼 Available Jobs")
            .WithColor(Color.Blue);

        foreach (var job in _jobs)
        {
            string req = job.Value.RequiredEducation == null ? "None" : 
                _education[job.Value.RequiredEducation].Name;
            embed.AddField(job.Value.Name, 
                $"Pay: ${job.Value.MinPay:N0} - ${job.Value.MaxPay:N0}\n" +
                $"Required Education: {req}", false);
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("apply")]
    [Summary("Apply for a job")]
    public async Task ApplyJob(string jobName)
    {
        var user = _db.GetAccount(Context.User.Id);
        
        if (!_jobs.TryGetValue(jobName.ToLower(), out var job))
        {
            await ReplyAsync("❌ Job not found! Use !jobs to see available positions.");
            return;
        }

        if (job.RequiredEducation != null)
        {
            var hasRequiredEducation = user.Inventory.ContainsKey(job.RequiredEducation);
            if (!hasRequiredEducation)
            {
                var eduName = _education[job.RequiredEducation].Name;
                await ReplyAsync($"❌ You need {eduName} for this job!");
                return;
            }
        }

        user.CurrentJob = jobName.ToLower();
        _db.UpdateAccount(user);

        await ReplyAsync($"💼 Congratulations! You are now working as a {job.Name}!");
    }

    [Command("sell")]
    [Summary("Sell items from your inventory")]
    public async Task SellItem(string itemName, int amount = 1)
    {
        var user = _db.GetAccount(Context.User.Id);
        
        if (!user.Inventory.ContainsKey(itemName))
        {
            await ReplyAsync("❌ You don't have this item!");
            return;
        }

        if (user.Inventory[itemName] < amount)
        {
            await ReplyAsync("❌ You don't have enough of this item!");
            return;
        }

        // Calculate sell value (50% of buy price if it's a shop item)
        int value = 0;
        var shopItem = ShopCommands.GetShopCategories().SelectMany(c => c.Value.Items)
            .FirstOrDefault(i => i.Key == itemName).Value;
        
        if (shopItem != null)
            value = shopItem.Price / 2;
        else if (ShopCommands.GetCatchableItems().Any(c => c.Value.ContainsKey(itemName)))
            value = ShopCommands.GetCatchableItems().SelectMany(c => c.Value)
                .First(i => i.Key == itemName).Value.Value;
        else
            value = 100; // Default value for other items

        int totalValue = value * amount;
        user.Inventory[itemName] -= amount;
        user.Balance += totalValue;
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ Sold {amount}x {itemName} for ${totalValue:N0}!");
    }
}

public class EducationInfo
{
    public string Name { get; }
    public int Cost { get; }
    public int SuccessRate { get; }

    public EducationInfo(string name, int cost, int successRate)
    {
        Name = name;
        Cost = cost;
        SuccessRate = successRate;
    }
}
