namespace YouthGroupAttendance.Frontend.Helpers;

public static class GradeHelper
{
    /// <summary>
    /// Returns the cutover date: the end (Sunday) of the first full week of June.
    /// Before this date, the academic year is the current year.
    /// On or after this date, the academic year advances by one.
    /// </summary>
    public static DateTime GetGradeCutoverDate(int year)
    {
        var june1 = new DateTime(year, 6, 1);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)june1.DayOfWeek + 7) % 7;
        var firstMonday = june1.AddDays(daysUntilMonday);
        return firstMonday.AddDays(6);
    }

    public const string Graduated = "Graduated";

    /// <summary>The calendar year in which this academic year's seniors graduate.</summary>
    public static int GetAcademicYear()
    {
        var now = DateTime.UtcNow;
        return now >= GetGradeCutoverDate(now.Year) ? now.Year + 1 : now.Year;
    }

    /// <summary>Start of the current academic year (the most recent June cutover).</summary>
    public static DateTime GetAcademicYearStart() => GetGradeCutoverDate(GetAcademicYear() - 1);

    public static int GetGradeLevel(int graduationYear) => 12 - (graduationYear - GetAcademicYear());

    public static int GradeLevelToGraduationYear(int gradeLevel) => GetAcademicYear() + (12 - gradeLevel);

    public static bool IsGraduated(int graduationYear) => GetGradeLevel(graduationYear) > 12;

    /// <summary>
    /// Converts a GraduationYear to the current grade string (e.g. "10th"),
    /// or "Graduated" once the student is past 12th grade.
    /// Academic year advances at the end of the first full week of June.
    /// </summary>
    public static string GraduationYearToGrade(int graduationYear)
    {
        var gradeLevel = GetGradeLevel(graduationYear);
        return gradeLevel > 12 ? Graduated : $"{gradeLevel}th";
    }

    /// <summary>
    /// Returns the list of grade options for dropdowns.
    /// </summary>
    public static string[] GradeOptions => new[]
    {
        "6th", "7th", "8th", "9th", "10th", "11th", "12th"
    };
}
