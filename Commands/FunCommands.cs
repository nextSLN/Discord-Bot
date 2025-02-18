using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class FunCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();
    private static readonly Dictionary<ulong, UnoGame> _unoGames = new();
    private static readonly Dictionary<ulong, TradeOffer> _trades = new();
    private static readonly Dictionary<ulong, Race> _races = new();
    private readonly Dictionary<ulong, TriviaSession> _triviaGames = new();

    // Fake sports teams for betting
    private static readonly Dictionary<string, double> _teams = new()
    {
        { "Red Dragons", 1.5 }, { "Blue Knights", 2.0 }, { "Green Eagles", 1.8 },
        { "Yellow Lions", 2.2 }, { "Purple Warriors", 1.7 }, { "Black Panthers", 1.9 }
    };

    private static readonly Dictionary<string, QuestInfo> _quests = new()
    {
        { "dragon", new QuestInfo("Slay the Dragon", "🐲", 1000, 
            new[] { "sword", "armor" }, new[] { "dragon_scale", "dragon_tooth" }) },
        { "treasure", new QuestInfo("Deep Sea Treasure", "🏴‍☠️", 800, 
            new[] { "boat", "map" }, new[] { "pearl", "gold_coin" }) },
        { "forest", new QuestInfo("Enchanted Forest", "🌲", 500, 
            new[] { "bow", "torch" }, new[] { "magic_wood", "fairy_dust" }) }
    };

    public FunCommands(DatabaseService db)  // Remove IReplyCallback parameter
    {
        _db = db;
    }

    [Command("roll")]
    [Summary("Roll a dice (1-6)")]
    public async Task RollDice()
    {
        int result = _random.Next(1, 7);
        await ReplyAsync($"🎲 You rolled a {result}!");
    }

    [Command("8ball")]
    [Summary("Ask the magic 8ball a question")]
    public async Task EightBall([Remainder] string question)
    {
        string[] responses = {
            "🟢 It is certain.",
            "🟢 Without a doubt.",
            "🟡 Ask again later.",
            "🟡 Cannot predict now.",
            "🔴 Don't count on it.",
            "🔴 My sources say no."
        };
        
        await ReplyAsync($"Question: {question}\nAnswer: {responses[_random.Next(responses.Length)]}");
    }

    [Command("joke")]
    [Summary("Tell a random joke")]
    public async Task TellJoke()
    {
        string[] jokes = {
            "Why don't programmers like nature? It has too many bugs! 🐛",
            "What do you call a bear with no teeth? A gummy bear! 🐻",
            "Why did the scarecrow win an award? He was outstanding in his field! 🌾"
        };
        
        await ReplyAsync(jokes[_random.Next(jokes.Length)]);
    }

    [Command("trade")]
    [Summary("Offer a trade to another user")]
    public async Task Trade(IUser target, string offerItem, string requestItem)
    {
        var user = _db.GetAccount(Context.User.Id);
        if (!user.Inventory.ContainsKey(offerItem))
        {
            await ReplyAsync("❌ You don't have this item!");
            return;
        }

        _trades[Context.User.Id] = new TradeOffer
        {
            OfferId = Context.User.Id,
            TargetId = target.Id,
            OfferItem = offerItem,
            RequestItem = requestItem
        };

        await ReplyAsync($"📦 {target.Mention}, {Context.User.Username} wants to trade their {offerItem} for your {requestItem}. Type !accept or !decline");
    }

    [Command("accept")]
    public async Task AcceptTrade()
    {
        var trade = _trades.Values.FirstOrDefault(t => t.TargetId == Context.User.Id);
        if (trade == null)
        {
            await ReplyAsync("❌ No pending trades!");
            return;
        }

        var sender = _db.GetAccount(trade.OfferId);
        var receiver = _db.GetAccount(trade.TargetId);

        if (!sender.Inventory.ContainsKey(trade.OfferItem) || !receiver.Inventory.ContainsKey(trade.RequestItem))
        {
            await ReplyAsync("❌ One or both items no longer available!");
            return;
        }

        // Execute trade
        sender.Inventory[trade.OfferItem]--;
        receiver.Inventory[trade.RequestItem]--;
        sender.Inventory[trade.RequestItem] = sender.Inventory.GetValueOrDefault(trade.RequestItem) + 1;
        receiver.Inventory[trade.OfferItem] = receiver.Inventory.GetValueOrDefault(trade.OfferItem) + 1;

        _db.UpdateAccount(sender);
        _db.UpdateAccount(receiver);
        _trades.Remove(trade.OfferId);

        await ReplyAsync("✅ Trade completed successfully!");
    }

    [Command("quest")]
    [Summary("Start an adventure quest. Usage: !quest <questname> (Available: dragon, treasure, forest)")]
    public async Task StartQuest(string questName)
    {
        if (!_quests.ContainsKey(questName))
        {
            await ReplyAsync($"❌ Available quests: {string.Join(", ", _quests.Keys)}");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        var quest = _quests[questName];

        // Check requirements
        foreach (var req in quest.Requirements)
        {
            if (!user.Inventory.ContainsKey(req))
            {
                await ReplyAsync($"❌ You need a {req} for this quest!");
                return;
            }
        }

        // Random success chance
        if (_random.Next(100) < 70)  // 70% success rate
        {
            string reward = quest.Rewards[_random.Next(quest.Rewards.Length)];
            user.Inventory[reward] = user.Inventory.GetValueOrDefault(reward) + 1;
            user.Balance += quest.GoldReward;
            _db.UpdateAccount(user);

            await ReplyAsync($"✨ Quest completed! You found {reward} and earned ${quest.GoldReward}!");
        }
        else
        {
            await ReplyAsync("😢 Quest failed! Better luck next time!");
        }
    }

    [Command("race")]
    [Summary("Start a racing game. Usage: !race [bet amount]")]
    public async Task StartRace(int bet = 100)
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        string[] racers = { "🐎", "🐪", "🐢", "🐰", "🦘" };
        int[] positions = new int[racers.Length];

        var embed = new EmbedBuilder()
            .WithTitle("🏁 Race Started!")
            .WithDescription(CreateRaceTrack(racers, positions))
            .WithFooter($"Bet: ${bet}")
            .Build();

        var message = await ReplyAsync(embed: embed);

        // Simulate race
        while (positions.Max() < 20)
        {
            await Task.Delay(1000);
            for (int i = 0; i < positions.Length; i++)
            {
                if (_random.Next(100) < 50) positions[i]++;
            }

            embed = new EmbedBuilder()
                .WithTitle("🏁 Race In Progress!")
                .WithDescription(CreateRaceTrack(racers, positions))
                .WithFooter($"Bet: ${bet}")
                .Build();

            await message.ModifyAsync(m => m.Embed = embed);
        }

        int winner = Array.IndexOf(positions, positions.Max());
        if (winner == 0)  // If horse wins
        {
            user.Balance += bet * 2;
            _db.UpdateAccount(user);
            await ReplyAsync($"🎉 Your horse won! You earned ${bet * 2}!");
        }
        else
        {
            user.Balance -= bet;
            _db.UpdateAccount(user);
            await ReplyAsync($"😢 {racers[winner]} won! You lost ${bet}!");
        }
    }

    [Command("bet")]
    [Summary("Bet on sports matches. Usage: !bet <team name> <amount>")]
    public async Task SportsBet([Remainder]string input)
    {
        var parts = input.Split(' ');
        if (parts.Length < 2 || !int.TryParse(parts[parts.Length - 1], out int amount))
        {
            await ReplyAsync("❌ Usage: !bet <team name> <amount>");
            return;
        }

        string teamName = string.Join(" ", parts.Take(parts.Length - 1));
        var team = _teams.Keys.FirstOrDefault(t => t.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        
        if (team == null)
        {
            await ReplyAsync($"❌ Available teams: {string.Join(", ", _teams.Keys)}");
            return;
        }

        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < amount)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        bool won = _random.Next(100) < 50;  // 50% chance to win
        double multiplier = _teams[team];
        int winnings = won ? (int)(amount * multiplier) : -amount;

        user.Balance += winnings;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🏆 Sports Betting")
            .WithDescription($"Team: {team}\nBet: ${amount}\nMultiplier: {multiplier}x")
            .AddField("Result", won ? "Victory! 🎉" : "Defeat 😢")
            .AddField(won ? "Winnings" : "Loss", $"${Math.Abs(winnings)}")
            .WithColor(won ? Color.Green : Color.Red)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("uno")]
    [Summary("Start a UNO game. Use !play <color><number> to play cards")]
    public async Task StartUno()
    {
        if (_unoGames.ContainsKey(Context.User.Id))
        {
            await ReplyAsync("❌ You're already in a game!");
            return;
        }

        var game = new UnoGame();
        _unoGames[Context.User.Id] = game;

        var embed = new EmbedBuilder()
            .WithTitle("🎴 UNO Game Started!")
            .AddField("Your Hand", string.Join(" ", game.PlayerHand))
            .AddField("Top Card", game.TopCard)
            .WithFooter("Use !play <color><number> to play a card (e.g., !play 🔴7)")
            .WithColor(Color.Blue)
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("play")]
    [Summary("Play a UNO card. Usage: !play <color><number> (e.g., !play 🔴7)")]
    public async Task PlayCard([Remainder] string card)
    {
        if (!_unoGames.TryGetValue(Context.User.Id, out var game))
        {
            await ReplyAsync("❌ No active game! Start one with !uno");
            return;
        }

        if (game.PlayCard(card))
        {
            if (game.PlayerHand.Count == 0)
            {
                await ReplyAsync("🎉 You won!");
                _unoGames.Remove(Context.User.Id);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎴 UNO")
                .AddField("Your Hand", string.Join(" ", game.PlayerHand))
                .AddField("Top Card", game.TopCard)
                .WithColor(Color.Blue)
                .Build();

            await ReplyAsync(embed: embed);
        }
        else
        {
            await ReplyAsync("❌ Invalid card! The card must match the color or number of the top card.");
        }
    }

    [Command("rockpaperscissors")]
    [Alias("rps")]
    [Summary("Play rock paper scissors")]
    public async Task RockPaperScissors(string choice)
    {
        string[] options = { "rock", "paper", "scissors" };
        string botChoice = options[_random.Next(options.Length)];
        
        if (!options.Contains(choice.ToLower()))
        {
            await ReplyAsync("❌ Please choose rock, paper, or scissors!");
            return;
        }

        string result = (choice, botChoice) switch
        {
            var (p, b) when p == b => "Tie!",
            ("rock", "scissors") => "You win!",
            ("paper", "rock") => "You win!",
            ("scissors", "paper") => "You win!",
            _ => "You lose!"
        };

        await ReplyAsync($"You chose {choice}, I chose {botChoice}. {result}");
    }

    [Command("wouldyourather")]
    [Alias("wyr")]
    [Summary("Get a would you rather question")]
    public async Task WouldYouRather()
    {
        string[] questions = {
            "🤔 Would you rather be able to fly or be invisible?",
            "🤔 Would you rather live without music or without movies?",
            "🤔 Would you rather be the funniest or smartest person in the room?"
            // Add more questions
        };

        var embed = new EmbedBuilder()
            .WithTitle("Would You Rather")
            .WithDescription(questions[_random.Next(questions.Length)])
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: embed);
        await message.AddReactionAsync(new Emoji("1️⃣"));
        await message.AddReactionAsync(new Emoji("2️⃣"));
    }

    [Command("trivia")]
    [Summary("Start a trivia game")]
    public async Task Trivia()
    {
        if (_triviaGames.ContainsKey(Context.User.Id))
        {
            await ReplyAsync("❌ You're already in a trivia game!");
            return;
        }

        var session = new TriviaSession();
        _triviaGames[Context.User.Id] = session;

        var embed = new EmbedBuilder()
            .WithTitle("🎯 Trivia")
            .WithDescription(session.CurrentQuestion.Question)
            .WithColor(Color.Blue);

        for (int i = 0; i < session.CurrentQuestion.Options.Length; i++)
        {
            embed.AddField($"Option {i + 1}", session.CurrentQuestion.Options[i], true);
        }

        var message = await ReplyAsync(embed: embed.Build());
        
        // Add reaction options
        for (int i = 0; i < session.CurrentQuestion.Options.Length; i++)
        {
            await message.AddReactionAsync(new Emoji($"{i + 1}️⃣"));
        }
    }

    private string CreateRaceTrack(string[] racers, int[] positions)
    {
        var track = new System.Text.StringBuilder();
        for (int i = 0; i < racers.Length; i++)
        {
            track.Append(new string('_', positions[i]))
                 .Append(racers[i])
                 .Append(new string('_', 20 - positions[i]))
                 .Append("🏁\n");
        }
        return track.ToString();
    }
}

