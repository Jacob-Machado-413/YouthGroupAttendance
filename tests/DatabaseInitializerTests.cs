using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YouthGroupAttendance.Api.Data;

namespace YouthGroupAttendance.Api.Tests;

public class DatabaseInitializerTests
{
    [Fact]
    public void LegacyEnsureCreatedDatabase_IsAdoptedWithoutLosingData()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<YouthGroupContext>().UseSqlite(connection).Options;
        using (var legacy = new YouthGroupContext(options))
        {
            legacy.Database.EnsureCreated();
            legacy.Students.Add(new Models.Student { FullName = "Existing Student", GraduationYear = 2027 });
            legacy.SaveChanges();
        }

        using var db = new YouthGroupContext(options);
        DatabaseInitializer.Initialize(db);
        DatabaseInitializer.Initialize(db);

        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Equal("Existing Student", db.Students.Single().FullName);
    }
}
