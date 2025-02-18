using Discord.Commands;
using System.Threading.Tasks;
using System;
using Discord;

public class BasicCommands : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Returns pong")]
    public async Task PingAsync()
    {
        await ReplyAsync("Pong!");
    }

    [Command("say")]
    public async Task Say([Remainder] string text)
    {
        await ReplyAsync(text);
    }

    [Command("time")]
    public async Task Time()
    {
        await ReplyAsync(DateTime.Now.ToString());
    }

    [Command("shutdown")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Shutdown()
    {
        await ReplyAsync("Shutting down...");
        await Context.Client.StopAsync();
        Environment.Exit(0);
    }
}
