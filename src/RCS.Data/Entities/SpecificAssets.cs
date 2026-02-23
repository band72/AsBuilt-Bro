using RCS.Data.Entities;

namespace RCS.Data.Entities;

// Water
public class WaterPipe : Pipe { }
public class WaterPoint : Structure { }
public class WaterFitting : Fitting { }
public class WaterValve : Valve { }
public class WaterHydrant : Hydrant { }
public class WaterMeter : Meter { }
public class WaterLocateBox : LocateBox { }

// Waste Water (WW)
public class WWGravityPipe : Pipe { }
public class WWPressurePipe : Pipe { }
public class WWPoint : Structure { }
public class WWFitting : Fitting { }
public class Manhole : Structure { } 
public class WWServicePoint : Structure { } // Meter or Service Point
public class WWValve : Valve { }
public class WWLocateBox : LocateBox { }

// Reclaimed
public class ReclaimedPipe : Pipe { }
public class ReclaimedPoint : Structure { }
public class ReclaimedFitting : Fitting { }
public class ReclaimedValve : Valve { }
public class ReclaimedHydrant : Hydrant { }
public class ReclaimedMeter : Meter { }
public class ReclaimedLocateBox : LocateBox { }

// Chilled
public class ChilledPipe : Pipe { }
public class ChilledPoint : Structure { }
public class ChilledFitting : Fitting { }
public class ChilledValve : Valve { }
public class ChilledMeter : Meter { }
public class ChilledLocateBox : LocateBox { }

// Gas (G)
public class GGravityPipe : Pipe { }
public class GPressurePipe : Pipe { }
public class GPoint : Structure { }
public class GFitting : Fitting { }
public class GManhole : Structure { }
public class GServicePoint : Structure { }
public class GValve : Valve { }
public class GLocateBox : LocateBox { }

// Electric (E)
public class EGravityPipe : Pipe { }
public class EPressurePipe : Pipe { }
public class EPoint : Structure { }
public class EFitting : Fitting { }
public class EManhole : Structure { }
public class EServicePoint : Structure { }
public class EValve : Valve { }
public class ELocateBox : LocateBox { }

// Storm (ST)
public class STGravityPipe : Pipe { }
public class STPressurePipe : Pipe { }
public class STPoint : Structure { }
public class STFitting : Fitting { }
public class STManhole : Structure { }
public class STServicePoint : Structure { }
public class STValve : Valve { }
public class STLocateBox : LocateBox { }
