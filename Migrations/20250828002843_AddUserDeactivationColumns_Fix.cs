using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeactivationColumns_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDeactivated", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "DeactivatedAt", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "DeactivatedReason", table: "AspNetUsers");
        }
    }
}
