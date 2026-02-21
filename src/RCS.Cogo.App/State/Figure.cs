using System.Collections.Generic;

namespace RCS.Cogo.App.State;

public class Figure
{
    public string Name { get; }
    public List<string> PointIds { get; } = new();

    public Figure(string name)
    {
        Name = name;
    }

    public void AddPoint(string pointId)
    {
        PointIds.Add(pointId);
    }
}
