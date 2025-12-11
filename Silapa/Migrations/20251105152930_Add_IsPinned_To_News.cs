using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace silapa.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsPinned_To_News : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "news",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "news");
        }
    }
}
