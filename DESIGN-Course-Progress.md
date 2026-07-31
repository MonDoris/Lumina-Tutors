# Thiết kế E-Learning: Course Structure & Progress Tracking

> Nền móng cho hướng E-Learning của Lumina Tutors — mô hình **Hybrid**: Course là kho nội
> dung độc lập của trường, gán được cho lớp (blended) hoặc ghi danh lẻ (tự học / B2C sau này).
> Đã implement: Domain entities, EF configurations, `ICourseService` + `CourseService`.
> Chưa implement: Controller + Views, migration (xem mục 8).

## 1. Quyết định thiết kế

| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Quan hệ với `Lesson` hiện tại | **Tách riêng** — `CourseLesson` mới, không đụng `Lesson` (giáo án gắn `SubjectAssignment`) | `Lesson` phục vụ lớp live theo học kỳ, chết theo năm học; `Course` tái sử dụng nhiều năm/lớp |
| Ghi danh | 1 học sinh = 1 `CourseEnrollment`/khóa (unique), bất kể nguồn (gán lớp / tay / tự ghi danh) | Tiến độ là của cá nhân; gán lớp chỉ là *cách tạo* enrollment hàng loạt |
| Progress | Row `LessonProgress` tạo **lazy** khi mở bài lần đầu; không có row = chưa bắt đầu | Tránh phình bảng (enrollment × lessons) |
| Cache tiến độ | `ProgressPercent`, `CompletedLessonCount` denormalize trên enrollment | Trang "Khóa của tôi" & báo cáo GV không phải join bảng hot |
| Multi-tenant | `Course`, `ClassCourseAssignment`, `CourseEnrollment`, `LessonProgress` là `TenantEntity` | Theo chuẩn codebase — luôn filter `SchoolId` |

## 2. ERD

```mermaid
erDiagram
    Course ||--o{ CourseModule : "1-n"
    CourseModule ||--o{ CourseLesson : "1-n"
    CourseLesson ||--o{ CourseLessonMaterial : "1-n"
    CourseLesson }o--|| QuizExam : "Quiz lesson"
    Course ||--o{ ClassCourseAssignment : "gán lớp"
    ClassCourseAssignment }o--|| Class : ""
    Course ||--o{ CourseEnrollment : "ghi danh"
    CourseEnrollment }o--|| User : "Student"
    CourseEnrollment ||--o{ LessonProgress : "1-n"
    LessonProgress }o--|| CourseLesson : ""
    LessonProgress }o--o| StudentQuizAttempt : "bằng chứng quiz"
```

Chuỗi cascade (SQL Server an toàn, không multiple cascade paths):

- `Course → CourseModule → CourseLesson → CourseLessonMaterial` (Cascade)
- `CourseEnrollment → LessonProgress` (Cascade)
- `CourseLesson → CourseEnrollment.LastLessonId` (SetNull — resume point tự dọn)
- Mọi FK khác: Restrict — service chịu trách nhiệm chặn/dọn (xem quy tắc xóa §5)

## 3. Entity chính

| Entity | Vai trò | Field đáng chú ý |
|---|---|---|
| `Course` | Khóa học (kho nội dung) | `Status` (Draft/Published/Archived), `IsSequential`, `SubjectId?`, `GradeLevelId?` |
| `CourseModule` | Chương/Chủ đề | `SortOrder`, drip: `UnlockAfterDays?`/`AvailableFrom?`; **PPCT VN**: `SemesterNo?` (HK 1/2), `StartWeek?` (tuần 1–53) |
| `CourseLesson` | Bài học | `ContentType` (Article/Video/Quiz), `VideoUrl`, `VideoDurationSec`, `QuizExamId?`, `MinWatchPercent` (mặc định 90), `IsPreviewable`, `IsPublished`; **GDPT 2018**: `Objectives` (yêu cầu cần đạt), `CognitiveLevel?` (Nhận biết/Thông hiểu/Vận dụng/Vận dụng cao), `PeriodCount` (số tiết) |
| `ClassCourseAssignment` | Gán khóa ↔ lớp | unique (CourseId, ClassId); `StartDate/EndDate` = cửa sổ truy cập; `IsActive` |
| `CourseEnrollment` | Ghi danh cá nhân | unique (CourseId, StudentId); `Source`, cache `ProgressPercent`/`CompletedLessonCount`, `LastLessonId` (resume) |
| `LessonProgress` | Tiến độ 1 bài | unique (EnrollmentId, CourseLessonId); `WatchedSec`, `LastPositionSec`, `TimeSpentSec`, `QuizAttemptId?` |

