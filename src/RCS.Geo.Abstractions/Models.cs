namespace RCS.Geo.Abstractions;

public sealed record ProjectedPoint(double Easting, double Northing, string CrsId);
public sealed record GeographicPoint(double Latitude, double Longitude);

public sealed record CoordinateSystemId(string Authority, string Code)
{
    public string FullId => $"{Authority}:{Code}";
    
    public override string ToString() => FullId;
    
    public static implicit operator string(CoordinateSystemId id) => id.FullId;
}

public interface ICoordinateTransformService
{
    GeographicPoint ToLatLon(ProjectedPoint point, string targetCrsId = "EPSG:4326");
    ProjectedPoint ToStatePlane(GeographicPoint point, string sourceCrsId, string targetCrsId);
}

public interface ICrsRegistry
{
    string GetWkt(string crsId);
}
