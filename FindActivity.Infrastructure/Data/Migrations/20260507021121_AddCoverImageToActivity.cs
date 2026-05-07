using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindActivity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverImageToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImagePath",
                table: "Activities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImagePath",
                table: "Activities");
        }
    }
}
