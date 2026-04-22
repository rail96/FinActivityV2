using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindActivity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStateToActivities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Activities",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "WA");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Activities");
        }
    }
}
