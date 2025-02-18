using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace WebPanel.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly Bot _bot;
        private static readonly Dictionary<string, bool> _commandStatus = new();

        public BotController(Bot bot)
        {
            _bot = bot;
            // Initialize command statuses
            foreach (var command in _bot.GetCommands())
            {
                _commandStatus[command.Name] = true;
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                isConnected = _bot.IsConnected,
                latency = _bot.GetLatency(),
                serverCount = _bot.GetServerCount(),
                userCount = _bot.GetUserCount(),
                commandsEnabled = _bot.GetCommands().ToDictionary(c => c.Name, c => true)
            });
        }

        [HttpPost("toggle/{commandName}")]
        public IActionResult ToggleCommand(string commandName)
        {
            if (_commandStatus.ContainsKey(commandName))
            {
                _commandStatus[commandName] = !_commandStatus[commandName];
                return Ok(new { command = commandName, enabled = _commandStatus[commandName] });
            }
            return NotFound($"Command {commandName} not found");
        }

        [HttpPost("restart")]
        public async Task<IActionResult> Restart()
        {
            await _bot.Stop();
            await _bot.Start();
            return Ok();
        }
    }
}
