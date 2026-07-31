using LuminaTutors.Application.DTOs.Grading;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Grading;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="GradingService"/> — nhập/xóa điểm (kèm các ràng buộc TT22),
/// tính điểm trung bình môn, khóa sổ điểm và tạo kỳ thi.
/// </summary>
public class GradingServiceTests : ServiceTestBase
{
    private GradingService CreateSut() => new(Uow.Object, Mapper, NullLogger<GradingService>.Instance);

    // Phân công môn của giáo viên 50, trường 1.
    private static SubjectAssignment Assignment(int teacherId = 50, int schoolId = 1) =>
        new() { Id = 1, SchoolId = schoolId, TeacherId = teacherId, ClassId = 1, SubjectId = 1, SemesterId = 1 };

    private static GradeCategory Category(
        string code = "DTX", bool multiple = true, byte? max = null, byte coefficient = 1) =>
        new() { Id = 1, CategoryCode = code, CategoryName = code, Coefficient = coefficient, IsMultipleAllowed = multiple, MaxCountPerSemester = max };

    private static ScoreEntry Score(decimal score, string categoryCode) =>
        new() { Score = score, GradeCategory = new GradeCategory { CategoryCode = categoryCode } };

    private static EnterScoreRequest EnterReq(decimal score) =>
        new(StudentId: 100, SubjectAssignmentId: 1, GradeCategoryId: 1, EntryOrder: 1, Score: score, ExamDate: null, Note: null);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. EnterScoreAsync — các ràng buộc
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EnterScore_KhongPhaiGiaoVienPhuTrach_TraVeForbidden()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment(teacherId: 99)); // GV khác

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(8));

        ShouldFail(result, "FORBIDDEN");
    }

    [Fact]
    public async Task EnterScore_DiemNgoaiKhoang_TraVeInvalidScore()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(11)); // > 10

        ShouldFail(result, "INVALID_SCORE");
    }

    [Fact]
    public async Task EnterScore_SoDiemDaKhoa_TraVeGradebookLocked()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());
        Repo(gb => gb.GradeBooks).SetupFind(new GradeBook { IsLocked = true });

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(8));

        ShouldFail(result, "GRADEBOOK_LOCKED");
    }

    [Fact]
    public async Task EnterScore_LoaiDiemKhongTonTai_TraVeNotFound()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());
        Repo(gb => gb.GradeBooks).SetupFind();
        Repo(gc => gc.GradeCategories).SetupGetById(null);

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(8));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task EnterScore_LoaiChiNhap1Lan_DaCoDiem_TraVeScoreExists()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());
        Repo(gb => gb.GradeBooks).SetupFind();
        Repo(gc => gc.GradeCategories).SetupGetById(Category(code: "DCK", multiple: false));
        Repo(se => se.ScoreEntries).SetupFind(Score(9, "DCK")); // đã có điểm cuối kỳ

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(8));

        ShouldFail(result, "SCORE_EXISTS");
    }

    [Fact]
    public async Task EnterScore_VuotSoCotToiDa_TraVeMaxExceeded()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());
        Repo(gb => gb.GradeBooks).SetupFind();
        Repo(gc => gc.GradeCategories).SetupGetById(Category(code: "DTX", multiple: true, max: 3));
        Repo(se => se.ScoreEntries).SetupFind(Score(7, "DTX"), Score(8, "DTX"), Score(9, "DTX")); // đã đủ 3 cột

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(6));

        ShouldFail(result, "MAX_EXCEEDED");
    }

    [Fact]
    public async Task EnterScore_HopLe_LuuDiem()
    {
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());
        Repo(gb => gb.GradeBooks).SetupFind();
        Repo(gc => gc.GradeCategories).SetupGetById(Category(code: "DTX", multiple: true));
        Repo(se => se.ScoreEntries).SetupFind(); // chưa có cột nào
        var added = Repo(se => se.ScoreEntries).CaptureAdds();

        var result = await CreateSut().EnterScoreAsync(1, 50, EnterReq(8.5m));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Score.Should().Be(8.5m);
        added.Should().ContainSingle();
        added[0].EntryOrder.Should().Be(1);
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. DeleteScoreAsync
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteScore_KhongTonTai_TraVeNotFound()
    {
        Repo(se => se.ScoreEntries).SetupFindWithInclude();

        var result = await CreateSut().DeleteScoreAsync(1, 50);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task DeleteScore_KhongCoQuyen_TraVeForbidden()
    {
        var entry = new ScoreEntry { Id = 1, SubjectAssignment = Assignment(teacherId: 99) };
        Repo(se => se.ScoreEntries).SetupFindWithInclude(entry);

        var result = await CreateSut().DeleteScoreAsync(1, 50);

        ShouldFail(result, "FORBIDDEN");
    }

    [Fact]
    public async Task DeleteScore_DaKhoa_TraVeLocked()
    {
        var entry = new ScoreEntry { Id = 1, IsLocked = true, SubjectAssignment = Assignment(teacherId: 50) };
        Repo(se => se.ScoreEntries).SetupFindWithInclude(entry);

        var result = await CreateSut().DeleteScoreAsync(1, 50);

        ShouldFail(result, "LOCKED");
    }

    [Fact]
    public async Task DeleteScore_HopLe_XoaVaLuu()
    {
        var entry = new ScoreEntry { Id = 1, SubjectAssignment = Assignment(teacherId: 50) };
        Repo(se => se.ScoreEntries).SetupFindWithInclude(entry);

        var result = await CreateSut().DeleteScoreAsync(1, 50);

        result.IsSuccess.Should().BeTrue();
        Repo(se => se.ScoreEntries).Verify(r => r.Remove(entry), Times.Once());
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. CalculateAverageAsync (công thức ĐTBm theo TT22)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CalculateAverage_TinhDungCongThucTT22_VaTaoSoDiem()
    {
        // ĐTX=[8], ĐGK=7, ĐCK=9 ⇒ (8 + 7×2 + 9×3) / (1 + 5) = 49/6 = 8.2
        Repo(se => se.ScoreEntries).SetupFindWithInclude(Score(8, "DTX"), Score(7, "DGK"), Score(9, "DCK"));
        Repo(sa => sa.SubjectAssignments).SetupGetById(Assignment());
        Repo(gb => gb.GradeBooks).SetupFindNoInclude();               // chưa có sổ điểm ⇒ tạo mới
        Repo(sp => sp.StudentProfiles).SetupFindNoInclude(Fake.StudentProfile(userId: 100));
        Repo(u => u.Users).SetupGetById(Fake.User(id: 100));
        var addedBooks = Repo(gb => gb.GradeBooks).CaptureAdds();

        var result = await CreateSut().CalculateAverageAsync(100, 1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.AverageScore.Should().Be(8.2m);
        result.Data.IsCalculated.Should().BeTrue();
        addedBooks.Should().ContainSingle();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. LockGradeBook / CreateExam / CalculateAllAverages
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LockGradeBook_KhongCoDuLieu_TraVeNotFound()
    {
        Repo(gb => gb.GradeBooks).SetupFind();

        var result = await CreateSut().LockGradeBookAsync(1, approvedByUserId: 9);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task LockGradeBook_HopLe_KhoaCaSoDiemVaDauDiem()
    {
        var gb = new GradeBook { Id = 1, SubjectAssignmentId = 1 };
        var se = new ScoreEntry { Id = 1, SubjectAssignmentId = 1 };
        Repo(g => g.GradeBooks).SetupFind(gb);
        Repo(s => s.ScoreEntries).SetupFind(se);

        var result = await CreateSut().LockGradeBookAsync(1, approvedByUserId: 9);

        result.IsSuccess.Should().BeTrue();
        gb.IsLocked.Should().BeTrue();
        se.IsLocked.Should().BeTrue();
        gb.ApprovedByUserId.Should().Be(9);
    }

    [Fact]
    public async Task CreateExam_LoaiKhongHopLe_TraVeInvalidType()
    {
        var req = new CreateExamRequest("Thi HK1", "KHONG_TON_TAI", 1, 1, 1,
            new DateOnly(2026, 1, 10), new TimeOnly(7, 30), 60);

        var result = await CreateSut().CreateExamAsync(1, 9, req);

        ShouldFail(result, "INVALID_TYPE");
    }

    [Fact]
    public async Task CreateExam_HopLe_LuuVaTraVeDto()
    {
        var added = Repo(e => e.Exams).CaptureAdds();
        var req = new CreateExamRequest("Thi HK1", "Final", 1, 1, 1,
            new DateOnly(2026, 1, 10), new TimeOnly(7, 30), 90);

        var result = await CreateSut().CreateExamAsync(1, 9, req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ExamName.Should().Be("Thi HK1");
        added.Should().ContainSingle();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task CalculateAllAverages_TraVeSoHocSinh()
    {
        // 3 đầu điểm nhưng chỉ 2 học sinh riêng biệt
        Repo(se => se.ScoreEntries).SetupFind(
            new ScoreEntry { StudentId = 100 },
            new ScoreEntry { StudentId = 100 },
            new ScoreEntry { StudentId = 101 });

        var result = await CreateSut().CalculateAllAveragesAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(2);
    }
}
