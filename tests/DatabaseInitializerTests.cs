using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YouthGroupAttendance.Api.Data;

namespace YouthGroupAttendance.Api.Tests;

public class DatabaseInitializerTests : IDisposable
{
    private readonly string _dbPath = $"InitDb_{Guid.NewGuid():N}.db";

    private YouthGroupContext CreateContext() =>
        new(new DbContextOptionsBuilder<YouthGroupContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            if (File.Exists(_dbPath + suffix))
                File.Delete(_dbPath + suffix);
    }

    [Fact]
    public void NewDatabase_IsCreatedByMigrations()
    {
        using var db = CreateContext();

        DatabaseInitializer.Initialize(db);

        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Equal(db.Database.GetMigrations(), db.Database.GetAppliedMigrations());
    }

    [Fact]
    public void LegacyEnsureCreatedDatabase_IsAdoptedWithoutLosingData()
    {
        using (var legacy = CreateContext())
        {
            legacy.Database.EnsureCreated();
            legacy.Students.Add(new Models.Student { FullName = "Existing Student", GraduationYear = 2027 });
            legacy.SaveChanges();
        }

        using var db = CreateContext();
        DatabaseInitializer.Initialize(db);

        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Equal("Existing Student", db.Students.Single().FullName);
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using var db = CreateContext();

        DatabaseInitializer.Initialize(db);
        DatabaseInitializer.Initialize(db);

        Assert.Equal(db.Database.GetMigrations(), db.Database.GetAppliedMigrations());
    }
}
