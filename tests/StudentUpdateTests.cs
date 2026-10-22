using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using YouthGroupAttendance.Api.Data;
using YouthGroupAttendance.Api.DTOs;

namespace YouthGroupAttendance.Api.Tests;

public class StudentUpdateTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly YouthGroupContext _context;

    public StudentUpdateTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        _context = factory.Services.GetRequiredService<YouthGroupContext>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _context.Attendances.RemoveRange(_context.Attendances);
        _context.Students.RemoveRange(_context.Students);
        await _context.SaveChangesAsync();
    }

    private async Task<AttendanceResponse> CheckIn(string name, string grade = "10th", DateTime? date = null)
    {
        var response = await _client.PostAsJsonAsync("/api/attendance", new AttendanceRequest
        {
            FullName = name,
            Grade = grade,
            Date = date
        });
        return (await response.Content.ReadFromJsonAsync<AttendanceResponse>())!;
    }

    [Fact]
    public async Task UpdateStudent_ChangesAllDetailsAndKeepsAttendance()
    {
        var created = await CheckIn("Jon Smith");

        var response = await _client.PutAsJsonAsync($"/api/students/{created.StudentId}", new UpdateStudentRequest
        {
            FullName = "  John Smith  ",
            GraduationYear = 2028,
            Gender = "male",
            School = " Central High "
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<StudentDetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal("John Smith", updated.FullName);
        Assert.Equal(2028, updated.GraduationYear);
        Assert.Equal("Male", updated.Gender);
        Assert.Equal("Central High", updated.School);
        Assert.Single(updated.Attendances);
    }

    [Fact]
    public async Task UpdateStudent_BlankGenderAndSchool_ClearsThem()
    {
        var created = await CheckIn("Clear Me");
        await _client.PutAsJsonAsync($"/api/students/{created.StudentId}", new UpdateStudentRequest
        {
            FullName = "Clear Me", GraduationYear = created.GraduationYear, Gender = "Female", School = "North"
        });

        var response = await _client.PutAsJsonAsync($"/api/students/{created.StudentId}", new UpdateStudentRequest
        {
            FullName = "Clear Me", GraduationYear = created.GraduationYear, Gender = "", School = "  "
        });

        var updated = await response.Content.ReadFromJsonAsync<StudentDetailResponse>();
        Assert.NotNull(updated);
        Assert.Null(updated.Gender);
        Assert.Null(updated.School);
    }

    [Fact]
    public async Task UpdateStudent_NameTakenByAnotherStudent_ReturnsConflict()
    {
        await CheckIn("Taken Name");
        var other = await CheckIn("Other Person");

        var response = await _client.PutAsJsonAsync($"/api/students/{other.StudentId}", new UpdateStudentRequest
        {
            FullName = "taken name", GraduationYear = other.GraduationYear
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudent_ChangingOnlyCapitalizationOfOwnName_IsAllowed()
    {
        var created = await CheckIn("lower case");

        var response = await _client.PutAsJsonAsync($"/api/students/{created.StudentId}", new UpdateStudentRequest
        {
            FullName = "Lower Case", GraduationYear = created.GraduationYear
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("", 2028, null)]
    [InlineData("Valid Name", 1990, null)]
    [InlineData("Valid Name", 2028, "Robot")]
    public async Task UpdateStudent_InvalidInput_ReturnsBadRequest(string name, int gradYear, string? gender)
    {
        var created = await CheckIn("Bad Input");

        var response = await _client.PutAsJsonAsync($"/api/students/{created.StudentId}", new UpdateStudentRequest
        {
            FullName = name, GraduationYear = gradYear, Gender = gender
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudent_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/students/99999", new UpdateStudentRequest
        {
            FullName = "Nobody", GraduationYear = 2028
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Trends_ReportsAveragePerMeetingWithinRange()
    {
        var d1 = new DateTime(2026, 9, 6);
        var d2 = new DateTime(2026, 9, 13);
        var outside = new DateTime(2026, 10, 4);
        await CheckIn("A", date: d1);
        await CheckIn("B", date: d1);
        await CheckIn("C", date: d1);
        await CheckIn("A", date: d2);
        await CheckIn("D", date: outside);

        var trends = await _client.GetFromJsonAsync<AttendanceTrendsResponse>(
            "/api/attendance/trends?from=2026-09-01&to=2026-09-30");

        Assert.NotNull(trends);
        Assert.Equal(4, trends.TotalAttendances);
        Assert.Equal(2, trends.MeetingCount);
        Assert.Equal(3, trends.UniqueStudents);
        Assert.Equal(2.0, trends.AveragePerMeeting);
    }

    [Fact]
    public async Task Trends_EmptyRange_HasZeroAverage()
    {
        var trends = await _client.GetFromJsonAsync<AttendanceTrendsResponse>(
            "/api/attendance/trends?from=2020-01-01&to=2020-01-31");

        Assert.NotNull(trends);
        Assert.Equal(0, trends.MeetingCount);
        Assert.Equal(0, trends.AveragePerMeeting);
    }
}
