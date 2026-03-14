using RCS.Data;
using RCS.Data.Entities;
using RCS.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;

namespace RCS.Cogo.Wpf.ViewModels;

public class InstalledAssetsViewModel : ViewModelBase
{
    private readonly AppDbContext _dbContext;
    public Action<string>? LogAction { get; set; }
    
    public event EventHandler<InstalledAsset>? AssetSelected;
    
    public void NotifyAssetSelected(InstalledAsset asset)
    {
        AssetSelected?.Invoke(this, asset);
    }
    
    // Pipe Crossing Service
    private readonly InstalledAssetService<PipeCrossing> _pipeCrossingService;

    // Figure Service (Alignments, Parcels)
    private readonly InstalledAssetService<Figure> _figureService;

    // Services for Water
    private readonly InstalledAssetService<WaterPipe> _waterPipeService;
    private readonly InstalledAssetService<WaterPoint> _waterPointService;
    private readonly InstalledAssetService<WaterFitting> _waterFittingService;
    private readonly InstalledAssetService<WaterValve> _waterValveService;
    private readonly InstalledAssetService<WaterHydrant> _waterHydrantService;
    private readonly InstalledAssetService<WaterMeter> _waterMeterService;
    private readonly InstalledAssetService<WaterLocateBox> _waterLocateBoxService;

    // Services for WW
    private readonly InstalledAssetService<WWGravityPipe> _wwGravityPipeService;
    private readonly InstalledAssetService<WWPressurePipe> _wwPressurePipeService;
    private readonly InstalledAssetService<WWPoint> _wwPointService;
    private readonly InstalledAssetService<WWFitting> _wwFittingService;
    private readonly InstalledAssetService<Manhole> _manholeService;
    private readonly InstalledAssetService<WWServicePoint> _wwServicePointService;
    private readonly InstalledAssetService<WWValve> _wwValveService;
    private readonly InstalledAssetService<WWLocateBox> _wwLocateBoxService;

    // Services for Reclaimed
    private readonly InstalledAssetService<ReclaimedPipe> _reclaimedPipeService;
    private readonly InstalledAssetService<ReclaimedPoint> _reclaimedPointService;
    private readonly InstalledAssetService<ReclaimedFitting> _reclaimedFittingService;
    private readonly InstalledAssetService<ReclaimedValve> _reclaimedValveService;
    private readonly InstalledAssetService<ReclaimedHydrant> _reclaimedHydrantService;
    private readonly InstalledAssetService<ReclaimedMeter> _reclaimedMeterService;
    private readonly InstalledAssetService<ReclaimedLocateBox> _reclaimedLocateBoxService;

    // Services for Chilled
    private readonly InstalledAssetService<ChilledPipe> _chilledPipeService;
    private readonly InstalledAssetService<ChilledPoint> _chilledPointService;
    private readonly InstalledAssetService<ChilledFitting> _chilledFittingService;
    private readonly InstalledAssetService<ChilledValve> _chilledValveService;
    private readonly InstalledAssetService<ChilledMeter> _chilledMeterService;
    private readonly InstalledAssetService<ChilledLocateBox> _chilledLocateBoxService;

    // Gas (G)
    private readonly InstalledAssetService<GGravityPipe> _gGravityPipeService;
    private readonly InstalledAssetService<GPressurePipe> _gPressurePipeService;
    private readonly InstalledAssetService<GPoint> _gPointService;
    private readonly InstalledAssetService<GFitting> _gFittingService;
    private readonly InstalledAssetService<GManhole> _gManholeService;
    private readonly InstalledAssetService<GServicePoint> _gServicePointService;
    private readonly InstalledAssetService<GValve> _gValveService;
    private readonly InstalledAssetService<GLocateBox> _gLocateBoxService;

    // Electric (E)
    private readonly InstalledAssetService<EGravityPipe> _eGravityPipeService;
    private readonly InstalledAssetService<EPressurePipe> _ePressurePipeService;
    private readonly InstalledAssetService<EPoint> _ePointService;
    private readonly InstalledAssetService<EFitting> _eFittingService;
    private readonly InstalledAssetService<EManhole> _eManholeService;
    private readonly InstalledAssetService<EServicePoint> _eServicePointService;
    private readonly InstalledAssetService<EValve> _eValveService;
    private readonly InstalledAssetService<ELocateBox> _eLocateBoxService;

    // Storm (ST)
    private readonly InstalledAssetService<STGravityPipe> _stGravityPipeService;
    private readonly InstalledAssetService<STPressurePipe> _stPressurePipeService;
    private readonly InstalledAssetService<STPoint> _stPointService;
    private readonly InstalledAssetService<STFitting> _stFittingService;
    private readonly InstalledAssetService<STManhole> _stManholeService;
    private readonly InstalledAssetService<STServicePoint> _stServicePointService;
    private readonly InstalledAssetService<STValve> _stValveService;
    private readonly InstalledAssetService<STLocateBox> _stLocateBoxService;

    private readonly ProjectAssetService _projectService;

    // Collections
    public ObservableCollection<PipeCrossing> PipeCrossings { get; } = new();
    
    // Figures (Linework & Alignments)
    public ObservableCollection<Figure> FigureAssets { get; } = new();

    // Water Collections
    public ObservableCollection<WaterPipe> WaterPipes { get; } = new();
    public ObservableCollection<WaterPoint> WaterPoints { get; } = new(); // "Water Points along Pipe"
    public ObservableCollection<WaterFitting> WaterFittings { get; } = new();
    public ObservableCollection<WaterValve> WaterValves { get; } = new();
    public ObservableCollection<WaterHydrant> WaterHydrants { get; } = new();
    public ObservableCollection<WaterMeter> WaterMeters { get; } = new();
    public ObservableCollection<WaterLocateBox> WaterLocateBoxes { get; } = new();

    // WW Collections
    public ObservableCollection<WWGravityPipe> WWGravityPipes { get; } = new();
    public ObservableCollection<WWPressurePipe> WWPressurePipes { get; } = new();
    public ObservableCollection<WWPoint> WWPoints { get; } = new(); // "WW Points along Pipe"
    public ObservableCollection<WWFitting> WWFittings { get; } = new();
    public ObservableCollection<Manhole> Manholes { get; } = new();
    public ObservableCollection<WWServicePoint> WWServicePoints { get; } = new(); // "WW Service Point & Meter"
    public ObservableCollection<WWValve> WWValves { get; } = new();
    public ObservableCollection<WWLocateBox> WWLocateBoxes { get; } = new();

    // Reclaimed Collections
    public ObservableCollection<ReclaimedPipe> ReclaimedPipes { get; } = new();
    public ObservableCollection<ReclaimedPoint> ReclaimedPoints { get; } = new(); // "Reclaimed Points along Pipe"
    public ObservableCollection<ReclaimedFitting> ReclaimedFittings { get; } = new();
    public ObservableCollection<ReclaimedValve> ReclaimedValves { get; } = new();
    public ObservableCollection<ReclaimedHydrant> ReclaimedHydrants { get; } = new();
    public ObservableCollection<ReclaimedMeter> ReclaimedMeters { get; } = new();
    public ObservableCollection<ReclaimedLocateBox> ReclaimedLocateBoxes { get; } = new();

    // Chilled Collections
    public ObservableCollection<ChilledPipe> ChilledPipes { get; } = new();
    public ObservableCollection<ChilledPoint> ChilledPoints { get; } = new(); // "Chilled Points along Pipe"
    public ObservableCollection<ChilledFitting> ChilledFittings { get; } = new();
    public ObservableCollection<ChilledValve> ChilledValves { get; } = new();
    public ObservableCollection<ChilledMeter> ChilledMeters { get; } = new();
    public ObservableCollection<ChilledLocateBox> ChilledLocateBoxes { get; } = new();

    // Gas Collections
    public ObservableCollection<GGravityPipe> GGravityPipes { get; } = new();
    public ObservableCollection<GPressurePipe> GPressurePipes { get; } = new();
    public ObservableCollection<GPoint> GPoints { get; } = new();
    public ObservableCollection<GFitting> GFittings { get; } = new();
    public ObservableCollection<GManhole> GManholes { get; } = new();
    public ObservableCollection<GServicePoint> GServicePoints { get; } = new();
    public ObservableCollection<GValve> GValves { get; } = new();
    public ObservableCollection<GLocateBox> GLocateBoxes { get; } = new();

    // Electric Collections
    public ObservableCollection<EGravityPipe> EGravityPipes { get; } = new();
    public ObservableCollection<EPressurePipe> EPressurePipes { get; } = new();
    public ObservableCollection<EPoint> EPoints { get; } = new();
    public ObservableCollection<EFitting> EFittings { get; } = new();
    public ObservableCollection<EManhole> EManholes { get; } = new();
    public ObservableCollection<EServicePoint> EServicePoints { get; } = new();
    public ObservableCollection<EValve> EValves { get; } = new();
    public ObservableCollection<ELocateBox> ELocateBoxes { get; } = new();

    // Storm Collections
    public ObservableCollection<STGravityPipe> STGravityPipes { get; } = new();
    public ObservableCollection<STPressurePipe> STPressurePipes { get; } = new();
    public ObservableCollection<STPoint> STPoints { get; } = new();
    public ObservableCollection<STFitting> STFittings { get; } = new();
    public ObservableCollection<STManhole> STManholes { get; } = new();
    public ObservableCollection<STServicePoint> STServicePoints { get; } = new();
    public ObservableCollection<STValve> STValves { get; } = new();
    public ObservableCollection<STLocateBox> STLocateBoxes { get; } = new();

    private string _currentProjectId = "";
    private string _currentProjectNumber = "";

    public bool HasActiveProject => !string.IsNullOrEmpty(_currentProjectId);

    public InstalledAssetsViewModel()
    {
        _dbContext = new AppDbContext();
        DbInitializer.Initialize(_dbContext);

        _pipeCrossingService = new InstalledAssetService<PipeCrossing>(_dbContext);
        _figureService = new InstalledAssetService<Figure>(_dbContext);
        _projectService = new ProjectAssetService(_dbContext);

        // Water Init
        _waterPipeService = new InstalledAssetService<WaterPipe>(_dbContext);
        _waterPointService = new InstalledAssetService<WaterPoint>(_dbContext);
        _waterFittingService = new InstalledAssetService<WaterFitting>(_dbContext);
        _waterValveService = new InstalledAssetService<WaterValve>(_dbContext);
        _waterHydrantService = new InstalledAssetService<WaterHydrant>(_dbContext);
        _waterMeterService = new InstalledAssetService<WaterMeter>(_dbContext);
        _waterLocateBoxService = new InstalledAssetService<WaterLocateBox>(_dbContext);

        // WW Init
        _wwGravityPipeService = new InstalledAssetService<WWGravityPipe>(_dbContext);
        _wwPressurePipeService = new InstalledAssetService<WWPressurePipe>(_dbContext);
        _wwPointService = new InstalledAssetService<WWPoint>(_dbContext);
        _wwFittingService = new InstalledAssetService<WWFitting>(_dbContext);
        _manholeService = new InstalledAssetService<Manhole>(_dbContext);
        _wwServicePointService = new InstalledAssetService<WWServicePoint>(_dbContext);
        _wwValveService = new InstalledAssetService<WWValve>(_dbContext);
        _wwLocateBoxService = new InstalledAssetService<WWLocateBox>(_dbContext);

        // Reclaimed Init
        _reclaimedPipeService = new InstalledAssetService<ReclaimedPipe>(_dbContext);
        _reclaimedPointService = new InstalledAssetService<ReclaimedPoint>(_dbContext);
        _reclaimedFittingService = new InstalledAssetService<ReclaimedFitting>(_dbContext);
        _reclaimedValveService = new InstalledAssetService<ReclaimedValve>(_dbContext);
        _reclaimedHydrantService = new InstalledAssetService<ReclaimedHydrant>(_dbContext);
        _reclaimedMeterService = new InstalledAssetService<ReclaimedMeter>(_dbContext);
        _reclaimedLocateBoxService = new InstalledAssetService<ReclaimedLocateBox>(_dbContext);
        
        // Chilled Init
        _chilledPipeService = new InstalledAssetService<ChilledPipe>(_dbContext);
        _chilledPointService = new InstalledAssetService<ChilledPoint>(_dbContext);
        _chilledFittingService = new InstalledAssetService<ChilledFitting>(_dbContext);
        _chilledValveService = new InstalledAssetService<ChilledValve>(_dbContext);
        _chilledMeterService = new InstalledAssetService<ChilledMeter>(_dbContext);
        _chilledLocateBoxService = new InstalledAssetService<ChilledLocateBox>(_dbContext);

        // Gas Init
        _gGravityPipeService = new InstalledAssetService<GGravityPipe>(_dbContext);
        _gPressurePipeService = new InstalledAssetService<GPressurePipe>(_dbContext);
        _gPointService = new InstalledAssetService<GPoint>(_dbContext);
        _gFittingService = new InstalledAssetService<GFitting>(_dbContext);
        _gManholeService = new InstalledAssetService<GManhole>(_dbContext);
        _gServicePointService = new InstalledAssetService<GServicePoint>(_dbContext);
        _gValveService = new InstalledAssetService<GValve>(_dbContext);
        _gLocateBoxService = new InstalledAssetService<GLocateBox>(_dbContext);

        // Electric Init
        _eGravityPipeService = new InstalledAssetService<EGravityPipe>(_dbContext);
        _ePressurePipeService = new InstalledAssetService<EPressurePipe>(_dbContext);
        _ePointService = new InstalledAssetService<EPoint>(_dbContext);
        _eFittingService = new InstalledAssetService<EFitting>(_dbContext);
        _eManholeService = new InstalledAssetService<EManhole>(_dbContext);
        _eServicePointService = new InstalledAssetService<EServicePoint>(_dbContext);
        _eValveService = new InstalledAssetService<EValve>(_dbContext);
        _eLocateBoxService = new InstalledAssetService<ELocateBox>(_dbContext);

        // Storm Init
        _stGravityPipeService = new InstalledAssetService<STGravityPipe>(_dbContext);
        _stPressurePipeService = new InstalledAssetService<STPressurePipe>(_dbContext);
        _stPointService = new InstalledAssetService<STPoint>(_dbContext);
        _stFittingService = new InstalledAssetService<STFitting>(_dbContext);
        _stManholeService = new InstalledAssetService<STManhole>(_dbContext);
        _stServicePointService = new InstalledAssetService<STServicePoint>(_dbContext);
        _stValveService = new InstalledAssetService<STValve>(_dbContext);
        _stLocateBoxService = new InstalledAssetService<STLocateBox>(_dbContext);
    }

    public async Task ReloadAsync()
    {
        if (!string.IsNullOrEmpty(_currentProjectId))
        {
            await LoadProjectAsync(_currentProjectId, _currentProjectNumber);
        }
    }

