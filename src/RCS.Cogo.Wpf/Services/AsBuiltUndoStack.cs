using System;
using System.Collections.Generic;
using System.Linq;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Services;

/// <summary>A reversible operation within an As-Built job.</summary>
public interface IAsBuiltUndoAction
{
    string Description { get; }
    void Undo(AsBuiltJob job);
    void Redo(AsBuiltJob job);
}

/// <summary>
/// Reversible operation for adding/removing PointRows inside an AsBuiltJob.
/// Captures exactly what was added vs. what was removed.
/// </summary>
public sealed class EditPointRowsAction : IAsBuiltUndoAction
{
    private readonly string _desc;
    private readonly List<PointRow> _added;
    private readonly List<PointRow> _removed;

    public EditPointRowsAction(string description, IEnumerable<PointRow>? added = null, IEnumerable<PointRow>? removed = null)
    {
        _desc = description;
        _added = added?.ToList() ?? new List<PointRow>();
        _removed = removed?.ToList() ?? new List<PointRow>();
    }

    public string Description => _desc;

    public void Undo(AsBuiltJob job)
    {
        foreach (var r in _added) job.PointRows.Remove(r);
        foreach (var r in _removed) job.PointRows.Add(r);
    }

    public void Redo(AsBuiltJob job)
    {
        foreach (var r in _removed) job.PointRows.Remove(r);
        foreach (var r in _added) job.PointRows.Add(r);
    }
}

/// <summary>
/// Reversible operation for modifying string/description values of a collection of points.
/// Typically used for "Auto-Fix Descriptions".
/// </summary>
public sealed class AutoFixDescriptionsAction : IAsBuiltUndoAction
{
    private readonly string _desc;
    private readonly List<(PointRow Row, string OldDesc, string NewDesc)> _changes;

    public AutoFixDescriptionsAction(string description, IEnumerable<(PointRow Row, string OldDesc, string NewDesc)> changes)
    {
        _desc = description;
        _changes = changes.ToList();
    }

    public string Description => _desc;

    public void Undo(AsBuiltJob job)
    {
        foreach (var change in _changes)
            change.Row.Description = change.OldDesc;
    }

    public void Redo(AsBuiltJob job)
    {
        foreach (var change in _changes)
            change.Row.Description = change.NewDesc;
    }
}

/// <summary>
/// Manages undo/redo stacks for As-Built bulk operations.
/// Thread-safe for UI dispatch.
/// </summary>
public sealed class AsBuiltUndoStack
{
    private const int MaxDepth = 50;

    private readonly Stack<IAsBuiltUndoAction> _undo = new();
    private readonly Stack<IAsBuiltUndoAction> _redo = new();
    private readonly AsBuiltJob _job;
    private readonly Action _onChanged;

    public AsBuiltUndoStack(AsBuiltJob job, Action onChanged)
    {
        _job       = job;
        _onChanged = onChanged;
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string UndoDescription => CanUndo ? $"Undo: {_undo.Peek().Description}" : "Undo";
    public string RedoDescription => CanRedo ? $"Redo: {_redo.Peek().Description}" : "Redo";

    /// <summary>Pushes a new action onto the undo stack and clears redo.</summary>
    public void Push(IAsBuiltUndoAction action)
    {
        _undo.Push(action);
        _redo.Clear();
        // Trim depth
        if (_undo.Count > MaxDepth)
        {
            var items = _undo.ToArray();
            _undo.Clear();
            foreach (var a in items.Take(MaxDepth).Reverse()) _undo.Push(a);
        }
        _onChanged();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var action = _undo.Pop();
        action.Undo(_job);
        _redo.Push(action);
        _onChanged();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var action = _redo.Pop();
        action.Redo(_job);
        _undo.Push(action);
        _onChanged();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _onChanged();
    }
}
