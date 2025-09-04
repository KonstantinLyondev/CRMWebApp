using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMWebApp.Migrations
{
    public partial class MakeClientEmailIndexNonUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Drop всички индекси, които включват Email (и unique, и non-unique)
            migrationBuilder.Sql(@"
DECLARE @obj INT = OBJECT_ID(N'dbo.Clients');
IF @obj IS NOT NULL
BEGIN
    DECLARE @col INT = (SELECT column_id FROM sys.columns WHERE object_id = @obj AND name = N'Email');
    IF @col IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(MAX) = N'';
        SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON [dbo].[Clients];'
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = @obj AND ic.column_id = @col;

        IF LEN(@sql) > 0
            EXEC sys.sp_executesql @sql;
    END
END
");

            // 2) Премахни default constraint (ако има) и намали дължината на Email до 256
            migrationBuilder.Sql(@"
DECLARE @df SYSNAME, @dropSql NVARCHAR(MAX);
SELECT @df = d.name
FROM sys.default_constraints d
JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
WHERE d.parent_object_id = OBJECT_ID(N'dbo.Clients') AND c.name = N'Email';

IF @df IS NOT NULL
BEGIN
    SET @dropSql = N'ALTER TABLE dbo.Clients DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
    EXEC sys.sp_executesql @dropSql;
END;

ALTER TABLE dbo.Clients ALTER COLUMN Email NVARCHAR(256) NULL;
");

            // 3) NON-UNIQUE филтриран индекс (допуска дубликати при активни клиенти)
            migrationBuilder.Sql(@"
CREATE INDEX [IX_Clients_UserId_Email_Active]
ON [dbo].[Clients]([UserId], [Email])
WHERE [Email] IS NOT NULL AND [IsDeleted] = 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop на non-unique индекса
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Clients_UserId_Email_Active' AND object_id = OBJECT_ID(N'dbo.Clients'))
    DROP INDEX [IX_Clients_UserId_Email_Active] ON [dbo].[Clients];
");

            // Връщане на Email към NVARCHAR(450)
            migrationBuilder.Sql(@"
DECLARE @df SYSNAME, @dropSql NVARCHAR(MAX);
SELECT @df = d.name
FROM sys.default_constraints d
JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
WHERE d.parent_object_id = OBJECT_ID(N'dbo.Clients') AND c.name = N'Email';

IF @df IS NOT NULL
BEGIN
    SET @dropSql = N'ALTER TABLE dbo.Clients DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
    EXEC sys.sp_executesql @dropSql;
END;

ALTER TABLE dbo.Clients ALTER COLUMN Email NVARCHAR(450) NULL;
");
        }
    }
}
