using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

public class ModerationCommands : ModuleBase<SocketCommandContext>
{
    [Command("clear")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [Summary("Clear specified number of messages")]
    public async Task ClearMessages(int amount)
    {
        if (amount < 1 || amount > 100)
        {
            await ReplyAsync("❌ Please specify a number between 1 and 100.");
            return;
        }

        var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
        await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
        
        var reply = await ReplyAsync($"✅ Deleted {amount} messages.");
        await Task.Delay(3000);
        await reply.DeleteAsync();
    }

    [Command("kick")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    [Summary("Kick a user")]
    public async Task Kick(IGuildUser user, [Remainder] string? reason = default)
    {
        await user.KickAsync(reason ?? "No reason provided");
        await ReplyAsync($"👢 {user.Username} has been kicked. Reason: {reason ?? "No reason provided"}");
    }

    [Command("warn")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [Summary("Warn a user")]
    public async Task Warn(IGuildUser user, [Remainder] string reason)
    {
        var embed = new EmbedBuilder()
            .WithTitle("⚠️ Warning")
            .WithColor(Color.Orange)
            .AddField("User", user.Username, true)
            .AddField("Moderator", Context.User.Username, true)
            .AddField("Reason", reason)
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
        try
        {
            await user.SendMessageAsync($"You have been warned in {Context.Guild.Name} for: {reason}");
        }
        catch
        {
            await ReplyAsync("⚠️ Could not DM user the warning.");
        }
    }
}
