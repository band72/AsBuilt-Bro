using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Cogo.App.Scripting;

/// <summary>
/// Registry for discovering and retrieving available commands.
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICommand command)
    {
        if (!_commands.ContainsKey(command.Name))
        {
            _commands[command.Name] = command;
        }
    }

    public ICommand? GetCommand(string name)
    {
        return _commands.TryGetValue(name, out var cmd) ? cmd : null;
    }

    public IEnumerable<ICommand> GetAllCommands() => _commands.Values;
}
