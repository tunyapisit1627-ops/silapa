using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace silapa.Migrations
{
    /// <inheritdoc />
    public partial class AddContentFieldsToSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutHeading",
                table: "setupsystem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AboutText1",
                table: "setupsystem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AboutText2",
                table: "setupsystem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HeroVideoPath",
                table: "setupsystem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SloganEnglish",
                table: "setupsystem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SloganThai",
                table: "setupsystem",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AboutHeading",
                table: "setupsystem");

            migrationBuilder.DropColumn(
                name: "AboutText1",
                table: "setupsystem");

            migrationBuilder.DropColumn(
                name: "AboutText2",
                table: "setupsystem");

            migrationBuilder.DropColumn(
                name: "HeroVideoPath",
                table: "setupsystem");

            migrationBuilder.DropColumn(
                name: "SloganEnglish",
                table: "setupsystem");

            migrationBuilder.DropColumn(
                name: "SloganThai",
                table: "setupsystem");
        }
    }
}
