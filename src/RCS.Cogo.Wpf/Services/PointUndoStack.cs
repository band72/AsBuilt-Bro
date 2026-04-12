using System;
using System.Collections.Generic;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.Wpf.Services;

// ── Action contract ───────────────────────────────────────────────────────────

/// <summary>A reversible COGO point operation.</summary>
public interface IUndoAction
{
    string Description { get; }
    void Undo(ICogoContext ctx);
    void Redo(ICogoContext ctx);
}

// ── Concrete actions ──────────────────────────────────────────────────────────

/// <summary>Undo/redo for a batch point delete.</summary>
public sealed class DeletePointsAction : IUndoAction
{
    private readonly List<(string Id, Point3D Pt, string Desc)> _deleted;

    public DeletePointsAction(IEnumerable<(string Id, Point3D Pt, string Desc)> deleted)
        => _deleted = new List<(string, Point3D, string)>(deleted);

    public string Description => $"Delete {_deleted.Count} point(s)";

    public void Undo(ICogoContext ctx)
    {
        foreach (var (id, pt, desc) in _deleted)
            ctx.AddPoint(id, pt, desc);
    }

    public void Redo(ICogoContext ctx)
    {
        foreach (var (id, _, _) in _deleted)
            ctx.RemovePoint(id);
    }
}

/// <summary>Undo/redo for a sequential renumber operation.</summary>
public sealed class RenumberAction : IUndoAction
{
    /// <summary>Map of newId → oldId so we can reverse the rename.</summary>
    private readonly List<(string OldId, string NewId)> _renames;

    public RenumberAction(IEnumerable<(string OldId, string NewId)> renames)
        => _renames = new List<(string, string)>(renames);

    public string Description => $"Renumber {_renames.Count} point(s)";

    public void Undo(ICogoContext ctx)
    {
        // Reverse in reverse order to avoid ID collisions
        for (int i = _renames.Count - 1; i >= 0; i--)
            ctx.RenamePoint(_renames[i].NewId, _renames[i].OldId);
    }

    public void Redo(ICogoContext ctx)
    {
        foreach (var (oldId, newId) in _renames)
            ctx.RenamePoint(oldId, newId);
    }
}

/// <summary>Undo/redo for a CSV or KML import (batch add).</summary>
public sealed class ImportPointsAction : IUndoAction
{
    private readonly List<(string Id, Point3D Pt, string Desc)> _added;
    private readonly string _sourceName;

    public ImportPointsAction(
        IEnumerable<(string Id, Point3D Pt, string Desc)> added, string sourceName)
    {
        _added = new List<(string, Point3D, string)>(added);
        _sourceName = sourceName;
    }

    public string Description => $"Import {_added.Count} point(s) from {_sourceName}";

    public void Undo(ICogoContext ctx)
    {
        foreach (var (id, _, _) in _added)
            ctx.RemovePoint(id);
    }

    public void Redo(ICogoContext ctx)
    {
        foreach (var (id, pt, desc) in _added)
            ctx.AddPoint(id, pt, desc);
    }
}

// ── Stack manager ─────────────────────────────────────────────────────────────

/// <summary>
/// Manages undo/redo stacks for COGO point operations.
/// Thread-safe for UI dispatch (single-threaded WPF) — no lock needed.
/// </summary>
public sealed class PointUndoStack
{
    private const int MaxDepth = 50;

    private readonly Stack<IUndoAction> _undo = new();
    private readonly Stack<IUndoAction> _redo = new();
    private readonly ICogoContext       _ctx;
    private readonly Action             _onChanged;

    public PointUndoStack(ICogoContext ctx, Action onChanged)
    {
        _ctx       = ctx;
        _onChanged = onChanged;
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string UndoDescription => CanUndo ? $"Undo: {_undo.Peek().Description}" : "Undo";
    public string RedoDescription => CanRedo ? $"Redo: {_redo.Peek().Description}" : "Redo";

    /// <summary>Pushes a new action onto the undo stack and clears redo.</summary>
    public void Push(IUndoAction action)
    {
        _undo.Push(action);
        _redo.Clear();
        // Trim depth
        while (_undo.Count > MaxDepth)
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
        action.Undo(_ctx);
        _redo.Push(action);
        _onChanged();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var action = _redo.Pop();
        action.Redo(_ctx);
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
