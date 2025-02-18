using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public class HelpCommands : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commands;
    private readonly Dictionary<string, string> _categoryEmoji = new()
    {
        { "Economy", "💰" },
        { "Gambling", "🎲" },
        { "Fun", "🎮" },
        { "Shop", "🛍️" },
        { "Utility", "🔧" },
        { "Cards", "🎴" }
    };

    public HelpCommands(CommandService commands)
    {
        _commands = commands;
    }

    [Command("help")]
    [Summary("Shows all command categories")]
    public async Task Help(int page = 1)
    {
        var categories = _commands.Modules
            .Select(m => m.Name.Replace("Commands", ""))
            .Distinct()
            .ToList();

        var embed = new EmbedBuilder()
            .WithTitle("📚 Command Categories")
            .WithColor(Color.Blue)
            .WithDescription("Use `!help <category>` to see specific commands\n" +
                           "Example: `!help economy`");

        foreach (var category in categories)
        {
            var commandCount = _commands.Commands
                .Count(c => c.Module.Name.StartsWith(category, StringComparison.OrdinalIgnoreCase));

            embed.AddField(
                $"{_categoryEmoji.GetValueOrDefault(category, "📝")} {category}",
                $"{commandCount} commands\n`!help {category.ToLower()}`",
                true
            );
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("help")]
    public async Task HelpCategory(string category, int page = 1)
    {
        var moduleCommands = _commands.Commands
            .Where(c => c.Module.Name.StartsWith(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name)
            .ToList();

        if (!moduleCommands.Any())
        {
            await ReplyAsync("❌ Category not found!");
            return;
        }

        const int itemsPerPage = 10;
        var pages = (moduleCommands.Count - 1) / itemsPerPage + 1;
        page = Math.Clamp(page, 1, pages);

        var commands = moduleCommands
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage);

        var emoji = _categoryEmoji.GetValueOrDefault(category, "📝");
        var embed = new EmbedBuilder()
            .WithTitle($"{emoji} {category} Commands")
            .WithColor(Color.Blue)
            .WithFooter($"Page {page}/{pages} • Use !help {category.ToLower()} <page>");

        foreach (var command in commands)
        {
            string aliases = command.Aliases.Any() 
                ? $"\nAliases: {string.Join(", ", command.Aliases)}"
                : "";
                
            embed.AddField(
                $"!{command.Name}",
                $"{command.Summary ?? "No description provided"}{aliases}",
                false
            );
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("quickhelp")]
    [Summary("Shows most commonly used commands")]
    public async Task QuickHelp()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🏃 Quick Start Commands")
            .WithColor(Color.Gold)
            .AddField("💰 Economy", "`!daily` `!work` `!balance` `!deposit` `!withdraw`")
            .AddField("🎲 Gambling", "`!slots` `!coinflip` `!roulette` `!crash`")
            .AddField("🛍️ Shop", "`!shop` `!buy` `!inventory` `!sell`")
            .AddField("🎮 Games", "`!blackjack` `!uno` `!race` `!quest`")
            .WithFooter("Use !help <category> for detailed command lists");

        await ReplyAsync(embed: embed.Build());
    }
}
