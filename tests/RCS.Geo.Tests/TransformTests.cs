namespace RCS.Geo.Tests;

using System;
using RCS.Geo.Abstractions;
using RCS.Geo.Core;
using RCS.Geo.ProjNet;
using Xunit;

public class TransformTests
{
    private readonly ICoordinateTransformService _transformService;

    public TransformTests()
    {
        var registry = new StaticCrsRegistry();
        _transformService = new ProjNetCoordinateTransformService(registry);
    }

    [Fact]
    public void Can_Transform_FloridaEast_UsFoot_To_LatLon()
    {
        // Arrange
        // Florida East NAD83(2011) US survey foot: EPSG:6438
        // Let's use a roughly central point in Florida East
        // Lat: 28.5, Lon: -81.0 (Approx Orlando)
        // From EPSG guidance or EPSG registry, we'll do a roundtrip first to find true coords, 
        // or just test the reverse transform works.
        double lat = 28.538336;
        double lon = -81.379234;

        var wgs84Point = new GeographicPoint(lat, lon);
        
        // Act
        // Go to state plane
        var statePlanePoint = _transformService.ToStatePlane(wgs84Point, "EPSG:4326", "EPSG:6438");
        
        // Go back to lat/lon
        var roundTripPoint = _transformService.ToLatLon(statePlanePoint, "EPSG:4326");

        // Assert
        Assert.Equal(lat, roundTripPoint.Latitude, 6);
        Assert.Equal(lon, roundTripPoint.Longitude, 6);
    }

    [Fact]
    public void Can_Transform_FloridaWest_UsFoot_RoundTrip()
    {
        double lat = 27.950575;
        double lon = -82.457176; // Tampa

        var wgs84Point = new GeographicPoint(lat, lon);
        
        var statePlanePoint = _transformService.ToStatePlane(wgs84Point, "EPSG:4326", "EPSG:6443");
        var roundTripPoint = _transformService.ToLatLon(statePlanePoint, "EPSG:4326");

        Assert.Equal(lat, roundTripPoint.Latitude, 6);
        Assert.Equal(lon, roundTripPoint.Longitude, 6);
    }

    [Fact]
    public void Can_Transform_FloridaNorth_UsFoot_RoundTrip()
    {
        double lat = 30.332184;
        double lon = -81.655647; // Jacksonville

        var wgs84Point = new GeographicPoint(lat, lon);
        
        var statePlanePoint = _transformService.ToStatePlane(wgs84Point, "EPSG:4326", "EPSG:6439");
        var roundTripPoint = _transformService.ToLatLon(statePlanePoint, "EPSG:4326");

        Assert.Equal(lat, roundTripPoint.Latitude, 6);
        Assert.Equal(lon, roundTripPoint.Longitude, 6);
    }

    [Fact]
    public void Expected_Values_From_Known_Point()
    {
        // Based on NOAA / NGS coordinate conversion for a known point or we can just ensure 
        // the coordinate transform doesn't fail and gives a reasonable expected output.
        // For EPSG:6438 (Florida East NAD83(2011) ftUS):
        // False Easting = 656166.6667
        // Central Meridian = -81
        var wgs84Point = new GeographicPoint(28.0, -81.0);
        var statePlanePoint = _transformService.ToStatePlane(wgs84Point, "EPSG:4326", "EPSG:6438");

        // The point is exactly on the central meridian, so Easting should be very close to the false easting
        Assert.InRange(statePlanePoint.Easting, 656165.0, 656168.0);
        
        // Northing should be positive, around 1 million+
        Assert.True(statePlanePoint.Northing > 0);
    }

    [Fact]
    public void Missing_Crs_Throws_GeoTransformException()
    {
        var wgs84Point = new GeographicPoint(28.0, -81.0);
        
        var ex = Assert.Throws<GeoTransformException>(() => 
            _transformService.ToStatePlane(wgs84Point, "EPSG:4326", "EPSG:99999")
        );
        
        Assert.Equal("EPSG:4326", ex.SourceCrsId);
        Assert.Equal("EPSG:99999", ex.TargetCrsId);
    }
}
