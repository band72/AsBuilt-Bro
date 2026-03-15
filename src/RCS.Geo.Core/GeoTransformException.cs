namespace RCS.Geo.Core;

using System;

public class GeoTransformException : Exception
{
    public string? SourceCrsId { get; }
    public string? TargetCrsId { get; }

    public GeoTransformException(string message) 
        : base(message) { }

    public GeoTransformException(string message, Exception innerException) 
        : base(message, innerException) { }

    public GeoTransformException(string message, string sourceCrsId, string targetCrsId, Exception innerException) 
        : base(message, innerException)
    {
        SourceCrsId = sourceCrsId;
        TargetCrsId = targetCrsId;
    }
}
