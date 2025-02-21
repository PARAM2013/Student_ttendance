using Microsoft.AspNetCore.Mvc.Rendering;

namespace Student_Attendance.ViewModels
{
    public class BulkAttendanceViewModel
    {
        public SelectList Teachers { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public int SelectedTeacherId { get; set; }
        public SelectList Divisions { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Subjects { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>()); // Add this
    }
}
