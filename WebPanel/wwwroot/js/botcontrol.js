function updateStatus() {
    fetch('/api/bot/status')
        .then(response => response.json())
        .then(data => {
            $('#connectionStatus').text(data.isConnected ? 'Connected' : 'Disconnected');
            $('#botLatency').text(`${data.latency}ms`);
            $('#serverCount').text(data.serverCount);
            $('#userCount').text(data.userCount);

            // Update command toggles
            const commandsDiv = $('#commandToggles');
            commandsDiv.empty();
            
            Object.entries(data.commandsEnabled).forEach(([command, enabled]) => {
                const toggle = `
                    <div class="col-md-4 mb-2">
                        <div class="form-check form-switch">
                            <input class="form-check-input" type="checkbox" 
                                   id="toggle_${command}" 
                                   ${enabled ? 'checked' : ''}
                                   onchange="toggleCommand('${command}')">
                            <label class="form-check-label" for="toggle_${command}">
                                ${command}
                            </label>
                        </div>
                    </div>
                `;
                commandsDiv.append(toggle);
            });
        });
}

function toggleCommand(commandName) {
    fetch(`/api/bot/toggle/${commandName}`, {
        method: 'POST'
    }).then(updateStatus);
}

function restartBot() {
    if (confirm('Are you sure you want to restart the bot?')) {
        fetch('/api/bot/restart', {
            method: 'POST'
        }).then(() => {
            alert('Bot restart initiated');
            setTimeout(updateStatus, 5000);
        });
    }
}

// Update status every 5 seconds
setInterval(updateStatus, 5000);
updateStatus();