    public async Task LoadProjectAsync(string projectId, string projectNumber)
    {
        _currentProjectId = projectId;
        _currentProjectNumber = projectNumber;
        
        _dbContext.ChangeTracker.Clear();

        await _projectService.EnsureProjectExistsAsync(projectId, projectNumber, "Project " + projectNumber);

        async Task Load<T>(InstalledAssetService<T> service, ObservableCollection<T> collection) where T : InstalledAsset
        {
            var items = await service.LoadAsync(projectId);
            collection.Clear();
            foreach (var i in items) collection.Add(i);
        }

        await Load(_pipeCrossingService, PipeCrossings);
        var allFigures = await _figureService.LoadAsync(projectId);
        FigureAssets.Clear();
        foreach (var f in allFigures) FigureAssets.Add(f);

        // Water
        await Load(_waterPipeService, WaterPipes);
        await Load(_waterPointService, WaterPoints);
        await Load(_waterFittingService, WaterFittings);
        await Load(_waterValveService, WaterValves);
        await Load(_waterHydrantService, WaterHydrants);
        await Load(_waterMeterService, WaterMeters);
        await Load(_waterLocateBoxService, WaterLocateBoxes);

        // WW
        await Load(_wwGravityPipeService, WWGravityPipes);
        await Load(_wwPressurePipeService, WWPressurePipes);
        await Load(_wwPointService, WWPoints);
        await Load(_wwFittingService, WWFittings);
        await Load(_manholeService, Manholes);
        await Load(_wwServicePointService, WWServicePoints);
        await Load(_wwValveService, WWValves);
        await Load(_wwLocateBoxService, WWLocateBoxes);

        // Reclaimed
        await Load(_reclaimedPipeService, ReclaimedPipes);
        await Load(_reclaimedPointService, ReclaimedPoints);
        await Load(_reclaimedFittingService, ReclaimedFittings);
        await Load(_reclaimedValveService, ReclaimedValves);
        await Load(_reclaimedHydrantService, ReclaimedHydrants);
        await Load(_reclaimedMeterService, ReclaimedMeters);
        await Load(_reclaimedLocateBoxService, ReclaimedLocateBoxes);

        // Chilled
        await Load(_chilledPipeService, ChilledPipes);
        await Load(_chilledPointService, ChilledPoints);
        await Load(_chilledFittingService, ChilledFittings);
        await Load(_chilledValveService, ChilledValves);
        await Load(_chilledMeterService, ChilledMeters);
        await Load(_chilledLocateBoxService, ChilledLocateBoxes);

        // Gas
        await Load(_gGravityPipeService, GGravityPipes);
        await Load(_gPressurePipeService, GPressurePipes);
        await Load(_gPointService, GPoints);
        await Load(_gFittingService, GFittings);
        await Load(_gManholeService, GManholes);
        await Load(_gServicePointService, GServicePoints);
        await Load(_gValveService, GValves);
        await Load(_gLocateBoxService, GLocateBoxes);

        // Electric
        await Load(_eGravityPipeService, EGravityPipes);
        await Load(_ePressurePipeService, EPressurePipes);
        await Load(_ePointService, EPoints);
        await Load(_eFittingService, EFittings);
        await Load(_eManholeService, EManholes);
        await Load(_eServicePointService, EServicePoints);
        await Load(_eValveService, EValves);
        await Load(_eLocateBoxService, ELocateBoxes);

        // Storm
        await Load(_stGravityPipeService, STGravityPipes);
        await Load(_stPressurePipeService, STPressurePipes);
        await Load(_stPointService, STPoints);
        await Load(_stFittingService, STFittings);
        await Load(_stManholeService, STManholes);
        await Load(_stServicePointService, STServicePoints);
        await Load(_stValveService, STValves);
        await Load(_stLocateBoxService, STLocateBoxes);
    }

    public async Task SaveItemAsync(object item)
    {
        if (string.IsNullOrEmpty(_currentProjectId)) return;
        
        if (item is PipeCrossing pc) await _pipeCrossingService.UpsertAsync(_currentProjectId, pc);
        else if (item is Figure f)
        {
            if (string.IsNullOrEmpty(f.PartKey)) 
            {
                f.PartKey = "FIG-" + Guid.NewGuid().ToString().Substring(0, 5);
            }
            await _figureService.UpsertAsync(_currentProjectId, f);
        }
        
        // Water
        else if (item is WaterPipe wp) await _waterPipeService.UpsertAsync(_currentProjectId, wp);
        else if (item is WaterPoint wpo) await _waterPointService.UpsertAsync(_currentProjectId, wpo);
        else if (item is WaterFitting wf) await _waterFittingService.UpsertAsync(_currentProjectId, wf);
        else if (item is WaterValve wv) await _waterValveService.UpsertAsync(_currentProjectId, wv);
        else if (item is WaterHydrant wh) await _waterHydrantService.UpsertAsync(_currentProjectId, wh);
        else if (item is WaterMeter wm) await _waterMeterService.UpsertAsync(_currentProjectId, wm);
        else if (item is WaterLocateBox wlb) await _waterLocateBoxService.UpsertAsync(_currentProjectId, wlb);

        // WW
        else if (item is WWGravityPipe wwgp) await _wwGravityPipeService.UpsertAsync(_currentProjectId, wwgp);
        else if (item is WWPressurePipe wwpp) await _wwPressurePipeService.UpsertAsync(_currentProjectId, wwpp);
        else if (item is WWPoint wwp) await _wwPointService.UpsertAsync(_currentProjectId, wwp);
        else if (item is WWFitting wwf) await _wwFittingService.UpsertAsync(_currentProjectId, wwf);
        else if (item is Manhole man) await _manholeService.UpsertAsync(_currentProjectId, man);
        else if (item is WWServicePoint wwsp) await _wwServicePointService.UpsertAsync(_currentProjectId, wwsp);
        else if (item is WWValve wwv) await _wwValveService.UpsertAsync(_currentProjectId, wwv);
        else if (item is WWLocateBox wwlb) await _wwLocateBoxService.UpsertAsync(_currentProjectId, wwlb);

        // Reclaimed
        else if (item is ReclaimedPipe rp) await _reclaimedPipeService.UpsertAsync(_currentProjectId, rp);
        else if (item is ReclaimedPoint rpo) await _reclaimedPointService.UpsertAsync(_currentProjectId, rpo);
        else if (item is ReclaimedFitting rf) await _reclaimedFittingService.UpsertAsync(_currentProjectId, rf);
        else if (item is ReclaimedValve rv) await _reclaimedValveService.UpsertAsync(_currentProjectId, rv);
        else if (item is ReclaimedHydrant rh) await _reclaimedHydrantService.UpsertAsync(_currentProjectId, rh);
        else if (item is ReclaimedMeter rm) await _reclaimedMeterService.UpsertAsync(_currentProjectId, rm);
        else if (item is ReclaimedLocateBox rlb) await _reclaimedLocateBoxService.UpsertAsync(_currentProjectId, rlb);

        // Chilled
        else if (item is ChilledPipe cp) await _chilledPipeService.UpsertAsync(_currentProjectId, cp);
        else if (item is ChilledPoint cpo) await _chilledPointService.UpsertAsync(_currentProjectId, cpo);
        else if (item is ChilledFitting cf) await _chilledFittingService.UpsertAsync(_currentProjectId, cf);
        else if (item is ChilledValve cv) await _chilledValveService.UpsertAsync(_currentProjectId, cv);
        else if (item is ChilledMeter cm) await _chilledMeterService.UpsertAsync(_currentProjectId, cm);
        else if (item is ChilledLocateBox clb) await _chilledLocateBoxService.UpsertAsync(_currentProjectId, clb);

        // Gas
        else if (item is GGravityPipe ggp) await _gGravityPipeService.UpsertAsync(_currentProjectId, ggp);
        else if (item is GPressurePipe gpp) await _gPressurePipeService.UpsertAsync(_currentProjectId, gpp);
        else if (item is GPoint gp) await _gPointService.UpsertAsync(_currentProjectId, gp);
        else if (item is GFitting gf) await _gFittingService.UpsertAsync(_currentProjectId, gf);
        else if (item is GManhole gm) await _gManholeService.UpsertAsync(_currentProjectId, gm);
        else if (item is GServicePoint gsp) await _gServicePointService.UpsertAsync(_currentProjectId, gsp);
        else if (item is GValve gv) await _gValveService.UpsertAsync(_currentProjectId, gv);
        else if (item is GLocateBox glb) await _gLocateBoxService.UpsertAsync(_currentProjectId, glb);

        // Electric
        else if (item is EGravityPipe egp) await _eGravityPipeService.UpsertAsync(_currentProjectId, egp);
        else if (item is EPressurePipe epp) await _ePressurePipeService.UpsertAsync(_currentProjectId, epp);
        else if (item is EPoint ep) await _ePointService.UpsertAsync(_currentProjectId, ep);
        else if (item is EFitting ef) await _eFittingService.UpsertAsync(_currentProjectId, ef);
        else if (item is EManhole em) await _eManholeService.UpsertAsync(_currentProjectId, em);
        else if (item is EServicePoint esp) await _eServicePointService.UpsertAsync(_currentProjectId, esp);
        else if (item is EValve ev) await _eValveService.UpsertAsync(_currentProjectId, ev);
        else if (item is ELocateBox elb) await _eLocateBoxService.UpsertAsync(_currentProjectId, elb);

        // Storm
        else if (item is STGravityPipe stgp) await _stGravityPipeService.UpsertAsync(_currentProjectId, stgp);
        else if (item is STPressurePipe stpp) await _stPressurePipeService.UpsertAsync(_currentProjectId, stpp);
        else if (item is STPoint stp) await _stPointService.UpsertAsync(_currentProjectId, stp);
        else if (item is STFitting stf) await _stFittingService.UpsertAsync(_currentProjectId, stf);
        else if (item is STManhole stm) await _stManholeService.UpsertAsync(_currentProjectId, stm);
        else if (item is STServicePoint stsp) await _stServicePointService.UpsertAsync(_currentProjectId, stsp);
        else if (item is STValve stv) await _stValveService.UpsertAsync(_currentProjectId, stv);
        else if (item is STLocateBox stlb) await _stLocateBoxService.UpsertAsync(_currentProjectId, stlb);
    }

    public async Task AddItemAsync(InstalledAsset item)
    {
        // Add to appropriate collection and save
        if (item is PipeCrossing pc) { PipeCrossings.Add(pc); await _pipeCrossingService.UpsertAsync(_currentProjectId, pc); }
        else if (item is Figure f) 
        { 
            if (string.IsNullOrEmpty(f.PartKey)) 
            {
                f.PartKey = "FIG-" + Guid.NewGuid().ToString().Substring(0, 5);
            }
            
            FigureAssets.Add(f);

            await _figureService.UpsertAsync(_currentProjectId, f); 
        }
        
        // Water
        else if (item is WaterPipe wp) { WaterPipes.Add(wp); await _waterPipeService.UpsertAsync(_currentProjectId, wp); }
        else if (item is WaterPoint wpo) { WaterPoints.Add(wpo); await _waterPointService.UpsertAsync(_currentProjectId, wpo); }
        else if (item is WaterFitting wf) { WaterFittings.Add(wf); await _waterFittingService.UpsertAsync(_currentProjectId, wf); }
        else if (item is WaterValve wv) { WaterValves.Add(wv); await _waterValveService.UpsertAsync(_currentProjectId, wv); }
        else if (item is WaterHydrant wh) { WaterHydrants.Add(wh); await _waterHydrantService.UpsertAsync(_currentProjectId, wh); }
        else if (item is WaterMeter wm) { WaterMeters.Add(wm); await _waterMeterService.UpsertAsync(_currentProjectId, wm); }
        else if (item is WaterLocateBox wlb) { WaterLocateBoxes.Add(wlb); await _waterLocateBoxService.UpsertAsync(_currentProjectId, wlb); }

        // WW
        else if (item is WWGravityPipe wwgp) { WWGravityPipes.Add(wwgp); await _wwGravityPipeService.UpsertAsync(_currentProjectId, wwgp); }
        else if (item is WWPressurePipe wwpp) { WWPressurePipes.Add(wwpp); await _wwPressurePipeService.UpsertAsync(_currentProjectId, wwpp); }
        else if (item is WWPoint wwp) { WWPoints.Add(wwp); await _wwPointService.UpsertAsync(_currentProjectId, wwp); }
        else if (item is WWFitting wwf) { WWFittings.Add(wwf); await _wwFittingService.UpsertAsync(_currentProjectId, wwf); }
        else if (item is Manhole man) { Manholes.Add(man); await _manholeService.UpsertAsync(_currentProjectId, man); }
        else if (item is WWServicePoint wwsp) { WWServicePoints.Add(wwsp); await _wwServicePointService.UpsertAsync(_currentProjectId, wwsp); }
        else if (item is WWValve wwv) { WWValves.Add(wwv); await _wwValveService.UpsertAsync(_currentProjectId, wwv); }
        else if (item is WWLocateBox wwlb) { WWLocateBoxes.Add(wwlb); await _wwLocateBoxService.UpsertAsync(_currentProjectId, wwlb); }

        // Reclaimed
        else if (item is ReclaimedPipe rp) { ReclaimedPipes.Add(rp); await _reclaimedPipeService.UpsertAsync(_currentProjectId, rp); }
        else if (item is ReclaimedPoint rpo) { ReclaimedPoints.Add(rpo); await _reclaimedPointService.UpsertAsync(_currentProjectId, rpo); }
        else if (item is ReclaimedFitting rf) { ReclaimedFittings.Add(rf); await _reclaimedFittingService.UpsertAsync(_currentProjectId, rf); }
        else if (item is ReclaimedValve rv) { ReclaimedValves.Add(rv); await _reclaimedValveService.UpsertAsync(_currentProjectId, rv); }
        else if (item is ReclaimedHydrant rh) { ReclaimedHydrants.Add(rh); await _reclaimedHydrantService.UpsertAsync(_currentProjectId, rh); }
        else if (item is ReclaimedMeter rm) { ReclaimedMeters.Add(rm); await _reclaimedMeterService.UpsertAsync(_currentProjectId, rm); }
        else if (item is ReclaimedLocateBox rlb) { ReclaimedLocateBoxes.Add(rlb); await _reclaimedLocateBoxService.UpsertAsync(_currentProjectId, rlb); }

        // Chilled
        else if (item is ChilledPipe cp) { ChilledPipes.Add(cp); await _chilledPipeService.UpsertAsync(_currentProjectId, cp); }
        else if (item is ChilledPoint cpo) { ChilledPoints.Add(cpo); await _chilledPointService.UpsertAsync(_currentProjectId, cpo); }
        else if (item is ChilledFitting cf) { ChilledFittings.Add(cf); await _chilledFittingService.UpsertAsync(_currentProjectId, cf); }
        else if (item is ChilledValve cv) { ChilledValves.Add(cv); await _chilledValveService.UpsertAsync(_currentProjectId, cv); }
        else if (item is ChilledMeter cm) { ChilledMeters.Add(cm); await _chilledMeterService.UpsertAsync(_currentProjectId, cm); }
        else if (item is ChilledLocateBox clb) { ChilledLocateBoxes.Add(clb); await _chilledLocateBoxService.UpsertAsync(_currentProjectId, clb); }

        // Gas
        else if (item is GGravityPipe ggp) { GGravityPipes.Add(ggp); await _gGravityPipeService.UpsertAsync(_currentProjectId, ggp); }
        else if (item is GPressurePipe gpp) { GPressurePipes.Add(gpp); await _gPressurePipeService.UpsertAsync(_currentProjectId, gpp); }
        else if (item is GPoint gp) { GPoints.Add(gp); await _gPointService.UpsertAsync(_currentProjectId, gp); }
        else if (item is GFitting gf) { GFittings.Add(gf); await _gFittingService.UpsertAsync(_currentProjectId, gf); }
        else if (item is GManhole gm) { GManholes.Add(gm); await _gManholeService.UpsertAsync(_currentProjectId, gm); }
        else if (item is GServicePoint gsp) { GServicePoints.Add(gsp); await _gServicePointService.UpsertAsync(_currentProjectId, gsp); }
        else if (item is GValve gv) { GValves.Add(gv); await _gValveService.UpsertAsync(_currentProjectId, gv); }
        else if (item is GLocateBox glb) { GLocateBoxes.Add(glb); await _gLocateBoxService.UpsertAsync(_currentProjectId, glb); }

        // Electric
        else if (item is EGravityPipe egp) { EGravityPipes.Add(egp); await _eGravityPipeService.UpsertAsync(_currentProjectId, egp); }
        else if (item is EPressurePipe epp) { EPressurePipes.Add(epp); await _ePressurePipeService.UpsertAsync(_currentProjectId, epp); }
        else if (item is EPoint ep) { EPoints.Add(ep); await _ePointService.UpsertAsync(_currentProjectId, ep); }
        else if (item is EFitting ef) { EFittings.Add(ef); await _eFittingService.UpsertAsync(_currentProjectId, ef); }
        else if (item is EManhole em) { EManholes.Add(em); await _eManholeService.UpsertAsync(_currentProjectId, em); }
        else if (item is EServicePoint esp) { EServicePoints.Add(esp); await _eServicePointService.UpsertAsync(_currentProjectId, esp); }
        else if (item is EValve ev) { EValves.Add(ev); await _eValveService.UpsertAsync(_currentProjectId, ev); }
        else if (item is ELocateBox elb) { ELocateBoxes.Add(elb); await _eLocateBoxService.UpsertAsync(_currentProjectId, elb); }

        // Storm
        else if (item is STGravityPipe stgp) { STGravityPipes.Add(stgp); await _stGravityPipeService.UpsertAsync(_currentProjectId, stgp); }
        else if (item is STPressurePipe stpp) { STPressurePipes.Add(stpp); await _stPressurePipeService.UpsertAsync(_currentProjectId, stpp); }
        else if (item is STPoint stp) { STPoints.Add(stp); await _stPointService.UpsertAsync(_currentProjectId, stp); }
        else if (item is STFitting stf) { STFittings.Add(stf); await _stFittingService.UpsertAsync(_currentProjectId, stf); }
        else if (item is STManhole stm) { STManholes.Add(stm); await _stManholeService.UpsertAsync(_currentProjectId, stm); }
        else if (item is STServicePoint stsp) { STServicePoints.Add(stsp); await _stServicePointService.UpsertAsync(_currentProjectId, stsp); }
        else if (item is STValve stv) { STValves.Add(stv); await _stValveService.UpsertAsync(_currentProjectId, stv); }
        else if (item is STLocateBox stlb) { STLocateBoxes.Add(stlb); await _stLocateBoxService.UpsertAsync(_currentProjectId, stlb); }
    }

