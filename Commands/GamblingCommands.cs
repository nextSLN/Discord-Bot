using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

public class GamblingCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();
    
    // Move these to static fields
    private static ulong _currentJackpot = 0;
    private static readonly List<ulong> _jackpotParticipants = new();
    private static readonly System.Timers.Timer _jackpotTimer = new(300000); // 5 minutes

    private static readonly Dictionary<string, double> _crypto = new()
    {
        { "btc", 40000.0 }, { "eth", 2500.0 }, { "doge", 0.1 }
    };

    private static readonly Dictionary<string, Team> _teams = new()
    {
        { "Red Dragons", new Team("Red Dragons", 1.5) },     
        { "Blue Knights", new Team("Blue Knights", 1.8) },   
        { "Golden Eagles", new Team("Golden Eagles", 2.0) }, 
        { "Shadow Wolves", new Team("Shadow Wolves", 2.1) }, 
        { "Phoenix Rise", new Team("Phoenix Rise", 2.2) },
        { "Thunder Lions", new Team("Thunder Lions", 2.3) },
        { "Silver Hawks", new Team("Silver Hawks", 2.4) },   
        { "Crystal Tigers", new Team("Crystal Tigers", 2.7) },
        { "Star Raiders", new Team("Star Raiders", 3.0) },
        { "Storm Giants", new Team("Storm Giants", 3.2) },
        { "Night Owls", new Team("Night Owls", 3.5) },
        { "Fire Foxes", new Team("Fire Foxes", 3.8) }
    };

    private static readonly System.Timers.Timer _championshipTimer = new(1200000); // 20 minutes
    private static readonly System.Timers.Timer _championshipEndTimer = new(3600000); // 1 hour
    private static bool _championshipActive = false;
    private static readonly Dictionary<ulong, ChampionshipBet> _championshipBets = new();
    private static readonly Dictionary<string, int> _championshipWins = new();
    private static readonly string[] _matchEvents = {
        "⚽ {0} scores a goal!",
        "🏃 Great run by {0}!",
        "🔴 Red card shown to {0}!",
        "🟡 Yellow card to {0}",
        "🎯 Penalty awarded to {0}!",
        "🥅 Amazing save by {0}'s goalkeeper!",
        "📺 VAR check in progress...",
        "🔄 Substitution for {0}",
        "🚑 Injury concern for {0}",
        "🎯 Free kick in dangerous position for {0}"
    };

    // Add this field to track match history
    private static readonly List<(string Team1, string Team2, int Score1, int Score2)> _matchHistory = new();

    // Add these fields at the top of the class
    private static bool _isTransitioning = false;
    private static readonly object _championshipLock = new();

    public GamblingCommands(DatabaseService db)
    {
        _db = db;
        
        _championshipTimer.Interval = 30000; // 30 seconds
        _championshipEndTimer.Interval = 300000; // 5 minutes
        _championshipTimer.AutoReset = true;
        _championshipEndTimer.AutoReset = false;
        
        // Don't add event handlers here
        _ = StartChampionship();
    }

    [Command("slots")]
    public async Task Slots(int bet)
    {
        if (bet < 1) 
        {
            await ReplyAsync("❌ Minimum bet is $1!");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ You don't have enough money!");
            return;
        }

        string[] emotes = { "🍎", "🍋", "🍒", "💎", "7️⃣" };
        var slot1 = _random.Next(emotes.Length);
        var slot2 = _random.Next(emotes.Length);
        var slot3 = _random.Next(emotes.Length);

        int winnings = 0;
        if (slot1 == slot2 && slot2 == slot3) winnings = bet * 5;
        else if (slot1 == slot2 || slot2 == slot3) winnings = bet * 2;

        user.Balance += (winnings - bet);
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🎰 Slots")
            .WithDescription($"{emotes[slot1]} | {emotes[slot2]} | {emotes[slot3]}")
            .WithColor(winnings > 0 ? Color.Green : Color.Red)
            .AddField("Bet", $"${bet:N0}", true)
            .AddField("Winnings", $"${winnings:N0}", true)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("coinflip")]
    [Alias("cf")]
    [Summary("Flip a coin. Usage: !coinflip <heads/tails> [bet amount]")]
    public async Task CoinFlip(string choice, int bet = 10)
    {
        choice = choice.ToLower();
        if (choice != "heads" && choice != "tails")
        {
            await ReplyAsync("❌ Please choose heads or tails!");
            return;
        }

        if (bet < 1)
        {
            await ReplyAsync("❌ Minimum bet is $1!");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ You don't have enough money!");
            return;
        }

        bool isHeads = _random.Next(2) == 0;
        bool won = (isHeads && choice == "heads") || (!isHeads && choice == "tails");
        
        user.Balance += won ? bet : -bet;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🪙 Coinflip")
            .WithColor(won ? Color.Green : Color.Red)
            .AddField("Result", isHeads ? "Heads" : "Tails", true)
            .AddField("Bet", $"${bet:N0}", true)
            .AddField("Outcome", won ? $"You won ${bet:N0}!" : $"You lost ${bet:N0}!", false)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("roulette")]
    [Summary("Play roulette. Usage: !roulette <bet> <choice>")]
    public async Task Roulette(int bet, string choice)
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        choice = choice.ToLower();
        string[] validChoices = { "red", "black", "green" };
        if (!validChoices.Contains(choice))
        {
            await ReplyAsync("❌ Choose red, black, or green!");
            return;
        }

        int roll = _random.Next(37); // 0-36
        string result = roll == 0 ? "green" : (roll % 2 == 0 ? "red" : "black");
        int multiplier = result == "green" ? 14 : 2;
        bool won = choice == result;

        user.Balance += won ? bet * multiplier - bet : -bet;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🎲 Roulette")
            .WithDescription($"Ball landed on: {result} ({roll})")
            .AddField("Result", won ? $"Won ${bet * multiplier}!" : $"Lost ${bet}")
            .WithColor(won ? Color.Green : Color.Red)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("crash")]
    [Summary("Play crash game. Usage: !crash <bet>")]
    public async Task Crash(int bet)
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        double multiplier = 1.0;
        var message = await ReplyAsync("📈 Game starting at 1.00x...");

        while (_random.NextDouble() > 0.15 && multiplier < 10.0) // 15% chance to crash each tick
        {
            await Task.Delay(1000);
            multiplier += 0.2;
            await message.ModifyAsync(m => m.Content = $"📈 Multiplier: {multiplier:F2}x");
        }

        user.Balance -= bet;
        int winnings = (int)(bet * multiplier);
        user.Balance += winnings;
        _db.UpdateAccount(user);

        await ReplyAsync($"💥 Crashed at {multiplier:F2}x! {(winnings > bet ? $"Won ${winnings - bet}!" : $"Lost ${bet - winnings}")}");
    }

    [Command("lottery")]
    [Summary("Buy a lottery ticket for $100")]
    public async Task Lottery()
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < 100)
        {
            await ReplyAsync("❌ Lottery ticket costs $100!");
            return;
        }

        user.Balance -= 100;
        var numbers = new int[3];
        for (int i = 0; i < 3; i++)
            numbers[i] = _random.Next(1, 10);

        int matches = numbers.Distinct().Count();
        int prize = matches switch
        {
            1 => 50,    // All different numbers
            2 => 200,   // Two matching numbers
            3 => 1000   // All matching numbers
        };

        user.Balance += prize;

        _db.UpdateAccount(user);

        await ReplyAsync($"🎰 Numbers: {string.Join(" ", numbers)}\n" +
                        $"{(prize > 100 ? $"Won ${prize}!" : $"Lost ${100 - prize}")}");
    }

    [Command("dice")]
    [Summary("Roll dice against the house. Usage: !dice <bet> <number 1-6>")]
    public async Task DiceGame(int bet, int guess)
    {
        if (guess < 1 || guess > 6)
        {
            await ReplyAsync("❌ Choose a number between 1 and 6!");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        int roll = _random.Next(1, 7);
        bool won = roll == guess;
        int winnings = won ? bet * 5 : -bet;

        user.Balance += winnings;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🎲 Dice Game")
            .WithDescription($"Rolled: {roll}\nYour guess: {guess}")
            .AddField(won ? "You won!" : "You lost!", $"${Math.Abs(winnings):N0}")
            .WithColor(won ? Color.Green : Color.Red)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("scratchy")]
    [Summary("Buy a scratch ticket for $50")]
    public async Task ScratchTicket()
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < 50)
        {
            await ReplyAsync("❌ Scratch tickets cost $50!");
            return;
        }

        user.Balance -= 50;
        string[,] grid = new string[3, 3];
        int matches = 0;

        // Fill grid with random symbols
        string[] symbols = { "💎", "🍀", "🎰", "💰", "🎲" };
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                grid[i, j] = symbols[_random.Next(symbols.Length)];

        // Check for matches
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] == grid[i, 1] && grid[i, 1] == grid[i, 2]) matches++;
            if (grid[0, i] == grid[1, i] && grid[1, i] == grid[2, i]) matches++;
        }

        // Diagonal matches
        if (grid[0, 0] == grid[1, 1] && grid[1, 1] == grid[2, 2]) matches++;
        if (grid[0, 2] == grid[1, 1] && grid[1, 1] == grid[2, 0]) matches++;

        int prize = matches switch
        {
            0 => 0,
            1 => 100,
            2 => 250,
            3 => 500,
            _ => 1000
        };

        user.Balance += prize;
        _db.UpdateAccount(user);

        var display = "";
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
                display += grid[i, j] + " ";
            display += "\n";
        }

        var embed = new EmbedBuilder()
            .WithTitle("🎫 Scratch Ticket")
            .WithDescription(display)
            .AddField("Lines Matched", matches)
            .AddField("Prize", $"${prize:N0}")
            .WithColor(prize > 0 ? Color.Green : Color.Red)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("highlow")]
    [Summary("Guess if the next number will be higher or lower. Usage: !highlow <bet> <high/low>")]
    public async Task HighLow(int bet, string guess)
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        guess = guess.ToLower();
        if (guess != "high" && guess != "low")
        {
            await ReplyAsync("❌ Choose 'high' or 'low'!");
            return;
        }

        int first = _random.Next(1, 101);
        int second = _random.Next(1, 101);
        bool guessedRight = (guess == "high" && second > first) || 
                           (guess == "low" && second < first);

        int winnings = guessedRight ? bet : -bet;
        user.Balance += winnings;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🎲 High/Low Game")
            .WithDescription($"First number: {first}\nSecond number: {second}")
            .AddField("Your guess", guess)
            .AddField(guessedRight ? "You won!" : "You lost!", $"${Math.Abs(winnings):N0}")
            .WithColor(guessedRight ? Color.Green : Color.Red)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("jackpot")]
    [Summary("View or join the current jackpot pool. Usage: !jackpot [bet]")]
    public async Task Jackpot(int? bet = null)
    {
        if (!bet.HasValue)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎰 Current Jackpot")
                .WithDescription($"Prize pool: ${_currentJackpot:N0}")
                .AddField("Participants", _jackpotParticipants.Count)
                .Build();

            await ReplyAsync(embed: embed);
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet.Value)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        user.Balance -= bet.Value;
        _currentJackpot += (ulong)bet.Value;
        _jackpotParticipants.Add(Context.User.Id);
        _db.UpdateAccount(user);

        if (!_jackpotTimer.Enabled)
        {
            _jackpotTimer.Elapsed += async (s, e) => await EndJackpot();
            _jackpotTimer.Start();
        }

        await ReplyAsync($"✅ You joined the jackpot with ${bet.Value:N0}!");
    }

    private async Task EndJackpot()
    {
        if (_jackpotParticipants.Count == 0) return;

        int winnerIndex = _random.Next(_jackpotParticipants.Count);
        ulong winnerId = _jackpotParticipants[winnerIndex];
        var winner = _db.GetAccount(winnerId);

        winner.Balance += (int)_currentJackpot;
        _db.UpdateAccount(winner);

        var channel = Context.Channel;
        await channel.SendMessageAsync($"🎉 <@{winnerId}> won the jackpot of ${_currentJackpot:N0}!");

        _currentJackpot = 0;
        _jackpotParticipants.Clear();
        _jackpotTimer.Stop();
    }

    [Command("crypto")]
    [Summary("View crypto prices")]
    public async Task ShowCrypto()
    {
        var embed = new EmbedBuilder()
            .WithTitle("💰 Crypto Prices")
            .WithColor(Color.Gold);

        foreach (var (coin, price) in _crypto)
        {
            double change = (_random.NextDouble() * 10) - 5; // -5% to +5%
            _crypto[coin] = Math.Max(price * (1 + change/100), 0.01);
            
            embed.AddField(coin.ToUpper(), 
                $"${_crypto[coin]:N2}\n{(change >= 0 ? "📈" : "📉")} {change:N2}%",
                true);
        }

        await ReplyAsync(embed: embed.Build());  // Add .Build()
    }

    [Command("buy_crypto")]
    [Summary("Buy cryptocurrency")]
    public async Task BuyCrypto(string coin, double amount)
    {
        var user = _db.GetAccount(Context.User.Id);
        coin = coin.ToLower();

        if (!_crypto.ContainsKey(coin))
        {
            await ReplyAsync("❌ Invalid cryptocurrency! Use !crypto to see available options.");
            return;
        }

        double cost = amount * _crypto[coin];
        if (user.Balance < cost)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        user.Balance -= (int)cost;
        string invKey = $"crypto_{coin}";
        user.Inventory[invKey] = user.Inventory.GetValueOrDefault(invKey, 0) + (int)(amount * 1000000); // Store as millionths
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ Bought {amount} {coin.ToUpper()} for ${cost:N2}!");
    }

    [Command("sell_crypto")]
    [Summary("Sell cryptocurrency")]
    public async Task SellCrypto(string coin, double amount)
    {
        var user = _db.GetAccount(Context.User.Id);
        coin = coin.ToLower();
        string invKey = $"crypto_{coin}";

        if (!user.Inventory.ContainsKey(invKey) || user.Inventory[invKey] < amount * 1000000)
        {
            await ReplyAsync("❌ You don't have enough crypto!");
            return;
        }

        double value = amount * _crypto[coin];
        user.Balance += (int)value;
        user.Inventory[invKey] -= (int)(amount * 1000000);
        _db.UpdateAccount(user);

        await ReplyAsync($"✅ Sold {amount} {coin.ToUpper()} for ${value:N2}!");
    }

    [Command("championship")]
    [Summary("View current championship standings")]
    public async Task ViewChampionship()
    {
        if (!_championshipActive)
        {
            await ReplyAsync("🏆 No championship is currently active! One will start soon.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("⚽ Championship Standings")
            .WithColor(Color.Gold);

        // Order teams by points to show proper standings
        foreach (var team in _teams.Values.OrderByDescending(t => t.Points))
        {
            embed.AddField(
                $"{team.Name}",
                $"Points: {team.Points}\n" +
                $"Championships: {_championshipWins.GetValueOrDefault(team.Name, 0)}\n" +
                $"Form: {string.Join("", team.RecentForm.Select(f => f ? "✅" : "❌").TakeLast(5))}",
                false
            );
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("betteam")]
    [Summary("Bet on a team to win the championship")]
    public async Task BetOnTeam([Remainder]string input)
    {
        try
        {
            var parts = input.Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[^1], out int amount))
            {
                await ReplyAsync("❌ Usage: !betteam <team name> <amount>");
                return;
            }

            string teamName = string.Join(" ", parts.Take(parts.Length - 1));

            if (!_championshipActive)
            {
                await ReplyAsync("❌ No championship is currently active! One will start soon.");
                return;
            }

            if (!_teams.ContainsKey(teamName))
            {
                await ReplyAsync($"❌ Invalid team! Available teams: {string.join(", ", _teams.Keys)}");
                return;
            }

            var user = _db.GetAccount(Context.User.Id);
            if (user.Balance < amount)
            {
                await ReplyAsync("❌ Insufficient funds!");
                return;
            }

            user.Balance -= amount;
            _championshipBets[Context.User.Id] = new ChampionshipBet
            {
                TeamName = teamName,
                Amount = amount,
                Multiplier = _teams[teamName].Odds
            };

            _db.UpdateAccount(user);
            await ReplyAsync($"✅ Bet ${amount} on {teamName} to win the championship!");
        }
        catch (Exception ex)
        {
            await ReplyAsync("❌ An error occurred while placing your bet. Please try again.");
            Console.WriteLine($"Error in BetOnTeam: {ex.Message}");
        }
    }

    private static int _currentMatchIndex = 0;  // Add this field at the top with other static fields

    private async Task StartChampionship()
    {
        lock (_championshipLock)
        {
            if (_isTransitioning || _championshipActive) return;
            _isTransitioning = true;
        }

        try
        {
            _championshipActive = true;
            foreach (var team in _teams.Values)
            {
                team.Points = 0;
                team.RecentForm.Clear();
            }

            _championshipTimer.Elapsed -= async (s, e) => await UpdateChampionship();
            _championshipEndTimer.Elapsed -= async (s, e) => await EndChampionship();
            
            _championshipTimer.Elapsed += async (s, e) => await UpdateChampionship();
            _championshipEndTimer.Elapsed += async (s, e) => await EndChampionship();
            
            _championshipTimer.Start();
            _championshipEndTimer.Start();

            await ReplyAsync("🏆 A new championship has started! Use !betteam to place your bets!");
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task UpdateChampionship()
    {
        try
        {
            var teams = _teams.Keys.ToList();
            
            // Only process one match per update
            if (_currentMatchIndex >= teams.Count - 1)
            {
                _currentMatchIndex = 0;  // Reset for next round
                await ShowStandings();
                return;
            }

            string team1 = teams[_currentMatchIndex];
            string team2 = teams[_currentMatchIndex + 1];
            _currentMatchIndex += 2;

            var embed = new EmbedBuilder()
                .WithTitle($"⚽ Match Starting: {team1} vs {team2}")
                .WithColor(Color.Blue)
                .WithDescription("Match highlights will appear below...")
                .Build();

            var message = await Context.Channel.SendMessageAsync(embed: embed);

            // Simulate match highlights
            int team1Goals = 0;
            int team2Goals = 0;
            
            for (int minute = 1; minute <= 5; minute++)
            {
                await Task.Delay(4000);

                string eventTeam = _random.Next(2) == 0 ? team1 : team2;
                string eventText = string.Format(_matchEvents[_random.Next(_matchEvents.Length)], eventTeam);
                
                if (eventText.Contains("scores"))
                {
                    if (eventTeam == team1) team1Goals++;
                    else team2Goals++;
                }

                embed = new EmbedBuilder()
                    .WithTitle($"⚽ {team1} {team1Goals} - {team2Goals} {team2}")
                    .WithColor(Color.Blue)
                    .WithDescription($"{minute * 18}' {eventText}")
                    .Build();

                await message.ModifyAsync(m => m.Embed = embed);
            }

            // Update points
            string matchResult;
            if (team1Goals > team2Goals)
            {
                _teams[team1].Points += 3;
                _teams[team1].RecentForm.Add(true);
                _teams[team2].RecentForm.Add(false);
                matchResult = $"{team1} wins!";
            }
            else if (team2Goals > team1Goals)
            {
                _teams[team2].Points += 3;
                _teams[team2].RecentForm.Add(true);
                _teams[team1].RecentForm.Add(false);
                matchResult = $"{team2} wins!";
            }
            else
            {
                _teams[team1].Points += 1;
                _teams[team2].Points += 1;
                _teams[team1].RecentForm.Add(true);
                _teams[team2].RecentForm.Add(true);
                matchResult = "It's a draw!";
            }

            // Keep only last 5 results
            if (_teams[team1].RecentForm.Count > 5) _teams[team1].RecentForm.RemoveAt(0);
            if (_teams[team2].RecentForm.Count > 5) _teams[team2].RecentForm.RemoveAt(0);

            // After calculating goals, add to match history
            _matchHistory.Add((team1, team2, team1Goals, team2Goals));
            if (_matchHistory.Count > 50) // Keep only last 50 matches
            {
                _matchHistory.RemoveAt(0);
            }

            // Show final result
            var resultEmbed = new EmbedBuilder()
                .WithTitle("⚽ Game Ended!")
                .WithDescription($"{team1} {team1Goals} - {team2Goals} {team2}\n{matchResult}")
                .AddField($"{team1}", $"Total Points: {_teams[team1].Points}", true)
                .AddField($"{team2}", $"Total Points: {_teams[team2].Points}", true)
                .WithColor(team1Goals > team2Goals ? Color.Green : (team2Goals > team1Goals ? Color.Red : Color.LightGrey))
                .Build();

            await Context.Channel.SendMessageAsync(embed: resultEmbed);
            
            // Add a longer delay between matches
            await Task.Delay(5000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateChampionship: {ex.Message}");
        }
    }

    private async Task ShowStandings()
    {
        var embed = new EmbedBuilder()
            .WithTitle("📊 Cyprus First Division Standings")
            .WithColor(Color.Gold);

        foreach (var team in _teams.Values.OrderByDescending(t => t.Points))
        {
            embed.AddField(team.Name, 
                $"Points: {team.Points}\n" +
                $"Championships: {_championshipWins.GetValueOrDefault(team.Name, 0)}\n" +
                $"Form: {string.Join("", team.RecentForm.Select(f => f ? "✅" : "❌").TakeLast(5))}\n" +
                $"Odds: {team.Odds:F2}",
                false);
        }

        // Add last 5 matches
        if (_matchHistory.Any())
        {
            var recentMatches = _matchHistory.TakeLast(5)
                .Select(m => $"{m.Team1} {m.Score1}-{m.Score2} {m.Team2}");
            embed.AddField("Recent Results", string.Join("\n", recentMatches), false);
        }

        await Context.Channel.SendMessageAsync(embed: embed.Build());
    }

    private async Task EndChampionship()
    {
        lock (_championshipLock)
        {
            if (_isTransitioning || !_championshipActive) return;
            _isTransitioning = true;
        }

        try
        {
            _championshipActive = false;
            _championshipTimer.Stop();
            _championshipEndTimer.Stop();

            var winner = _teams.Values.OrderByDescending(t => t.Points).First();
            
            // Update championship wins
            _championshipWins[winner.Name] = _championshipWins.GetValueOrDefault(winner.Name) + 1;

            // Pay out bets with appropriate multipliers
            foreach (var bet in _championshipBets)
            {
                var user = _db.GetAccount(bet.Key);
                if (bet.Value.TeamName == winner.Name)
                {
                    int winnings = (int)(bet.Value.Amount * bet.Value.Multiplier);
                    user.Balance += winnings;
                    _db.UpdateAccount(user);

                    try
                    {
                        if (Context?.Channel is IMessageChannel betChannel) // Changed variable name here
                        {
                            await betChannel.SendMessageAsync($"🎉 <@{bet.Key}> won ${winnings:N0} from their championship bet!");
                        }
                    }
                    catch { /* Ignore send errors */ }
                }
            }

            _championshipBets.Clear();

            // Announce winner in all relevant channels
            if (Context?.Channel is IMessageChannel announceChannel) // Changed variable name here
            {
                var embed = new EmbedBuilder()
                    .WithTitle("🏆 Championship Ended!")
                    .WithDescription($"{winner.Name} has won with {winner.Points} points!")
                    .AddField("Total Championships", $"{_championshipWins[winner.Name]}")
                    .WithColor(Color.Gold)
                    .Build();

                await announceChannel.SendMessageAsync(embed: embed);
            }

            // Reset teams and start new championship after delay
            await Task.Delay(5000);
            await StartChampionship();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in EndChampionship: {ex.Message}");
        }
        finally
        {
            _isTransitioning = false;
        }
    }
}

public class Team
{
    public string Name { get; set; }
    public int Points { get; set; }
    public double Odds { get; set; }
    public List<bool> RecentForm { get; } = new();

    public Team(string name, double odds)
    {
        Name = name;
        Odds = odds;
        Points = 0;
    }
}

public class ChampionshipBet
{
    public string TeamName { get; set; }
    public int Amount { get; set; }
    public double Multiplier { get; set; }
}
