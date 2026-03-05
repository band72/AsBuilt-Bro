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
    
    // Survey Linework (Method 1)
    public DbSet<Figure> Figures { get; set; }
    public DbSet<FigureVertex> FigureVertices { get; set; }
    public DbSet<SurveyPoint> SurveyPoints { get; set; }
    
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

    // Gas (G)
    public DbSet<GGravityPipe> GGravityPipes { get; set; }
    public DbSet<GPressurePipe> GPressurePipes { get; set; }
    public DbSet<GPoint> GPoints { get; set; }
    public DbSet<GFitting> GFittings { get; set; }
    public DbSet<GManhole> GManholes { get; set; }
    public DbSet<GServicePoint> GServicePoints { get; set; }
    public DbSet<GValve> GValves { get; set; }
    public DbSet<GLocateBox> GLocateBoxes { get; set; }

    // Electric (E)
    public DbSet<EGravityPipe> EGravityPipes { get; set; }
    public DbSet<EPressurePipe> EPressurePipes { get; set; }
    public DbSet<EPoint> EPoints { get; set; }
    public DbSet<EFitting> EFittings { get; set; }
    public DbSet<EManhole> EManholes { get; set; }
    public DbSet<EServicePoint> EServicePoints { get; set; }
    public DbSet<EValve> EValves { get; set; }
    public DbSet<ELocateBox> ELocateBoxes { get; set; }

    // Storm (ST)
    public DbSet<STGravityPipe> STGravityPipes { get; set; }
    public DbSet<STPressurePipe> STPressurePipes { get; set; }
    public DbSet<STPoint> STPoints { get; set; }
    public DbSet<STFitting> STFittings { get; set; }
    public DbSet<STManhole> STManholes { get; set; }
    public DbSet<STServicePoint> STServicePoints { get; set; }
    public DbSet<STValve> STValves { get; set; }
    public DbSet<STLocateBox> STLocateBoxes { get; set; }

    // Master Codes
    public DbSet<CogoCodeEntity> CogoCodes { get; set; }
    
    // Master Materials
    public DbSet<MaterialEntity> Materials { get; set; }

    // Part / Pipe Specifications
    public DbSet<PartSpecificationEntity> PartSpecifications { get; set; }

    // Validation Rules
    public DbSet<ValidationRuleEntity> ValidationRules { get; set; }
    public DbSet<AppGlobalSetting> GlobalSettings { get; set; }

    // Symbols
    public DbSet<SymbolManagerEntity> SymbolManagers { get; set; }

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
        // Ensure ProjectNumber is indexed (but not unique because of '0000' fallbacks)
        modelBuilder.Entity<ProjectEntity>()
            .HasIndex(p => p.ProjectNumber);

        // Indexes for Assets (ProjectId)
        modelBuilder.Entity<PipeCrossing>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Structure>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Meter>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Pipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Fitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Valve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Figure>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<Figure>().HasIndex(e => e.Layer);
        modelBuilder.Entity<FigureVertex>().HasIndex(e => e.FigureId);
        modelBuilder.Entity<SurveyPoint>().HasIndex(e => e.ProjectId);
        
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

        // Gas (G)
        modelBuilder.Entity<GGravityPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GPressurePipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GManhole>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GServicePoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<GLocateBox>().HasIndex(e => e.ProjectId);

        // Electric (E)
        modelBuilder.Entity<EGravityPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<EPressurePipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<EPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<EFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<EManhole>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<EServicePoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<EValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<ELocateBox>().HasIndex(e => e.ProjectId);

        // Storm (ST)
        modelBuilder.Entity<STGravityPipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STPressurePipe>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STPoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STFitting>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STManhole>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STServicePoint>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STValve>().HasIndex(e => e.ProjectId);
        modelBuilder.Entity<STLocateBox>().HasIndex(e => e.ProjectId);
    }
}
