using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RCS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalReportingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "WWLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "WWLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "WWLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "WWLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "WWLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "WWLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "WaterLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "WaterLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "WaterLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "WaterLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "WaterLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "WaterLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "WaterHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "WaterHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "WaterHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "WaterHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "WaterHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "WaterHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "Structures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "Structures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "Structures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "Structures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "Structures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "Structures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "STLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "STLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "STLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "STLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "STLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "STLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "ReclaimedLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "ReclaimedLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "ReclaimedLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "ReclaimedLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "ReclaimedLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "ReclaimedLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "ReclaimedHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "ReclaimedHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "ReclaimedHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "ReclaimedHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "ReclaimedHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "ReclaimedHydrants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsBuiltDate",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityNumber",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapitalProjectNumber",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataSource",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hyperlink",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "Pipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "Pipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "Pipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevationAtInvertEnd",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GradeElevationAtInvertStart",
                table: "Pipes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "Pipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "Pipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "Pipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "Meters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "Meters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "Meters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "Meters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "Meters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "Meters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "GLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "GLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "GLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "GLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "GLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "GLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "Fittings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "Fittings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "Fittings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "Fittings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "Fittings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "Fittings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "Figures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "ELocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "ELocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "ELocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "ELocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "ELocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "ELocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropType",
                table: "ChilledLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeManufacturer",
                table: "ChilledLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExteriorJointTapeType",
                table: "ChilledLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertElevationsWithDirections",
                table: "ChilledLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipeRole",
                table: "ChilledLocateBoxes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidBarcode",
                table: "ChilledLocateBoxes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropType",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "WWLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "WaterLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "WaterHydrants");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "Structures");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "STLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "ReclaimedLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "ReclaimedHydrants");

            migrationBuilder.DropColumn(
                name: "AsBuiltDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AvailabilityNumber",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CapitalProjectNumber",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DataSource",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Hyperlink",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "GradeElevationAtInvertEnd",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "GradeElevationAtInvertStart",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "Pipes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "GLocateBoxes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "Fittings");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "Figures");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "ELocateBoxes");

            migrationBuilder.DropColumn(
                name: "DropType",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeManufacturer",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "ExteriorJointTapeType",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "InvertElevationsWithDirections",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "PipeRole",
                table: "ChilledLocateBoxes");

            migrationBuilder.DropColumn(
                name: "RfidBarcode",
                table: "ChilledLocateBoxes");
        }
    }
}
