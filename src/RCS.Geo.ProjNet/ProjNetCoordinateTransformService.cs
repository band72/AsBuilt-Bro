namespace RCS.Geo.ProjNet;

using System;
using System.Collections.Concurrent;

using global::ProjNet.CoordinateSystems;
using global::ProjNet.CoordinateSystems.Transformations;
using RCS.Geo.Abstractions;
using RCS.Geo.Core;

public class ProjNetCoordinateTransformService : ICoordinateTransformService
{
    private readonly ICrsRegistry _crsRegistry;
    private readonly CoordinateSystemFactory _csFactory;
    private readonly CoordinateTransformationFactory _ctFactory;
    private readonly ConcurrentDictionary<string, ICoordinateTransformation> _transformations;

    public ProjNetCoordinateTransformService(ICrsRegistry crsRegistry)
    {
        _crsRegistry = crsRegistry ?? throw new ArgumentNullException(nameof(crsRegistry));
        _csFactory = new CoordinateSystemFactory();
        _ctFactory = new CoordinateTransformationFactory();
        _transformations = new ConcurrentDictionary<string, ICoordinateTransformation>();
    }

    public GeographicPoint ToLatLon(ProjectedPoint point, string targetCrsId = "EPSG:4326")
    {
        if (point == null) throw new ArgumentNullException(nameof(point));

        try
        {
            var transform = GetTransformation(point.CrsId, targetCrsId);
            
            // ProjNet transforms from Projected (Easting, Northing) to Geographic (Longitude, Latitude)
            double[] fromPoint = new[] { point.Easting, point.Northing };
            double[] toPoint = transform.MathTransform.Transform(fromPoint);

            // GeographicPoint takes (Latitude, Longitude)
            return new GeographicPoint(toPoint[1], toPoint[0]);
        }
        catch (Exception ex)
        {
            throw new GeoTransformException($"Failed to transform point from {point.CrsId} to {targetCrsId}", point.CrsId, targetCrsId, ex);
        }
    }

    public ProjectedPoint ToStatePlane(GeographicPoint point, string sourceCrsId, string targetCrsId)
    {
        if (point == null) throw new ArgumentNullException(nameof(point));

        try
        {
            var transform = GetTransformation(sourceCrsId, targetCrsId);
            
            // ProjNet transforms from Geographic (Longitude, Latitude) to Projected (Easting, Northing)
            double[] fromPoint = new[] { point.Longitude, point.Latitude };
            double[] toPoint = transform.MathTransform.Transform(fromPoint);

            return new ProjectedPoint(toPoint[0], toPoint[1], targetCrsId);
        }
        catch (Exception ex)
        {
            throw new GeoTransformException($"Failed to transform point from {sourceCrsId} to {targetCrsId}", sourceCrsId, targetCrsId, ex);
        }
    }

    private ICoordinateTransformation GetTransformation(string sourceId, string targetId)
    {
        string key = $"{sourceId}->{targetId}";
        return _transformations.GetOrAdd(key, _ =>
        {
            try
            {
                var sourceWkt = _crsRegistry.GetWkt(sourceId);
                var targetWkt = _crsRegistry.GetWkt(targetId);

                var sourceCrs = _csFactory.CreateFromWkt(sourceWkt);
                var targetCrs = _csFactory.CreateFromWkt(targetWkt);

                return _ctFactory.CreateFromCoordinateSystems(sourceCrs, targetCrs);
            }
            catch (Exception ex)
            {
                throw new GeoTransformException($"Error generating transformation sequence from {sourceId} to {targetId}: {ex.Message}", sourceId, targetId, ex);
            }
        });
    }
}
