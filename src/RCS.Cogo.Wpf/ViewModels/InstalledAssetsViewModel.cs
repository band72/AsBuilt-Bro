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
    
    // Pipe Crossing Service
    private readonly InstalledAssetService<PipeCrossing> _pipeCrossingService;

    // Horizontal Alignment Service
    private readonly InstalledAssetService<HorizontalAlignment> _horizontalAlignmentService;

    // Profile Alignment Service
    private readonly InstalledAssetService<ProfileAlignment> _profileAlignmentService;

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
    
    // Horizontal Alignments
    public ObservableCollection<HorizontalAlignment> HorizontalAlignments { get; } = new();

    // Profile Alignments
    public ObservableCollection<ProfileAlignment> ProfileAlignments { get; } = new();

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
        _horizontalAlignmentService = new InstalledAssetService<HorizontalAlignment>(_dbContext);
        _profileAlignmentService = new InstalledAssetService<ProfileAlignment>(_dbContext);
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
        
        await _projectService.EnsureProjectExistsAsync(projectId, projectNumber, "Project " + projectNumber);

        async Task Load<T>(InstalledAssetService<T> service, ObservableCollection<T> collection) where T : InstalledAsset
        {
            var items = await service.LoadAsync(projectId);
            collection.Clear();
            foreach (var i in items) collection.Add(i);
        }

        await Load(_pipeCrossingService, PipeCrossings);
        await Load(_horizontalAlignmentService, HorizontalAlignments);
        await Load(_profileAlignmentService, ProfileAlignments);

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
        else if (item is HorizontalAlignment ha) 
        {
            if (string.IsNullOrEmpty(ha.PartKey)) ha.PartKey = "HA-" + (HorizontalAlignments.Count + 100).ToString();
            await _horizontalAlignmentService.UpsertAsync(_currentProjectId, ha);
        }
        else if (item is ProfileAlignment pa) 
        {
            if (string.IsNullOrEmpty(pa.PartKey)) pa.PartKey = "VA-" + (ProfileAlignments.Count + 100).ToString();
            await _profileAlignmentService.UpsertAsync(_currentProjectId, pa);
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
        else if (item is HorizontalAlignment ha) { 
            if (string.IsNullOrEmpty(ha.PartKey)) ha.PartKey = "HA-" + (HorizontalAlignments.Count + 100).ToString();
            HorizontalAlignments.Add(ha); 
            await _horizontalAlignmentService.UpsertAsync(_currentProjectId, ha); 
        }
        else if (item is ProfileAlignment pa) { 
            if (string.IsNullOrEmpty(pa.PartKey)) pa.PartKey = "VA-" + (ProfileAlignments.Count + 100).ToString();
            ProfileAlignments.Add(pa); 
            await _profileAlignmentService.UpsertAsync(_currentProjectId, pa); 
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
        else if (item is HorizontalAlignment ha) await _horizontalAlignmentService.DeleteAsync(_currentProjectId, ha.Id);
        else if (item is ProfileAlignment pa) await _profileAlignmentService.DeleteAsync(_currentProjectId, pa.Id);
        
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
        void Write<T>(string suffix, ObservableCollection<T> items, Func<T, string> formatter)
        {
            string path = System.IO.Path.Combine(dir, $"{name}_{suffix}.csv");
            using var sw = new System.IO.StreamWriter(path);
            foreach(var item in items) sw.WriteLine(formatter(item));
        }

        // Pipe Crossings
        Write("PipeCrossings", PipeCrossings, i => 
            $"PartKey,Description,Northing,Easting,Notes,Manufacturer,Size,Material,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{C(i.Notes)},{C(i.Manufacturer)},{C(i.Size)},{C(i.Material)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}");

        // Horizontal Alignments
        Write("HorizontalAlignments", HorizontalAlignments, i =>
            $"AlignmentName,Description,ScriptContent\n" +
            $"{C(i.AlignmentName)},{C(i.Description)},{C(i.ScriptContent)}");

        // Profile Alignments
        Write("ProfileAlignments", ProfileAlignments, i =>
            $"ProfileName,Description,ScriptContent\n" +
            $"{C(i.ProfileName)},{C(i.Description)},{C(i.ScriptContent)}");

        // Formatters
        string FormatPipe<T>(T i) where T : Pipe => 
            $"PartKey,Description,Diameter,Size,Material,N_Start,E_Start,N_End,E_End,Inv_Start,Inv_End,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{i.Diameter},{C(i.Size)},{C(i.Material)},{i.NorthingStart},{i.EastingStart},{i.NorthingEnd},{i.EastingEnd},{i.InvertStart},{i.InvertEnd},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";

        string FormatPoint<T>(T i) where T : Structure => 
            $"PartKey,Description,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";

        string FormatFitting<T>(T i) where T : Fitting => 
            $"PartKey,Description,Type,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{C(i.Type)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
            
        string FormatValve<T>(T i) where T : Valve => 
            $"PartKey,Description,Type,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{C(i.Type)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
        
        string FormatMeter<T>(T i) where T : Meter => 
            $"PartKey,Description,Size,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{C(i.Size)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
            
        string FormatHydrant<T>(T i) where T : Hydrant => 
            $"PartKey,Description,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";
            
        string FormatLocateBox<T>(T i) where T : LocateBox => 
            $"PartKey,Description,Northing,Easting,Elevation,Notes,Manufacturer,Year,Confidence,Source,Warning\n" +
            $"{C(i.PartKey)},{C(i.Description)},{i.Northing},{i.Easting},{i.Elevation},{C(i.Notes)},{C(i.Manufacturer)},{C(i.YearManufactured)},{C(i.Confidence)},{C(i.Source)},{C(i.Warning)}";

        // Export Calls
        // Water
        Write("WaterPipeRun", WaterPipes, FormatPipe);
        Write("WaterPointsAlongPipe", WaterPoints, FormatPoint);
        Write("WaterFitting", WaterFittings, FormatFitting);
        Write("WaterValve", WaterValves, FormatValve);
        Write("WaterHydrant", WaterHydrants, FormatHydrant);
        Write("WaterMeter", WaterMeters, FormatMeter);
        Write("WaterLocateBox", WaterLocateBoxes, FormatLocateBox);

        // WW
        Write("WWGravityPipeRun", WWGravityPipes, FormatPipe);
        Write("WWPressurePipeRun", WWPressurePipes, FormatPipe);
        Write("WWPointsAlongPipe", WWPoints, FormatPoint);
        Write("WWFitting", WWFittings, FormatFitting);
        Write("Manhole", Manholes, FormatPoint);
        Write("WWServicePointMeter", WWServicePoints, FormatPoint);
        Write("WWValve", WWValves, FormatValve);
        Write("WWLocateBox", WWLocateBoxes, FormatLocateBox);

        // Reclaimed
        Write("ReclaimedPipeRun", ReclaimedPipes, FormatPipe);
        Write("ReclaimedPointsAlongPipe", ReclaimedPoints, FormatPoint);
        Write("ReclaimedFitting", ReclaimedFittings, FormatFitting);
        Write("ReclaimedValve", ReclaimedValves, FormatValve);
        Write("ReclaimedHydrant", ReclaimedHydrants, FormatHydrant);
        Write("ReclaimedMeter", ReclaimedMeters, FormatMeter);
        Write("ReclaimedLocateBox", ReclaimedLocateBoxes, FormatLocateBox);

        // Chilled
        Write("ChilledPipeRun", ChilledPipes, FormatPipe);
        Write("ChilledPointsAlongPipe", ChilledPoints, FormatPoint);
        Write("ChilledFitting", ChilledFittings, FormatFitting);
        Write("ChilledValve", ChilledValves, FormatValve);
        Write("ChilledMeter", ChilledMeters, FormatMeter);
        Write("ChilledLocateBox", ChilledLocateBoxes, FormatLocateBox);

        // Gas
        Write("GasGravityPipeRun", GGravityPipes, FormatPipe);
        Write("GasPressurePipeRun", GPressurePipes, FormatPipe);
        Write("GasPointsAlongPipe", GPoints, FormatPoint);
        Write("GasFitting", GFittings, FormatFitting);
        Write("GasManhole", GManholes, FormatPoint);
        Write("GasServicePointMeter", GServicePoints, FormatPoint);
        Write("GasValve", GValves, FormatValve);
        Write("GasLocateBox", GLocateBoxes, FormatLocateBox);

        // Electric
        Write("ElectricGravityPipeRun", EGravityPipes, FormatPipe);
        Write("ElectricPressurePipeRun", EPressurePipes, FormatPipe);
        Write("ElectricPointsAlongPipe", EPoints, FormatPoint);
        Write("ElectricFitting", EFittings, FormatFitting);
        Write("ElectricManhole", EManholes, FormatPoint);
        Write("ElectricServicePointMeter", EServicePoints, FormatPoint);
        Write("ElectricValve", EValves, FormatValve);
        Write("ElectricLocateBox", ELocateBoxes, FormatLocateBox);

        // Storm
        Write("STGravityPipeRun", STGravityPipes, FormatPipe);
        Write("STPressurePipeRun", STPressurePipes, FormatPipe);
        Write("STPointsAlongPipe", STPoints, FormatPoint);
        Write("STFitting", STFittings, FormatFitting);
        Write("STManhole", STManholes, FormatPoint);
        Write("STServicePointMeter", STServicePoints, FormatPoint);
        Write("STValve", STValves, FormatValve);
        Write("STLocateBox", STLocateBoxes, FormatLocateBox);
    }
}
