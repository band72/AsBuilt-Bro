using System.Collections.Generic;
using RCS.Alignments.Core;
using RCS.Cogo.App.State;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Scripting;

/// <summary>
/// Defines the execution context for COGO commands.
/// Maintains the state of the survey project.
/// </summary>
public interface ICogoContext
{
    /// <summary>
    /// Gets or sets the current occupied station point.
    /// </summary>
    Point3D? CurrentStation { get; set; }

    /// <summary>
    /// Gets or sets the current backsight point.
    /// </summary>
    /// <summary>
    /// Gets or sets the current backsight point.
    /// </summary>
    Point3D? CurrentBacksight { get; set; }

    /// <summary>
    /// Gets or sets whether Traverse Mode is active (auto-updates Station/Backsight).
    /// </summary>
    bool TraverseMode { get; set; }

    // Environment Settings
    string Units { get; set; } // Foot/Meter
    double Temperature { get; set; }
    double Pressure { get; set; }
    double ScaleFactor { get; set; }
    bool AtmosCorrection { get; set; }
    bool CurvatureRefraction { get; set; }
    bool AutoPoint { get; set; }
    string AngleFormat { get; set; } // Right/Left
    string VerticalFormat { get; set; } // Zenith/Horiz
    string EdmMode { get; set; } 
    string PrismMode { get; set; }
    double MapCheckClosureTolerance { get; set; }
    bool ShowAlignmentLabels { get; set; }
    bool ShowVerticalAlignmentLabels { get; set; }
    bool ShowVPIs { get; set; }
    bool ShowGradePercent { get; set; }

    /// <summary>
    /// Gets or sets the currently active figure being constructed.
    /// </summary>
    Figure? CurrentFigure { get; set; }

    /// <summary>
    /// Stores the two possible intersection points from the last intersection command (like RKRK).
    /// </summary>
    (Point3D? Left, Point3D? Right) LastIntersections { get; set; }

    /// <summary>
    /// Adds a new figure to the project.
    /// </summary>
    void AddFigure(Figure figure);

    /// <summary>
    /// Active Alignment construction
    /// </summary>
    RCS.Alignments.Core.Alignment? CurrentAlignment { get; set; }
    RCS.Alignments.Core.Profile? CurrentProfile { get; set; }

    void AddAlignment(RCS.Alignments.Core.Alignment alignment);
    RCS.Alignments.Core.Alignment? GetAlignment(string name);
    IEnumerable<RCS.Alignments.Core.Alignment> GetAllAlignments();

    // --- Cross Section Session State ---
    string?  XsAlignmentName  { get; set; }
    List<(double Station, double Offset, double Elevation)>? XsGroundShots { get; set; }
    double   XsTemplateWidthL { get; set; }
    double   XsTemplateWidthR { get; set; }
    double   XsForeslopeL     { get; set; }
    double   XsForeslopeR     { get; set; }
    List<CrossSection>? CrossSections { get; set; }

    /// <summary>
    /// Retrieves a figure by name.
    /// </summary>
    Figure? GetFigure(string name);

    /// <summary>
    /// Adds a new point to the project database.
    /// </summary>
    void AddPoint(string pointId, Point3D point, string description = "");

    /// <summary>
    /// Retrieves a point by its ID.
    /// </summary>
    Point3D? GetPoint(string pointId);

    /// <summary>
    /// Gets the next available numeric Point ID.
    /// </summary>
    int GetNextPointId();

    /// <summary>
    /// Logs a message to the output console.
    /// </summary>
    void Log(string message);

    /// <summary>
    /// Gets a read-only list of all points.
    /// </summary>
    IEnumerable<(string Id, Point3D Point, string Description)> GetAllPoints();

    /// <summary>
    /// Gets a read-only list of all figures.
    /// </summary>
    IEnumerable<Figure> GetAllFigures();
    /// <summary>
    /// Deletes a point by its ID.
    /// </summary>
    bool DeletePoint(string pointId);

    /// <summary>
    /// Deletes a figure by its name.
    /// </summary>
    bool DeleteFigure(string name);

    /// <summary>
    /// Clears the output log.
    /// </summary>
    void ClearLog();

    /// <summary>
    /// Action to save the active script as a Horizontal Alignment.
    /// </summary>
    System.Action<string, string>? SaveHorizontalAlignmentAction { get; set; }

    /// <summary>
    /// Action to save the active script as a Profile Alignment.
    /// </summary>
    System.Action<string, string>? SaveProfileAlignmentAction { get; set; }

    /// <summary>
    /// Action to sync in-memory points.
    /// </summary>
    System.Action? SyncPointsAction { get; set; }

    /// <summary>
    /// Action to open the Help UI with all commands.
    /// </summary>
    System.Action<IEnumerable<ICommand>>? OpenHelpWindowAction { get; set; }

    /// <summary>
    /// Gets or sets the folder path of the currently active project.
    /// </summary>
    string? ProjectDirectory { get; set; }
}
