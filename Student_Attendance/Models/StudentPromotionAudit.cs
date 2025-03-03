public class StudentPromotionAudit
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int FromAcademicYearId { get; set; }
    public int ToAcademicYearId { get; set; }
    public int FromClassId { get; set; }
    public int ToClassId { get; set; }
    public int FromSemester { get; set; }
    public int ToSemester { get; set; }
    public string PromotedBy { get; set; }
    public DateTime PromotedOn { get; set; }
}