## 4. Quy tắc hoàn thành & tính tiến độ

- **Video**: heartbeat mỗi 15–30s → `WatchedSec` cộng dồn; hoàn thành khi `WatchedSec ≥ MinWatchPercent% × VideoDurationSec`. Server **clamp delta** = `min(elapsed × 1.5 + 10, 300)` giây — tua nhanh/gửi delta ảo không ăn gian được. Video không rõ duration → cho hoàn thành tay.
- **Article**: học sinh bấm "Hoàn thành".
- **Quiz**: phải có `StudentQuizAttempt.SubmittedAt != null` cho `QuizExamId` liên kết; attempt id lưu vào `LessonProgress.QuizAttemptId` làm bằng chứng.
- `ProgressPercent = bài Completed / tổng bài IsPublished` (chỉ đếm bài published). Đạt 100% → enrollment `Completed` + `CompletedAt`. Thêm bài mới sau đó → tự quay lại `Active`.
- Cấu trúc khóa thay đổi (publish/unpublish/xóa bài) → `RecalcCourseEnrollmentsAsync` tính lại toàn bộ enrollment của khóa (1 query group-by).

## 5. Quy tắc khóa/mở & xóa

**Lock khi học** (thứ tự kiểm tra trong `OpenLessonAsync`):
1. Course phải `Published`; enrollment không `Dropped`.
2. Cửa sổ truy cập của lớp (`StartDate`/`EndDate` trên assignment) còn hiệu lực.
3. Module drip: qua `AvailableFrom` và đủ `UnlockAfterDays` (nếu đặt cả hai → phải thỏa cả hai).
4. `IsSequential` → mọi bài published đứng trước (thứ tự module.SortOrder → lesson.SortOrder) phải Completed.
5. Chưa ghi danh chỉ mở được bài `IsPreviewable`.

**Quy tắc xóa** (bảo toàn dữ liệu học tập):
- Xóa Course: chỉ khi `Draft` + chưa có enrollment/assignment. Ngược lại dùng `Archived`.
- Xóa Module/Lesson: chỉ khi chưa có `LessonProgress` — đã có học sinh học thì unpublish thay vì xóa.
- Gỡ gán lớp (`DeactivateClassAssignment`): học sinh đã ghi danh **giữ quyền truy cập**, chỉ ngừng ghi danh mới.

## 5b. Khung GDPT 2018 & thống kê hoàn thành (bổ sung)

**Khung Việt Nam**: chương gắn Học kỳ + Tuần PPCT; bài học có "Yêu cầu cần đạt" (hiển thị
cho học sinh đầu bài), mức độ tư duy 4 bậc và số tiết. Enum `CognitiveLevel`:
Recognition/Comprehension/Application/AdvancedApplication ↔ Nhận biết/Thông hiểu/Vận dụng/Vận dụng cao.

**Thanh mức độ hoàn thành của học sinh** (không tính enrollment Dropped):
- Card danh sách khóa (`/Course`): bar % hoàn thành **trung bình** (`AvgProgressPercent` — EF AVG subquery).
- Trang builder (`/Course/Detail`): panel `CourseStatsDto` — bar trung bình + phân bố 4 khoảng
  (0–25/25–50/50–75/75–100) + bar trung bình theo **từng lớp** được gán (`ClassAssignmentDto.AvgProgressPercent`).
- Báo cáo (`/Course/Report`): thêm `LessonStatRowDto[]` — bar tỷ lệ hoàn thành theo **từng bài học**
  (group theo chương, đỏ khi < 30% — chỉ ra bài học sinh hay bỏ dở), cùng panel phân bố.
- Service: `GetCourseStatsAsync` (public, cho builder) + `ComputeStats`/`ComputeLessonStatsAsync` (private, dùng chung với Report).

## 6. API surface (ĐÃ implement — CourseController + LearnController)

