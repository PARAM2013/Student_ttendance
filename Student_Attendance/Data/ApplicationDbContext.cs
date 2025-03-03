using Microsoft.EntityFrameworkCore;
using Student_Attendance.Models;

namespace Student_Attendance.Data
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<AcademicYear> AcademicYears { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Division> Divisions { get; set; }
    public DbSet<StudentSubject> StudentSubjects { get; set; }
    public DbSet<TeacherSubject> TeacherSubjects { get; set; }
    public DbSet<Institute> Institutes { get; set; }
    public DbSet<AttendanceAudit> AttendanceAudits { get; set; }
    public DbSet<StudentAttendanceArchive> StudentAttendanceArchives { get; set; }
    public DbSet<StudentEnrollmentHistory> StudentEnrollmentHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Configure Subject relationships
      modelBuilder.Entity<Subject>()
          .HasMany(s => s.StudentSubjects)
          .WithOne(ss => ss.Subject)
          .HasForeignKey(ss => ss.SubjectId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Subject>()
          .HasMany(s => s.AttendanceRecords)
          .WithOne(ar => ar.Subject)
          .HasForeignKey(ar => ar.SubjectId)
          .OnDelete(DeleteBehavior.NoAction);

      // Configure Course relationships
      modelBuilder.Entity<Course>()
          .HasMany(c => c.Subjects)
          .WithOne(s => s.Course)
          .HasForeignKey(s => s.CourseId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Course>()
          .HasMany(c => c.Students)
          .WithOne(s => s.Course)
          .HasForeignKey(s => s.CourseId)
          .OnDelete(DeleteBehavior.NoAction);

      // Configure Student relationships
      modelBuilder.Entity<Student>()
          .HasMany(s => s.StudentSubjects)
          .WithOne(ss => ss.Student)
          .HasForeignKey(ss => ss.StudentId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<AttendanceRecord>()
         .HasOne(ar => ar.Student)
         .WithMany()
         .HasForeignKey(ar => ar.StudentId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<AttendanceRecord>()
          .HasOne(ar => ar.Subject)
          .WithMany()
          .HasForeignKey(ar => ar.SubjectId)
           .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Student>()
        .HasOne(s => s.AcademicYear)
       .WithMany()
       .HasForeignKey(s => s.AcademicYearId)
       .OnDelete(DeleteBehavior.NoAction);
      modelBuilder.Entity<Student>()
         .HasOne(s => s.Course)
        .WithMany()
        .HasForeignKey(s => s.CourseId)
        .OnDelete(DeleteBehavior.NoAction);
      modelBuilder.Entity<Student>()
         .HasOne(s => s.Division)
        .WithMany()
        .HasForeignKey(s => s.DivisionId)
        .OnDelete(DeleteBehavior.NoAction);
      modelBuilder.Entity<Student>()
        .HasOne(s => s.Class)
        .WithMany()
        .HasForeignKey(s => s.ClassId)
        .OnDelete(DeleteBehavior.NoAction);
      modelBuilder.Entity<Specialization>()
         .HasOne(s => s.Course)
        .WithMany()
        .HasForeignKey(s => s.CourseId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Subject>()
        .HasOne(s => s.Course)
       .WithMany()
       .HasForeignKey(s => s.CourseId)
       .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Class>()
       .HasOne(c => c.Course)
       .WithMany()
        .HasForeignKey(c => c.CourseId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Class>()
     .HasOne(c => c.AcademicYear)
     .WithMany()
      .HasForeignKey(c => c.AcademicYearId)
       .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Division>()
       .HasOne(d => d.Class)
       .WithMany()
       .HasForeignKey(d => d.ClassId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentSubject>()
        .HasOne(ss => ss.Student)
        .WithMany()
        .HasForeignKey(ss => ss.StudentId)
         .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentSubject>()
         .HasOne(ss => ss.Subject)
          .WithMany()
          .HasForeignKey(ss => ss.SubjectId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<TeacherSubject>()
         .HasOne(ts => ts.User)
          .WithMany()
          .HasForeignKey(ts => ts.UserId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<TeacherSubject>()
         .HasOne(ts => ts.Subject)
          .WithMany()
          .HasForeignKey(ts => ts.SubjectId)
         .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<TeacherSubject>()
        .HasOne(ts => ts.AcademicYear)
        .WithMany()
        .HasForeignKey(ts => ts.AcademicYearId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<AttendanceAudit>()
        .HasOne(a => a.AttendanceRecord)
        .WithMany()
        .HasForeignKey(a => a.AttendanceRecordId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<AttendanceAudit>()
        .HasOne(a => a.ModifiedBy)
        .WithMany()
        .HasForeignKey(a => a.ModifiedById)
        .OnDelete(DeleteBehavior.NoAction);

      // Configure Class-Subject relationship
      modelBuilder.Entity<Class>()
        .HasMany(c => c.Subjects)
        .WithOne(s => s.Class)
        .HasForeignKey(s => s.ClassId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentSubject>()
        .HasOne(ss => ss.Student)
        .WithMany(s => s.StudentSubjects)
        .HasForeignKey(ss => ss.StudentId);

      modelBuilder.Entity<StudentSubject>()
          .HasOne(ss => ss.Subject)
          .WithMany()
          .HasForeignKey(ss => ss.SubjectId);

      modelBuilder.Entity<StudentEnrollmentHistory>()
        .HasOne(s => s.Student)
        .WithMany()
        .HasForeignKey(s => s.StudentId)
        .OnDelete(DeleteBehavior.NoAction);

      // Add these configurations
      modelBuilder.Entity<StudentEnrollmentHistory>()
        .HasOne(s => s.AcademicYear)
        .WithMany()
        .HasForeignKey(s => s.AcademicYearId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentEnrollmentHistory>()
        .HasOne(s => s.Course)
        .WithMany()
        .HasForeignKey(s => s.CourseId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentEnrollmentHistory>()
        .HasOne(s => s.Class)
        .WithMany()
        .HasForeignKey(s => s.ClassId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentEnrollmentHistory>()
        .HasOne(s => s.Division)
        .WithMany()
        .HasForeignKey(s => s.DivisionId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentAttendanceArchive>()
        .HasOne(s => s.AcademicYear)
        .WithMany()
        .HasForeignKey(s => s.AcademicYearId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentAttendanceArchive>()
        .HasOne(sa => sa.Student)
        .WithMany()
        .HasForeignKey(sa => sa.StudentId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentAttendanceArchive>()
        .HasOne(sa => sa.AcademicYear)
        .WithMany()
        .HasForeignKey(sa => sa.AcademicYearId)
        .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<StudentAttendanceArchive>()
        .HasIndex(sa => new { sa.StudentId, sa.Date, sa.SubjectId })
        .IsUnique();
    }

  }
}