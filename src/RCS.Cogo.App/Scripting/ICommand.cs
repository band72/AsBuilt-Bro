using System.Threading.Tasks;

namespace RCS.Cogo.App.Scripting;

/// <summary>
/// Represents a generic executable COGO command.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// The keyword that triggers this command (e.g., "AD", "INV").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the command with the provided arguments.
    /// </summary>
    /// <param name="args">The list of arguments (command name is usually args[0]).</param>
    /// <param name="context">The execution context.</param>
    Task ExecuteAsync(string[] args, ICogoContext context);

    /// <summary>
    /// Gets a help string or usage syntax.
    /// </summary>
    string Description { get; }
}
