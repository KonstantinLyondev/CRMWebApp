using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Migrations
{
    public partial class AddAuditLogNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Добавя колоната Notes само ако липсва
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.AuditLogs') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AuditLogs', 'Notes') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD Notes NVARCHAR(512) NULL;
    END
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Маха колоната Notes само ако съществува
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.AuditLogs') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AuditLogs', 'Notes') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs DROP COLUMN Notes;
    END
END
");
        }
    }
}
