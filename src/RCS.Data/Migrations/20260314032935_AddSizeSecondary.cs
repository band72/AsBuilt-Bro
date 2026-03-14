using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RCS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeSecondary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChilledLocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChilledLocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CogoCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocalCode = table.Column<string>(type: "TEXT", nullable: false),
                    SystemCode = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Block = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CogoCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ELocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ELocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Figures",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Layer = table.Column<string>(type: "TEXT", nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    DescriptionText = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptContent = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Figures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fittings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fittings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "TEXT", nullable: false),
                    SettingValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "GLocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GLocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: false),
                    Discipline = table.Column<string>(type: "TEXT", nullable: false),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<string>(type: "TEXT", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartSpecifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartNumber = table.Column<string>(type: "TEXT", nullable: false),
                    OuterDiameter = table.Column<double>(type: "REAL", nullable: true),
                    NominalDiameter = table.Column<double>(type: "REAL", nullable: true),
                    PipeThickness = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    Deflection = table.Column<double>(type: "REAL", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartSpecifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipeCrossings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipeCrossings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pipes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    NorthingStart = table.Column<double>(type: "REAL", nullable: true),
                    EastingStart = table.Column<double>(type: "REAL", nullable: true),
                    NorthingEnd = table.Column<double>(type: "REAL", nullable: true),
                    EastingEnd = table.Column<double>(type: "REAL", nullable: true),
                    InvertStart = table.Column<double>(type: "REAL", nullable: true),
                    InvertEnd = table.Column<double>(type: "REAL", nullable: true),
                    Diameter = table.Column<double>(type: "REAL", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "ReclaimedHydrants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclaimedHydrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReclaimedLocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclaimedLocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "STLocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STLocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Structures",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Structures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyPoints",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    PointNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Northing = table.Column<double>(type: "REAL", nullable: false),
                    Easting = table.Column<double>(type: "REAL", nullable: false),
                    Elevation = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymbolManager",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientCode = table.Column<string>(type: "TEXT", nullable: true),
                    SystemCode = table.Column<string>(type: "TEXT", nullable: true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolManager", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: false),
                    RuleDescription = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Valves",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaterHydrants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterHydrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaterLocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterLocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WWLocateBoxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceSheetRowIndex = table.Column<string>(type: "TEXT", nullable: true),
                    PartKey = table.Column<string>(type: "TEXT", nullable: true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureType = table.Column<string>(type: "TEXT", nullable: true),
                    Subtype = table.Column<string>(type: "TEXT", nullable: true),
                    FacilityOwner = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    SizeSecondary = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    PipeClass = table.Column<string>(type: "TEXT", nullable: true),
                    LiningManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    LiningMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerPartNo = table.Column<string>(type: "TEXT", nullable: true),
                    YearManufactured = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Warning = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TopOutsideWallElev = table.Column<double>(type: "REAL", nullable: true),
                    OuterWallThicknessTop = table.Column<double>(type: "REAL", nullable: true),
                    InnerDiameter = table.Column<double>(type: "REAL", nullable: true),
                    AdjustedInvert = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Northing = table.Column<double>(type: "REAL", nullable: true),
                    Easting = table.Column<double>(type: "REAL", nullable: true),
                    Elevation = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WWLocateBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FigureVertices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FigureId = table.Column<string>(type: "TEXT", nullable: false),
                    PointId = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Bulge = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FigureVertices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FigureVertices_Figures_FigureId",
                        column: x => x.FigureId,
                        principalTable: "Figures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FigureVertices_SurveyPoints_PointId",
                        column: x => x.PointId,
                        principalTable: "SurveyPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChilledLocateBoxes_ProjectId",
                table: "ChilledLocateBoxes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ELocateBoxes_ProjectId",
                table: "ELocateBoxes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Figures_Layer",
                table: "Figures",
                column: "Layer");

            migrationBuilder.CreateIndex(
                name: "IX_Figures_ProjectId",
                table: "Figures",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FigureVertices_FigureId",
                table: "FigureVertices",
                column: "FigureId");

            migrationBuilder.CreateIndex(
                name: "IX_FigureVertices_PointId",
                table: "FigureVertices",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_Fittings_ProjectId",
                table: "Fittings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GLocateBoxes_ProjectId",
                table: "GLocateBoxes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_ProjectId",
                table: "Meters",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PipeCrossings_ProjectId",
                table: "PipeCrossings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Pipes_ProjectId",
                table: "Pipes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectNumber",
                table: "Projects",
                column: "ProjectNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ReclaimedHydrants_ProjectId",
                table: "ReclaimedHydrants",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclaimedLocateBoxes_ProjectId",
                table: "ReclaimedLocateBoxes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_STLocateBoxes_ProjectId",
                table: "STLocateBoxes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Structures_ProjectId",
                table: "Structures",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPoints_ProjectId",
                table: "SurveyPoints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Valves_ProjectId",
                table: "Valves",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterHydrants_ProjectId",
                table: "WaterHydrants",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterLocateBoxes_ProjectId",
                table: "WaterLocateBoxes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WWLocateBoxes_ProjectId",
                table: "WWLocateBoxes",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChilledLocateBoxes");

            migrationBuilder.DropTable(
                name: "CogoCodes");

            migrationBuilder.DropTable(
                name: "ELocateBoxes");

            migrationBuilder.DropTable(
                name: "FigureVertices");

            migrationBuilder.DropTable(
                name: "Fittings");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "GLocateBoxes");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Meters");

            migrationBuilder.DropTable(
                name: "PartSpecifications");

            migrationBuilder.DropTable(
                name: "PipeCrossings");

            migrationBuilder.DropTable(
                name: "Pipes");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "ReclaimedHydrants");

            migrationBuilder.DropTable(
                name: "ReclaimedLocateBoxes");

            migrationBuilder.DropTable(
                name: "STLocateBoxes");

            migrationBuilder.DropTable(
                name: "Structures");

            migrationBuilder.DropTable(
                name: "SymbolManager");

            migrationBuilder.DropTable(
                name: "ValidationRules");

            migrationBuilder.DropTable(
                name: "Valves");

            migrationBuilder.DropTable(
                name: "WaterHydrants");

            migrationBuilder.DropTable(
                name: "WaterLocateBoxes");

            migrationBuilder.DropTable(
                name: "WWLocateBoxes");

            migrationBuilder.DropTable(
                name: "Figures");

            migrationBuilder.DropTable(
                name: "SurveyPoints");
        }
    }
}
