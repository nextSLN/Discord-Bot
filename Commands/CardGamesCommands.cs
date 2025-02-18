using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class CardGamesCommands : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseService _db;
    private static readonly Random _random = new();
    private static readonly Dictionary<string, BlackjackGame> _activeGames = new();

    public CardGamesCommands(DatabaseService db)
    {
        _db = db;
    }

    [Command("blackjack")]
    [Alias("bj")]
    [Summary("Play blackjack. Usage: !blackjack <bet amount>")]
    public async Task StartBlackjack(int bet)
    {
        var user = _db.GetAccount(Context.User.Id);
        if (user.Balance < bet)
        {
            await ReplyAsync("❌ Insufficient funds!");
            return;
        }

        var game = new BlackjackGame(bet);
        _activeGames[Context.User.Id.ToString()] = game;

        var embed = new EmbedBuilder()
            .WithTitle("🎰 Blackjack")
            .AddField("Your Hand", string.Join(" ", game.PlayerHand) + $" ({game.GetHandValue(game.PlayerHand)})")
            .AddField("Dealer's Hand", $"{game.DealerHand[0]} ?")
            .WithFooter("Type !hit to draw another card or !stand to hold")
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("hit")]
    [Summary("Draw another card in blackjack")]
    public async Task Hit()
    {
        if (!_activeGames.TryGetValue(Context.User.Id.ToString(), out var game))
        {
            await ReplyAsync("❌ No active game! Start one with !blackjack <bet>");
            return;
        }

        game.PlayerHand.Add(game.DrawCard());
        int value = game.GetHandValue(game.PlayerHand);

        if (value > 21)
        {
            await EndGame(game, false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("🎰 Blackjack")
            .AddField("Your Hand", string.Join(" ", game.PlayerHand) + $" ({value})")
            .AddField("Dealer's Hand", $"{game.DealerHand[0]} ?")
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("stand")]
    [Summary("Stand with your current hand in blackjack")]
    public async Task Stand()
    {
        if (!_activeGames.TryGetValue(Context.User.Id.ToString(), out var game))
        {
            await ReplyAsync("❌ No active game!");
            return;
        }

        while (game.GetHandValue(game.DealerHand) < 17)
        {
            game.DealerHand.Add(game.DrawCard());
        }

        int playerValue = game.GetHandValue(game.PlayerHand);
        int dealerValue = game.GetHandValue(game.DealerHand);

        bool playerWins = dealerValue > 21 || playerValue > dealerValue;
        await EndGame(game, playerWins);
    }

    private async Task EndGame(BlackjackGame game, bool playerWins)
    {
        var user = _db.GetAccount(Context.User.Id);
        user.Balance += playerWins ? game.Bet : -game.Bet;
        _db.UpdateAccount(user);

        var embed = new EmbedBuilder()
            .WithTitle("🎰 Blackjack - Game Over")
            .AddField("Your Hand", string.Join(" ", game.PlayerHand) + $" ({game.GetHandValue(game.PlayerHand)})")
            .AddField("Dealer's Hand", string.Join(" ", game.DealerHand) + $" ({game.GetHandValue(game.DealerHand)})")
            .AddField("Result", playerWins ? $"You won ${game.Bet}!" : $"You lost ${game.Bet}")
            .WithColor(playerWins ? Color.Green : Color.Red)
            .Build();

        await ReplyAsync(embed: embed);
        _activeGames.Remove(Context.User.Id.ToString());
    }
}

public class BlackjackGame
{
    private static readonly string[] Cards = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
    private static readonly string[] Suits = { "♠️", "♥️", "♣️", "♦️" };
    private readonly Random _random = new();
    private readonly List<string> _deck;

    public List<string> PlayerHand { get; } = new();
    public List<string> DealerHand { get; } = new();
    public int Bet { get; }

    public BlackjackGame(int bet)
    {
        Bet = bet;
        _deck = GenerateDeck();
        
        // Initial deal
        PlayerHand.Add(DrawCard());
        DealerHand.Add(DrawCard());
        PlayerHand.Add(DrawCard());
        DealerHand.Add(DrawCard());
    }

    private List<string> GenerateDeck()
    {
        var deck = new List<string>();
        foreach (var suit in Suits)
            foreach (var card in Cards)
                deck.Add($"{card}{suit}");
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

    public int GetHandValue(List<string> hand)
    {
        int value = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            string rank = card[..^2];
            if (rank == "A")
                aces++;
            else if (rank is "K" or "Q" or "J")
                value += 10;
            else
                value += int.Parse(rank);
        }

        for (int i = 0; i < aces; i++)
        {
            if (value + 11 <= 21)
                value += 11;
            else
                value += 1;
        }

        return value;
    }
}
