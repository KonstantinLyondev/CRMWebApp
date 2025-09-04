using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToInteraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Interactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Interactions");
        }
    }
}
