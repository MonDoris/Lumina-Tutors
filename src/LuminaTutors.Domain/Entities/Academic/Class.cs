using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Domain.Entities.Academic;

public class Class : AuditableEntity
{
    public int SchoolId { get; set; }
    public int AcademicYearId { get; set; }
    public int GradeLevelId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int? HomeRoomTeacherId { get; set; }
    public byte MaxStudents { get; set; } = 40;
    public string? RoomNumber { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public School School { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
    public User? HomeRoomTeacher { get; set; }
    public ICollection<ClassEnrollment> Enrollments { get; set; } = [];
    public ICollection<SubjectAssignment> SubjectAssignments { get; set; } = [];
    public ICollection<SchoolEvent> Events { get; set; } = [];
}
