using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeactivatedAtAndReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeactivatedReason",
                table: "AspNetUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}