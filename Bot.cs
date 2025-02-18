using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class Bot
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IConfiguration _config;
    private IServiceProvider _services;

    public bool IsConnected => _client?.ConnectionState == ConnectionState.Connected;

    public int GetLatency() => _client?.Latency ?? -1;

    public int GetServerCount() => _client?.Guilds.Count ?? 0;

    public int GetUserCount() => _client?.Guilds.Sum(g => g.MemberCount) ?? 0;

    public IEnumerable<CommandInfo> GetCommands() => _commands.Commands;

    public Bot(IConfiguration config)
    {
        _config = config;
        
        var socketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true
        };

        _client = new DiscordSocketClient(socketConfig);
        _commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Debug,
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Async
        });

        var database = new DatabaseService();
        var shop = new ShopCommands(database);  // Create shop instance
        var helpCommands = new HelpCommands(_commands); // Add this line
        
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddSingleton(_config)
            .AddSingleton(database)
            .AddSingleton(shop)  // Add shop to services
            .AddSingleton(helpCommands) // Add this line
            .BuildServiceProvider();

        // Initialize commands immediately
        _commands.AddModulesAsync(typeof(Bot).Assembly, _services).GetAwaiter().GetResult();

        _client.Log += Log;
        _client.Ready += Ready;
        _client.MessageReceived += HandleMessage;
        _client.ButtonExecuted += HandleButtonAsync;
        _client.SelectMenuExecuted += HandleSelectMenuAsync;
        _client.Ready += HandleSlashCommandsAsync;

        var builder = WebApplication.CreateBuilder();
        
        // Set the content root and web root paths
        builder.Environment.ContentRootPath = AppDomain.CurrentDomain.BaseDirectory;
        builder.Environment.WebRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        
        builder.Services.AddRazorPages()
            .AddRazorRuntimeCompilation();  // Add this line
        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton(this);

        var app = builder.Build();
        
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", context => 
            {
                context.Response.Redirect("/Index");
                return Task.CompletedTask;
            });
            endpoints.MapRazorPages();
            endpoints.MapControllers();
            endpoints.MapHub<BotHub>("/bothub");
        });
        
        // Start web panel on port 5000
        app.Run("http://localhost:5000");
    }

    private async Task HandleMessage(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        var argPos = 0;
        if (!message.HasStringPrefix("!", ref argPos)) return;

        var context = new SocketCommandContext(_client, message);
        var result = await _commands.ExecuteAsync(context, argPos, _services);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"Command failed: {result.Error} - {result.ErrorReason}");
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    await context.Channel.SendMessageAsync("Unknown command. Use !help to see available commands.");
                    break;
                case CommandError.BadArgCount:
                    await context.Channel.SendMessageAsync("Incorrect command usage. Check !help for proper usage.");
                    break;
                default:
                    await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
                    break;
            }
        }
    }

    private async Task Ready()
    {
        Console.WriteLine($"Bot is connected as {_client.CurrentUser.Username}");
        Console.WriteLine($"Loaded commands: {string.Join(", ", _commands.Commands.Select(c => c.Name))}");
        await Task.CompletedTask;
    }

    private async Task HandleButtonAsync(SocketMessageComponent component)
    {
        switch (component.Data.CustomId)
        {
            case "support_button":
                await component.RespondAsync("Support server link: https://discord.gg/yourserver", ephemeral: true);
                break;
            default:
                await component.RespondAsync("Unknown button interaction", ephemeral: true);
                break;
        }
    }

    private async Task HandleSelectMenuAsync(SocketMessageComponent component)
    {
        if (component.Data.CustomId == "category_select")
        {
            var category = component.Data.Values.First();
            var commands = _commands.Commands
                .Where(c => c.Module.Name.Replace("Commands", "").ToLower() == category)
                .OrderBy(c => c.Name);

            var embed = new EmbedBuilder()
                .WithTitle($"{char.ToUpper(category[0]) + category[1..]} Commands")
                .WithColor(Color.Blue);

            foreach (var cmd in commands)
            {
                embed.AddField($"!{cmd.Name}", 
                    cmd.Summary ?? "No description available", 
                    true);
            }

            await component.RespondAsync(embed: embed.Build(), ephemeral: true);
        }
    }

    private async Task HandleSlashCommandsAsync()
    {
        var guildCommand = new SlashCommandBuilder()
            .WithName("help")
            .WithDescription("Shows all available commands");

        try
        {
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(new[]
            {
                guildCommand.Build()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering slash commands: {ex.Message}");
        }
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await _client.LoginAsync(TokenType.Bot, _config["Token"]);
        await _client.StartAsync();
        await Task.Delay(-1, cancellationToken);
    }

    public async Task Stop()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private void ConfigureServices(IServiceCollection services)  // Change return type to void
    {
        // Add your service configurations here
        services.AddSingleton(_client);
        services.AddSingleton(_commands);
        services.AddSingleton(_config);
    }
}
