using LiteDB;
using RCS.Cogo.App.Models;
using RCS.Cogo.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Cogo.App.Persistence;

public class LiteDbProjectService
{
    /// <summary>
    /// Saves the project to a LiteDB file.
    /// Uses 'project_meta' for the main project object and separate collections for large data.
    /// </summary>
    public void SaveProject(string filePath, Project project)
    {
        using var db = new LiteDatabase(filePath);
        
        // 1. Project Metadata
        var meta = db.GetCollection<Project>("project_metadata");
        
        // We clear just the metadata collection to insert the fresh state.
        // We detach the heavy lists from the Project object before saving metadata to keep it light.
        // We will save lists separately.
        
        // Clone project structure without data lists for metadata storage
        var projectMeta = new Project
        {
            Id = project.Id,
            AvailNo = project.AvailNo,
            ProjectName = project.ProjectName,
            Utility = project.Utility,
            Units = project.Units,
            Revision = project.Revision,
            Projection = project.Projection,
            Settings = project.Settings,
            ReportConfig = project.ReportConfig,
            Deliverables = project.Deliverables ?? new List<Deliverable>(),
            // Ensure data lists are empty for metadata to prevent double storage
            Points = new List<PointEntry>(),
            PipeRuns = new List<RCS.Piping.Core.Models.PipeRun>(),
            Structures = new List<RCS.Piping.Core.Models.PipeStructure>(),
            Materials = new List<RCS.Piping.Core.Models.MaterialItem>(),
        };
        
        meta.DeleteAll();
        meta.Insert(projectMeta);

        // 2. Points
        var colPoints = db.GetCollection<PointEntry>("points");
        colPoints.DeleteAll(); // Full replace on save for now (simplest consistency)
        if (project.Points != null && project.Points.Count > 0)
        {
            colPoints.InsertBulk(project.Points);
            colPoints.EnsureIndex(x => x.Id); // Index by Point ID
        }

        // 3. PipeRuns
        var colRuns = db.GetCollection<RCS.Piping.Core.Models.PipeRun>("pipe_runs");
        colRuns.DeleteAll();
        if (project.PipeRuns != null && project.PipeRuns.Count > 0)
        {
            colRuns.InsertBulk(project.PipeRuns);
            colRuns.EnsureIndex(x => x.Id);
        }

        // 4. Structures
        var colStructures = db.GetCollection<RCS.Piping.Core.Models.PipeStructure>("structures");
        colStructures.DeleteAll();
        if (project.Structures != null && project.Structures.Count > 0)
        {
            colStructures.InsertBulk(project.Structures);
            colStructures.EnsureIndex(x => x.Id);
        }

        // 5. Materials
        // Assuming Materials are small, but consistent to store in DB
        var colMaterials = db.GetCollection<RCS.Piping.Core.Models.MaterialItem>("materials");
        colMaterials.DeleteAll();
        if (project.Materials != null && project.Materials.Count > 0)
        {
            colMaterials.InsertBulk(project.Materials);
        }
    }

    /// <summary>
    /// Loads the project from a LiteDB file.
    /// Reconstructs the Project object and repopulates lists.
    /// </summary>
    public Project LoadProject(string filePath)
    {
        using var db = new LiteDatabase(filePath);
        
        // 1. Metadata
        var meta = db.GetCollection<Project>("project_metadata");
        var project = meta.FindAll().FirstOrDefault();
        
        if (project == null)
        {
            // Fallback or new project if empty
            project = new Project { ProjectName = "Loaded Project" };
        }

        // 2. Load Collections
        var colPoints = db.GetCollection<PointEntry>("points");
        project.Points = colPoints.FindAll().ToList();

        var colRuns = db.GetCollection<RCS.Piping.Core.Models.PipeRun>("pipe_runs");
        project.PipeRuns = colRuns.FindAll().ToList();

        var colStructures = db.GetCollection<RCS.Piping.Core.Models.PipeStructure>("structures");
        project.Structures = colStructures.FindAll().ToList();

        var colMaterials = db.GetCollection<RCS.Piping.Core.Models.MaterialItem>("materials");
        project.Materials = colMaterials.FindAll().ToList();
        
        // Load Settings & Config if available (already inside Project metadata)

        return project;
    }

    /// <summary>
    /// Compacts the database to reduce file size.
    /// In LiteDB, Rebuild() performs compaction and repairs structure.
    // Returns bytes reduced.
    /// </summary>
    public bool CompactDatabase(string filePath)
    {
        using var db = new LiteDatabase(filePath);
        return db.Rebuild() > 0;
    }

    /// <summary>
    /// Verifies database integrity by attempting to read all collections.
    /// </summary>
    public bool VerifyDatabase(string filePath)
    {
        try
        {
            using var db = new LiteDatabase(filePath);
            // Iterate over known collections to ensure they are readable
            var points = db.GetCollection<PointEntry>("points").Count();
            var runs = db.GetCollection<RCS.Piping.Core.Models.PipeRun>("pipe_runs").Count();
            var structs = db.GetCollection<RCS.Piping.Core.Models.PipeStructure>("structures").Count();
            var meta = db.GetCollection<Project>("project_metadata").Count();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Repairs the database. In LiteDB, this is synonymous with Rebuild.
    /// </summary>
    public bool RepairDatabase(string filePath)
    {
        using var db = new LiteDatabase(filePath);
        return db.Rebuild() >= 0;
    }
}
