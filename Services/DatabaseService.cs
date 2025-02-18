using System.Collections.Concurrent;
using System.Text.Json;
using System.IO;
using DiscordBot.Models;

public class DatabaseService
{
    private readonly string _economyPath = "economy.json";
    private ConcurrentDictionary<ulong, UserAccount> _accounts;

    public DatabaseService()
    {
        _accounts = LoadAccounts();
    }

    private ConcurrentDictionary<ulong, UserAccount> LoadAccounts()
    {
        if (File.Exists(_economyPath))
        {
            var json = File.ReadAllText(_economyPath);
            return JsonSerializer.Deserialize<ConcurrentDictionary<ulong, UserAccount>>(json) 
                   ?? new ConcurrentDictionary<ulong, UserAccount>();
        }
        return new ConcurrentDictionary<ulong, UserAccount>();
    }

    public void SaveAccounts()
    {
        var json = JsonSerializer.Serialize(_accounts);
        File.WriteAllText(_economyPath, json);
    }

    public UserAccount GetAccount(ulong userId)
    {
        return _accounts.GetOrAdd(userId, id => new UserAccount { UserId = id });
    }

    public void UpdateAccount(UserAccount account)
    {
        _accounts[account.UserId] = account;
        SaveAccounts();
    }

    private void SaveUserAccount(UserAccount account)
    {
        var data = new Dictionary<string, object>
        {
            ["UserId"] = account.UserId,
            ["Balance"] = account.Balance,
            ["BankBalance"] = account.BankBalance,
            ["LastDaily"] = account.LastDaily,
            ["LastWorked"] = account.LastWorked,
            ["LastRobbed"] = account.LastRobbed,
            ["LastFished"] = account.LastFished,
            ["LastDeposit"] = account.LastDeposit,
            ["LastWithdraw"] = account.LastWithdraw,
            ["Inventory"] = account.Inventory,
            ["BankLevel"] = account.BankLevel,
            ["HasPassive"] = account.HasPassive,
            ["PassiveUntil"] = account.PassiveUntil,
            ["LuckCharmExpiry"] = account.LuckCharmExpiry,
            ["XPBoostExpiry"] = account.XPBoostExpiry,
            ["CurrentJob"] = account.CurrentJob,
            ["Education"] = account.Education,
            ["StudyProgress"] = account.StudyProgress,
            ["LastStudied"] = account.LastStudied
        };
        var json = JsonSerializer.Serialize(data);
        File.WriteAllText($"{account.UserId}.json", json);
    }

    private UserAccount LoadUserAccount(ulong id)
    {
        if (File.Exists($"{id}.json"))
        {
            var json = File.ReadAllText($"{id}.json");
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            var account = new UserAccount
            {
                UserId = id,
                Balance = Convert.ToInt32(data["Balance"]),
                BankBalance = Convert.ToInt32(data["BankBalance"]),
                LastDaily = DateTime.Parse(data["LastDaily"].ToString()),
                LastWorked = DateTime.Parse(data["LastWorked"].ToString()),
                LastRobbed = DateTime.Parse(data["LastRobbed"].ToString()),
                LastFished = DateTime.Parse(data["LastFished"].ToString()),
                LastDeposit = DateTime.Parse(data["LastDeposit"].ToString()),
                LastWithdraw = DateTime.Parse(data["LastWithdraw"].ToString()),
                Inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(data["Inventory"].ToString()),
                BankLevel = Convert.ToInt32(data["BankLevel"]),
                HasPassive = Convert.ToBoolean(data["HasPassive"]),
                PassiveUntil = DateTime.Parse(data["PassiveUntil"].ToString()),
                LuckCharmExpiry = DateTime.Parse(data["LuckCharmExpiry"].ToString()),
                XPBoostExpiry = DateTime.Parse(data["XPBoostExpiry"].ToString()),
                CurrentJob = data["CurrentJob"].ToString(),
                Education = data.GetValueOrDefault("Education")?.ToString(),
                StudyProgress = data.GetValueOrDefault("StudyProgress") != null 
                    ? Convert.ToInt32(data["StudyProgress"]) 
                    : 0,
                LastStudied = data.GetValueOrDefault("LastStudied") != null 
                    ? DateTime.Parse(data["LastStudied"].ToString()) 
                    : DateTime.MinValue
            };
            return account;
        }
        return new UserAccount { UserId = id };
    }
}

public class UserAccount
{
    public ulong UserId { get; set; }
    public int Balance { get; set; } = 100;
    public int BankBalance { get; set; } = 0;
    public DateTime LastDaily { get; set; } = DateTime.MinValue;
    public DateTime LastWorked { get; set; } = DateTime.MinValue;
    public DateTime LastRobbed { get; set; } = DateTime.MinValue;
    public DateTime LastFished { get; set; } = DateTime.MinValue;
    public DateTime LastDeposit { get; set; } = DateTime.MinValue;
    public DateTime LastWithdraw { get; set; } = DateTime.MinValue;
    public Dictionary<string, int> Inventory { get; set; } = new();
    public int BankLevel { get; set; } = 1;  // New property for bank upgrades
    public int MaxBankBalance => BankLevel * 100000;  // Maximum bank storage based on level
    public bool HasPassive { get; set; } = false;  // Passive mode to prevent robberies
    public DateTime PassiveUntil { get; set; } = DateTime.MinValue;
    public DateTime LuckCharmExpiry { get; set; } = DateTime.MinValue;
    public DateTime XPBoostExpiry { get; set; } = DateTime.MinValue;
    public string CurrentJob { get; set; } = "";
    public string Education { get; set; }  // New property for education
    public int StudyProgress { get; set; }  // New property for study progress
    public DateTime LastStudied { get; set; } = DateTime.MinValue;  // New property for last studied date
}

public class JobInfo
{
    public string Name { get; }
    public int MinPay { get; }
    public int MaxPay { get; }
    public string RequiredEducation { get; }

    public JobInfo(string name, int minPay, int maxPay, string requiredEducation)
    {
        Name = name;
        MinPay = minPay;
        MaxPay = maxPay;
        RequiredEducation = requiredEducation;
    }
}
