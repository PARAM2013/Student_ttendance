using Microsoft.AspNetCore.Mvc.Rendering;

public class ArchivedAttendanceViewModel
{
    public string StudentName { get; set; }
    public string EnrollmentNo { get; set; }
    public string SubjectName { get; set; }
    public DateTime Date { get; set; }
    public bool IsPresent { get; set; }
    public string AcademicYear { get; set; }
}

public class AttendanceHistoryViewModel
{
    public List<ArchivedAttendanceViewModel> Records { get; set; }
    public SelectList AcademicYears { get; set; }
    public SelectList Students { get; set; }
    public SelectList Classes { get; set; }
    public SelectList Teachers { get; set; }
    public SelectList Subjects { get; set; }
    public SelectList Divisions { get; set; }

    public int? SelectedStudentId { get; set; }
    public int? SelectedAcademicYearId { get; set; }
    public int? SelectedClassId { get; set; }
    public int? SelectedTeacherId { get; set; }
    public int? SelectedSubjectId { get; set; }
    public int? SelectedDivisionId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
