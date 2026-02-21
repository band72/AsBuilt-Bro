using RCS.Data;
using RCS.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace RCS.Services;

public interface IProjectAssetService
{
    Task EnsureProjectExistsAsync(string projectId, string projectNumber, string projectName);
}

public class ProjectAssetService : IProjectAssetService
{
    private readonly AppDbContext _context;

    public ProjectAssetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureProjectExistsAsync(string projectId, string projectNumber, string projectName)
    {
        var existing = await _context.Projects.FindAsync(projectId);
        
        if (existing == null)
        {
            var p = new ProjectEntity
            {
                ProjectId = projectId,
                ProjectNumber = projectNumber,
                ProjectName = projectName,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };
            await _context.Projects.AddAsync(p);
            await _context.SaveChangesAsync();
        }
        else
        {
            existing.ProjectNumber = projectNumber;
            existing.ProjectName = projectName;
            existing.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