public class TradeOffer
{
    public ulong OfferId { get; set; }
    public ulong TargetId { get; set; }
    public string OfferItem { get; set; } = "";
    public string RequestItem { get; set; } = "";
}

public class QuestInfo
{
    public string Name { get; }
    public string Emoji { get; }
    public int GoldReward { get; }
    public string[] Requirements { get; }
    public string[] Rewards { get; }

    public QuestInfo(string name, string emoji, int goldReward, string[] requirements, string[] rewards)
    {
        Name = name;
        Emoji = emoji;
        GoldReward = goldReward;
        Requirements = requirements;
        Rewards = rewards;
    }
}

public class Race
{
    public string[] Racers { get; set; } = Array.Empty<string>();
    public int[] Positions { get; set; } = Array.Empty<int>();
    public int Bet { get; set; }
}

public class UnoGame
{
    private static readonly string[] Colors = { "🔴", "🔵", "🟡", "🟢" };
    private static readonly string[] Numbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    private static readonly Random _random = new();
    private readonly List<string> _deck;

    public List<string> PlayerHand { get; } = new();
    public string TopCard { get; private set; } = "";

    public UnoGame()
    {
        _deck = GenerateDeck();
        for (int i = 0; i < 7; i++)
            PlayerHand.Add(DrawCard());
        TopCard = DrawCard();
    }

