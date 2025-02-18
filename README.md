# Discord Bot

This is a simple **Discord bot** built using **C#** and the **Discord.Net** library. You can use it to interact with Discord servers and customize its behavior according to your needs.

## Requirements

- [Visual Studio](https://visualstudio.microsoft.com/) (or any C# IDE)
- [.NET 6.0 or later](https://dotnet.microsoft.com/download) 
- [Discord Bot Token](https://discord.com/developers/applications) (Create a bot on Discord and get a token)

## Setup Instructions

### 1. Clone the Repository

Clone this repository to your local machine:


git clone https://github.com/nextSLN/Discord-Bot.git
### 2. Install Dependencies
Open the project in Visual Studio or your preferred C# IDE.

Restore the required NuGet packages:

dotnet restore
### 3. Create the config.json File
In the root of the project directory, create a config.json file. This file will contain your Discord bot token and the command prefix.

Add the following content to config.json:

{
  "Token": "your-token-here",
  "Prefix": "!"
}
Replace your-token-here with your actual Discord bot token.
You can change the Prefix to any character or string you want to trigger bot commands.
### 4. Build and Run the Bot
Once you have everything set up, you can build and run the bot using Visual Studio or the .NET CLI:

dotnet build
dotnet run
### 5. Enjoy the Bot!
Once the bot is running, it will connect to your Discord server, and you can interact with it using the prefix you set in the config.json file.

Troubleshooting

Invalid token: Ensure that the config.json file contains a valid token.
Bot permissions: Make sure the bot has the necessary permissions to interact with your server.
