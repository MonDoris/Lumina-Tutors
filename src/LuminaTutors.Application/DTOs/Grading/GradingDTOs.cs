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
    [Required(ErrorMessage = "Vui lòng chọn học sinh.")] int     StudentId,
    [Required(ErrorMessage = "Vui lòng chọn phân công môn học.")] int     SubjectAssignmentId,
    [Required(ErrorMessage = "Vui lòng chọn loại điểm.")] int     GradeCategoryId,
    [Required(ErrorMessage = "Vui lòng nhập thứ tự cột điểm.")] byte    EntryOrder,
    [Required(ErrorMessage = "Vui lòng nhập điểm số."), Range(0.0, 10.0, ErrorMessage = "Điểm số phải từ 0 đến 10.")] decimal Score,
    DateOnly? ExamDate,
    string?   Note
);

public record BulkEnterScoreRequest(
    [Required(ErrorMessage = "Vui lòng chọn phân công môn học.")] int SubjectAssignmentId,
    [Required(ErrorMessage = "Vui lòng chọn loại điểm.")] int GradeCategoryId,
    [Required(ErrorMessage = "Vui lòng nhập thứ tự cột điểm.")] byte EntryOrder,
    [Required(ErrorMessage = "Vui lòng nhập danh sách điểm.")] List<StudentScoreItem> Scores
);

public record StudentScoreItem(
    [Required(ErrorMessage = "Vui lòng chọn học sinh.")] int     StudentId,
    [Required(ErrorMessage = "Vui lòng nhập điểm số."), Range(0.0, 10.0, ErrorMessage = "Điểm số phải từ 0 đến 10.")] decimal Score,
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
    [Required(ErrorMessage = "Vui lòng nhập tên kỳ thi."), MaxLength(200, ErrorMessage = "Tên kỳ thi tối đa 200 ký tự.")] string ExamName,
    [Required(ErrorMessage = "Vui lòng chọn loại kỳ thi.")] string   ExamType,
    [Required(ErrorMessage = "Vui lòng chọn môn học.")] int      SubjectId,
    [Required(ErrorMessage = "Vui lòng chọn khối lớp.")] int      GradeLevelId,
    [Required(ErrorMessage = "Vui lòng chọn học kỳ.")] int      SemesterId,
    [Required(ErrorMessage = "Vui lòng nhập ngày thi.")] DateOnly ExamDate,
    [Required(ErrorMessage = "Vui lòng nhập giờ bắt đầu.")] TimeOnly StartTime,
    [Required(ErrorMessage = "Vui lòng nhập thời gian làm bài."), Range(30, 180, ErrorMessage = "Thời gian làm bài phải từ 30 đến 180 phút.")] int DurationMinutes,
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
