using System;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

/// <summary>
/// RENUMBER &lt;StartPt&gt; &lt;EndPt&gt; &lt;NewStart&gt;
/// Renumbers a contiguous range of existing point IDs to a new starting number.
/// All figure references that use the old IDs are updated in place.
///
/// Examples:
///   RENUMBER 1 50 1001          → renames points 1–50 to 1001–1050
///   RENUMBER 100 105 200        → renames points 100–105 to 200–205
/// </summary>
public class RenumberCommand : ICommand
{
    public string Name        => "RENUMBER";
    public string Description => "Renumbers a range of point IDs. Usage: RENUMBER <StartPt> <EndPt> <NewStart>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 4)
        {
            context.Log("Usage: RENUMBER <StartPt> <EndPt> <NewStart>");
            context.Log("  Example: RENUMBER 1 50 1001  →  renames pts 1–50 to 1001–1050");
            return Task.CompletedTask;
        }

        if (!int.TryParse(args[1], out int startPt)  ||
            !int.TryParse(args[2], out int endPt)    ||
            !int.TryParse(args[3], out int newStart))
        {
            context.Log("RENUMBER: all three arguments must be integers.");
            return Task.CompletedTask;
        }

        if (startPt > endPt)
        {
            context.Log($"RENUMBER: StartPt ({startPt}) must be ≤ EndPt ({endPt}).");
            return Task.CompletedTask;
        }

        int count = endPt - startPt + 1;

        // ── Collision guard: make sure no new ID collides with an existing point
        //    that is NOT in the range being renamed.
        var allPoints = context.GetAllPoints().ToList();
        var rangeIds  = Enumerable.Range(startPt, count)
                                  .Select(i => i.ToString())
                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < count; i++)
        {
            string prospective = (newStart + i).ToString();
            bool occupied = allPoints.Any(p =>
                string.Equals(p.Id, prospective, StringComparison.OrdinalIgnoreCase) &&
                !rangeIds.Contains(p.Id));

            if (occupied)
            {
                context.Log($"RENUMBER: target point {prospective} already exists and is outside the rename range. Aborting.");
                return Task.CompletedTask;
            }
        }

        // ── Verify all source points exist ─────────────────────────────────────
        int missing = 0;
        for (int i = startPt; i <= endPt; i++)
        {
            if (context.GetPoint(i.ToString()) == null)
            {
                context.Log($"RENUMBER: point {i} not found — skipping.");
                missing++;
            }
        }

        if (missing == count)
        {
            context.Log("RENUMBER: none of the specified points exist. Nothing was changed.");
            return Task.CompletedTask;
        }

        // ── Step 1: copy to temporary IDs to avoid collisions mid-move ─────────
        string tempPrefix = $"__RNBR_{Guid.NewGuid():N}_";
        var movedPairs = new System.Collections.Generic.List<(string TempId, string NewId, string OldId)>();

        for (int i = startPt; i <= endPt; i++)
        {
            string oldId = i.ToString();
            var    pt    = context.GetPoint(oldId);
            if (pt == null) continue;

            // Find description for this point
            var match = allPoints.FirstOrDefault(p =>
                string.Equals(p.Id, oldId, StringComparison.OrdinalIgnoreCase));
            string desc = match.Id != null ? match.Description ?? "" : "";

            string tempId = $"{tempPrefix}{oldId}";
            string newId  = (newStart + (i - startPt)).ToString();

            context.AddPoint(tempId, pt, desc);
            context.DeletePoint(oldId);
            movedPairs.Add((tempId, newId, oldId));
        }

        // ── Step 2: rename from temp IDs to final IDs ──────────────────────────
        foreach (var (tempId, newId, _) in movedPairs)
        {
            var pt = context.GetPoint(tempId);
            if (pt == null) continue;

            var tempMatch = context.GetAllPoints()
                .FirstOrDefault(p => string.Equals(p.Id, tempId, StringComparison.OrdinalIgnoreCase));
            string desc = tempMatch.Id != null ? tempMatch.Description ?? "" : "";

            context.AddPoint(newId, pt, desc);
            context.DeletePoint(tempId);
        }

        // ── Step 3: Update figure references ───────────────────────────────────
        //    Build an old→new mapping for the renamed range.
        var idMap = movedPairs
            .ToDictionary(t => t.OldId, t => t.NewId, StringComparer.OrdinalIgnoreCase);

        foreach (var fig in context.GetAllFigures())
        {
            bool figChanged = false;
            var  newPtIds   = new System.Collections.Generic.List<string>();

            foreach (var ptId in fig.PointIds)
            {
                if (idMap.TryGetValue(ptId, out var mapped))
                {
                    newPtIds.Add(mapped);
                    figChanged = true;
                }
                else
                {
                    newPtIds.Add(ptId);
                }
            }

            if (figChanged)
            {
                // Rebuild the figure with updated point IDs
                fig.PointIds.Clear();
                foreach (var id in newPtIds) fig.AddPoint(id);
            }
        }

        context.Log($"RENUMBER: {movedPairs.Count} point(s) renamed — range [{startPt}–{endPt}] → [{newStart}–{newStart + movedPairs.Count - 1}].");
        return Task.CompletedTask;
    }
}
