namespace YouthGroupAttendance.Api.Helpers;

/// <summary>
/// Converts between grade levels and graduation years.
/// The academic year advances at the end (Sunday) of the first full week of June.
/// Keep in sync with frontend/Helpers/GradeHelper.cs.
/// </summary>
public static class GradeCalculator
{
    public const string Graduated = "Graduated";

    public static DateTime GetCutoverDate(int year)
    {
        var june1 = new DateTime(year, 6, 1);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)june1.DayOfWeek + 7) % 7;
        var firstMonday = june1.AddDays(daysUntilMonday);
        return firstMonday.AddDays(6);
    }

    /// <summary>
    /// The calendar year in which the current academic year's seniors graduate.
    /// </summary>
    public static int GetAcademicYear(DateTime now) =>
        now >= GetCutoverDate(now.Year) ? now.Year + 1 : now.Year;

    public static int GetGradeLevel(int graduationYear, DateTime now) =>
        12 - (graduationYear - GetAcademicYear(now));

    public static bool IsGraduated(int graduationYear, DateTime now) =>
        GetGradeLevel(graduationYear, now) > 12;

    public static string GraduationYearToGrade(int graduationYear, DateTime now)
    {
        var gradeLevel = GetGradeLevel(graduationYear, now);
        return gradeLevel > 12 ? Graduated : $"{gradeLevel}th";
    }

    public static int GradeToGraduationYear(string grade, DateTime now)
    {
        var digits = new string(grade.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var gradeLevel) && gradeLevel >= 1 && gradeLevel <= 12)
            return GetAcademicYear(now) + (12 - gradeLevel);
        return now.Year + 1;
    }
}
