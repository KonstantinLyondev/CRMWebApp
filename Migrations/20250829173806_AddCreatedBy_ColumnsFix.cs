using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedBy_ColumnsFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Clients",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Deals",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Interactions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CreatedById",
                table: "Clients",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_CreatedById",
                table: "Deals",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_CreatedById",
                table: "Interactions",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_AspNetUsers_CreatedById",
                table: "Clients",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_AspNetUsers_CreatedById",
                table: "Deals",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Interactions_AspNetUsers_CreatedById",
                table: "Interactions",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Clients_AspNetUsers_CreatedById", "Clients");
            migrationBuilder.DropForeignKey("FK_Deals_AspNetUsers_CreatedById", "Deals");
            migrationBuilder.DropForeignKey("FK_Interactions_AspNetUsers_CreatedById", "Interactions");

            migrationBuilder.DropIndex("IX_Clients_CreatedById", "Clients");
            migrationBuilder.DropIndex("IX_Deals_CreatedById", "Deals");
            migrationBuilder.DropIndex("IX_Interactions_CreatedById", "Interactions");

            migrationBuilder.DropColumn("CreatedById", "Clients");
            migrationBuilder.DropColumn("CreatedById", "Deals");
            migrationBuilder.DropColumn("CreatedById", "Interactions");
        }
    }
}
