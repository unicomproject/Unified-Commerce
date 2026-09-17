using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.IntegrationTests.TestSupport;

internal static class MigrationTestSql
{
    // Reduced-schema tests exercise one migration, not intervening catalog/seed migrations.
    public static string ForMigration(EPosDbContext db, string migrationId)
    {
        var migrations = db.GetService<IMigrationsAssembly>();
        var migration = migrations.CreateMigration(
            migrations.Migrations[migrationId], db.Database.ProviderName!);
        return string.Join("\n", db.GetService<IMigrationsSqlGenerator>()
            .Generate(migration.UpOperations, migration.TargetModel)
            .Select(command => command.CommandText));
    }
}
