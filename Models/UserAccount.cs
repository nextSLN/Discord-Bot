namespace DiscordBot.Models
{
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
        public int BankLevel { get; set; } = 1;
        public int MaxBankBalance => BankLevel * 100000;
        public bool HasPassive { get; set; } = false;
        public DateTime PassiveUntil { get; set; } = DateTime.MinValue;
        public DateTime LuckCharmExpiry { get; set; } = DateTime.MinValue;
        public DateTime XPBoostExpiry { get; set; } = DateTime.MinValue;
        public string CurrentJob { get; set; } = "";
        public string Education { get; set; }
        public int StudyProgress { get; set; }
        public DateTime LastStudied { get; set; } = DateTime.MinValue;
    }
}
