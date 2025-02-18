using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Linq;

public class UtilityCommands : ModuleBase<SocketCommandContext>
{
    [Command("userinfo")]
    [Summary("Get information about a user")]
    public async Task UserInfo(IGuildUser? user = null)
    {
        user ??= Context.User as IGuildUser;
        if (user == null) 
        {
            await ReplyAsync("Error: Could not find user.");
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle($"User Info - {user.Username}")
            .WithColor(Color.Blue)
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .AddField("ID", user.Id, true)
            .AddField("Joined Server", user.JoinedAt?.ToString("MM/dd/yyyy") ?? "Unknown", true)
            .AddField("Account Created", user.CreatedAt.ToString("MM/dd/yyyy"), true)
            .AddField("Roles", string.Join(", ", user.RoleIds.Select(roleId => Context.Guild.GetRole(roleId).Name)))
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("serverinfo")]
    [Summary("Get information about the server")]
    public async Task ServerInfo()
    {
        var guild = Context.Guild;
        
        var embed = new EmbedBuilder()
            .WithTitle($"Server Info - {guild.Name}")
            .WithColor(Color.Green)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Owner", guild.Owner.Username, true)
            .AddField("Members", guild.MemberCount, true)
            .AddField("Created", guild.CreatedAt.ToString("MM/dd/yyyy"), true)
            .AddField("Text Channels", guild.TextChannels.Count, true)
            .AddField("Voice Channels", guild.VoiceChannels.Count, true)
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("poll")]
    [Summary("Create a simple poll")]
    public async Task CreatePoll(string question, params string[] options)
    {
        if (options.Length < 2 || options.Length > 9)
        {
            await ReplyAsync("❌ Please provide between 2 and 9 options!");
            return;
        }

        string[] numberEmojis = { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣" };
        
        var embed = new EmbedBuilder()
            .WithTitle("📊 " + question)
            .WithColor(Color.Gold)
            .WithDescription(string.Join("\n", options.Select((opt, i) => $"{numberEmojis[i]} {opt}")))
            .WithFooter("Vote by reacting!")
            .Build();

        var pollMessage = await ReplyAsync(embed: embed);
        
        for (int i = 0; i < options.Length; i++)
        {
            await pollMessage.AddReactionAsync(new Emoji(numberEmojis[i]));
        }
    }
}
