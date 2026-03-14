using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RCS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPipeCrossingProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CrossingNumber",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FinishedGradeElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowerCover",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowerPipeSize",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LowerPipeTopElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowerPipeType",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Separation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UpperCover",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UpperPipeBottomElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpperPipeSize",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UpperPipeTopElevation",
                table: "PipeCrossings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpperPipeType",
                table: "PipeCrossings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrossingNumber",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "FinishedGradeElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "LowerCover",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "LowerPipeSize",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "LowerPipeTopElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "LowerPipeType",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "Separation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "UpperCover",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "UpperPipeBottomElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "UpperPipeSize",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "UpperPipeTopElevation",
                table: "PipeCrossings");

            migrationBuilder.DropColumn(
                name: "UpperPipeType",
                table: "PipeCrossings");
        }
    }
}
