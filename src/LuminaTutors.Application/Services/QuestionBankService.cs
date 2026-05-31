using System.Net.Http;
using System.Text.RegularExpressions;
using LuminaTutors.Application.DTOs.QuestionBank;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class QuestionBankService : IQuestionBankService
{
    private readonly IUnitOfWork                   _uow;
    private readonly IHttpClientFactory            _httpFactory;
    private readonly ILogger<QuestionBankService>  _logger;

    public QuestionBankService(
        IUnitOfWork uow,
        IHttpClientFactory httpFactory,
        ILogger<QuestionBankService> logger)
    {
        _uow         = uow;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<QuestionListItemDto>>> GetQuestionsAsync(
        int schoolId, QuestionFilterRequest filter, CancellationToken ct = default)
    {
        var query = await _uow.QuestionBanks.FindAsync(
            q => q.SchoolId == schoolId
              && (filter.SubjectId == null   || q.SubjectId == filter.SubjectId)
              && (filter.QuestionType == null || q.QuestionType == filter.QuestionType)
              && (filter.Difficulty == null   || q.DifficultyLevel == filter.Difficulty)
              && (filter.ChapterTag == null   || q.ChapterTag == filter.ChapterTag)
              && (filter.IsApproved == null   || q.IsApproved == filter.IsApproved)
              && (filter.Keyword == null
                  || q.QuestionText.Contains(filter.Keyword)
                  || (q.Tags != null && q.Tags.Contains(filter.Keyword))),
            q => q.Include(x => x.Subject).OrderByDescending(x => x.CreatedAt),
            ct);

        var total = query.Count;
        var items = query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(q => new QuestionListItemDto(
                q.Id, q.Subject.SubjectName, q.QuestionType, q.DifficultyLevel,
                q.QuestionText.Length > 120 ? q.QuestionText[..120] + "…" : q.QuestionText,
                q.ChapterTag, q.IsApproved, q.CreatedAt))
            .ToList();

        return Result<PagedResult<QuestionListItemDto>>.Success(
            PagedResult<QuestionListItemDto>.Create(items, total, filter.Page, filter.PageSize));
    }

    public async Task<Result<QuestionDto>> GetByIdAsync(
        int schoolId, int questionId, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == questionId,
            query => query.Include(x => x.Subject)
                          .Include(x => x.GradeLevel)
                          .Include(x => x.CreatedByTeacher)
                          .Include(x => x.Options),
            ct);

        if (q is null) return Result<QuestionDto>.Failure("Không tìm thấy câu hỏi.");
        return Result<QuestionDto>.Success(MapToDto(q));
    }

    public async Task<Result<QuestionDto>> CreateAsync(
        int schoolId, int teacherId, CreateQuestionRequest req, CancellationToken ct = default)
    {
        var entity = new QuestionBank
        {
            SchoolId            = schoolId,
            SubjectId           = req.SubjectId,
            CreatedByTeacherId  = teacherId,
            QuestionText        = req.QuestionText.Trim(),
            QuestionType        = req.QuestionType,
            DifficultyLevel     = req.Difficulty,
            GradeLevelId        = req.GradeLevelId,
            ChapterTag          = req.ChapterTag?.Trim(),
            Tags                = req.Tags?.Trim(),
            ExplanationText     = req.ExplanationText?.Trim(),
            CorrectAnswer       = req.CorrectAnswer?.Trim(),
            SourceUrl           = req.SourceUrl?.Trim(),
            IsApproved          = false
        };

        foreach (var opt in req.Options)
        {
            entity.Options.Add(new QuestionOption
            {
                OptionLabel = opt.OptionLabel,
                OptionText  = opt.OptionText.Trim(),
                IsCorrect   = opt.IsCorrect
            });
        }

        await _uow.QuestionBanks.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetByIdAsync(schoolId, entity.Id, ct);
    }

    public async Task<Result<QuestionDto>> UpdateAsync(
        int schoolId, int questionId, UpdateQuestionRequest req, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == questionId,
            query => query.Include(x => x.Options),
            ct);

        if (q is null) return Result<QuestionDto>.Failure("Không tìm thấy câu hỏi.");

        q.QuestionText    = req.QuestionText.Trim();
        q.QuestionType    = req.QuestionType;
        q.DifficultyLevel = req.Difficulty;
        q.GradeLevelId    = req.GradeLevelId;
        q.ChapterTag      = req.ChapterTag?.Trim();
        q.Tags            = req.Tags?.Trim();
        q.ExplanationText = req.ExplanationText?.Trim();
        q.CorrectAnswer   = req.CorrectAnswer?.Trim();
        q.SourceUrl       = req.SourceUrl?.Trim();
        q.IsApproved      = false; // reset on edit

        // Rebuild options
        q.Options.Clear();
        foreach (var opt in req.Options)
        {
            q.Options.Add(new QuestionOption
            {
                QuestionId  = q.Id,
                OptionLabel = opt.OptionLabel,
                OptionText  = opt.OptionText.Trim(),
                IsCorrect   = opt.IsCorrect
            });
        }

        await _uow.SaveChangesAsync(ct);
        return await GetByIdAsync(schoolId, questionId, ct);
    }

    public async Task<Result> DeleteAsync(int schoolId, int questionId, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == questionId, ct: ct);
        if (q is null) return Result.Failure("Không tìm thấy câu hỏi.");
        _uow.QuestionBanks.Remove(q);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ApproveAsync(int schoolId, int questionId, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.FindOneAsync(
            x => x.SchoolId == schoolId && x.Id == questionId, ct: ct);
        if (q is null) return Result.Failure("Không tìm thấy câu hỏi.");
        q.IsApproved = true;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── URL Import ────────────────────────────────────────────────────────────

    public async Task<Result<ImportPreviewDto>> PreviewUrlImportAsync(
        int schoolId, int teacherId, ImportFromUrlRequest req, CancellationToken ct = default)
    {
        try
        {
            var html      = await FetchHtmlAsync(req.SourceUrl, ct);
            var parsed    = ParseQuestionsFromHtml(html, req.SourceUrl, req.TargetSubjectId, req.DefaultDifficulty, schoolId, teacherId);
            var preview   = parsed.Take(5).Select(MapToDto).ToList();
            return Result<ImportPreviewDto>.Success(new ImportPreviewDto(
                req.SourceUrl, parsed.Count, preview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview URL {Url}", req.SourceUrl);
            return Result<ImportPreviewDto>.Failure($"Không thể tải trang: {ex.Message}");
        }
    }

    public async Task<Result<ImportJobDto>> ExecuteUrlImportAsync(
        int schoolId, int teacherId, ImportFromUrlRequest req, CancellationToken ct = default)
    {
        var subject = await _uow.Subjects.GetByIdAsync(req.TargetSubjectId, ct);
        if (subject is null) return Result<ImportJobDto>.Failure("Môn học không tồn tại.");

        var job = new QuestionImportJob
        {
            SchoolId          = schoolId,
            RequestedByUserId = teacherId,
            TargetSubjectId   = req.TargetSubjectId,
            SourceUrl         = req.SourceUrl,
            Status            = ImportJobStatus.Processing
        };
        await _uow.QuestionImportJobs.AddAsync(job, ct);
        await _uow.SaveChangesAsync(ct);

        try
        {
            var html    = await FetchHtmlAsync(req.SourceUrl, ct);
            var parsed  = ParseQuestionsFromHtml(html, req.SourceUrl, req.TargetSubjectId, req.DefaultDifficulty, schoolId, teacherId);

            foreach (var q in parsed)
                await _uow.QuestionBanks.AddAsync(q, ct);

            job.Status        = ImportJobStatus.Completed;
            job.ImportedCount = parsed.Count;
            job.ProcessedAt   = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            return Result<ImportJobDto>.Success(MapJobToDto(job, subject.SubjectName));
        }
        catch (Exception ex)
        {
            job.Status       = ImportJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.ProcessedAt  = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            _logger.LogError(ex, "URL import failed for {Url}", req.SourceUrl);
            return Result<ImportJobDto>.Failure($"Import thất bại: {ex.Message}");
        }
    }

    // ── File Import ───────────────────────────────────────────────────────────

    public async Task<Result<int>> ImportFromExcelAsync(
        int schoolId, int teacherId, int subjectId, IFormFile file, CancellationToken ct = default)
    {
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Result<int>.Failure("Chỉ hỗ trợ file .xlsx");

        try
        {
            var rows    = ParseExcelFile(file);
            int created = 0;

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.QuestionText)) continue;
                var entity = new QuestionBank
                {
                    SchoolId           = schoolId,
                    SubjectId          = subjectId,
                    CreatedByTeacherId = teacherId,
                    QuestionText       = row.QuestionText.Trim(),
                    QuestionType       = row.QuestionType,
                    DifficultyLevel    = row.Difficulty,
                    ChapterTag         = row.ChapterTag?.Trim(),
                    CorrectAnswer      = row.CorrectAnswer?.Trim(),
                    IsApproved         = false
                };
                foreach (var opt in row.Options)
                    entity.Options.Add(opt);

                await _uow.QuestionBanks.AddAsync(entity, ct);
                created++;
            }
            await _uow.SaveChangesAsync(ct);
            return Result<int>.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel import failed");
            return Result<int>.Failure($"Lỗi đọc file: {ex.Message}");
        }
    }

    public async Task<Result<int>> ImportFromWordAsync(
        int schoolId, int teacherId, int subjectId, IFormFile file, CancellationToken ct = default)
    {
        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return Result<int>.Failure("Chỉ hỗ trợ file .docx");

        try
        {
            var text    = await ExtractWordTextAsync(file);
            var parsed  = ParseQuestionsFromText(text, subjectId, DifficultyLevel.Medium, schoolId, teacherId);

            foreach (var q in parsed)
                await _uow.QuestionBanks.AddAsync(q, ct);

            await _uow.SaveChangesAsync(ct);
            return Result<int>.Success(parsed.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Word import failed");
            return Result<int>.Failure($"Lỗi đọc file: {ex.Message}");
        }
    }

    // ── Import from PDF ───────────────────────────────────────────────────────

    public async Task<Result<int>> ImportFromPdfAsync(
        int schoolId, int teacherId, int subjectId, IFormFile file, CancellationToken ct = default)
    {
        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Result<int>.Failure("Chỉ hỗ trợ file .pdf");

        // Validate FK params trước khi thử save
        if (schoolId  <= 0) return Result<int>.Failure("SchoolId không hợp lệ (= 0). Vui lòng đăng nhập lại.");
        if (teacherId <= 0) return Result<int>.Failure("TeacherId không hợp lệ (= 0). Vui lòng đăng nhập lại.");
        if (subjectId <= 0) return Result<int>.Failure("SubjectId không hợp lệ. Vui lòng chọn môn học.");

        // Kiểm tra Subject tồn tại
        var subject = await _uow.Subjects.GetByIdAsync(subjectId, ct);
        if (subject == null)
            return Result<int>.Failure($"Môn học ID={subjectId} không tồn tại trong hệ thống.");

        try
        {
            var text = await ExtractPdfTextAsync(file);
            _logger.LogInformation("PDF extracted {Chars} chars from {File}", text.Length, file.FileName);

            if (string.IsNullOrWhiteSpace(text))
                return Result<int>.Failure("Không thể đọc nội dung file PDF. Hãy đảm bảo PDF không bị scan (ảnh).");

            var parsed = ParseQuestionsFromText(text, subjectId, DifficultyLevel.Medium, schoolId, teacherId);
            _logger.LogInformation("PDF parsed {Count} questions", parsed.Count);

            if (parsed.Count == 0)
                return Result<int>.Failure("Không tìm thấy câu hỏi nào trong file. File phải có định dạng 'Câu 1: ...' và đáp án 'A. ...'");

            // Truncate QuestionText nếu quá dài
            foreach (var q in parsed)
            {
                if (q.QuestionText.Length > 2000)
                    q.QuestionText = q.QuestionText[..2000];
                foreach (var opt in q.Options)
                    if (opt.OptionText.Length > 500)
                        opt.OptionText = opt.OptionText[..500];
            }

            foreach (var q in parsed)
                await _uow.QuestionBanks.AddAsync(q, ct);

            await _uow.SaveChangesAsync(ct);
            return Result<int>.Success(parsed.Count);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            _logger.LogError(dbEx, "PDF import DB error: {Inner}", inner);
            return Result<int>.Failure($"Lỗi lưu DB: {inner}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF import failed for file {FileName}", file.FileName);
            return Result<int>.Failure($"Lỗi xử lý PDF: {ex.GetType().Name} — {ex.Message}");
        }
    }

    private static async Task<string> ExtractPdfTextAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var sb = new System.Text.StringBuilder();

        // ── Bước 1: thử decode các content stream bị nén FlateDecode ────────────
        try
        {
            var raw = System.Text.Encoding.Latin1.GetString(bytes);

            // Tìm tất cả stream...endstream
            var streamMatches = Regex.Matches(raw,
                @"stream\r?\n([\s\S]*?)\r?\nendstream", RegexOptions.Multiline);

            foreach (Match sm in streamMatches)
            {
                var streamBytes = System.Text.Encoding.Latin1.GetBytes(sm.Groups[1].Value);
                string decoded;
                try
                {
                    // Thử giải nén zlib (FlateDecode) — bỏ qua 2 byte zlib header
                    using var compressed = new MemoryStream(streamBytes, 2, streamBytes.Length - 2);
                    using var deflate    = new System.IO.Compression.DeflateStream(
                        compressed, System.IO.Compression.CompressionMode.Decompress);
                    using var reader     = new System.IO.StreamReader(deflate, System.Text.Encoding.Latin1);
                    decoded = await reader.ReadToEndAsync();
                }
                catch
                {
                    // Stream không bị nén — dùng thẳng
                    decoded = System.Text.Encoding.Latin1.GetString(streamBytes);
                }

                // Extract text từ PDF operators Tj và TJ
                sb.Append(ExtractTextFromContentStream(decoded));
            }
        }
        catch { /* ignore — fallback below */ }

        // ── Bước 2: fallback — quét BT...ET trực tiếp trên raw bytes ─────────────
        if (sb.Length == 0)
        {
            var raw = System.Text.Encoding.Latin1.GetString(bytes);
            var btBlocks = Regex.Matches(raw, @"BT\s*([\s\S]*?)\s*ET");
            foreach (Match bt in btBlocks)
                sb.Append(ExtractTextFromContentStream(bt.Groups[1].Value));
        }

        // ── Bước 3: unescape PDF string escapes (\n \r \t \( \) \\) ─────────────
        var result = sb.ToString();
        result = result.Replace(@"\n", "\n").Replace(@"\r", "\r")
                       .Replace(@"\t", "\t").Replace(@"\(", "(")
                       .Replace(@"\)", ")").Replace(@"\\", "\\");

        return result;
    }

    private static string ExtractTextFromContentStream(string content)
    {
        var sb = new System.Text.StringBuilder();

        // Ưu tiên TJ (array form) vì chứa đầy đủ text nhất
        // TJ operator: [(text)(text2) -kerning ...] TJ
        var hasTj = false;
        foreach (Match m in Regex.Matches(content, @"\[([\s\S]*?)\]\s*TJ"))
        {
            hasTj = true;
            var parts = Regex.Matches(m.Groups[1].Value, @"\(([^)]*)\)");
            foreach (Match p in parts)
                sb.Append(p.Groups[1].Value);
            sb.AppendLine();
        }

        // Nếu không có TJ thì fallback sang Tj
        if (!hasTj)
        {
            foreach (Match m in Regex.Matches(content, @"\(([^)]*)\)\s*Tj"))
                sb.AppendLine(m.Groups[1].Value.Trim());

            // ' operator: (text) ' — move to next line and show
            foreach (Match m in Regex.Matches(content, @"\(([^)]*)\)\s*'"))
                sb.AppendLine(m.Groups[1].Value.Trim());
        }

        return sb.ToString();
    }

    // ── Import Jobs ───────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ImportJobDto>>> GetImportJobsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var jobs = await _uow.QuestionImportJobs.FindAsync(
            j => j.SchoolId == schoolId,
            q => q.Include(j => j.TargetSubject).OrderByDescending(j => j.CreatedAt),
            ct);

        var dtos = jobs.Select(j => MapJobToDto(j, j.TargetSubject?.SubjectName ?? "")).ToList();
        return Result<IReadOnlyList<ImportJobDto>>.Success(dtos);
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    public async Task<Result<QuestionBankStatsDto>> GetStatsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var all = await _uow.QuestionBanks.FindAsync(
            q => q.SchoolId == schoolId,
            q => q.Include(x => x.Subject),
            ct);

        var stats = new QuestionBankStatsDto(
            TotalQuestions:    all.Count,
            ApprovedQuestions: all.Count(q => q.IsApproved),
            PendingApproval:   all.Count(q => !q.IsApproved),
            BySubject: all.GroupBy(q => q.Subject?.SubjectName ?? "?")
                          .ToDictionary(g => g.Key, g => g.Count()),
            ByType:    all.GroupBy(q => q.QuestionType.ToString())
                          .ToDictionary(g => g.Key, g => g.Count()),
            ByDifficulty: all.GroupBy(q => q.DifficultyLevel.ToString())
                             .ToDictionary(g => g.Key, g => g.Count())
        );

        return Result<QuestionBankStatsDto>.Success(stats);
    }

    // ── Subjects helper ───────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<SubjectOptionDto>>> GetSubjectsAsync(CancellationToken ct = default)
    {
        var all = await _uow.Subjects.FindAsync(_ => true, ct: ct);
        var dtos = all.OrderBy(s => s.SubjectName)
                      .Select(s => new SubjectOptionDto(s.Id, s.SubjectName))
                      .ToList();
        return Result<IReadOnlyList<SubjectOptionDto>>.Success(dtos);
    }

    // ── HTML Scraper ──────────────────────────────────────────────────────────

    private async Task<string> FetchHtmlAsync(string url, CancellationToken ct)
    {
        using var client = _httpFactory.CreateClient("Scraper");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; LuminaTutorsBot/1.0)");
        client.Timeout = TimeSpan.FromSeconds(15);

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Heuristic HTML parser — recognises common Vietnamese question patterns:
    /// "Câu X:", numbered lines, A/B/C/D option lines, "Đáp án:" lines.
    /// </summary>
    private static List<QuestionBank> ParseQuestionsFromHtml(
        string html, string sourceUrl, int subjectId, DifficultyLevel difficulty,
        int schoolId, int teacherId)
    {
        // Strip HTML tags, decode entities
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s{2,}", " ");

        return ParseQuestionsFromText(text, subjectId, difficulty, schoolId, teacherId, sourceUrl);
    }

    private static List<QuestionBank> ParseQuestionsFromText(
        string text, int subjectId, DifficultyLevel difficulty,
        int schoolId, int teacherId, string? sourceUrl = null)
    {
        var questions = new List<QuestionBank>();

        // Split into lines and look for question blocks
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0)
                        .ToList();

        QuestionBank? current = null;
        char nextLabel = 'A';

        // Regex patterns
        var questionPattern = new Regex(@"^(Câu\s*\d+|Question\s*\d+|\d+[\.\)])\s*[:\.\)]?\s*(.+)", RegexOptions.IgnoreCase);
        var optionPattern   = new Regex(@"^([A-D])[\.\)]\s*(.+)", RegexOptions.IgnoreCase);
        var answerPattern   = new Regex(@"^(Đáp án|Answer|Đáp)\s*[:\.]?\s*([A-D])", RegexOptions.IgnoreCase);

        foreach (var line in lines)
        {
            var qm = questionPattern.Match(line);
            if (qm.Success)
            {
                if (current != null && current.QuestionText.Length > 5)
                    questions.Add(current);

                current    = new QuestionBank
                {
                    SchoolId           = schoolId,
                    SubjectId          = subjectId,
                    CreatedByTeacherId = teacherId,
                    QuestionText       = qm.Groups[2].Value.Trim(),
                    QuestionType       = QuestionType.MultipleChoice,
                    DifficultyLevel    = difficulty,
                    SourceUrl          = sourceUrl,
                    IsApproved         = false
                };
                nextLabel = 'A';
                continue;
            }

            if (current != null)
            {
                var om = optionPattern.Match(line);
                if (om.Success)
                {
                    var label = char.ToUpper(om.Groups[1].Value[0]);
                    // Bỏ qua nếu label đã tồn tại (tránh vi phạm unique constraint)
                    if (!current.Options.Any(o => o.OptionLabel == label))
                    {
                        current.Options.Add(new QuestionOption
                        {
                            OptionLabel = label,
                            OptionText  = om.Groups[2].Value.Trim(),
                            IsCorrect   = false
                        });
                    }
                    continue;
                }

                var am = answerPattern.Match(line);
                if (am.Success)
                {
                    var correctLabel = am.Groups[2].Value[0];
                    foreach (var opt in current.Options)
                        opt.IsCorrect = opt.OptionLabel == correctLabel;
                    continue;
                }

                // continuation of previous question text
                if (current.Options.Count == 0 && !string.IsNullOrWhiteSpace(line))
                    current.QuestionText += " " + line;
            }
        }

        if (current != null && current.QuestionText.Length > 5)
            questions.Add(current);

        // Validate: keep only questions with ≥ 2 options or fill-blank type
        return questions.Where(q =>
            q.Options.Count >= 2 ||
            q.QuestionType == QuestionType.ShortAnswer ||
            q.QuestionType == QuestionType.Essay
        ).Take(500).ToList();
    }

    // ── Excel Parser (no external library — reads as CSV-like zip) ────────────

    private static List<ExcelRow> ParseExcelFile(IFormFile file)
    {
        // Simple CSV-compatible fallback: if user saves Excel as CSV
        // For real .xlsx without EPPlus, we read the shared strings XML manually
        var rows = new List<ExcelRow>();

        using var stream = file.OpenReadStream();
        using var zip    = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);

        // Read shared strings
        var ssEntry = zip.GetEntry("xl/sharedStrings.xml");
        var strings = new List<string>();
        if (ssEntry != null)
        {
            using var sr = new System.IO.StreamReader(ssEntry.Open());
            var xml      = sr.ReadToEnd();
            strings = Regex.Matches(xml, @"<t[^>]*>([^<]*)</t>")
                           .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value))
                           .ToList();
        }

        // Read sheet1
        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null) return rows;

        using var sheetSr = new System.IO.StreamReader(sheetEntry.Open());
        var sheetXml = sheetSr.ReadToEnd();

        var rowMatches = Regex.Matches(sheetXml, @"<row[^>]*>(.*?)</row>", RegexOptions.Singleline);
        bool headerSkipped = false;

        foreach (Match row in rowMatches)
        {
            var cells = Regex.Matches(row.Groups[1].Value, @"<c r=""([^""]+)""[^>]*t=""([^""]*)""[^>]*><v>(\d+)</v></c>|<c r=""([^""]+)""[^>]*><v>([^<]*)</v></c>")
                             .ToList();

            var cellValues = new Dictionary<int, string>(); // col index → value
            foreach (Match cell in cells)
            {
                string colRef   = cell.Groups[1].Success ? cell.Groups[1].Value : cell.Groups[4].Value;
                string cellType = cell.Groups[2].Value;
                string rawVal   = cell.Groups[3].Success ? cell.Groups[3].Value : cell.Groups[5].Value;

                int colIdx = ColToIndex(colRef);
                string val = cellType == "s" && int.TryParse(rawVal, out int si) && si < strings.Count
                    ? strings[si]
                    : rawVal;

                cellValues[colIdx] = val;
            }

            if (!headerSkipped) { headerSkipped = true; continue; }

            if (!cellValues.TryGetValue(0, out var questionText) || string.IsNullOrWhiteSpace(questionText))
                continue;

            var exRow = new ExcelRow
            {
                QuestionText = questionText,
                QuestionType = ParseQuestionType(cellValues.GetValueOrDefault(1, "")),
                Difficulty   = ParseDifficulty(cellValues.GetValueOrDefault(2, "")),
                ChapterTag   = cellValues.GetValueOrDefault(3),
                CorrectAnswer= cellValues.GetValueOrDefault(8) // col I = correct answer
            };

            // Columns D-G = options A-D
            for (int i = 4; i <= 7; i++)
            {
                if (cellValues.TryGetValue(i, out var optText) && !string.IsNullOrWhiteSpace(optText))
                {
                    char label = (char)('A' + (i - 4));
                    string? correctCol = cellValues.GetValueOrDefault(8, "");
                    exRow.Options.Add(new QuestionOption
                    {
                        OptionLabel = label,
                        OptionText  = optText,
                        IsCorrect   = correctCol?.Contains(label.ToString(), StringComparison.OrdinalIgnoreCase) == true
                    });
                }
            }

            rows.Add(exRow);
        }

        return rows;
    }

    private static int ColToIndex(string colRef)
    {
        int idx = 0;
        foreach (char c in colRef.ToUpper())
        {
            if (!char.IsLetter(c)) break;
            idx = idx * 26 + (c - 'A' + 1);
        }
        return idx - 1;
    }

    private static QuestionType ParseQuestionType(string s) => s.ToLower() switch
    {
        "trắc nghiệm" or "mcq" or "multiplechoice" => QuestionType.MultipleChoice,
        "đúng sai" or "truefalse" or "tf"           => QuestionType.TrueFalse,
        "điền vào chỗ trống" or "fillblank"          => QuestionType.ShortAnswer,
        "tự luận" or "essay"                         => QuestionType.Essay,
        _                                            => QuestionType.MultipleChoice
    };

    private static DifficultyLevel ParseDifficulty(string s) => s.ToLower() switch
    {
        "dễ" or "easy"       => DifficultyLevel.Easy,
        "khó" or "hard"      => DifficultyLevel.Hard,
        _                    => DifficultyLevel.Medium
    };

    // ── Word Text Extractor ───────────────────────────────────────────────────

    private static async Task<string> ExtractWordTextAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var zip    = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        var entry        = zip.GetEntry("word/document.xml");
        if (entry == null) return string.Empty;

        using var sr = new System.IO.StreamReader(entry.Open());
        var xml      = await sr.ReadToEndAsync();

        // Extract text between <w:t> tags
        var text = string.Join("\n", Regex.Matches(xml, @"<w:t[^>]*>([^<]+)</w:t>")
                                         .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value)));
        return text;
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static QuestionDto MapToDto(QuestionBank q) => new(
        q.Id,
        q.SubjectId,
        q.Subject?.SubjectName ?? "",
        q.QuestionText,
        q.QuestionType,
        q.DifficultyLevel,
        q.GradeLevelId,
        q.GradeLevel?.GradeName,
        q.ChapterTag,
        q.Tags,
        q.ExplanationText,
        q.CorrectAnswer,
        q.SourceUrl,
        q.IsApproved,
        q.CreatedByTeacher?.FullName ?? "",
        q.CreatedAt,
        q.Options.OrderBy(o => o.OptionLabel)
                 .Select(o => new QuestionOptionDto(o.Id, o.OptionLabel, o.OptionText, o.IsCorrect))
                 .ToList()
    );

    private static ImportJobDto MapJobToDto(QuestionImportJob j, string subjectName) => new(
        j.Id, j.SourceUrl, subjectName, j.Status,
        j.ImportedCount, j.ErrorMessage, j.CreatedAt, j.ProcessedAt);

    private class ExcelRow
    {
        public string         QuestionText  { get; set; } = "";
        public QuestionType   QuestionType  { get; set; } = QuestionType.MultipleChoice;
        public DifficultyLevel Difficulty   { get; set; } = DifficultyLevel.Medium;
        public string?        ChapterTag    { get; set; }
        public string?        CorrectAnswer { get; set; }
        public List<QuestionOption> Options { get; set; } = new();
    }
}
