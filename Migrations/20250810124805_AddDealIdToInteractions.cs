using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDealIdToInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DealId",
                table: "Interactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_DealId",
                table: "Interactions",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interactions_Deals_DealId",
                table: "Interactions",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interactions_Deals_DealId",
                table: "Interactions");

            migrationBuilder.DropIndex(
                name: "IX_Interactions_DealId",
                table: "Interactions");

            migrationBuilder.DropColumn(
                name: "DealId",
                table: "Interactions");
        }
    }
}
