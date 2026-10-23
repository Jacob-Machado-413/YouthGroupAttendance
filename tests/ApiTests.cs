using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using YouthGroupAttendance.Api.Data;
using YouthGroupAttendance.Api.DTOs;

namespace YouthGroupAttendance.Api.Tests;

public class ApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        var context = factory.Services.GetRequiredService<YouthGroupContext>();
        context.Attendances.RemoveRange(context.Attendances);
        context.Students.RemoveRange(context.Students);
        await context.SaveChangesAsync();
    }

    private async Task<AttendanceResponse> CheckIn(string name, DateTime? date = null, string? eventType = null)
    {
        var response = await _client.PostAsJsonAsync("/api/attendance",
            new AttendanceRequest { FullName = name, Grade = "10th", Date = date, EventType = eventType });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AttendanceResponse>())!;
    }

    [Fact]
    public async Task CheckIn_ReusesExistingStudent_CaseInsensitive()
    {
        var first = await CheckIn("Jane Doe", eventType: "socialgamenight");
        var second = await CheckIn("jane doe", eventType: "Bogus");

        Assert.False(second.IsNewStudent);
        Assert.Equal(first.StudentId, second.StudentId);
        Assert.Equal("SocialGameNight", first.EventType);
        Assert.Equal("RegularYouthGroup", second.EventType);
    }

    [Fact]
    public async Task StudentsByDate_ReturnsEachStudentOnceForThatDate()
    {
        var today = DateTime.UtcNow.Date;
        await CheckIn("Today", today);
        await CheckIn("Today", today);
        await CheckIn("Yesterday", today.AddDays(-1));

        var students = await _client.GetFromJsonAsync<List<StudentResponse>>(
            $"/api/attendance/students-by-date?date={today:yyyy-MM-dd}");

        Assert.Equal("Today", Assert.Single(students!).FullName);
    }

    [Fact]
    public async Task UpdateStudent_NormalizesInput_AndRejectsDuplicateName()
    {
        await CheckIn("Taken Name");
        var created = await CheckIn("Jon Smith");
        var url = $"/api/students/{created.StudentId}";

        var response = await _client.PutAsJsonAsync(url, new UpdateStudentRequest
        {
            FullName = "  John Smith  ", GraduationYear = 2028, Gender = "male", School = " "
        });
        var updated = (await response.Content.ReadFromJsonAsync<StudentDetailResponse>())!;
        Assert.Equal("John Smith", updated.FullName);
        Assert.Equal("Male", updated.Gender);
        Assert.Null(updated.School);

        var conflict = await _client.PutAsJsonAsync(url,
            new UpdateStudentRequest { FullName = "taken name", GraduationYear = 2028 });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task MergeStudents_MovesAttendancesAndDeletesSource()
    {
        var dest = await CheckIn("Jane Doe");
        var source = await CheckIn("Jane M. Doe");

        var response = await _client.PostAsJsonAsync("/api/students/merge",
            new MergeStudentsRequest { SourceStudentId = source.StudentId, DestinationStudentId = dest.StudentId });
        var result = await response.Content.ReadFromJsonAsync<MergeStudentsResponse>();

        Assert.Equal(2, result!.MergedStudent.Attendances.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/students/{source.StudentId}")).StatusCode);
    }

    [Fact]
    public async Task Trends_ReportsAveragePerMeetingWithinRange()
    {
        foreach (var (name, day) in new[] { ("A", 6), ("B", 6), ("C", 6), ("A", 13), ("D", 40) })
            await CheckIn(name, new DateTime(2026, 9, 1).AddDays(day - 1));

        var trends = await _client.GetFromJsonAsync<AttendanceTrendsResponse>(
            "/api/attendance/trends?from=2026-09-01&to=2026-09-30");

        Assert.Equal(2, trends!.MeetingCount);
        Assert.Equal(3, trends.UniqueStudents);
        Assert.Equal(2.0, trends.AveragePerMeeting);
    }
}
