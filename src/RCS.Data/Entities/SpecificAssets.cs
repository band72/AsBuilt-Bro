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
