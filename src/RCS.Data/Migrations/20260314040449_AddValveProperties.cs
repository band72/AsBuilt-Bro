using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RCS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddValveProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "NutElevation",
                table: "Valves",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenDirection",
                table: "Valves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TurnsToOpen",
                table: "Valves",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NutElevation",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "OpenDirection",
                table: "Valves");

            migrationBuilder.DropColumn(
                name: "TurnsToOpen",
                table: "Valves");
        }
    }
}
