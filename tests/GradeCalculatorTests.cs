using YouthGroupAttendance.Api.Helpers;

namespace YouthGroupAttendance.Api.Tests;

public class GradeCalculatorTests
{
    [Theory]
    [InlineData("2026-06-06", 2026)]
    [InlineData("2026-06-07", 2027)] // Sunday ending the first full week of June
    public void AcademicYear_AdvancesAtJuneCutover(string now, int expected) =>
        Assert.Equal(expected, GradeCalculator.GetAcademicYear(DateTime.Parse(now)));

    [Fact]
    public void RisingSenior_EnteredInSummer_IsSeniorInFall()
    {
        // Regression: a September cutover stored summer "12th" entries as the class of 2026.
        var gradYear = GradeCalculator.GradeToGraduationYear("12th", new DateTime(2026, 7, 15));

        Assert.Equal(2027, gradYear);
        Assert.Equal("12th", GradeCalculator.GraduationYearToGrade(gradYear, new DateTime(2026, 10, 22)));
        Assert.True(GradeCalculator.IsGraduated(2027, new DateTime(2027, 6, 13)));
    }
}
