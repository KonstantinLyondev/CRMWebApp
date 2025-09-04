using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CRMWebApp.Data
{
    public static class CommentFixer
    {
        public static void Run(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.ExecuteSqlRaw(@"
                BEGIN TRY
                    ALTER TABLE Interactions ALTER COLUMN Comment NVARCHAR(500) NULL;
                    PRINT '✔ Column Comment was altered to allow NULLs.';
                END TRY
                BEGIN CATCH
                    PRINT '⚠ Column Comment could not be altered. Reason: ' + ERROR_MESSAGE();
                END CATCH
            ");
        }
    }
}