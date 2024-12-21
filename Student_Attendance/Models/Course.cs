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
    }
}