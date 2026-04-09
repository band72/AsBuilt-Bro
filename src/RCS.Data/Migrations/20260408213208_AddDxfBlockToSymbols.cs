using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RCS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDxfBlockToSymbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Fittings");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "WWLocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "WWLocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "WWLocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "WWLocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "WWLocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "WWLocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "WWLocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "WWLocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "WWLocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "WWLocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "WaterLocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "WaterLocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "WaterLocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "WaterLocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "WaterLocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "WaterLocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "WaterLocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "WaterLocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "WaterLocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "WaterLocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "WaterHydrants",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "WaterHydrants",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "WaterHydrants",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "WaterHydrants",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "WaterHydrants",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "WaterHydrants",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "WaterHydrants",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "WaterHydrants",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "WaterHydrants",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "WaterHydrants",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Valves",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Valves",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "Valves",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Valves",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "Valves",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Valves",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "Valves",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "Valves",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "Valves",
                newName: "StartEasting");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Structures",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "Structures",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Structures",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "Structures",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Structures",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "Structures",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "Structures",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Structures",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "Structures",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "Structures",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "STLocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "STLocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "STLocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "STLocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "STLocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "STLocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "STLocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "STLocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "STLocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "STLocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "ReclaimedLocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "ReclaimedLocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "ReclaimedLocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "ReclaimedLocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "ReclaimedLocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "ReclaimedLocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "ReclaimedLocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ReclaimedLocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "ReclaimedLocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "ReclaimedLocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "ReclaimedHydrants",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "ReclaimedHydrants",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "ReclaimedHydrants",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "ReclaimedHydrants",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "ReclaimedHydrants",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "ReclaimedHydrants",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "ReclaimedHydrants",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ReclaimedHydrants",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "ReclaimedHydrants",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "ReclaimedHydrants",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Pipes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "Pipes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Pipes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "Pipes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Pipes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "NorthingStart",
                table: "Pipes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "NorthingEnd",
                table: "Pipes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "InvertStart",
                table: "Pipes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "InvertEnd",
                table: "Pipes",
                newName: "StartEasting");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "Pipes",
                newName: "Slope");

            migrationBuilder.RenameColumn(
                name: "GradeElevationAtInvertStart",
                table: "Pipes",
                newName: "RimElevation");

            migrationBuilder.RenameColumn(
                name: "GradeElevationAtInvertEnd",
                table: "Pipes",
                newName: "NutElevation");

            migrationBuilder.RenameColumn(
                name: "EastingStart",
                table: "Pipes",
                newName: "Northing");

            migrationBuilder.RenameColumn(
                name: "EastingEnd",
                table: "Pipes",
                newName: "LowestInvertElevation");

            migrationBuilder.RenameColumn(
                name: "Diameter",
                table: "Pipes",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Pipes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "Pipes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "Pipes",
                newName: "Length");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "PipeCrossings",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "PipeCrossings",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "PipeCrossings",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "PipeCrossings",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "PipeCrossings",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "PipeCrossings",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "FinishedGradeElevation",
                table: "PipeCrossings",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "PipeCrossings",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "PipeCrossings",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "PipeCrossings",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Meters",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "Meters",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Meters",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "Meters",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Meters",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "Meters",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "Meters",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Meters",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "Meters",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "Meters",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "GLocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "GLocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "GLocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "GLocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "GLocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "GLocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "GLocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "GLocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "GLocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "GLocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Fittings",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Fittings",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "Fittings",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Fittings",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "Fittings",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Fittings",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "Fittings",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "Fittings",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Fittings",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "Fittings",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Figures",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "Figures",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Figures",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "Figures",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Figures",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "Figures",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "Figures",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "Figures",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "ELocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "ELocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "ELocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "ELocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "ELocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "ELocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "ELocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ELocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "ELocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "ELocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "ChilledLocateBoxes",
                newName: "ValveType");

            migrationBuilder.RenameColumn(
                name: "TopOutsideWallElev",
                table: "ChilledLocateBoxes",
                newName: "UpstreamInvert");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "ChilledLocateBoxes",
                newName: "UpstreamPointId");

            migrationBuilder.RenameColumn(
                name: "OuterWallThicknessTop",
                table: "ChilledLocateBoxes",
                newName: "UpstreamGrade");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "ChilledLocateBoxes",
                newName: "OpenDirection");

            migrationBuilder.RenameColumn(
                name: "InnerDiameter",
                table: "ChilledLocateBoxes",
                newName: "TurnsToOpen");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                table: "ChilledLocateBoxes",
                newName: "TopElevation");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ChilledLocateBoxes",
                newName: "ManholeType");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "ChilledLocateBoxes",
                newName: "DownstreamPointId");

            migrationBuilder.RenameColumn(
                name: "AdjustedInvert",
                table: "ChilledLocateBoxes",
                newName: "StartNorthing");

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "WWLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "WaterLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "WaterHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DxfBlock",
                table: "SymbolManager",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "Structures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "STLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "ReclaimedLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "ReclaimedHydrants",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Easting",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "Meters",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "GLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "Fittings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DownstreamPointId",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Easting",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "Figures",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Northing",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartNorthing",
                table: "Figures",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "ELocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BlockScale",
                table: "CogoCodes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Cover",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Depth",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DepthToNut",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamGrade",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DownstreamInvert",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndEasting",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndNorthing",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevation",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowestInvertElevation",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RimElevation",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Slope",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartEasting",
                table: "ChilledLocateBoxes",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetSubtypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    SubtypeName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetSubtypes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetSubtypes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "DxfBlock",
                table: "SymbolManager");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "Easting",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "DownstreamPointId",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Easting",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Northing",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "StartNorthing",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "BlockScale",
                table: "CogoCodes");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DepthToNut",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamGrade",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DownstreamInvert",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndEasting",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "EndNorthing",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "GradeElevation",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "LowestInvertElevation",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RimElevation",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "Slope",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "StartEasting",
                table: "ChilledLocateBoxes");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "WWLocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "WWLocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "WWLocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "WWLocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "WWLocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "WWLocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "WWLocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "WWLocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "WWLocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "WWLocateBoxes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "WaterLocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "WaterLocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "WaterLocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "WaterLocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "WaterLocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "WaterLocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "WaterLocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "WaterLocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "WaterLocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "WaterLocateBoxes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "WaterHydrants",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "WaterHydrants",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "WaterHydrants",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "WaterHydrants",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "WaterHydrants",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "WaterHydrants",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "WaterHydrants",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "WaterHydrants",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "WaterHydrants",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "WaterHydrants",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "Valves",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "Valves",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "Valves",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "Valves",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "Valves",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "Valves",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartEasting",
                table: "Valves",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "Valves",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "Valves",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "Structures",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "Structures",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "Structures",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "Structures",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "Structures",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "Structures",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "Structures",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "Structures",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "Structures",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "Structures",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "STLocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "STLocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "STLocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "STLocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "STLocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "STLocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "STLocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "STLocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "STLocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "STLocateBoxes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "ReclaimedLocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "ReclaimedLocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "ReclaimedLocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "ReclaimedLocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "ReclaimedLocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "ReclaimedLocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "ReclaimedLocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "ReclaimedLocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "ReclaimedLocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "ReclaimedLocateBoxes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "ReclaimedHydrants",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "ReclaimedHydrants",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "ReclaimedHydrants",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "ReclaimedHydrants",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "ReclaimedHydrants",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "ReclaimedHydrants",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "ReclaimedHydrants",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "ReclaimedHydrants",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "ReclaimedHydrants",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "ReclaimedHydrants",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "Pipes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "Pipes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "Pipes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "Pipes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "Pipes",
                newName: "NorthingStart");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "Pipes",
                newName: "NorthingEnd");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "Pipes",
                newName: "InvertStart");

            migrationBuilder.RenameColumn(
                name: "StartEasting",
                table: "Pipes",
                newName: "InvertEnd");

            migrationBuilder.RenameColumn(
                name: "Slope",
                table: "Pipes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "RimElevation",
                table: "Pipes",
                newName: "GradeElevationAtInvertStart");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "Pipes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "NutElevation",
                table: "Pipes",
                newName: "GradeElevationAtInvertEnd");

            migrationBuilder.RenameColumn(
                name: "Northing",
                table: "Pipes",
                newName: "EastingStart");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "Pipes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "LowestInvertElevation",
                table: "Pipes",
                newName: "EastingEnd");

            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "Pipes",
                newName: "Diameter");

            migrationBuilder.RenameColumn(
                name: "Length",
                table: "Pipes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "Pipes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "PipeCrossings",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "PipeCrossings",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "PipeCrossings",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "PipeCrossings",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "PipeCrossings",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "PipeCrossings",
                newName: "FinishedGradeElevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "PipeCrossings",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "PipeCrossings",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "PipeCrossings",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "PipeCrossings",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "Meters",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "Meters",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "Meters",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "Meters",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "Meters",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "Meters",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "Meters",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "Meters",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "Meters",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "Meters",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "GLocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "GLocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "GLocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "GLocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "GLocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "GLocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "GLocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "GLocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "GLocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "GLocateBoxes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "Fittings",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "Fittings",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "Fittings",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "Fittings",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "Fittings",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "Fittings",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "Fittings",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "Fittings",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "Fittings",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "Fittings",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "Figures",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "Figures",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "Figures",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "Figures",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "Figures",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "Figures",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "Figures",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "Figures",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "ELocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "ELocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "ELocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "ELocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "ELocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "ELocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "ELocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "ELocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "ELocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "ELocateBoxes",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "ValveType",
                table: "ChilledLocateBoxes",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "UpstreamPointId",
                table: "ChilledLocateBoxes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "UpstreamInvert",
                table: "ChilledLocateBoxes",
                newName: "TopOutsideWallElev");

            migrationBuilder.RenameColumn(
                name: "UpstreamGrade",
                table: "ChilledLocateBoxes",
                newName: "OuterWallThicknessTop");

            migrationBuilder.RenameColumn(
                name: "TurnsToOpen",
                table: "ChilledLocateBoxes",
                newName: "InnerDiameter");

            migrationBuilder.RenameColumn(
                name: "TopElevation",
                table: "ChilledLocateBoxes",
                newName: "Elevation");

            migrationBuilder.RenameColumn(
                name: "StartNorthing",
                table: "ChilledLocateBoxes",
                newName: "AdjustedInvert");

            migrationBuilder.RenameColumn(
                name: "OpenDirection",
                table: "ChilledLocateBoxes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ManholeType",
                table: "ChilledLocateBoxes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DownstreamPointId",
                table: "ChilledLocateBoxes",
                newName: "Confidence");

            migrationBuilder.AddColumn<string>(
                name: "Confidence",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Confidence",
                table: "Fittings",
                type: "TEXT",
                nullable: true);
        }
    }
}
