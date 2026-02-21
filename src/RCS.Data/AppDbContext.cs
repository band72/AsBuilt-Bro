using Microsoft.EntityFrameworkCore;
using RCS.Data.Entities;

namespace RCS.Data;

public class AppDbContext : DbContext
{
    public DbSet<ProjectEntity> Projects { get; set; }
    public DbSet<PipeCrossing> PipeCrossings { get; set; }
    public DbSet<Structure> Structures { get; set; }
    public DbSet<Meter> Meters { get; set; }
    public DbSet<Pipe> Pipes { get; set; }
    public DbSet<Fitting> Fittings { get; set; }
    public DbSet<Valve> Valves { get; set; }
    
    // Water
    public DbSet<WaterPipe> WaterPipes { get; set; }
    public DbSet<WaterPoint> WaterPoints { get; set; }
    public DbSet<WaterFitting> WaterFittings { get; set; }
    public DbSet<WaterValve> WaterValves { get; set; }
    public DbSet<WaterHydrant> WaterHydrants { get; set; }
    public DbSet<WaterMeter> WaterMeters { get; set; }
    public DbSet<WaterLocateBox> WaterLocateBoxes { get; set; }

    // WW
    public DbSet<WWGravityPipe> WWGravityPipes { get; set; }
    public DbSet<WWPressurePipe> WWPressurePipes { get; set; }
    public DbSet<WWPoint> WWPoints { get; set; }
    public DbSet<WWFitting> WWFittings { get; set; }
    public DbSet<Manhole> Manholes { get; set; }
    public DbSet<WWServicePoint> WWServicePoints { get; set; }
    public DbSet<WWValve> WWValves { get; set; }
    public DbSet<WWLocateBox> WWLocateBoxes { get; set; }

    // Reclaimed
    public DbSet<ReclaimedPipe> ReclaimedPipes { get; set; }
    public DbSet<ReclaimedPoint> ReclaimedPoints { get; set; }
    public DbSet<ReclaimedFitting> ReclaimedFittings { get; set; }
    public DbSet<ReclaimedValve> ReclaimedValves { get; set; }
    public DbSet<ReclaimedHydrant> ReclaimedHydrants { get; set; }
    public DbSet<ReclaimedMeter> ReclaimedMeters { get; set; }
    public DbSet<ReclaimedLocateBox> ReclaimedLocateBoxes { get; set; }

    // Chilled
    public DbSet<ChilledPipe> ChilledPipes { get; set; }
    public DbSet<ChilledPoint> ChilledPoints { get; set; }
    public DbSet<ChilledFitting> ChilledFittings { get; set; }
    public DbSet<ChilledValve> ChilledValves { get; set; }
    public DbSet<ChilledMeter> ChilledMeters { get; set; }
    public DbSet<ChilledLocateBox> ChilledLocateBoxes { get; set; }

    // Master Codes
    public DbSet<CogoCodeEntity> CogoCodes { get; set; }
    
    // Master Materials
    public DbSet<MaterialEntity> Materials { get; set; }

    // Validation Rules
    public DbSet<ValidationRuleEntity> ValidationRules { get; set; }
    public DbSet<AppGlobalSetting> GlobalSettings { get; set; }

    public string DbPath { get; private set; } = string.Empty;

    public AppDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "rcs_installed_assets.db");
    }
    
    public AppDbContext(string dbPath)
    {
        DbPath = dbPath;
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensure ProjectNumber is unique
        modelBuilder.Entity<ProjectEntity>()
            .HasIndex(p => p.ProjectNumber)
            .IsUnique();

        // Indexes for Assets (ProjectId)
        modelBuilder.Entity<PipeCrossing>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Structure>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Meter>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Pipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Fitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Valve>().HasIndex(e => e.ProjectId);
        
        // Indexes for new types
        modelBuilder.Entity<WaterPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WaterPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WaterFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WaterValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WaterHydrant>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WaterMeter>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WaterLocateBox>().HasIndex(e => e.ProjectId);

        modelBuilder.Entity<WWGravityPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WWPressurePipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WWPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WWFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Manhole>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WWServicePoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WWValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<WWLocateBox>().HasIndex(e => e.ProjectId);

        modelBuilder.Entity<ReclaimedPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ReclaimedPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ReclaimedFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ReclaimedValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ReclaimedHydrant>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ReclaimedMeter>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ReclaimedLocateBox>().HasIndex(e => e.ProjectId);

        modelBuilder.Entity<ChilledPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ChilledPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ChilledFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ChilledValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ChilledMeter>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ChilledLocateBox>().HasIndex(e => e.ProjectId);
    }
}
