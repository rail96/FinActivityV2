using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindActivity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBannedUntilToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BannedUntilUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannedUntilUtc",
                table: "AspNetUsers");
        }
    }
}
