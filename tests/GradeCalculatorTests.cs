using YouthGroupAttendance.Api.Helpers;

namespace YouthGroupAttendance.Api.Tests;

public class GradeCalculatorTests
{
    [Fact]
    public void Cutover_2026_IsSundayOfFirstFullWeekOfJune()
    {
        // June 1, 2026 is a Monday, so the first full week ends Sunday June 7.
        Assert.Equal(new DateTime(2026, 6, 7), GradeCalculator.GetCutoverDate(2026));
    }

    [Theory]
    [InlineData("2026-06-06", 2026)]
    [InlineData("2026-06-07", 2027)]
    [InlineData("2026-07-15", 2027)]
    [InlineData("2026-10-22", 2027)]
    [InlineData("2027-03-01", 2027)]
    public void AcademicYear_AdvancesAtJuneCutover(string now, int expected)
    {
        Assert.Equal(expected, GradeCalculator.GetAcademicYear(DateTime.Parse(now)));
    }

    [Fact]
    public void RisingSenior_EnteredInSummer_IsSeniorInFall()
    {
        // Regression: a September cutover stored summer "12th" entries as the class of 2026.
        var gradYear = GradeCalculator.GradeToGraduationYear("12th", new DateTime(2026, 7, 15));

        Assert.Equal(2027, gradYear);
        Assert.Equal("12th", GradeCalculator.GraduationYearToGrade(gradYear, new DateTime(2026, 10, 22)));
    }

    [Theory]
    [InlineData(2026, "Graduated")]
    [InlineData(2027, "12th")]
    [InlineData(2030, "9th")]
    [InlineData(2033, "6th")]
    public void GraduationYearToGrade_InFall2026(int gradYear, string expected)
    {
        Assert.Equal(expected, GradeCalculator.GraduationYearToGrade(gradYear, new DateTime(2026, 10, 22)));
    }

    [Fact]
    public void Senior_BecomesGraduated_AfterJuneCutover()
    {
        Assert.False(GradeCalculator.IsGraduated(2027, new DateTime(2027, 6, 12)));
        Assert.True(GradeCalculator.IsGraduated(2027, new DateTime(2027, 6, 13)));
    }
}
