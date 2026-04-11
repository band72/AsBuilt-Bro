namespace RCS.Cogo.App.State;

public class CogoCode
{
    public string LocalCode { get; set; } = string.Empty;
    public string SystemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Block { get; set; } = string.Empty;

    /// <summary>DXF INSERT scale factor (X=Y=Z). Default 1.0 = no scaling.</summary>
    public double BlockScale { get; set; } = 1.0;

    public string SymbolImagePath
    {
        get
        {
            var baseDir = new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while (baseDir != null && baseDir.Name != "RCS.Cogo.Enterprise.Modern")
            {
                if (baseDir.GetDirectories("SymbolsLibrary").Length > 0)
                {
                    break;
                }
                baseDir = baseDir.Parent;
            }
            var libraryPath = baseDir != null 
                ? System.IO.Path.Combine(baseDir.FullName, "SymbolsLibrary") 
                : System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "SymbolsLibrary");
            
            var expectedPath = System.IO.Path.Combine(libraryPath, $"{LocalCode}_{SystemCode}.png");
            if (System.IO.File.Exists(expectedPath))
            {
                return expectedPath;
            }
            
            // AI Mapping Fallback based on Description
            string d = (Description ?? "").ToUpper();
            string sys = (SystemCode ?? "").ToUpper();
            if (string.IsNullOrWhiteSpace(d)) d = sys;

            string fallback = "";

            bool isSewer = d.Contains("WASTE") || d.Contains("SEW") || d.Contains("SANITARY") || sys.Contains("WW");
            bool isWater = !isSewer && (d.Contains("WATER") || sys == "W" || sys.Contains("WAT") || sys.Contains("FIRE"));
            bool isStorm = d.Contains("STORM") || d.Contains("DRAIN") || sys.Contains("ST") || sys == "D";
            bool isGas   = d.Contains("GAS") || sys.Contains("G");
            bool isElec  = d.Contains("ELEC") || d.Contains("POWER") || d.Contains("LIGHT") || d.Contains("POLE") || d.Contains("GUY") || d.Contains("WPP") || sys.Contains("E");
            bool isRec   = d.Contains("RECLAIM") || sys.Contains("REC");
            bool isChil  = d.Contains("CHILL") || sys.Contains("CH");

            bool isValve   = d.Contains("VALVE");
            bool isFitting = d.Contains("FITTING") || d.Contains("BEND") || d.Contains("TEE") || d.Contains("CAP") || d.Contains("ELBOW");
            bool isManhole = d.Contains("MANHOLE") || d.Contains("VAULT") || d.Contains("JUNCTION");
            bool isGrate   = d.Contains("CATCH BASIN") || d.Contains("DROP INLET") || d.Contains("INLET") || d.Contains("CB") || d.Contains("DI") || d.Contains("GRATE");
            bool isHeadwall= d.Contains("HEADWALL") || d.Contains("HW");
            bool isARV     = d.Contains("AIR RELEASE") || d.Contains("ARV") || d.Contains("AIRVAC");
            bool isMeter   = d.Contains("METER");
            bool isHydrant = d.Contains("HYDRANT") || d.Contains("FH");
            bool isPole    = d.Contains("POLE") || d.Contains("LIGHT") || d.Contains("WPP");
            bool isBox     = d.Contains("BOX") || d.Contains("PEDESTAL") || d.Contains("PULL");
            bool isLine    = d.Contains("LINE") || d.Contains("PIPE") || d.Contains("MAIN") || d.Contains("RUN") || d.Contains("CONDUIT");
            bool isBFP     = d.Contains("BACK FLOW") || d.Contains("BFP") || d.Contains("PREVENTER");
            bool isBlowOff = d.Contains("BLOW") || d.Contains("BO");

            if (isWater)
            {
                if (isValve) fallback = "WV_JEAWV.png";
                else if (isFitting) fallback = "WF_JEAWF.png";
                else if (isManhole) fallback = "WM_JEAWM.png";
                else if (isMeter) fallback = "WMET_JEAWMET.png";
                else if (isARV) fallback = "WAR_JEAAIR.png";
                else if (isBFP) fallback = "WBFP_JEAWBFP.png";
                else if (isBlowOff) fallback = "WBO_JEAWBO.png";
                else if (isHydrant) fallback = "HYD_JEAHYD.png";
                else if (isLine) fallback = "W_W.png";
                else fallback = "WAT_JEAW.png";
            }
            else if (isSewer)
            {
                if (isValve) fallback = "WWV_JEAWWV.png";
                else if (isFitting) fallback = "WWF_JEAWWF.png";
                else if (isManhole) fallback = "WWM_JEAWWM.png";
                else if (isLine) fallback = "WW_WW.png";
                else fallback = "SEW_JEAWW.png";
            }
            else if (isStorm)
            {
                if (isValve) fallback = "STV_JEASTV.png";
                else if (isFitting) fallback = "STF_JEASTF.png";
                else if (isManhole) fallback = "STM_JEASTM.png";
                else if (isGrate) fallback = "DI_JEADI.png";
                else if (isHeadwall) fallback = "HW_JEAHW.png";
                else if (isLine) fallback = "ST_ST.png";
                else fallback = "STORM_JEAST.png";
            }
            else if (isGas)
            {
                if (isValve) fallback = "GV_JEAGV.png";
                else if (isFitting) fallback = "GF_JEAGF.png";
                else if (isManhole) fallback = "GM_JEAGM.png";
                else if (isMeter) fallback = "GMET_JEAGMET.png";
                else if (isLine) fallback = "G_G.png";
                else fallback = "GAS_JEAG.png";
            }
            else if (isElec)
            {
                if (isValve) fallback = "EV_JEAEV.png";
                else if (isFitting) fallback = "EF_JEAEF.png";
                else if (isManhole) fallback = "EM_JEAEM.png";
                else if (isMeter) fallback = "EMET_JEAEMET.png";
                else if (isPole) fallback = "EPOLE_E-POLE.png";
                else if (isBox) fallback = "EBOX_E-BOX.png";
                else if (isLine) fallback = "E_E.png";
                else fallback = "ELEC_JEAE.png";
            }
            else if (isRec)
            {
                if (isValve) fallback = "RV_JEARV.png";
                else if (isFitting) fallback = "RF_JEARF.png";
                else if (isManhole) fallback = "RM_JEARM.png";
                else if (isLine) fallback = "R_R.png";
                else fallback = "REC_JEAR.png";
            }
            else if (isChil)
            {
                if (isValve) fallback = "CHV_JEACHV.png";
                else if (isFitting) fallback = "CHF_JEACHF.png";
                else if (isManhole) fallback = "CHM_JEACHM.png";
                else if (isLine) fallback = "CH_CH.png";
                else fallback = "CHIL_JEACH.png";
            }

            if (string.IsNullOrEmpty(fallback))
            {
                if (isValve) fallback = "WV_JEAWV.png";
                else if (isFitting) fallback = "WF_JEAWF.png";
                else if (isManhole) fallback = "WM_JEAWM.png";
                else if (isMeter) fallback = "WMET_JEAWMET.png";
                else if (isARV) fallback = "WAR_JEAAIR.png";
                else if (isBFP) fallback = "WBFP_JEAWBFP.png";
                else if (isHeadwall) fallback = "HW_JEAHW.png";
                else if (isGrate) fallback = "DI_JEADI.png";
                else if (isBlowOff) fallback = "WBO_JEAWBO.png";
                else if (isHydrant) fallback = "HYD_JEAHYD.png";
                else if (isPole) fallback = "EPOLE_E-POLE.png";
                else if (isBox) fallback = "EBOX_E-BOX.png";
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                var fbPath = System.IO.Path.Combine(libraryPath, fallback);
                if (System.IO.File.Exists(fbPath)) return fbPath;
            }

            return "";
        }
    }

    public CogoCode() { }

    public CogoCode(string local, string system, string desc, string block = "", double blockScale = 1.0)
    {
        LocalCode = local;
        SystemCode = system;
        Description = desc;
        Block = block;
        BlockScale = blockScale;
    }
}