    private List<string> GenerateDeck()
    {
        var deck = new List<string>();
        foreach (var color in Colors)
            foreach (var number in Numbers)
                deck.Add(color + number);
        return deck;
    }

    public string DrawCard()
    {
        if (_deck.Count == 0)
            _deck.AddRange(GenerateDeck());

        int index = _random.Next(_deck.Count);
        string card = _deck[index];
        _deck.RemoveAt(index);
        return card;
    }

    public bool PlayCard(string card)
    {
        if (!PlayerHand.Contains(card)) return false;
        
        string topColor = TopCard[0].ToString();
        string topNumber = TopCard[1].ToString();
        string playColor = card[0].ToString();
        string playNumber = card[1].ToString();

        if (playColor == topColor || playNumber == topNumber)
        {
            PlayerHand.Remove(card);
            TopCard = card;
            if (PlayerHand.Count == 0) return true;
            PlayerHand.Add(DrawCard());
            return true;
        }

        return false;
    }
}

public class TriviaSession
{
    public TriviaQuestion CurrentQuestion { get; private set; }
    public int Score { get; private set; }
    private readonly List<TriviaQuestion> _questions;
    private readonly Random _random = new();

    public TriviaSession()
    {
        _questions = InitializeQuestions();
        CurrentQuestion = GetRandomQuestion();
    }

    private List<TriviaQuestion> InitializeQuestions()
    {
        return new List<TriviaQuestion>
        {
            new TriviaQuestion(
                "What is the capital of France?",
                new[] { "London", "Paris", "Berlin", "Madrid" },
                1
            ),
            // Add more questions here
        };
    }

    private TriviaQuestion GetRandomQuestion()
    {
        return _questions[_random.Next(_questions.Count)];
    }
}

public class TriviaQuestion
{
    public string Question { get; }
    public string[] Options { get; }
    public int CorrectAnswer { get; }

    public TriviaQuestion(string question, string[] options, int correctAnswer)
    {
        Question = question;
        Options = options;
        CorrectAnswer = correctAnswer;
    }
}
