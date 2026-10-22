using Microsoft.EntityFrameworkCore;

namespace YouthGroupAttendance.Api.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations. Databases created by the old EnsureCreated()
    /// startup have the baseline schema but no migration history, so the baseline
    /// (first) migration is recorded as applied before migrating.
    /// </summary>
    public static void Initialize(YouthGroupContext db)
    {
        if (!db.Database.GetAppliedMigrations().Any() && TableExists(db, "Students"))
        {
            var baseline = db.Database.GetMigrations().First();
            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            db.Database.ExecuteSqlRaw(
                """INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ({0}, {1});""",
                baseline, typeof(DbContext).Assembly.GetName().Version!.ToString(3));
            Console.WriteLine($"Existing database adopted; baseline migration {baseline} marked as applied.");
        }

        db.Database.Migrate();
    }

    private static bool TableExists(YouthGroupContext db, string table) =>
        db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {0}", table)
            .Single() > 0;
}
