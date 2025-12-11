using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace silapa.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingIDToRegisterDirector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SettingID",
                table: "registerdirector",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettingID",
                table: "registerdirector");
        }
    }
}