    public async Task DeleteAssetAsync(InstalledAsset item)
    {
        if (item is PipeCrossing pc) await _pipeCrossingService.DeleteAsync(_currentProjectId, pc.Id);
        else if (item is Figure f) await _figureService.DeleteAsync(_currentProjectId, f.Id);
        
        // Water
        else if (item is WaterPipe wp) await _waterPipeService.DeleteAsync(_currentProjectId, wp.Id);
        else if (item is WaterPoint wpo) await _waterPointService.DeleteAsync(_currentProjectId, wpo.Id);
        else if (item is WaterFitting wf) await _waterFittingService.DeleteAsync(_currentProjectId, wf.Id);
        else if (item is WaterValve wv) await _waterValveService.DeleteAsync(_currentProjectId, wv.Id);
        else if (item is WaterHydrant wh) await _waterHydrantService.DeleteAsync(_currentProjectId, wh.Id);
        else if (item is WaterMeter wm) await _waterMeterService.DeleteAsync(_currentProjectId, wm.Id);
        else if (item is WaterLocateBox wlb) await _waterLocateBoxService.DeleteAsync(_currentProjectId, wlb.Id);

        // WW
        else if (item is WWGravityPipe wwgp) await _wwGravityPipeService.DeleteAsync(_currentProjectId, wwgp.Id);
        else if (item is WWPressurePipe wwpp) await _wwPressurePipeService.DeleteAsync(_currentProjectId, wwpp.Id);
        else if (item is WWPoint wwp) await _wwPointService.DeleteAsync(_currentProjectId, wwp.Id);
        else if (item is WWFitting wwf) await _wwFittingService.DeleteAsync(_currentProjectId, wwf.Id);
        else if (item is Manhole man) await _manholeService.DeleteAsync(_currentProjectId, man.Id);
        else if (item is WWServicePoint wwsp) await _wwServicePointService.DeleteAsync(_currentProjectId, wwsp.Id);
        else if (item is WWValve wwv) await _wwValveService.DeleteAsync(_currentProjectId, wwv.Id);
        else if (item is WWLocateBox wwlb) await _wwLocateBoxService.DeleteAsync(_currentProjectId, wwlb.Id);

        // Reclaimed
        else if (item is ReclaimedPipe rp) await _reclaimedPipeService.DeleteAsync(_currentProjectId, rp.Id);
        else if (item is ReclaimedPoint rpo) await _reclaimedPointService.DeleteAsync(_currentProjectId, rpo.Id);
        else if (item is ReclaimedFitting rf) await _reclaimedFittingService.DeleteAsync(_currentProjectId, rf.Id);
        else if (item is ReclaimedValve rv) await _reclaimedValveService.DeleteAsync(_currentProjectId, rv.Id);
        else if (item is ReclaimedHydrant rh) await _reclaimedHydrantService.DeleteAsync(_currentProjectId, rh.Id);
        else if (item is ReclaimedMeter rm) await _reclaimedMeterService.DeleteAsync(_currentProjectId, rm.Id);
        else if (item is ReclaimedLocateBox rlb) await _reclaimedLocateBoxService.DeleteAsync(_currentProjectId, rlb.Id);

        // Chilled
        else if (item is ChilledPipe cp) await _chilledPipeService.DeleteAsync(_currentProjectId, cp.Id);
        else if (item is ChilledPoint cpo) await _chilledPointService.DeleteAsync(_currentProjectId, cpo.Id);
        else if (item is ChilledFitting cf) await _chilledFittingService.DeleteAsync(_currentProjectId, cf.Id);
        else if (item is ChilledValve cv) await _chilledValveService.DeleteAsync(_currentProjectId, cv.Id);
        else if (item is ChilledMeter cm) await _chilledMeterService.DeleteAsync(_currentProjectId, cm.Id);
        else if (item is ChilledLocateBox clb) await _chilledLocateBoxService.DeleteAsync(_currentProjectId, clb.Id);

        // Gas
        else if (item is GGravityPipe ggp) await _gGravityPipeService.DeleteAsync(_currentProjectId, ggp.Id);
        else if (item is GPressurePipe gpp) await _gPressurePipeService.DeleteAsync(_currentProjectId, gpp.Id);
        else if (item is GPoint gp) await _gPointService.DeleteAsync(_currentProjectId, gp.Id);
        else if (item is GFitting gf) await _gFittingService.DeleteAsync(_currentProjectId, gf.Id);
        else if (item is GManhole gm) await _gManholeService.DeleteAsync(_currentProjectId, gm.Id);
        else if (item is GServicePoint gsp) await _gServicePointService.DeleteAsync(_currentProjectId, gsp.Id);
        else if (item is GValve gv) await _gValveService.DeleteAsync(_currentProjectId, gv.Id);
        else if (item is GLocateBox glb) await _gLocateBoxService.DeleteAsync(_currentProjectId, glb.Id);

        // Electric
        else if (item is EGravityPipe egp) await _eGravityPipeService.DeleteAsync(_currentProjectId, egp.Id);
        else if (item is EPressurePipe epp) await _ePressurePipeService.DeleteAsync(_currentProjectId, epp.Id);
        else if (item is EPoint ep) await _ePointService.DeleteAsync(_currentProjectId, ep.Id);
        else if (item is EFitting ef) await _eFittingService.DeleteAsync(_currentProjectId, ef.Id);
        else if (item is EManhole em) await _eManholeService.DeleteAsync(_currentProjectId, em.Id);
        else if (item is EServicePoint esp) await _eServicePointService.DeleteAsync(_currentProjectId, esp.Id);
        else if (item is EValve ev) await _eValveService.DeleteAsync(_currentProjectId, ev.Id);
        else if (item is ELocateBox elb) await _eLocateBoxService.DeleteAsync(_currentProjectId, elb.Id);

        // Storm
        else if (item is STGravityPipe stgp) await _stGravityPipeService.DeleteAsync(_currentProjectId, stgp.Id);
        else if (item is STPressurePipe stpp) await _stPressurePipeService.DeleteAsync(_currentProjectId, stpp.Id);
        else if (item is STPoint stp) await _stPointService.DeleteAsync(_currentProjectId, stp.Id);
        else if (item is STFitting stf) await _stFittingService.DeleteAsync(_currentProjectId, stf.Id);
        else if (item is STManhole stm) await _stManholeService.DeleteAsync(_currentProjectId, stm.Id);
        else if (item is STServicePoint stsp) await _stServicePointService.DeleteAsync(_currentProjectId, stsp.Id);
        else if (item is STValve stv) await _stValveService.DeleteAsync(_currentProjectId, stv.Id);
        else if (item is STLocateBox stlb) await _stLocateBoxService.DeleteAsync(_currentProjectId, stlb.Id);
    }

