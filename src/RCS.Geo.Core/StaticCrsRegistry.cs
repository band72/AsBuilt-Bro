namespace RCS.Geo.Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RCS.Geo.Abstractions;

public sealed class StaticCrsRegistry : ICrsRegistry
{
    private readonly ConcurrentDictionary<string, string> _wktDefinitions;

    public StaticCrsRegistry()
    {
        _wktDefinitions = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // WGS 84 (Latitude/Longitude)
        _wktDefinitions["EPSG:4326"] = @"GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.01745329251994328,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]]";

        // NAD83(2011) / Florida East (ftUS) (EPSG:6438)
        _wktDefinitions["EPSG:6438"] = @"PROJCS[""NAD83(2011) / Florida East (ftUS)"",GEOGCS[""NAD83(2011)"",DATUM[""NAD83_National_Spatial_Reference_System_2011"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],AUTHORITY[""EPSG"",""1116""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""6318""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",24.3333333333333],PARAMETER[""central_meridian"",-81],PARAMETER[""scale_factor"",0.999941177],PARAMETER[""false_easting"",656166.666666667],PARAMETER[""false_northing"",0],UNIT[""US survey foot"",0.304800609601219,AUTHORITY[""EPSG"",""9003""]],AXIS[""Easting"",EAST],AXIS[""Northing"",NORTH],AUTHORITY[""EPSG"",""6438""]]";

        // NAD83(2011) / Florida West (ftUS) (EPSG:6443)
        _wktDefinitions["EPSG:6443"] = @"PROJCS[""NAD83(2011) / Florida West (ftUS)"",GEOGCS[""NAD83(2011)"",DATUM[""NAD83_National_Spatial_Reference_System_2011"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],AUTHORITY[""EPSG"",""1116""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""6318""]],PROJECTION[""Transverse_Mercator""],PARAMETER[""latitude_of_origin"",24.3333333333333],PARAMETER[""central_meridian"",-82],PARAMETER[""scale_factor"",0.999941177],PARAMETER[""false_easting"",2624666.6666],PARAMETER[""false_northing"",0],UNIT[""US survey foot"",0.304800609601219,AUTHORITY[""EPSG"",""9003""]],AXIS[""Easting"",EAST],AXIS[""Northing"",NORTH],AUTHORITY[""EPSG"",""6443""]]";

        // NAD83(2011) / Florida North (ftUS) (EPSG:6439)
        _wktDefinitions["EPSG:6439"] = @"PROJCS[""NAD83(2011) / Florida North (ftUS)"",GEOGCS[""NAD83(2011)"",DATUM[""NAD83_National_Spatial_Reference_System_2011"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],AUTHORITY[""EPSG"",""1116""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""6318""]],PROJECTION[""Lambert_Conformal_Conic_2SP""],PARAMETER[""latitude_of_origin"",29],PARAMETER[""central_meridian"",-84.5],PARAMETER[""standard_parallel_1"",30.75],PARAMETER[""standard_parallel_2"",29.5833333333333],PARAMETER[""false_easting"",1968500],PARAMETER[""false_northing"",0],UNIT[""US survey foot"",0.304800609601219,AUTHORITY[""EPSG"",""9003""]],AXIS[""Easting"",EAST],AXIS[""Northing"",NORTH],AUTHORITY[""EPSG"",""6439""]]";
    }

    public string GetWkt(string crsId)
    {
        if (_wktDefinitions.TryGetValue(crsId, out var wkt))
        {
            return wkt;
        }

        throw new KeyNotFoundException($"CRS ID '{crsId}' is not registered in the static registry.");
    }

    public void RegisterWkt(string crsId, string wkt)
    {
        _wktDefinitions[crsId] = wkt;
    }
}
