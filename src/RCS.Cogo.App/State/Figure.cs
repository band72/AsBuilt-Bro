using System.Collections.Generic;

namespace RCS.Cogo.App.State;

public class Figure
{
    public string Name { get; }
    public List<string> PointIds { get; } = new();
    public List<FigureLabel> Labels { get; } = new();
    public bool MapCheckFailed { get; set; } = false;

    public Figure(string name)
    {
        Name = name;
    }

    public void AddPoint(string pointId)
    {
        PointIds.Add(pointId);
    }
}

public class FigureLabel
{
    public string Text { get; set; } = "";
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double RotationDegrees { get; set; }
}
