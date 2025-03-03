using System.ComponentModel.DataAnnotations;

namespace Student_Attendance.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;  // Add this property

        // Navigation properties
        public virtual ICollection<Subject> Subjects { get; set; }
        public virtual ICollection<Student> Students { get; set; }

        public Course()
        {
            Students = new HashSet<Student>();
            Subjects = new HashSet<Subject>();
        }
    }
}