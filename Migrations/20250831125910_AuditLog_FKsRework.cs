using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditLog_FKsRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_AuditLogs_AspNetUsers_UserId",
                    table: "AuditLogs");
            }
            catch { }

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InteractionId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE L SET L.ClientId = L.EntityId
                FROM AuditLogs L
                WHERE L.EntityType = 'Client';

                UPDATE L SET L.DealId = L.EntityId
                FROM AuditLogs L
                WHERE L.EntityType = 'Deal';

                UPDATE L SET L.InteractionId = L.EntityId
                FROM AuditLogs L
                WHERE L.EntityType = 'Interaction';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClientId",
                table: "AuditLogs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DealId",
                table: "AuditLogs",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_InteractionId",
                table: "AuditLogs",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Clients_ClientId",
                table: "AuditLogs",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Deals_DealId",
                table: "AuditLogs",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Interactions_InteractionId",
                table: "AuditLogs",
                column: "InteractionId",
                principalTable: "Interactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
UPDATE L
SET L.UserId = NULL
FROM AuditLogs L
WHERE L.UserId IS NOT NULL
  AND (
        LEN(LTRIM(RTRIM(L.UserId))) = 0
        OR NOT EXISTS (SELECT 1 FROM AspNetUsers U WHERE U.Id = L.UserId)
      );
");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditLogs_SingleTarget",
                table: "AuditLogs",
                sql: "(CASE WHEN [ClientId] IS NOT NULL THEN 1 ELSE 0 END +" +
                     " CASE WHEN [DealId] IS NOT NULL THEN 1 ELSE 0 END +" +
                     " CASE WHEN [InteractionId] IS NOT NULL THEN 1 ELSE 0 END) = 1");

            migrationBuilder.Sql(@"
              IF EXISTS (
              SELECT 1 FROM sys.indexes 
              WHERE name = 'IX_AuditLogs_EntityType_EntityId' 
              AND object_id = OBJECT_ID('dbo.AuditLogs')
              )
              DROP INDEX [IX_AuditLogs_EntityType_EntityId] ON [dbo].[AuditLogs];
              ");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditLogs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
  
            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE L
                SET L.EntityType = 'Client',
                    L.EntityId = L.ClientId
                FROM AuditLogs L
                WHERE L.ClientId IS NOT NULL;

                UPDATE L
                SET L.EntityType = 'Deal',
                    L.EntityId = L.DealId
                FROM AuditLogs L
                WHERE L.DealId IS NOT NULL;

                UPDATE L
                SET L.EntityType = 'Interaction',
                    L.EntityId = L.InteractionId
                FROM AuditLogs L
                WHERE L.InteractionId IS NOT NULL;
            ");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditLogs_SingleTarget",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Clients_ClientId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Deals_DealId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Interactions_InteractionId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ClientId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_DealId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_InteractionId",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DealId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "InteractionId",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}