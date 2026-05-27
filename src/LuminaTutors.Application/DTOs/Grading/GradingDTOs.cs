using System.ComponentModel.DataAnnotations;

namespace LuminaTutors.Application.DTOs.Grading;

// ─── Score Entry ──────────────────────────────────────────────────────────────

public record ScoreEntryDto(
    int      ScoreEntryId,
    int      StudentId,
    string   StudentCode,
    string   StudentName,
    string   CategoryCode,    // DTX | DGK | DCK
    string   CategoryName,
    byte     Coefficient,
    byte     EntryOrder,
    decimal  Score,
    DateOnly? ExamDate,
    string?  Note,
    bool     IsLocked
);

public record EnterScoreRequest(
    [Required] int     StudentId,
    [Required] int     SubjectAssignmentId,
    [Required] int     GradeCategoryId,
    [Required] byte    EntryOrder,
    [Required, Range(0.0, 10.0)] decimal Score,
    DateOnly? ExamDate,
    string?   Note
);

public record BulkEnterScoreRequest(
    [Required] int SubjectAssignmentId,
    [Required] int GradeCategoryId,
    [Required] byte EntryOrder,
    [Required] List<StudentScoreItem> Scores
);

public record StudentScoreItem(
    [Required] int     StudentId,
    [Required, Range(0.0, 10.0)] decimal Score,
    string? Note
);

// ─── Grade Book ───────────────────────────────────────────────────────────────

public record GradeBookRowDto(
    int     StudentId,
    string  StudentCode,
    string  StudentName,
    // DTX scores (dynamic count)
    List<decimal?> RegularScores,
    decimal? MidTermScore,
    decimal? FinalScore,
    decimal? AverageScore,   // ĐTBm theo TT22
    string?  Remark,         // EXCELLENT | GOOD | AVERAGE | BELOW_AVG | FAIL
    bool     IsCalculated,
    bool     IsLocked
);

public record SubjectGradeBookDto(
    int    SubjectAssignmentId,
    string SubjectName,
    string TeacherName,
    string ClassName,
    string SemesterName,
    int    RegularScoreSlots,   // Số cột ĐTX tối thiểu
    List<GradeBookRowDto> Rows
);

// ─── GPA / Semester Summary ───────────────────────────────────────────────────

public record StudentSemesterSummaryDto(
    int     StudentId,
    string  StudentName,
    int     SemesterId,
    string  SemesterName,
    List<SubjectAverageDto> SubjectAverages,
    decimal? SemesterGpa,      // Trung bình các môn có trọng số
    string?  SemesterRemark,
    int      AbsenceCount
);

public record SubjectAverageDto(
    string   SubjectName,
    decimal? AverageScore,
    string?  Remark
);

// ─── Exam / Exam Room ─────────────────────────────────────────────────────────

public record ExamDto(
    int      ExamId,
    string   ExamName,
    string   ExamType,
    string   SubjectName,
    string   GradeName,
    string   SemesterName,
    DateOnly ExamDate,
    string   StartTime,
    int      DurationMinutes,
    decimal  MaxScore,
    int      RoomCount,
    int      TotalStudents
);

public record CreateExamRequest(
    [Required, MaxLength(200)] string ExamName,
    [Required] string   ExamType,
    [Required] int      SubjectId,
    [Required] int      GradeLevelId,
    [Required] int      SemesterId,
    [Required] DateOnly ExamDate,
    [Required] TimeOnly StartTime,
    [Required, Range(30, 180)] int DurationMinutes,
    decimal MaxScore = 10M
);

public record ExamRoomDto(
    int    ExamRoomId,
    string RoomName,
    string SupervisorName,
    byte   Capacity,
    int    AssignedCount,
    List<ExamSeatDto> Seats
);

public record ExamSeatDto(
    string SeatNumber,
    int    StudentId,
    string StudentCode,
    string StudentName,
    string ClassName
);
