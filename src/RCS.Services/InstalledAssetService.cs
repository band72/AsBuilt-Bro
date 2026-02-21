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
        return await _context.Set<T>()
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();
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
