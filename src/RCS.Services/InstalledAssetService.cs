using Microsoft.EntityFrameworkCore;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Services;

public class InstalledAssetService<T> : IInstalledAssetService<T> where T : InstalledAsset
{
    private readonly AppDbContext _context;

    public InstalledAssetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<T>> LoadAsync(string projectId)
    {
        var query = _context.Set<T>().Where(x => x.ProjectId == projectId);
        if (typeof(T) == typeof(Figure))
        {
            query = query.Include("Vertices.Point");
        }
        return await query.ToListAsync();
    }

    public async Task<T> UpsertAsync(string projectId, T row)
    {
        // We try to find existing entity by ID
        var existing = await _context.Set<T>().FindAsync(row.Id);
        
        if (existing == null)
        {
            // Insert
            row.ProjectId = projectId;
            if (row.CreatedUtc == default) row.CreatedUtc = DateTime.UtcNow;
            row.UpdatedUtc = DateTime.UtcNow;
            await _context.Set<T>().AddAsync(row);
        }
        else
        {
            // Update
            // Keep CreatedUtc
            var created = existing.CreatedUtc;
            
            // Map properties from incoming 'row' to 'existing'
            _context.Entry(existing).CurrentValues.SetValues(row);
            
            // Handle Navigation Properties manually since SetValues only does scalars
            if (existing is Figure existingFig && row is Figure rowFig)
            {
                // Snapshot the new geometry nodes into cloned objects to prevent tracking collisions
                var newVertices = rowFig.Vertices.Select(v => new FigureVertex 
                {
                    PointId = v.PointId,
                    OrderIndex = v.OrderIndex,
                    Bulge = v.Bulge
                }).ToList();

                var oldVertices = _context.Set<FigureVertex>().Where(v => v.FigureId == existingFig.Id).ToList();
                _context.Set<FigureVertex>().RemoveRange(oldVertices);

                existingFig.Vertices.Clear();
                foreach (var rv in newVertices)
                {
                    // EF will trace as new inserts securely tied to the existing Figure
                    rv.Id = Guid.NewGuid().ToString(); 
                    rv.FigureId = existingFig.Id;
                    _context.Set<FigureVertex>().Add(rv);
                    existingFig.Vertices.Add(rv); // Vital: must map to memory so returned result displays perfectly in UI
                }
            }

            // Restore special fields
            existing.CreatedUtc = created;
            existing.UpdatedUtc = DateTime.UtcNow;
            existing.ProjectId = projectId; // Ensure consistency
        }

        await _context.SaveChangesAsync();
        return existing ?? row;
    }

    public async Task DeleteAsync(string projectId, string rowId)
    {
        var existing = await _context.Set<T>().FindAsync(rowId);
        if (existing != null && existing.ProjectId == projectId)
        {
            _context.Set<T>().Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
