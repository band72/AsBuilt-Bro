using RCS.Cogo.App.Commands;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App;

public static class AppInitializer
{
    public static CommandRegistry InitializeRegistry()
    {
        var registry = new CommandRegistry();
        
        // Register Alignment Commands
        registry.Register(new AlignmentCommand());
        registry.Register(new ProfileCommand());
        registry.Register(new VpiCommand());
        registry.Register(new HaLblCommand("HALBL-ON"));
        registry.Register(new HaLblCommand("HALBL-OFF"));
        registry.Register(new ResetConfigCommand("RESET-ON"));
        registry.Register(new ResetConfigCommand("RESET-OFF"));

        // Register Commands
        registry.Register(new NeCommand());
        registry.Register(new StnCommand());
        registry.Register(new ZdCommand());
        registry.Register(new InvCommand());
        registry.Register(new AdCommand());
        registry.Register(new BsCommand());
        registry.Register(new BegCommand());
        registry.Register(new ContCommand());
        registry.Register(new EndCommand());
        registry.Register(new MapCheckCommand());
        registry.Register(new SaveCommand());
        registry.Register(new LoadCommand(registry));
        registry.Register(new BdCommand());
        registry.Register(new AzAzCommand());
        registry.Register(new BbCommand());
        registry.Register(new LnLnCommand());
        registry.Register(new NezCommand());
        registry.Register(new AzCommand());
        registry.Register(new DistCommand());
        registry.Register(new AngCommand());
        registry.Register(new DelCommand());
        registry.Register(new HelpCommand(registry));
        registry.Register(new ClearCommand());
        registry.Register(new TravCommand());
        registry.Register(new PntCommand());
        registry.Register(new XcCommand());
        registry.Register(new MapChkCommand());
        registry.Register(new XptPtsCommand());
        registry.Register(new SyncPtsCommand());
        registry.Register(new CopyPtCommand());
        
        // Aliases
        registry.Register(new StartCommand());
        registry.Register(new PointCommand());
        registry.Register(new CloseCommand());
        registry.Register(new InverseCommand());
        registry.Register(new OcCommand());
        registry.Register(new FsCommand());
        
        registry.Register(new FigCommand());
        registry.Register(new PtCommand());
        registry.Register(new ACommand());
        registry.Register(new BCommand());
        registry.Register(new LCommand());
        registry.Register(new CCommand());
        registry.Register(new DCommand());
        registry.Register(new ArcArcCommand());
        registry.Register(new DispCommand());
        
        // Observations
        registry.Register(new FaceCommand("F1"));
        registry.Register(new FaceCommand("F2"));
        registry.Register(new DdCommand());

        // Environment
        registry.Register(new UnitsCommand());
        registry.Register(new AtmosCommand());
        registry.Register(new TempCommand());
        registry.Register(new PressCommand());
        registry.Register(new SfCommand());
        registry.Register(new CrCommand());
        registry.Register(new AnglesCommand());
        registry.Register(new VertCommand());
        registry.Register(new HorizCommand());
        registry.Register(new EdmCommand());
        registry.Register(new PrismCommand());
        registry.Register(new CollCommand());

        // Transformations
        registry.Register(new LnCommand());
        registry.Register(new TrnCommand());
        registry.Register(new RotCommand());

        registry.Register(new AreaCommand());
        registry.Register(new CalcCommand());
        registry.Register(new SdCommand());
        registry.Register(new VdCommand());
        registry.Register(new GradeCommand());
        registry.Register(new SlopeCommand());
        registry.Register(new StadiaCommand());

        // Design / Curve
        registry.Register(new PcCommand());
        registry.Register(new CrvCommand());
        registry.Register(new RtCommand());
        registry.Register(new C3Command());
        registry.Register(new OffsetCommand());
        registry.Register(new ModCommand());
        registry.Register(new StakeoutCommand("MCS"));
        registry.Register(new StakeoutCommand("MCE"));
        registry.Register(new StakeoutCommand("RKLN"));
        registry.Register(new StakeoutCommand("RKAZ"));
        registry.Register(new StakeoutCommand("RKBRG"));
        registry.Register(new StakeoutCommand("RKBRG"));
        registry.Register(new RkRkCommand()); // Real Implementation
        registry.Register(new StakeoutCommand("BL"));
        registry.Register(new StakeoutCommand("BL"));
        registry.Register(new StakeoutCommand("CL"));
        registry.Register(new StakeoutCommand("HI"));
        registry.Register(new StakeoutCommand("XS"));
        registry.Register(new ApCommand());
        registry.Register(new StakeoutCommand("LAT"));

        // System / System Macros
        registry.Register(new ResetCommand());
        registry.Register(new AboutCommand());
        registry.Register(new SetCommand());
        registry.Register(new EchoCommand());
        registry.Register(new LogCommand());
        registry.Register(new ListCommand());
        registry.Register(new ShowCommand());
        registry.Register(new ExportCommand());
        registry.Register(new ReportCommand());
        registry.Register(new SaveHalnCommand());
        registry.Register(new SavePflCommand());

        return registry;
    }
}