```
Teacher/Admin (policy TeacherOrAdmin):
GET   /Course                  danh sách khóa      GET  /Course/Detail/{id}   course builder
GET   /Course/Report/{id}      báo cáo tiến độ     GET  /Course/Assignments/{id}  (JSON)
POST  /Course/Create (form)    /Course/Update/{id} /Course/ChangeStatus/{id}  /Course/Delete/{id}
POST  /Course/SaveModule  /Course/DeleteModule/{id}  /Course/ReorderModules      (JSON)
POST  /Course/SaveLesson  /Course/DeleteLesson/{id}  /Course/ReorderLessons      (JSON)
POST  /Course/AssignClass  /Course/SyncAssignment/{id}  /Course/DeactivateAssignment/{id}
POST  /Course/EnrollStudent                                                     (JSON)

Student (policy AnyAuthenticated — service tự kiểm tra enrollment):
GET   /Learn                   khóa học của tôi    GET  /Learn/Course/{id}    mục lục
GET   /Learn/Lesson/{id}       học bài (player)
POST  /Learn/Heartbeat/{id}    heartbeat video 15s (JSON, fetch keepalive)
POST  /Learn/Complete/{id}     đánh dấu hoàn thành (JSON)
```

JSON endpoints dùng fetch + header `RequestVerificationToken`; response `{ ok, data?, error? }`.
`SchoolId`/`UserId` đọc từ claims như các controller hiện có.

## 7. File đã thêm/sửa

```
Domain/Entities/Learning/Course.cs                       (mới — 7 entities)
Domain/Enums/Enums.cs                                    (+5 enums E-Learning)
Domain/Interfaces/Repositories/IUnitOfWork.cs            (+7 repositories)
Infrastructure/Data/Configurations/Learning/CourseConfigurations.cs   (mới)
Infrastructure/Data/LuminaTutorsDbContext.cs             (+7 DbSets)
Infrastructure/Repositories/UnitOfWork.cs                (+7 repo properties)
Application/DTOs/Course/CourseDTOs.cs                    (mới)
Application/Interfaces/Services/ICourseService.cs        (mới)
Application/Services/CourseService.cs                    (mới)
Application/Extensions/ApplicationServiceExtensions.cs   (+ ICourseService DI)
Web/Controllers/CourseController.cs                      (mới — builder + JSON API)
Web/Controllers/LearnController.cs                       (mới — học tập + heartbeat)
Web/Views/Course/Index|Detail|Report.cshtml              (mới)
Web/Views/Learn/Index|Course|Lesson.cshtml               (mới — player HTML5 + YouTube IFrame API)
Web/Views/Shared/_Layout.cshtml                          (+2 link menu: GV "Khóa học E-Learning", HS "Khóa học của tôi")
```

## 8. Việc bạn cần chạy (máy dev có SDK)

```powershell
dotnet build

# Nếu CHƯA từng tạo migration cho Course (1 migration gộp tất cả):
dotnet ef migrations add AddCourseAndProgressTracking --project src/LuminaTutors.Infrastructure --startup-project src/LuminaTutors.Web

# Nếu ĐÃ chạy AddCourseAndProgressTracking rồi → tạo migration mới cho các trường GDPT:
dotnet ef migrations add AddVnCurriculumFields --project src/LuminaTutors.Infrastructure --startup-project src/LuminaTutors.Web

dotnet ef database update --project src/LuminaTutors.Infrastructure --startup-project src/LuminaTutors.Web
```

## 9. Bước tiếp theo (đứng trên nền này)

1. ~~CourseController + LearnController + Views~~ — ĐÃ XONG (player hỗ trợ mp4/m3u8 native + YouTube).
2. **Upload tài liệu bài học** (`CourseLessonMaterial` đã có schema, chưa có UI upload) + sanitize `ContentHtml` (hiện render `Html.Raw` — tin tưởng giáo viên).
3. **Video pipeline**: nâng cấp `VideoUrl` thành object storage + HLS + signed URL — schema không đổi.
4. **Certificate**: khi enrollment `Completed` → sinh PDF bằng QuestPDF (đã có trong project).
5. **Q&A theo bài học**, gamification (badge/streak) — bảng mới tham chiếu `CourseLesson`/`CourseEnrollment`.
6. **Learning analytics**: dashboard từ `LessonProgress.TimeSpentSec` + quiz score theo `ChapterTag`.
