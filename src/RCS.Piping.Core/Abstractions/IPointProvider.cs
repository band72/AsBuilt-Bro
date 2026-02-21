using RCS.Cogo.Core.Primitives;

namespace RCS.Piping.Core.Abstractions;

public interface IPointProvider
{
    Point3D? GetPoint(string id);
    bool PointExists(string id);
}
