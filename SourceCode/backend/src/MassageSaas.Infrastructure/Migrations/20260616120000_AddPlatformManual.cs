using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CsManualNormal",
                table: "PlatformSettings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CsManualA11y",
                table: "PlatformSettings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BsManualNormal",
                table: "PlatformSettings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BsManualA11y",
                table: "PlatformSettings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CsManualNormal", table: "PlatformSettings");
            migrationBuilder.DropColumn(name: "CsManualA11y", table: "PlatformSettings");
            migrationBuilder.DropColumn(name: "BsManualNormal", table: "PlatformSettings");
            migrationBuilder.DropColumn(name: "BsManualA11y", table: "PlatformSettings");
        }
    }
}
