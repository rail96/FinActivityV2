using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindActivity.Infrastructure.Data.Migrations
{
    public partial class RemoveLatLngAddAddressPlaceId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Lng",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "AddressPlaceId",
                table: "Activities",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressPlaceId",
                table: "Activities");

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Activities",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "Activities",
                type: "float",
                nullable: true);
        }
    }
}