    public void ExportToFolder(string baseName)
    {
        string dir = System.IO.Path.GetDirectoryName(baseName) ?? "";
        string name = System.IO.Path.GetFileNameWithoutExtension(baseName);
        
        string C(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        // Helper to write CSV
        void Write<T>(string suffix, ObservableCollection<T> items, string header, Func<T, string> formatter)
        {
            string path = System.IO.Path.Combine(dir, $"{name}_{suffix}.csv");
            using var sw = new System.IO.StreamWriter(path);
            sw.WriteLine(header);
            foreach(var item in items) sw.WriteLine(formatter(item));
        }

        string hCrossing = "PartKey,Description,Northing,Easting,Notes,Manufacturer,Size,Material,Year,Confidence,Source,Warning";
        string hFigure = "AssetId,Name,Layer,Description,ScriptContent";
        string hPipe = "PartKey,Description,Diameter,Size,Material,N_Start,E_Start,N_End,E_End,Inv_Start,Inv_End,Notes,Manufacturer,Year,Confidence,Source,Warning";
        string hPoint = "PartKey,Description,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning";
        string hFitting = "PartKey,Description,Type,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning";
        string hMeter = "PartKey,Description,Size,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning";

        // Pipe Crossings
        Write("PipeCrossings", PipeCrossings, hCrossing, i => 
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{C(i.Notes)},{C(i.Manufacturer)},{C(i.Size)},{C(i.Material)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}");

        // Figure Assets
        Write("FigureAssets", FigureAssets, hFigure, i =>
            $"{C(i.PartKey)},{C(i.Name)},{C(i.Layer)},{C(i.DescriptionText)},{C(i.ScriptContent)}");

        // Formatters
        string FormatPipe<T>(T i) where T : Pipe => 
            $"{C(i.PartKey)},{C(i.Description)},{i.Diameter},{C(i.Size)},{C(i.Material)},{i.NorthingStart},{i.EastingStart},{i.NorthingEnd},{i.EastingEnd},{i.InvertStart},{i.InvertEnd},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";

        string FormatPoint<T>(T i) where T : Structure => 
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";

        string FormatFitting<T>(T i) where T : Fitting => 
            $"{C(i.PartKey)},{C(i.Description)},{C(i.Type)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
            
        string FormatValve<T>(T i) where T : Valve => 
            $"{C(i.PartKey)},{C(i.Description)},{C(i.Type)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
        
        string FormatMeter<T>(T i) where T : Meter => 
            $"{C(i.PartKey)},{C(i.Description)},{C(i.Size)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
            
        string FormatHydrant<T>(T i) where T : Hydrant => 
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
            
        string FormatLocateBox<T>(T i) where T : LocateBox => 
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";

        // Export Calls
        // Water
        Write("WaterPipeRun", WaterPipes, hPipe, FormatPipe);
        Write("WaterPointsAlongPipe", WaterPoints, hPoint, FormatPoint);
        Write("WaterFitting", WaterFittings, hFitting, FormatFitting);
        Write("WaterValve", WaterValves, hFitting, FormatValve);
        Write("WaterHydrant", WaterHydrants, hPoint, FormatHydrant);
        Write("WaterMeter", WaterMeters, hMeter, FormatMeter);
        Write("WaterLocateBox", WaterLocateBoxes, hPoint, FormatLocateBox);

        // WW
        Write("WWGravityPipeRun", WWGravityPipes, hPipe, FormatPipe);
        Write("WWPressurePipeRun", WWPressurePipes, hPipe, FormatPipe);
        Write("WWPointsAlongPipe", WWPoints, hPoint, FormatPoint);
        Write("WWFitting", WWFittings, hFitting, FormatFitting);
        Write("Manhole", Manholes, hPoint, FormatPoint);
        Write("WWServicePointMeter", WWServicePoints, hPoint, FormatPoint);
        Write("WWValve", WWValves, hFitting, FormatValve);
        Write("WWLocateBox", WWLocateBoxes, hPoint, FormatLocateBox);

        // Reclaimed
        Write("ReclaimedPipeRun", ReclaimedPipes, hPipe, FormatPipe);
        Write("ReclaimedPointsAlongPipe", ReclaimedPoints, hPoint, FormatPoint);
        Write("ReclaimedFitting", ReclaimedFittings, hFitting, FormatFitting);
        Write("ReclaimedValve", ReclaimedValves, hFitting, FormatValve);
        Write("ReclaimedHydrant", ReclaimedHydrants, hPoint, FormatHydrant);
        Write("ReclaimedMeter", ReclaimedMeters, hMeter, FormatMeter);
        Write("ReclaimedLocateBox", ReclaimedLocateBoxes, hPoint, FormatLocateBox);

        // Chilled
        Write("ChilledPipeRun", ChilledPipes, hPipe, FormatPipe);
        Write("ChilledPointsAlongPipe", ChilledPoints, hPoint, FormatPoint);
        Write("ChilledFitting", ChilledFittings, hFitting, FormatFitting);
        Write("ChilledValve", ChilledValves, hFitting, FormatValve);
        Write("ChilledMeter", ChilledMeters, hMeter, FormatMeter);
        Write("ChilledLocateBox", ChilledLocateBoxes, hPoint, FormatLocateBox);

        // Gas
        Write("GasGravityPipeRun", GGravityPipes, hPipe, FormatPipe);
        Write("GasPressurePipeRun", GPressurePipes, hPipe, FormatPipe);
        Write("GasPointsAlongPipe", GPoints, hPoint, FormatPoint);
        Write("GasFitting", GFittings, hFitting, FormatFitting);
        Write("GasManhole", GManholes, hPoint, FormatPoint);
        Write("GasServicePointMeter", GServicePoints, hPoint, FormatPoint);
        Write("GasValve", GValves, hFitting, FormatValve);
        Write("GasLocateBox", GLocateBoxes, hPoint, FormatLocateBox);

        // Electric
        Write("ElectricGravityPipeRun", EGravityPipes, hPipe, FormatPipe);
        Write("ElectricPressurePipeRun", EPressurePipes, hPipe, FormatPipe);
        Write("ElectricPointsAlongPipe", EPoints, hPoint, FormatPoint);
        Write("ElectricFitting", EFittings, hFitting, FormatFitting);
        Write("ElectricManhole", EManholes, hPoint, FormatPoint);
        Write("ElectricServicePointMeter", EServicePoints, hPoint, FormatPoint);
        Write("ElectricValve", EValves, hFitting, FormatValve);
        Write("ElectricLocateBox", ELocateBoxes, hPoint, FormatLocateBox);

        // Storm
        Write("STGravityPipeRun", STGravityPipes, hPipe, FormatPipe);
        Write("STPressurePipeRun", STPressurePipes, hPipe, FormatPipe);
        Write("STPointsAlongPipe", STPoints, hPoint, FormatPoint);
        Write("STFitting", STFittings, hFitting, FormatFitting);
        Write("STManhole", STManholes, hPoint, FormatPoint);
        Write("STServicePointMeter", STServicePoints, hPoint, FormatPoint);
        Write("STValve", STValves, hFitting, FormatValve);
        Write("STLocateBox", STLocateBoxes, hPoint, FormatLocateBox);
    }

    public void ExportAllToSingleFile(string path, string format)
    {
        string C(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        bool isTab = format.Equals("txt", StringComparison.OrdinalIgnoreCase);
        bool isExcel = format.Equals("xls", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase);

        System.IO.StreamWriter? sw = null;
        ClosedXML.Excel.XLWorkbook? wb = null;
        ClosedXML.Excel.IXLWorksheet? ws = null;
        int currentRow = 1;
        string currentSheet = "";

        if (isExcel) 
            wb = new ClosedXML.Excel.XLWorkbook();
        else
            sw = new System.IO.StreamWriter(path);
            
        void Write<T>(string discipline, string title, string[] headers, ObservableCollection<T> items, Func<T, object?[]> formatter)
        {
            if (isExcel)
            {
                if (currentSheet != discipline)
                {
                    currentSheet = discipline;
                    ws = wb!.Worksheets.Add(discipline);
                    currentRow = 1;

                    // Header Setup for easier viewing
                    ws.SheetView.FreezeRows(1);
                }
                
                if (currentRow > 1) currentRow++;
                
                ws!.Cell(currentRow, 1).Value = $"--- {title} ---";
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;
                
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(currentRow, i + 1).Value = headers[i];
                    ws.Cell(currentRow, i + 1).Style.Font.Bold = true;
                }
                currentRow++;
                
                foreach(var item in items)
                {
                    var vals = formatter(item);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        var v = vals[i];
                        if (v is double d) ws.Cell(currentRow, i + 1).Value = d;
                        else if (v is int integer) ws.Cell(currentRow, i + 1).Value = integer;
                        else ws.Cell(currentRow, i + 1).Value = v?.ToString() ?? "";
                    }
                    currentRow++;
                }
            }
            else
            {
                string delim = isTab ? "\t" : ",";
                sw!.WriteLine($"--- {title} ---");
                sw.WriteLine(string.Join(delim, headers));
                
                foreach(var item in items)
                {
                    var vals = formatter(item);
                    var strVals = System.Linq.Enumerable.Select(vals, v => {
                        if (v == null) return "";
                        if (v is double || v is int) return v.ToString();
                        return isTab ? (v.ToString() ?? "").Replace("\t", " ") : C(v.ToString());
                    });
                    sw.WriteLine(string.Join(delim, strVals));
                }
                sw.WriteLine();
            }
        }

        string[] hGlobal = new[] { "PartKey", "Discipline", "FeatureType", "Subtype", "FacilityOwner", "Size", "SizeSecondary", "Material", "PipeClass", "LiningManufacturer", "LiningMaterial", "Orientation", "PipeRole", "RfidBarcode", "DropType", "InvertElevationsWithDirections", "ExteriorJointTapeType", "ExteriorJointTapeManufacturer", "Quantity", "Manufacturer", "ManufacturerPartNo", "YearManufactured", "Confidence", "Source", "Warning", "Notes" };

        string[] hCrossing = hGlobal.Concat(new[] { "Description", "Northing", "Easting", "CrossingNumber", "UpperPipeType", "UpperPipeSize", "FinishedGradeElevation", "UpperPipeTopElevation", "UpperCover", "UpperPipeBottomElevation", "LowerPipeType", "LowerPipeSize", "LowerPipeTopElevation", "LowerCover", "Separation" }).ToArray();
        string[] hFigure = new[] { "AssetId", "Name", "Layer", "Description", "ScriptContent" };
        string[] hPipe = hGlobal.Concat(new[] { "Description", "Diameter", "NorthingStart", "EastingStart", "NorthingEnd", "EastingEnd", "InvertStart", "InvertEnd", "GradeElevationAtInvertStart", "GradeElevationAtInvertEnd" }).ToArray();
        string[] hPoint = hGlobal.Concat(new[] { "Description", "Northing", "Easting", "Elevation" }).ToArray();
        string[] hFitting = hGlobal.Concat(new[] { "Description", "Type", "Northing", "Easting", "Elevation" }).ToArray();
        string[] hValve = hGlobal.Concat(new[] { "Description", "Type", "Northing", "Easting", "Elevation", "OpenDirection", "TurnsToOpen", "NutElevation" }).ToArray();
        string[] hMeter = hGlobal.Concat(new[] { "Description", "Northing", "Easting", "Elevation" }).ToArray();

        object?[] GetGlobals(InstalledAsset i) => new object?[] { i.PartKey, i.Discipline, i.FeatureType, i.Subtype, i.FacilityOwner, i.Size, i.SizeSecondary, i.Material, i.PipeClass, i.LiningManufacturer, i.LiningMaterial, i.Orientation, i.PipeRole, i.RfidBarcode, i.DropType, i.InvertElevationsWithDirections, i.ExteriorJointTapeType, i.ExteriorJointTapeManufacturer, i.Quantity, i.Manufacturer, i.ManufacturerPartNo, i.YearManufactured, i.Confidence, i.Source, i.Warning, i.Notes };

        object?[] FormatCrossing(PipeCrossing i) => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.CrossingNumber, i.UpperPipeType, i.UpperPipeSize, i.FinishedGradeElevation, i.UpperPipeTopElevation, i.UpperCover, i.UpperPipeBottomElevation, i.LowerPipeType, i.LowerPipeSize, i.LowerPipeTopElevation, i.LowerCover, i.Separation }).ToArray();
        object?[] FormatPipe<T>(T i) where T : Pipe => GetGlobals(i).Concat(new object?[] { i.Description, i.Diameter, i.NorthingStart, i.EastingStart, i.NorthingEnd, i.EastingEnd, i.InvertStart, i.InvertEnd, i.GradeElevationAtInvertStart, i.GradeElevationAtInvertEnd }).ToArray();
        object?[] FormatPoint<T>(T i) where T : Structure => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatFitting<T>(T i) where T : Fitting => GetGlobals(i).Concat(new object?[] { i.Description, i.Type, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatValve<T>(T i) where T : Valve => GetGlobals(i).Concat(new object?[] { i.Description, i.Type, i.Northing, i.Easting, i.Elevation, i.OpenDirection, i.TurnsToOpen, i.NutElevation }).ToArray();
        object?[] FormatMeter<T>(T i) where T : Meter => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatHydrant<T>(T i) where T : Hydrant => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatLocateBox<T>(T i) where T : LocateBox => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();

        Write("General", "PipeCrossings", hCrossing, PipeCrossings, FormatCrossing);
        Write("General", "FigureAssets", hFigure, FigureAssets, i => new object?[] { i.PartKey, i.Name, i.Layer, i.DescriptionText, i.ScriptContent });

        Write("Water", "WaterPipeRun", hPipe, WaterPipes, FormatPipe);
        Write("Water", "WaterPointsAlongPipe", hPoint, WaterPoints, FormatPoint);
        Write("Water", "WaterFitting", hFitting, WaterFittings, FormatFitting);
        Write("Water", "WaterValve", hValve, WaterValves, FormatValve);
        Write("Water", "WaterHydrant", hPoint, WaterHydrants, FormatHydrant);
        Write("Water", "WaterMeter", hMeter, WaterMeters, FormatMeter);
        Write("Water", "WaterLocateBox", hPoint, WaterLocateBoxes, FormatLocateBox);

        Write("WW", "WWGravityPipeRun", hPipe, WWGravityPipes, FormatPipe);
        Write("WW", "WWPressurePipeRun", hPipe, WWPressurePipes, FormatPipe);
        Write("WW", "WWPointsAlongPipe", hPoint, WWPoints, FormatPoint);
        Write("WW", "WWFitting", hFitting, WWFittings, FormatFitting);
        Write("WW", "Manhole", hPoint, Manholes, FormatPoint);
        Write("WW", "WWServicePointMeter", hPoint, WWServicePoints, FormatPoint);
        Write("WW", "WWValve", hValve, WWValves, FormatValve);
        Write("WW", "WWLocateBox", hPoint, WWLocateBoxes, FormatLocateBox);

        Write("Reclaimed", "ReclaimedPipeRun", hPipe, ReclaimedPipes, FormatPipe);
        Write("Reclaimed", "ReclaimedPointsAlongPipe", hPoint, ReclaimedPoints, FormatPoint);
        Write("Reclaimed", "ReclaimedFitting", hFitting, ReclaimedFittings, FormatFitting);
        Write("Reclaimed", "ReclaimedValve", hValve, ReclaimedValves, FormatValve);
        Write("Reclaimed", "ReclaimedHydrant", hPoint, ReclaimedHydrants, FormatHydrant);
        Write("Reclaimed", "ReclaimedMeter", hMeter, ReclaimedMeters, FormatMeter);
        Write("Reclaimed", "ReclaimedLocateBox", hPoint, ReclaimedLocateBoxes, FormatLocateBox);

        Write("Chilled", "ChilledPipeRun", hPipe, ChilledPipes, FormatPipe);
        Write("Chilled", "ChilledPointsAlongPipe", hPoint, ChilledPoints, FormatPoint);
        Write("Chilled", "ChilledFitting", hFitting, ChilledFittings, FormatFitting);
        Write("Chilled", "ChilledValve", hValve, ChilledValves, FormatValve);
        Write("Chilled", "ChilledMeter", hMeter, ChilledMeters, FormatMeter);
        Write("Chilled", "ChilledLocateBox", hPoint, ChilledLocateBoxes, FormatLocateBox);

        Write("Gas", "GasGravityPipeRun", hPipe, GGravityPipes, FormatPipe);
        Write("Gas", "GasPressurePipeRun", hPipe, GPressurePipes, FormatPipe);
        Write("Gas", "GasPointsAlongPipe", hPoint, GPoints, FormatPoint);
        Write("Gas", "GasFitting", hFitting, GFittings, FormatFitting);
        Write("Gas", "GasManhole", hPoint, GManholes, FormatPoint);
        Write("Gas", "GasServicePointMeter", hPoint, GServicePoints, FormatPoint);
        Write("Gas", "GasValve", hValve, GValves, FormatValve);
        Write("Gas", "GasLocateBox", hPoint, GLocateBoxes, FormatLocateBox);

        Write("Electric", "ElectricGravityPipeRun", hPipe, EGravityPipes, FormatPipe);
        Write("Electric", "ElectricPressurePipeRun", hPipe, EPressurePipes, FormatPipe);
        Write("Electric", "ElectricPointsAlongPipe", hPoint, EPoints, FormatPoint);
        Write("Electric", "ElectricFitting", hFitting, EFittings, FormatFitting);
        Write("Electric", "ElectricManhole", hPoint, EManholes, FormatPoint);
        Write("Electric", "ElectricServicePointMeter", hPoint, EServicePoints, FormatPoint);
        Write("Electric", "ElectricValve", hValve, EValves, FormatValve);
        Write("Electric", "ElectricLocateBox", hPoint, ELocateBoxes, FormatLocateBox);

        Write("Storm", "STGravityPipeRun", hPipe, STGravityPipes, FormatPipe);
        Write("Storm", "STPressurePipeRun", hPipe, STPressurePipes, FormatPipe);
        Write("Storm", "STPointsAlongPipe", hPoint, STPoints, FormatPoint);
        Write("Storm", "STFitting", hFitting, STFittings, FormatFitting);
        Write("Storm", "STManhole", hPoint, STManholes, FormatPoint);
        Write("Storm", "STServicePointMeter", hPoint, STServicePoints, FormatPoint);
        Write("Storm", "STValve", hValve, STValves, FormatValve);
        Write("Storm", "STLocateBox", hPoint, STLocateBoxes, FormatLocateBox);

        if (isExcel)
        {
            foreach (var worksheet in wb!.Worksheets)
            {
                worksheet.Columns().AdjustToContents();
            }
            wb.SaveAs(path);
            wb.Dispose();
        }
        else
        {
            sw?.Dispose();
        }
    }

    public void ExportUtilityReportZeroFoot(string path)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Utility Report-0-ft");
        
        // Define Headers
        string[] headers = new[]
        {
            "Pipe Run Number (WM#)",
            "Pipe Subtype",
            "Facility Owner",
            "Pipe Size (Inches)",
            "Pipe Class",
            "Pipe Manufacturer",
            "Pipe Material",
            "Pipe Lining Manufacturer",
            "Pipe Lining Material",
            "Measured Length (Feet)"
        };

        // Write Headers
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Freeze top row
        ws.SheetView.FreezeRows(1);

        int currentRow = 2;

        // Process Water Pipes
        foreach (var pipe in WaterPipes)
        {
            double nRun = (pipe.NorthingEnd ?? 0.0) - (pipe.NorthingStart ?? 0.0);
            double eRun = (pipe.EastingEnd ?? 0.0) - (pipe.EastingStart ?? 0.0);
            double measuredLength = Math.Round(Math.Sqrt(nRun * nRun + eRun * eRun), 2);

            ws.Cell(currentRow, 1).Value = pipe.PartKey ?? "";
            ws.Cell(currentRow, 2).Value = pipe.Subtype ?? "";
            ws.Cell(currentRow, 3).Value = pipe.FacilityOwner ?? "";
            ws.Cell(currentRow, 4).Value = pipe.Size ?? pipe.Diameter?.ToString() ?? "";
            ws.Cell(currentRow, 5).Value = pipe.PipeClass ?? "";
            ws.Cell(currentRow, 6).Value = pipe.Manufacturer ?? "";
            ws.Cell(currentRow, 7).Value = pipe.Material ?? "";
            ws.Cell(currentRow, 8).Value = pipe.LiningManufacturer ?? "";
            ws.Cell(currentRow, 9).Value = pipe.LiningMaterial ?? "";
            if (measuredLength > 0)
                ws.Cell(currentRow, 10).Value = measuredLength;

            currentRow++;
        }

        ws.Columns().AdjustToContents();

        // --- WATER POINTS REPORT ---
        var wsPoints = wb.Worksheets.Add("Water Points along a Pipe");
        
        string[] ptHeaders = new[]
        {
            "Pipe Location Number",
            "Pipe Location",
            "Pipe Subtype",
            "Facility Owner",
            "Pipe Size (Inches)",
            "Pipe Orientation",
            "Pipe Class",
            "Pipe Manufacturer",
            "Pipe Material",
            "Pipe Lining Manufacturer",
            "Pipe Lining Material",
            "Finished Grade Elevation (Feet)",
            "Pipe Top Elevation (Feet)",
            "Pipe Cover (Feet)",
            "X Coord (State Plane Easting Feet)",
            "Y Coord (State Plane Northing Feet)",
            "Latitude (Decimal Degrees)",
            "Longitude (Decimal Degrees)"
        };

        for (int i = 0; i < ptHeaders.Length; i++)
        {
            wsPoints.Cell(1, i + 1).Value = ptHeaders[i];
            wsPoints.Cell(1, i + 1).Style.Font.Bold = true;
        }

        wsPoints.SheetView.FreezeRows(1);
        int ptRow = 2;

        foreach (var pt in WaterPoints)
        {
            wsPoints.Cell(ptRow, 1).Value = pt.PartKey ?? "";
            wsPoints.Cell(ptRow, 2).Value = pt.Description ?? "";
            wsPoints.Cell(ptRow, 3).Value = pt.Subtype ?? "";
            wsPoints.Cell(ptRow, 4).Value = pt.FacilityOwner ?? "";
            wsPoints.Cell(ptRow, 5).Value = pt.Size ?? "";
            wsPoints.Cell(ptRow, 6).Value = pt.Orientation ?? "";
            wsPoints.Cell(ptRow, 7).Value = pt.PipeClass ?? "";
            wsPoints.Cell(ptRow, 8).Value = pt.Manufacturer ?? "";
            wsPoints.Cell(ptRow, 9).Value = pt.Material ?? "";
            wsPoints.Cell(ptRow, 10).Value = pt.LiningManufacturer ?? "";
            wsPoints.Cell(ptRow, 11).Value = pt.LiningMaterial ?? "";

            if (pt.Elevation.HasValue) wsPoints.Cell(ptRow, 12).Value = pt.Elevation.Value;
            if (pt.TopOutsideWallElev.HasValue) wsPoints.Cell(ptRow, 13).Value = pt.TopOutsideWallElev.Value;
            
            if (pt.Elevation.HasValue && pt.TopOutsideWallElev.HasValue)
            {
                wsPoints.Cell(ptRow, 14).Value = Math.Round(pt.Elevation.Value - pt.TopOutsideWallElev.Value, 2);
            }

            if (pt.Easting.HasValue) wsPoints.Cell(ptRow, 15).Value = pt.Easting.Value;
            if (pt.Northing.HasValue) wsPoints.Cell(ptRow, 16).Value = pt.Northing.Value;

            // Latitude and Longitude left blank for future coordinate converter hook.
            wsPoints.Cell(ptRow, 17).Value = "";
            wsPoints.Cell(ptRow, 18).Value = "";

            ptRow++;
        }

        wsPoints.Columns().AdjustToContents();

        // --- WATER FITTING REPORT ---
        var wsFittings = wb.Worksheets.Add("Water Fitting");
        
        string[] ftHeaders = new[]
        {
            "Fitting Number (WF#)",
            "Fitting Subtype",
            "Facility Owner",
            "Fitting Size Primary (Inches)",
            "Fitting Size Secondary (Inches)",
            "Manufacturer",
            "Fitting Material",
            "Lining Manufacturer",
            "Lining Material",
            "Fitting Top Elevation (Feet)",
            "Finished Grade Elevation (Feet)",
            "Fitting Depth (Feet)",
            "X Coord (State Plane Easting Feet)",
            "Y Coord (State Plane Northing Feet)",
            "Latitude (Decimal Degrees)",
            "Longitude (Decimal Degrees)"
        };

        for (int i = 0; i < ftHeaders.Length; i++)
        {
            wsFittings.Cell(1, i + 1).Value = ftHeaders[i];
            wsFittings.Cell(1, i + 1).Style.Font.Bold = true;
        }

        wsFittings.SheetView.FreezeRows(1);
        int ftRow = 2;

        foreach (var ft in WaterFittings)
        {
            wsFittings.Cell(ftRow, 1).Value = ft.PartKey ?? "";
            wsFittings.Cell(ftRow, 2).Value = ft.Subtype ?? "";
            wsFittings.Cell(ftRow, 3).Value = ft.FacilityOwner ?? "";
            wsFittings.Cell(ftRow, 4).Value = ft.Size ?? "";
            wsFittings.Cell(ftRow, 5).Value = ft.SizeSecondary ?? "";
            wsFittings.Cell(ftRow, 6).Value = ft.Manufacturer ?? "";
            wsFittings.Cell(ftRow, 7).Value = ft.Material ?? "";
            wsFittings.Cell(ftRow, 8).Value = ft.LiningManufacturer ?? "";
            wsFittings.Cell(ftRow, 9).Value = ft.LiningMaterial ?? "";

            if (ft.TopOutsideWallElev.HasValue) wsFittings.Cell(ftRow, 10).Value = ft.TopOutsideWallElev.Value;
            if (ft.Elevation.HasValue) wsFittings.Cell(ftRow, 11).Value = ft.Elevation.Value;
            
            if (ft.Elevation.HasValue && ft.TopOutsideWallElev.HasValue)
            {
                wsFittings.Cell(ftRow, 12).Value = Math.Round(ft.Elevation.Value - ft.TopOutsideWallElev.Value, 2);
            }

            if (ft.Easting.HasValue) wsFittings.Cell(ftRow, 13).Value = ft.Easting.Value;
            if (ft.Northing.HasValue) wsFittings.Cell(ftRow, 14).Value = ft.Northing.Value;

            wsFittings.Cell(ftRow, 15).Value = "";
            wsFittings.Cell(ftRow, 16).Value = "";

            ftRow++;
        }

        wsFittings.Columns().AdjustToContents();

        // --- WATER VALVE REPORT ---
        var wsValves = wb.Worksheets.Add("Water Valve");
        
        string[] vlvHeaders = new[]
        {
            "Valve Number (WV#)",
            "Valve Subtype",
            "Valve Type",
            "Facility Owner",
            "Valve Size",
            "Valve Orientation",
            "Valve Open Direction",
            "Turns to Open",
            "Valve Nut Elevation (Feet)",
            "Finished Grade Elevation (Feet)",
            "Depth to Nut (Feet)",
            "Valve Manufacturer",
            "X Coord (State Plane Easting Feet)",
            "Y Coord (State Plane Northing Feet)",
            "Latitude (Decimal Degrees)",
            "Longitude (Decimal Degrees)"
        };

        for (int i = 0; i < vlvHeaders.Length; i++)
        {
            wsValves.Cell(1, i + 1).Value = vlvHeaders[i];
            wsValves.Cell(1, i + 1).Style.Font.Bold = true;
        }

        wsValves.SheetView.FreezeRows(1);
        int vlvRow = 2;

        foreach (var vlv in WaterValves)
        {
            wsValves.Cell(vlvRow, 1).Value = vlv.PartKey ?? "";
            wsValves.Cell(vlvRow, 2).Value = vlv.Subtype ?? "";
            wsValves.Cell(vlvRow, 3).Value = vlv.Type ?? "";
            wsValves.Cell(vlvRow, 4).Value = vlv.FacilityOwner ?? "";
            wsValves.Cell(vlvRow, 5).Value = vlv.Size ?? "";
            wsValves.Cell(vlvRow, 6).Value = vlv.Orientation ?? "";
            wsValves.Cell(vlvRow, 7).Value = vlv.OpenDirection ?? "";
            
            if (vlv.TurnsToOpen.HasValue) wsValves.Cell(vlvRow, 8).Value = vlv.TurnsToOpen.Value;
            else wsValves.Cell(vlvRow, 8).Value = "";
            
            if (vlv.NutElevation.HasValue) wsValves.Cell(vlvRow, 9).Value = vlv.NutElevation.Value;
            if (vlv.Elevation.HasValue) wsValves.Cell(vlvRow, 10).Value = vlv.Elevation.Value;
            
            if (vlv.Elevation.HasValue && vlv.NutElevation.HasValue)
            {
                wsValves.Cell(vlvRow, 11).Value = Math.Round(vlv.Elevation.Value - vlv.NutElevation.Value, 2);
            }

            wsValves.Cell(vlvRow, 12).Value = vlv.Manufacturer ?? "";

            if (vlv.Easting.HasValue) wsValves.Cell(vlvRow, 13).Value = vlv.Easting.Value;
            if (vlv.Northing.HasValue) wsValves.Cell(vlvRow, 14).Value = vlv.Northing.Value;

            wsValves.Cell(vlvRow, 15).Value = "";
            wsValves.Cell(vlvRow, 16).Value = "";

            vlvRow++;
        }

        wsValves.Columns().AdjustToContents();

        // --- WATER HYDRANT REPORT ---
        var wsHydrants = wb.Worksheets.Add("Water Hydrant");
        string[] hydHeaders = new[] { "Hydrant Number (WH#)", "Facility Owner", "Hydrant Manufacture Date (Year)", "Hydrant Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)", "RFID/Barcode Number" };
        for (int i = 0; i < hydHeaders.Length; i++) { wsHydrants.Cell(1, i + 1).Value = hydHeaders[i]; wsHydrants.Cell(1, i + 1).Style.Font.Bold = true; }
        wsHydrants.SheetView.FreezeRows(1);
        int hydRow = 2;
        foreach (var hyd in WaterHydrants)
        {
            wsHydrants.Cell(hydRow, 1).Value = hyd.PartKey ?? "";
            wsHydrants.Cell(hydRow, 2).Value = hyd.FacilityOwner ?? "";
            wsHydrants.Cell(hydRow, 3).Value = hyd.YearManufactured ?? "";
            wsHydrants.Cell(hydRow, 4).Value = hyd.Manufacturer ?? "";
            if (hyd.Easting.HasValue) wsHydrants.Cell(hydRow, 5).Value = hyd.Easting.Value;
            if (hyd.Northing.HasValue) wsHydrants.Cell(hydRow, 6).Value = hyd.Northing.Value;
            wsHydrants.Cell(hydRow, 7).Value = ""; wsHydrants.Cell(hydRow, 8).Value = "";
            wsHydrants.Cell(hydRow, 9).Value = hyd.RfidBarcode ?? "";
            hydRow++;
        }
        wsHydrants.Columns().AdjustToContents();

        // --- WATER METER REPORT ---
        var wsMeters = wb.Worksheets.Add("Water Meter");
        string[] wmMeters = new[] { "Meter Box Number (WM#)", "Proposed Meter Size", "Meter Box Subtype", "Facility Owner", "Meter Box Orientation", "Meter Box Manufacturer/Supplier", "Meter Box Material", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wmMeters.Length; i++) { wsMeters.Cell(1, i + 1).Value = wmMeters[i]; wsMeters.Cell(1, i + 1).Style.Font.Bold = true; }
        wsMeters.SheetView.FreezeRows(1);
        int wmMeterRow = 2;
        foreach (var m in WaterMeters)
        {
            wsMeters.Cell(wmMeterRow, 1).Value = m.PartKey ?? "";
            wsMeters.Cell(wmMeterRow, 2).Value = m.Size ?? "";
            wsMeters.Cell(wmMeterRow, 3).Value = m.Subtype ?? "";
            wsMeters.Cell(wmMeterRow, 4).Value = m.FacilityOwner ?? "";
            wsMeters.Cell(wmMeterRow, 5).Value = m.Orientation ?? "";
            wsMeters.Cell(wmMeterRow, 6).Value = m.Manufacturer ?? "";
            wsMeters.Cell(wmMeterRow, 7).Value = m.Material ?? "";
            if (m.Easting.HasValue) wsMeters.Cell(wmMeterRow, 8).Value = m.Easting.Value;
            if (m.Northing.HasValue) wsMeters.Cell(wmMeterRow, 9).Value = m.Northing.Value;
            wsMeters.Cell(wmMeterRow, 10).Value = ""; wsMeters.Cell(wmMeterRow, 11).Value = "";
            wmMeterRow++;
        }
        wsMeters.Columns().AdjustToContents();

        // --- WATER LOCATE BOX REPORT ---
        var wsWL = wb.Worksheets.Add("Water Locate Box");
        string[] wlHeaders = new[] { "Locate Box Number (WL#)", "Locate Box Subtype", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wlHeaders.Length; i++) { wsWL.Cell(1, i + 1).Value = wlHeaders[i]; wsWL.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWL.SheetView.FreezeRows(1);
        int wlRow = 2;
        foreach (var wl in WaterLocateBoxes)
        {
            wsWL.Cell(wlRow, 1).Value = wl.PartKey ?? "";
            wsWL.Cell(wlRow, 2).Value = wl.Subtype ?? "";
            if (wl.Easting.HasValue) wsWL.Cell(wlRow, 3).Value = wl.Easting.Value;
            if (wl.Northing.HasValue) wsWL.Cell(wlRow, 4).Value = wl.Northing.Value;
            wsWL.Cell(wlRow, 5).Value = ""; wsWL.Cell(wlRow, 6).Value = "";
            wlRow++;
        }
        wsWL.Columns().AdjustToContents();

        // --- WW GRAVITY PIPE RUN ---
        var wsWwg = wb.Worksheets.Add("WW Gravity Pipe Run");
        string[] wwgHd = new[] { "Sewer Pipe Run Number (GM#)", "Sewer Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Pipe Run Length (feet)", "Downstream Pipe Invert Elevation (feet)", "Downstream Grade Elevation at Invert (feet)", "Upstream Pipe Invert Elevation (feet)", "Upstream Grade Elevation at Invert (feet)", "Slope (percent)" };
        for (int i = 0; i < wwgHd.Length; i++) { wsWwg.Cell(1, i + 1).Value = wwgHd[i]; wsWwg.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwg.SheetView.FreezeRows(1);
        int wwgRow = 2;
        foreach (var p in WWGravityPipes)
        {
            double l = Math.Round(Math.Sqrt(Math.Pow((p.NorthingEnd ?? 0)-(p.NorthingStart ?? 0), 2) + Math.Pow((p.EastingEnd ?? 0)-(p.EastingStart ?? 0), 2)), 2);
            wsWwg.Cell(wwgRow, 1).Value = p.PartKey ?? ""; wsWwg.Cell(wwgRow, 2).Value = p.Subtype ?? "";
            wsWwg.Cell(wwgRow, 3).Value = p.FacilityOwner ?? ""; wsWwg.Cell(wwgRow, 4).Value = p.Size ?? p.Diameter?.ToString() ?? "";
            wsWwg.Cell(wwgRow, 5).Value = p.PipeClass ?? ""; wsWwg.Cell(wwgRow, 6).Value = p.Manufacturer ?? "";
            wsWwg.Cell(wwgRow, 7).Value = p.Material ?? ""; wsWwg.Cell(wwgRow, 8).Value = p.LiningManufacturer ?? "";
            wsWwg.Cell(wwgRow, 9).Value = p.LiningMaterial ?? ""; wsWwg.Cell(wwgRow, 10).Value = l > 0 ? l.ToString() : "";
            wsWwg.Cell(wwgRow, 11).Value = p.InvertEnd.HasValue ? p.InvertEnd.Value.ToString() : "";
            wsWwg.Cell(wwgRow, 12).Value = p.GradeElevationAtInvertEnd.HasValue ? p.GradeElevationAtInvertEnd.Value.ToString() : "";
            wsWwg.Cell(wwgRow, 13).Value = p.InvertStart.HasValue ? p.InvertStart.Value.ToString() : "";
            wsWwg.Cell(wwgRow, 14).Value = p.GradeElevationAtInvertStart.HasValue ? p.GradeElevationAtInvertStart.Value.ToString() : "";
            if (l > 0 && p.InvertStart.HasValue && p.InvertEnd.HasValue) wsWwg.Cell(wwgRow, 15).Value = Math.Round(Math.Abs((p.InvertStart.Value - p.InvertEnd.Value) / l * 100), 2);
            wwgRow++;
        }
        wsWwg.Columns().AdjustToContents();

        // --- WW PRESSURE PIPE RUN ---
        var wsWwp = wb.Worksheets.Add("WW Pressure Pipe Run");
        string[] wwpHd = new[] { "Pipe Run Number (FM#)", "Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Measured Length (Feet)" };
        for (int i = 0; i < wwpHd.Length; i++) { wsWwp.Cell(1, i + 1).Value = wwpHd[i]; wsWwp.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwp.SheetView.FreezeRows(1);
        int wwpRow = 2;
        foreach (var p in WWPressurePipes)
        {
            double l = Math.Round(Math.Sqrt(Math.Pow((p.NorthingEnd ?? 0)-(p.NorthingStart ?? 0), 2) + Math.Pow((p.EastingEnd ?? 0)-(p.EastingStart ?? 0), 2)), 2);
            wsWwp.Cell(wwpRow, 1).Value = p.PartKey ?? ""; wsWwp.Cell(wwpRow, 2).Value = p.Subtype ?? "";
            wsWwp.Cell(wwpRow, 3).Value = p.FacilityOwner ?? ""; wsWwp.Cell(wwpRow, 4).Value = p.Size ?? p.Diameter?.ToString() ?? "";
            wsWwp.Cell(wwpRow, 5).Value = p.PipeClass ?? ""; wsWwp.Cell(wwpRow, 6).Value = p.Manufacturer ?? "";
            wsWwp.Cell(wwpRow, 7).Value = p.Material ?? ""; wsWwp.Cell(wwpRow, 8).Value = p.LiningManufacturer ?? "";
            wsWwp.Cell(wwpRow, 9).Value = p.LiningMaterial ?? ""; wsWwp.Cell(wwpRow, 10).Value = l > 0 ? l.ToString() : "";
            wwpRow++;
        }
        wsWwp.Columns().AdjustToContents();

        // --- WW POINTS ALONG PIPE ---
        var wsWwPt = wb.Worksheets.Add("WW Points along Pipe");
        string[] wwptHd = new[] { "Pipe Location Number (WWPOC, WWPOL#, etc)", "Pipe Location", "Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Orientation", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Finished Grade Elevation (Feet)", "Pipe Top Elevation (Feet)", "Pipe Cover (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wwptHd.Length; i++) { wsWwPt.Cell(1, i + 1).Value = wwptHd[i]; wsWwPt.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwPt.SheetView.FreezeRows(1);
        int wwptRow = 2;
        foreach (var pt in WWPoints)
        {
            wsWwPt.Cell(wwptRow, 1).Value = pt.PartKey ?? ""; wsWwPt.Cell(wwptRow, 2).Value = pt.Description ?? "";
            wsWwPt.Cell(wwptRow, 3).Value = pt.Subtype ?? ""; wsWwPt.Cell(wwptRow, 4).Value = pt.FacilityOwner ?? "";
            wsWwPt.Cell(wwptRow, 5).Value = pt.Size ?? ""; wsWwPt.Cell(wwptRow, 6).Value = pt.Orientation ?? "";
            wsWwPt.Cell(wwptRow, 7).Value = pt.PipeClass ?? ""; wsWwPt.Cell(wwptRow, 8).Value = pt.Manufacturer ?? "";
            wsWwPt.Cell(wwptRow, 9).Value = pt.Material ?? ""; wsWwPt.Cell(wwptRow, 10).Value = pt.LiningManufacturer ?? "";
            wsWwPt.Cell(wwptRow, 11).Value = pt.LiningMaterial ?? "";
            if (pt.Elevation.HasValue) wsWwPt.Cell(wwptRow, 12).Value = pt.Elevation.Value;
            if (pt.TopOutsideWallElev.HasValue) wsWwPt.Cell(wwptRow, 13).Value = pt.TopOutsideWallElev.Value;
            if (pt.Elevation.HasValue && pt.TopOutsideWallElev.HasValue) wsWwPt.Cell(wwptRow, 14).Value = Math.Round(pt.Elevation.Value - pt.TopOutsideWallElev.Value, 2);
            if (pt.Easting.HasValue) wsWwPt.Cell(wwptRow, 15).Value = pt.Easting.Value;
            if (pt.Northing.HasValue) wsWwPt.Cell(wwptRow, 16).Value = pt.Northing.Value;
            wwptRow++;
        }
        wsWwPt.Columns().AdjustToContents();

        // --- WW FITTING ---
        var wsWwf = wb.Worksheets.Add("WW Fitting");
        string[] wwfHd = new[] { "Fitting Number (WWF# or FMF#)", "Fitting Subtype", "Facility Owner", "Fitting Size Primary (Inches)", "Fitting Size Secondary (Inches)", "Manufacturer", "Fitting Material", "Lining Manufacturer", "Lining Material", "Fitting Top Elevation (Feet)", "Finished Grade Elevation (Feet)", "Fitting Depth (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wwfHd.Length; i++) { wsWwf.Cell(1, i + 1).Value = wwfHd[i]; wsWwf.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwf.SheetView.FreezeRows(1);
        int wwfRow = 2;
        foreach (var ft in WWFittings)
        {
            wsWwf.Cell(wwfRow, 1).Value = ft.PartKey ?? ""; wsWwf.Cell(wwfRow, 2).Value = ft.Subtype ?? "";
            wsWwf.Cell(wwfRow, 3).Value = ft.FacilityOwner ?? ""; wsWwf.Cell(wwfRow, 4).Value = ft.Size ?? "";
            wsWwf.Cell(wwfRow, 5).Value = ft.SizeSecondary ?? ""; wsWwf.Cell(wwfRow, 6).Value = ft.Manufacturer ?? "";
            wsWwf.Cell(wwfRow, 7).Value = ft.Material ?? ""; wsWwf.Cell(wwfRow, 8).Value = ft.LiningManufacturer ?? "";
            wsWwf.Cell(wwfRow, 9).Value = ft.LiningMaterial ?? "";
            if (ft.TopOutsideWallElev.HasValue) wsWwf.Cell(wwfRow, 10).Value = ft.TopOutsideWallElev.Value;
            if (ft.Elevation.HasValue) wsWwf.Cell(wwfRow, 11).Value = ft.Elevation.Value;
            if (ft.Elevation.HasValue && ft.TopOutsideWallElev.HasValue) wsWwf.Cell(wwfRow, 12).Value = Math.Round(ft.Elevation.Value - ft.TopOutsideWallElev.Value, 2);
            if (ft.Easting.HasValue) wsWwf.Cell(wwfRow, 13).Value = ft.Easting.Value;
            if (ft.Northing.HasValue) wsWwf.Cell(wwfRow, 14).Value = ft.Northing.Value;
            wwfRow++;
        }
        wsWwf.Columns().AdjustToContents();

        // --- MANHOLE ---
        var wsMh = wb.Worksheets.Add("Manhole");
        string[] mhHd = new[] { "Manhole Number (MH#)", "Manhole Subtype", "Facility Owner", "Manhole Type", "Manhole Drop Type", "Manufacturer or Supplier", "Manhole Size (Feet)", "Manhole Material", "Manhole Lining Material", "Manhole Lining Manufacturer", "Rim Elevation (Feet)", "Invert Elevations (Feet) with Directions", "Lowest Invert Elevation (feet)", "Exterior Joint Tape Type", "Exterior Joint Tape Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)", "RFID/Barcode Number" };
        for (int i = 0; i < mhHd.Length; i++) { wsMh.Cell(1, i + 1).Value = mhHd[i]; wsMh.Cell(1, i + 1).Style.Font.Bold = true; }
        wsMh.SheetView.FreezeRows(1);
        int mhRow = 2;
        foreach (var mh in Manholes)
        {
            wsMh.Cell(mhRow, 1).Value = mh.PartKey ?? ""; wsMh.Cell(mhRow, 2).Value = mh.Subtype ?? "";
            wsMh.Cell(mhRow, 3).Value = mh.FacilityOwner ?? ""; wsMh.Cell(mhRow, 4).Value = mh.FeatureType ?? "";
            wsMh.Cell(mhRow, 5).Value = mh.DropType ?? ""; wsMh.Cell(mhRow, 6).Value = mh.Manufacturer ?? "";
            wsMh.Cell(mhRow, 7).Value = mh.Size ?? ""; wsMh.Cell(mhRow, 8).Value = mh.Material ?? "";
            wsMh.Cell(mhRow, 9).Value = mh.LiningMaterial ?? ""; wsMh.Cell(mhRow, 10).Value = mh.LiningManufacturer ?? "";
            if (mh.Elevation.HasValue) wsMh.Cell(mhRow, 11).Value = mh.Elevation.Value;
            wsMh.Cell(mhRow, 12).Value = mh.InvertElevationsWithDirections ?? "";
            if (mh.FinalInvert.HasValue) wsMh.Cell(mhRow, 13).Value = mh.FinalInvert.Value;
            wsMh.Cell(mhRow, 14).Value = mh.ExteriorJointTapeType ?? ""; wsMh.Cell(mhRow, 15).Value = mh.ExteriorJointTapeManufacturer ?? "";
            if (mh.Easting.HasValue) wsMh.Cell(mhRow, 16).Value = mh.Easting.Value;
            if (mh.Northing.HasValue) wsMh.Cell(mhRow, 17).Value = mh.Northing.Value;
            wsMh.Cell(mhRow, 20).Value = mh.RfidBarcode ?? "";
            mhRow++;
        }
        wsMh.Columns().AdjustToContents();

        // --- WW SERVICE POINT & METER ---
        var wsWwSp = wb.Worksheets.Add("WW Service Point & Meter");
        string[] wwspHd = new[] { "Wastewater Service Point Number (WWSP# or WWM#)", "Service Point Subtype", "Finished Grade Elevation at Service Point", "Top of Pipe Elevation at Service Point (Feet)", "Depth of Cover (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wwspHd.Length; i++) { wsWwSp.Cell(1, i + 1).Value = wwspHd[i]; wsWwSp.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwSp.SheetView.FreezeRows(1);
        int wwspRow = 2;
        foreach (var sp in WWServicePoints)
        {
            wsWwSp.Cell(wwspRow, 1).Value = sp.PartKey ?? ""; wsWwSp.Cell(wwspRow, 2).Value = sp.Subtype ?? "";
            if (sp.Elevation.HasValue) wsWwSp.Cell(wwspRow, 3).Value = sp.Elevation.Value;
            if (sp.TopOutsideWallElev.HasValue) wsWwSp.Cell(wwspRow, 4).Value = sp.TopOutsideWallElev.Value;
            if (sp.Elevation.HasValue && sp.TopOutsideWallElev.HasValue) wsWwSp.Cell(wwspRow, 5).Value = Math.Round(sp.Elevation.Value - sp.TopOutsideWallElev.Value, 2);
            if (sp.Easting.HasValue) wsWwSp.Cell(wwspRow, 6).Value = sp.Easting.Value;
            if (sp.Northing.HasValue) wsWwSp.Cell(wwspRow, 7).Value = sp.Northing.Value;
            wwspRow++;
        }
        wsWwSp.Columns().AdjustToContents();

        // --- WW VALVE ---
        var wsWwv = wb.Worksheets.Add("WW Valve");
        string[] wwvHd = new[] { "Valve Number (WWV#)", "Valve Subtype", "Valve Type", "Facility Owner", "Valve Size", "Valve Orientation", "Valve Open Direction", "Turns to Open", "Valve Nut Elevation (Feet)", "Finished Grade Elevation (Feet)", "Depth to Nut (Feet)", "Valve Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wwvHd.Length; i++) { wsWwv.Cell(1, i + 1).Value = wwvHd[i]; wsWwv.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwv.SheetView.FreezeRows(1);
        int wwvRow = 2;
        foreach (var vlv in WWValves)
        {
            wsWwv.Cell(wwvRow, 1).Value = vlv.PartKey ?? ""; wsWwv.Cell(wwvRow, 2).Value = vlv.Subtype ?? "";
            wsWwv.Cell(wwvRow, 3).Value = vlv.Type ?? ""; wsWwv.Cell(wwvRow, 4).Value = vlv.FacilityOwner ?? "";
            wsWwv.Cell(wwvRow, 5).Value = vlv.Size ?? ""; wsWwv.Cell(wwvRow, 6).Value = vlv.Orientation ?? "";
            wsWwv.Cell(wwvRow, 7).Value = vlv.OpenDirection ?? "";
            if (vlv.TurnsToOpen.HasValue) wsWwv.Cell(wwvRow, 8).Value = vlv.TurnsToOpen.Value;
            if (vlv.NutElevation.HasValue) wsWwv.Cell(wwvRow, 9).Value = vlv.NutElevation.Value;
            if (vlv.Elevation.HasValue) wsWwv.Cell(wwvRow, 10).Value = vlv.Elevation.Value;
            if (vlv.Elevation.HasValue && vlv.NutElevation.HasValue) wsWwv.Cell(wwvRow, 11).Value = Math.Round(vlv.Elevation.Value - vlv.NutElevation.Value, 2);
            wsWwv.Cell(wwvRow, 12).Value = vlv.Manufacturer ?? "";
            if (vlv.Easting.HasValue) wsWwv.Cell(wwvRow, 13).Value = vlv.Easting.Value;
            if (vlv.Northing.HasValue) wsWwv.Cell(wwvRow, 14).Value = vlv.Northing.Value;
            wwvRow++;
        }
        wsWwv.Columns().AdjustToContents();

        // --- WW LOCATE BOX ---
        var wsWwl = wb.Worksheets.Add("WW Locate Box");
        string[] wwlHd = new[] { "Locate Box Number (WWL#)", "Locate Box Subtype", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i = 0; i < wwlHd.Length; i++) { wsWwl.Cell(1, i + 1).Value = wwlHd[i]; wsWwl.Cell(1, i + 1).Style.Font.Bold = true; }
        wsWwl.SheetView.FreezeRows(1);
        int wwlRow = 2;
        foreach (var wl in WWLocateBoxes)
        {
            wsWwl.Cell(wwlRow, 1).Value = wl.PartKey ?? ""; wsWwl.Cell(wwlRow, 2).Value = wl.Subtype ?? "";
            if (wl.Easting.HasValue) wsWwl.Cell(wwlRow, 3).Value = wl.Easting.Value;
            if (wl.Northing.HasValue) wsWwl.Cell(wwlRow, 4).Value = wl.Northing.Value;
            wwlRow++;
        }
        wsWwl.Columns().AdjustToContents();

        // --- RECLAIMED PIPE RUN ---
        var wsRp = wb.Worksheets.Add("Reclaimed Pipe Run");
        string[] rpHd = new[] { "Pipe Run Number (RM#)", "Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Measured Length (Feet)" };
        for (int i=0; i<rpHd.Length; i++) { wsRp.Cell(1, i+1).Value = rpHd[i]; wsRp.Cell(1, i+1).Style.Font.Bold = true; }
        wsRp.SheetView.FreezeRows(1);
        int rpRow = 2;
        foreach (var p in ReclaimedPipes)
        {
            double l = Math.Round(Math.Sqrt(Math.Pow((p.NorthingEnd ?? 0)-(p.NorthingStart ?? 0), 2) + Math.Pow((p.EastingEnd ?? 0)-(p.EastingStart ?? 0), 2)), 2);
            wsRp.Cell(rpRow, 1).Value = p.PartKey ?? ""; wsRp.Cell(rpRow, 2).Value = p.Subtype ?? ""; wsRp.Cell(rpRow, 3).Value = p.FacilityOwner ?? "";
            wsRp.Cell(rpRow, 4).Value = p.Size ?? p.Diameter?.ToString() ?? ""; wsRp.Cell(rpRow, 5).Value = p.PipeClass ?? "";
            wsRp.Cell(rpRow, 6).Value = p.Manufacturer ?? ""; wsRp.Cell(rpRow, 7).Value = p.Material ?? "";
            wsRp.Cell(rpRow, 8).Value = p.LiningManufacturer ?? ""; wsRp.Cell(rpRow, 9).Value = p.LiningMaterial ?? "";
            wsRp.Cell(rpRow, 10).Value = l > 0 ? l.ToString() : "";
            rpRow++;
        }
        wsRp.Columns().AdjustToContents();

        // --- RECLAIMED POINTS ALONG PIPE ---
        var wsRpt = wb.Worksheets.Add("Reclaimed Points along Pipe");
        string[] rptHd = new[] { "Pipe Location Number (RPOC#, RPOL#, etc)", "Pipe Location", "Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Orientation", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Finished Grade Elevation (Feet)", "Pipe Top Elevation (Feet)", "Pipe Cover (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<rptHd.Length; i++) { wsRpt.Cell(1, i+1).Value = rptHd[i]; wsRpt.Cell(1, i+1).Style.Font.Bold = true; }
        wsRpt.SheetView.FreezeRows(1);
        int rptRow = 2;
        foreach (var pt in ReclaimedPoints) {
            wsRpt.Cell(rptRow, 1).Value = pt.PartKey ?? ""; wsRpt.Cell(rptRow, 2).Value = pt.Description ?? ""; wsRpt.Cell(rptRow, 3).Value = pt.Subtype ?? ""; wsRpt.Cell(rptRow, 4).Value = pt.FacilityOwner ?? ""; wsRpt.Cell(rptRow, 5).Value = pt.Size ?? ""; wsRpt.Cell(rptRow, 6).Value = pt.Orientation ?? ""; wsRpt.Cell(rptRow, 7).Value = pt.PipeClass ?? ""; wsRpt.Cell(rptRow, 8).Value = pt.Manufacturer ?? ""; wsRpt.Cell(rptRow, 9).Value = pt.Material ?? ""; wsRpt.Cell(rptRow, 10).Value = pt.LiningManufacturer ?? ""; wsRpt.Cell(rptRow, 11).Value = pt.LiningMaterial ?? ""; if (pt.Elevation.HasValue) wsRpt.Cell(rptRow, 12).Value = pt.Elevation.Value; if (pt.TopOutsideWallElev.HasValue) wsRpt.Cell(rptRow, 13).Value = pt.TopOutsideWallElev.Value; if (pt.Elevation.HasValue && pt.TopOutsideWallElev.HasValue) wsRpt.Cell(rptRow, 14).Value = Math.Round(pt.Elevation.Value - pt.TopOutsideWallElev.Value, 2); if (pt.Easting.HasValue) wsRpt.Cell(rptRow, 15).Value = pt.Easting.Value; if (pt.Northing.HasValue) wsRpt.Cell(rptRow, 16).Value = pt.Northing.Value; rptRow++;
        }
        wsRpt.Columns().AdjustToContents();

        // --- RECLAIMED FITTING ---
        var wsRf = wb.Worksheets.Add("Reclaimed Fitting");
        string[] rfHd = new[] { "Fitting Number (RF#)", "Fitting Subtype", "Facility Owner", "Fitting Size Primary (Inches)", "Fitting Size Secondary (Inches)", "Manufacturer", "Fitting Material", "Lining Manufacturer", "Lining Material", "Fitting Top Elevation (Feet)", "Finished Grade Elevation (Feet)", "Depth (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<rfHd.Length; i++) { wsRf.Cell(1, i+1).Value = rfHd[i]; wsRf.Cell(1, i+1).Style.Font.Bold = true; }
        wsRf.SheetView.FreezeRows(1);
        int rfRow = 2;
        foreach (var ft in ReclaimedFittings) {
            wsRf.Cell(rfRow, 1).Value = ft.PartKey ?? ""; wsRf.Cell(rfRow, 2).Value = ft.Subtype ?? ""; wsRf.Cell(rfRow, 3).Value = ft.FacilityOwner ?? ""; wsRf.Cell(rfRow, 4).Value = ft.Size ?? ""; wsRf.Cell(rfRow, 5).Value = ft.SizeSecondary ?? ""; wsRf.Cell(rfRow, 6).Value = ft.Manufacturer ?? ""; wsRf.Cell(rfRow, 7).Value = ft.Material ?? ""; wsRf.Cell(rfRow, 8).Value = ft.LiningManufacturer ?? ""; wsRf.Cell(rfRow, 9).Value = ft.LiningMaterial ?? ""; if (ft.TopOutsideWallElev.HasValue) wsRf.Cell(rfRow, 10).Value = ft.TopOutsideWallElev.Value; if (ft.Elevation.HasValue) wsRf.Cell(rfRow, 11).Value = ft.Elevation.Value; if (ft.Elevation.HasValue && ft.TopOutsideWallElev.HasValue) wsRf.Cell(rfRow, 12).Value = Math.Round(ft.Elevation.Value - ft.TopOutsideWallElev.Value, 2); if (ft.Easting.HasValue) wsRf.Cell(rfRow, 13).Value = ft.Easting.Value; if (ft.Northing.HasValue) wsRf.Cell(rfRow, 14).Value = ft.Northing.Value; rfRow++;
        }
        wsRf.Columns().AdjustToContents();

        // --- RECLAIMED VALVE ---
        var wsRv = wb.Worksheets.Add("Reclaimed Valve");
        string[] rvHd = new[] { "Valve Number (RV#)", "Valve Subtype", "Valve Type", "Facility Owner", "Valve Size", "Valve Orientation", "Valve Open Direction", "Turns to Open", "Valve Nut Elevation (Feet)", "Finished Grade Elevation (Feet)", "Depth to Nut (Feet)", "Valve Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<rvHd.Length; i++) { wsRv.Cell(1, i+1).Value = rvHd[i]; wsRv.Cell(1, i+1).Style.Font.Bold = true; }
        wsRv.SheetView.FreezeRows(1);
        int rvRow = 2;
        foreach (var vlv in ReclaimedValves) {
            wsRv.Cell(rvRow, 1).Value = vlv.PartKey ?? ""; wsRv.Cell(rvRow, 2).Value = vlv.Subtype ?? ""; wsRv.Cell(rvRow, 3).Value = vlv.Type ?? ""; wsRv.Cell(rvRow, 4).Value = vlv.FacilityOwner ?? ""; wsRv.Cell(rvRow, 5).Value = vlv.Size ?? ""; wsRv.Cell(rvRow, 6).Value = vlv.Orientation ?? ""; wsRv.Cell(rvRow, 7).Value = vlv.OpenDirection ?? ""; if (vlv.TurnsToOpen.HasValue) wsRv.Cell(rvRow, 8).Value = vlv.TurnsToOpen.Value; if (vlv.NutElevation.HasValue) wsRv.Cell(rvRow, 9).Value = vlv.NutElevation.Value; if (vlv.Elevation.HasValue) wsRv.Cell(rvRow, 10).Value = vlv.Elevation.Value; if (vlv.Elevation.HasValue && vlv.NutElevation.HasValue) wsRv.Cell(rvRow, 11).Value = Math.Round(vlv.Elevation.Value - vlv.NutElevation.Value, 2); wsRv.Cell(rvRow, 12).Value = vlv.Manufacturer ?? ""; if (vlv.Easting.HasValue) wsRv.Cell(rvRow, 13).Value = vlv.Easting.Value; if (vlv.Northing.HasValue) wsRv.Cell(rvRow, 14).Value = vlv.Northing.Value; rvRow++;
        }
        wsRv.Columns().AdjustToContents();

        // --- RECLAIMED HYDRANT & METER & LOCATE BOX ---
        var wsRh = wb.Worksheets.Add("Reclaimed Hydrant");
        string[] rhHd = new[] { "Hydrant Number (RH#)", "Facility Owner", "Hydrant Manufacture Date (Year)", "Hydrant Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)", "RFID/Barcode Number" };
        for (int i=0; i<rhHd.Length; i++) { wsRh.Cell(1, i+1).Value = rhHd[i]; wsRh.Cell(1, i+1).Style.Font.Bold = true; }
        wsRh.SheetView.FreezeRows(1);
        int rhRow = 2; foreach(var h in ReclaimedHydrants) { wsRh.Cell(rhRow, 1).Value = h.PartKey??""; wsRh.Cell(rhRow, 2).Value = h.FacilityOwner??""; wsRh.Cell(rhRow, 3).Value = h.YearManufactured??""; wsRh.Cell(rhRow, 4).Value = h.Manufacturer??""; if(h.Easting.HasValue) wsRh.Cell(rhRow, 5).Value=h.Easting.Value; if(h.Northing.HasValue) wsRh.Cell(rhRow, 6).Value=h.Northing.Value; wsRh.Cell(rhRow, 9).Value = h.RfidBarcode??""; rhRow++; } wsRh.Columns().AdjustToContents();
        var wsRm = wb.Worksheets.Add("Reclaimed Meter");
        string[] rmHd = new[] { "Meter Box Number (RM#)", "Proposed Meter Size", "Meter Box Subtype", "Facility Owner", "Meter Orientation", "Meter Box Manufacturer/Supplier", "Meter Box Material", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<rmHd.Length; i++) { wsRm.Cell(1, i+1).Value = rmHd[i]; wsRm.Cell(1, i+1).Style.Font.Bold = true; }
        wsRm.SheetView.FreezeRows(1);
        int rmRow = 2; foreach(var m in ReclaimedMeters) { wsRm.Cell(rmRow, 1).Value = m.PartKey??""; wsRm.Cell(rmRow, 2).Value=m.Size??""; wsRm.Cell(rmRow, 3).Value=m.Subtype??""; wsRm.Cell(rmRow, 4).Value=m.FacilityOwner??""; wsRm.Cell(rmRow, 5).Value=m.Orientation??""; wsRm.Cell(rmRow, 6).Value=m.Manufacturer??""; wsRm.Cell(rmRow, 7).Value=m.Material??""; if(m.Easting.HasValue) wsRm.Cell(rmRow, 8).Value=m.Easting.Value; if(m.Northing.HasValue) wsRm.Cell(rmRow, 9).Value=m.Northing.Value; rmRow++; } wsRm.Columns().AdjustToContents();
        var wsRl = wb.Worksheets.Add("Reclaimed Locate Box");
        string[] rlHd = new[] { "Locate Box Number (RL#)", "Locate Box Subtype", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<rlHd.Length; i++) { wsRl.Cell(1, i+1).Value = rlHd[i]; wsRl.Cell(1, i+1).Style.Font.Bold = true; }
        wsRl.SheetView.FreezeRows(1);
        int rlRow = 2; foreach(var l in ReclaimedLocateBoxes) { wsRl.Cell(rlRow, 1).Value = l.PartKey??""; wsRl.Cell(rlRow, 2).Value=l.Subtype??""; if(l.Easting.HasValue) wsRl.Cell(rlRow, 3).Value=l.Easting.Value; if(l.Northing.HasValue) wsRl.Cell(rlRow, 4).Value=l.Northing.Value; rlRow++; } wsRl.Columns().AdjustToContents();

        // --- CHILLED PIPES & POINTS & FITTINGS & VALVES & METERS & LOCATE BOXES ---
        var wsCp = wb.Worksheets.Add("Chilled Pipe Run");
        string[] cpHd = new[] { "Pipe Run Number (CM#)", "Pipe Role", "Facility Owner", "Pipe Size (Inches)", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Measured Length (Feet)" };
        for (int i=0; i<cpHd.Length; i++) { wsCp.Cell(1, i+1).Value=cpHd[i]; wsCp.Cell(1, i+1).Style.Font.Bold=true; } wsCp.SheetView.FreezeRows(1); int cpRow = 2;
        foreach (var p in ChilledPipes) { double l = Math.Round(Math.Sqrt(Math.Pow((p.NorthingEnd ?? 0)-(p.NorthingStart ?? 0), 2) + Math.Pow((p.EastingEnd ?? 0)-(p.EastingStart ?? 0), 2)), 2); wsCp.Cell(cpRow, 1).Value = p.PartKey??""; wsCp.Cell(cpRow, 2).Value = p.PipeRole??""; wsCp.Cell(cpRow, 3).Value = p.FacilityOwner??""; wsCp.Cell(cpRow, 4).Value = p.Size??p.Diameter?.ToString()??""; wsCp.Cell(cpRow, 5).Value = p.PipeClass??""; wsCp.Cell(cpRow, 6).Value = p.Manufacturer??""; wsCp.Cell(cpRow, 7).Value = p.Material??""; wsCp.Cell(cpRow, 8).Value = p.LiningManufacturer??""; wsCp.Cell(cpRow, 9).Value = p.LiningMaterial??""; wsCp.Cell(cpRow, 10).Value = l>0?l.ToString():""; cpRow++; } wsCp.Columns().AdjustToContents();
        
        var wsCpt = wb.Worksheets.Add("Chilled Points along Pipe");
        string[] cptHd = new[] { "Pipe Location Number (CPOC#, CPOL#, etc)", "Pipe Location", "Facility Owner", "Pipe Size (Inches)", "Pipe Orientation", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Finished Grade Elevation (Feet)", "Pipe Top Elevation (Feet)", "Pipe Cover (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<cptHd.Length; i++) { wsCpt.Cell(1, i+1).Value=cptHd[i]; wsCpt.Cell(1, i+1).Style.Font.Bold=true; } wsCpt.SheetView.FreezeRows(1); int cptRow = 2;
        foreach(var pt in ChilledPoints) { wsCpt.Cell(cptRow, 1).Value=pt.PartKey??""; wsCpt.Cell(cptRow, 2).Value=pt.Description??""; wsCpt.Cell(cptRow, 3).Value=pt.FacilityOwner??""; wsCpt.Cell(cptRow, 4).Value=pt.Size??""; wsCpt.Cell(cptRow, 5).Value=pt.Orientation??""; wsCpt.Cell(cptRow, 6).Value=pt.PipeClass??""; wsCpt.Cell(cptRow, 7).Value=pt.Manufacturer??""; wsCpt.Cell(cptRow, 8).Value=pt.Material??""; wsCpt.Cell(cptRow, 9).Value=pt.LiningManufacturer??""; wsCpt.Cell(cptRow, 10).Value=pt.LiningMaterial??""; if(pt.Elevation.HasValue)wsCpt.Cell(cptRow, 11).Value=pt.Elevation.Value; if(pt.TopOutsideWallElev.HasValue)wsCpt.Cell(cptRow, 12).Value=pt.TopOutsideWallElev.Value; if(pt.Elevation.HasValue && pt.TopOutsideWallElev.HasValue)wsCpt.Cell(cptRow, 13).Value=Math.Round(pt.Elevation.Value - pt.TopOutsideWallElev.Value, 2); if(pt.Easting.HasValue)wsCpt.Cell(cptRow, 14).Value=pt.Easting.Value; if(pt.Northing.HasValue)wsCpt.Cell(cptRow, 15).Value=pt.Northing.Value; cptRow++; } wsCpt.Columns().AdjustToContents();
        
        var wsCf = wb.Worksheets.Add("Chilled Fitting");
        string[] cfHd = new[] { "Fitting Number (CF#)", "Fitting Subtype", "Facility Owner", "Fitting Size Primary (Inches)", "Fitting Size Secondary (Inches)", "Manufacturer", "Fitting Material", "Lining Manufacturer", "Lining Material", "Fitting Top Elevation (Feet)", "Finished Grade Elevation (Feet)", "Fitting Depth (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<cfHd.Length; i++) { wsCf.Cell(1, i+1).Value=cfHd[i]; wsCf.Cell(1, i+1).Style.Font.Bold=true; } wsCf.SheetView.FreezeRows(1); int cfRow=2;
        foreach(var ft in ChilledFittings) { wsCf.Cell(cfRow, 1).Value=ft.PartKey??""; wsCf.Cell(cfRow, 2).Value=ft.Subtype??""; wsCf.Cell(cfRow, 3).Value=ft.FacilityOwner??""; wsCf.Cell(cfRow, 4).Value=ft.Size??""; wsCf.Cell(cfRow, 5).Value=ft.SizeSecondary??""; wsCf.Cell(cfRow, 6).Value=ft.Manufacturer??""; wsCf.Cell(cfRow, 7).Value=ft.Material??""; wsCf.Cell(cfRow, 8).Value=ft.LiningManufacturer??""; wsCf.Cell(cfRow, 9).Value=ft.LiningMaterial??""; if(ft.TopOutsideWallElev.HasValue)wsCf.Cell(cfRow, 10).Value=ft.TopOutsideWallElev.Value; if(ft.Elevation.HasValue)wsCf.Cell(cfRow, 11).Value=ft.Elevation.Value; if(ft.Elevation.HasValue && ft.TopOutsideWallElev.HasValue)wsCf.Cell(cfRow, 12).Value=Math.Round(ft.Elevation.Value - ft.TopOutsideWallElev.Value, 2); if(ft.Easting.HasValue)wsCf.Cell(cfRow, 13).Value=ft.Easting.Value; if(ft.Northing.HasValue)wsCf.Cell(cfRow, 14).Value=ft.Northing.Value; cfRow++; } wsCf.Columns().AdjustToContents();
        
        var wsCv = wb.Worksheets.Add("Chilled Valve");
        string[] cvHd = new[] { "Valve Number (CV#)", "Valve Type", "Facility Owner", "Valve Size", "Valve Orientation", "Valve Open Direction", "Turns to Open", "Valve Nut Elevation (Feet)", "Finished Grade Elevation (Feet)", "Depth to Nut (Feet)", "Valve Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<cvHd.Length; i++) { wsCv.Cell(1, i+1).Value=cvHd[i]; wsCv.Cell(1, i+1).Style.Font.Bold=true; } wsCv.SheetView.FreezeRows(1); int cvRow=2;
        foreach(var v in ChilledValves) { wsCv.Cell(cvRow, 1).Value=v.PartKey??""; wsCv.Cell(cvRow, 2).Value=v.Type??""; wsCv.Cell(cvRow, 3).Value=v.FacilityOwner??""; wsCv.Cell(cvRow, 4).Value=v.Size??""; wsCv.Cell(cvRow, 5).Value=v.Orientation??""; wsCv.Cell(cvRow, 6).Value=v.OpenDirection??""; if(v.TurnsToOpen.HasValue)wsCv.Cell(cvRow, 7).Value=v.TurnsToOpen.Value; if(v.NutElevation.HasValue)wsCv.Cell(cvRow, 8).Value=v.NutElevation.Value; if(v.Elevation.HasValue)wsCv.Cell(cvRow, 9).Value=v.Elevation.Value; if(v.Elevation.HasValue&&v.NutElevation.HasValue)wsCv.Cell(cvRow, 10).Value=Math.Round(v.Elevation.Value-v.NutElevation.Value,2); wsCv.Cell(cvRow, 11).Value=v.Manufacturer??""; if(v.Easting.HasValue)wsCv.Cell(cvRow, 12).Value=v.Easting.Value; if(v.Northing.HasValue)wsCv.Cell(cvRow, 13).Value=v.Northing.Value; cvRow++; } wsCv.Columns().AdjustToContents();

        var wsCm = wb.Worksheets.Add("Chilled Meter");
        string[] cmHd = new[] { "Meter Room Number (CM#)", "Proposed Meter Size", "Facility Owner", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<cmHd.Length; i++) { wsCm.Cell(1, i+1).Value=cmHd[i]; wsCm.Cell(1, i+1).Style.Font.Bold=true; } wsCm.SheetView.FreezeRows(1); int cmRow=2;
        foreach(var m in ChilledMeters) { wsCm.Cell(cmRow, 1).Value=m.PartKey??""; wsCm.Cell(cmRow, 2).Value=m.Size??""; wsCm.Cell(cmRow, 3).Value=m.FacilityOwner??""; if(m.Easting.HasValue)wsCm.Cell(cmRow, 4).Value=m.Easting.Value; if(m.Northing.HasValue)wsCm.Cell(cmRow, 5).Value=m.Northing.Value; cmRow++; } wsCm.Columns().AdjustToContents();

        var wsCl = wb.Worksheets.Add("Chilled Locate Box");
        string[] clHd = new[] { "Locate Box Number (CL#)", "Locate Box Subtype", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<clHd.Length; i++) { wsCl.Cell(1, i+1).Value=clHd[i]; wsCl.Cell(1, i+1).Style.Font.Bold=true; } wsCl.SheetView.FreezeRows(1); int clRow=2;
        foreach(var l in ChilledLocateBoxes) { wsCl.Cell(clRow, 1).Value=l.PartKey??""; wsCl.Cell(clRow, 2).Value=l.Subtype??""; if(l.Easting.HasValue)wsCl.Cell(clRow, 3).Value=l.Easting.Value; if(l.Northing.HasValue)wsCl.Cell(clRow, 4).Value=l.Northing.Value; clRow++; } wsCl.Columns().AdjustToContents();

        // --- PIPE CROSSINGS ---
        var wsCross = wb.Worksheets.Add("Pipe Crossing Table");
        string[] crHd = new[] { "Crossing Number", "Upper Pipe Type", "Upper Pipe Size (Inches)", "Finished Grade Elevation (Feet)", "Upper Pipe Top Elevation (Feet)", "Cover to Top of Upper Pipe (Feet)", "Upper Pipe Bottom Elevation (Feet)", "Lower Pipe Type", "Lower Pipe Size (Inches)", "Lower Pipe Top Elevation (Feet)", "Cover to Top of Lower Pipe (Feet)", "Separation Between Pipes (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        for (int i=0; i<crHd.Length; i++) { wsCross.Cell(1, i+1).Value=crHd[i]; wsCross.Cell(1, i+1).Style.Font.Bold=true; } wsCross.SheetView.FreezeRows(1); int crRow=2;
        foreach(var c in PipeCrossings) { wsCross.Cell(crRow, 1).Value=c.CrossingNumber??""; wsCross.Cell(crRow, 2).Value=c.UpperPipeType??""; wsCross.Cell(crRow, 3).Value=c.UpperPipeSize??""; if(c.FinishedGradeElevation.HasValue)wsCross.Cell(crRow, 4).Value=c.FinishedGradeElevation.Value; if(c.UpperPipeTopElevation.HasValue)wsCross.Cell(crRow, 5).Value=c.UpperPipeTopElevation.Value; if(c.UpperCover.HasValue)wsCross.Cell(crRow, 6).Value=c.UpperCover.Value; if(c.UpperPipeBottomElevation.HasValue)wsCross.Cell(crRow, 7).Value=c.UpperPipeBottomElevation.Value; wsCross.Cell(crRow, 8).Value=c.LowerPipeType??""; wsCross.Cell(crRow, 9).Value=c.LowerPipeSize??""; if(c.LowerPipeTopElevation.HasValue)wsCross.Cell(crRow, 10).Value=c.LowerPipeTopElevation.Value; if(c.LowerCover.HasValue)wsCross.Cell(crRow, 11).Value=c.LowerCover.Value; if(c.Separation.HasValue)wsCross.Cell(crRow, 12).Value=c.Separation.Value; if(c.Easting.HasValue)wsCross.Cell(crRow, 13).Value=c.Easting.Value; if(c.Northing.HasValue)wsCross.Cell(crRow, 14).Value=c.Northing.Value; crRow++; } wsCross.Columns().AdjustToContents();

        // --- AS BUILT METADATA ---
        var CurrentProject = _dbContext.Projects.FirstOrDefault(p => p.ProjectId == _currentProjectId);
        var wsMeta = wb.Worksheets.Add("As Built Info");
        wsMeta.Cell(1, 1).Value = "Field"; wsMeta.Cell(1, 2).Value = "Information"; wsMeta.Cell(1, 1).Style.Font.Bold = true; wsMeta.Cell(1, 2).Style.Font.Bold = true;
        wsMeta.Cell(2, 1).Value = "Project Name"; wsMeta.Cell(2, 2).Value = CurrentProject?.ProjectName ?? "";
        wsMeta.Cell(3, 1).Value = "County"; wsMeta.Cell(3, 2).Value = CurrentProject?.County ?? "";
        wsMeta.Cell(4, 1).Value = "Hyperlink"; wsMeta.Cell(4, 2).Value = CurrentProject?.Hyperlink ?? "";
        wsMeta.Cell(5, 1).Value = "As Built Date"; wsMeta.Cell(5, 2).Value = CurrentProject?.AsBuiltDate ?? "";
        wsMeta.Cell(6, 1).Value = "Data Source"; wsMeta.Cell(6, 2).Value = CurrentProject?.DataSource ?? "";
        wsMeta.Cell(7, 1).Value = "Availability Number"; wsMeta.Cell(7, 2).Value = CurrentProject?.AvailabilityNumber ?? "";
        wsMeta.Cell(8, 1).Value = "Capital Project Number"; wsMeta.Cell(8, 2).Value = CurrentProject?.CapitalProjectNumber ?? "";
        wsMeta.Columns().AdjustToContents();

        wb.SaveAs(path);
    }
}
