using LuminaTutors.Application.DTOs.Grading;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LuminaTutors.Web.Pdf;

/// <summary>
/// Dựng file PDF "Sổ điểm môn học" từ <see cref="SubjectGradeBookDto"/>.
/// Bố cục khớp với bảng trên màn hình GradeBook: Mã HS · Họ tên · ĐTX(n) · ĐGK · ĐCK · ĐTBm · Nhận xét.
/// </summary>
public sealed class GradeBookPdfDocument : IDocument
{
    private readonly SubjectGradeBookDto _model;
    private readonly int _dtxSlots;

    public GradeBookPdfDocument(SubjectGradeBookDto model)
    {
        _model = model;
        // Số cột ĐTX = lớn nhất giữa cấu hình tối thiểu, mức 3 (như màn hình) và số điểm thực tế nhiều nhất của 1 HS.
        var maxRowScores = _model.Rows.Count > 0 ? _model.Rows.Max(r => r.RegularScores.Count) : 0;
        _dtxSlots = Math.Max(3, Math.Max(_model.RegularScoreSlots, maxRowScores));
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.2f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeTable);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("SỔ ĐIỂM MÔN HỌC").FontSize(16).Bold();
            col.Item().AlignCenter().Text(_model.SubjectName).FontSize(12).SemiBold();
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text($"Lớp: {_model.ClassName}");
                row.RelativeItem().AlignCenter().Text($"Học kỳ: {_model.SemesterName}");
                row.RelativeItem().AlignRight().Text($"GV: {_model.TeacherName}");
            });
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(60);    // Mã HS
                columns.RelativeColumn(3);      // Họ và tên
                for (var i = 0; i < _dtxSlots; i++)
                    columns.ConstantColumn(38);  // ĐTX i
                columns.ConstantColumn(38);     // ĐGK
                columns.ConstantColumn(38);     // ĐCK
                columns.ConstantColumn(44);     // ĐTBm
                columns.RelativeColumn(1.6f);   // Nhận xét
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "Mã HS");
                HeaderCell(header.Cell(), "Họ và tên");
                for (var i = 0; i < _dtxSlots; i++)
                    HeaderCell(header.Cell(), $"ĐTX {i + 1}");
                HeaderCell(header.Cell(), "ĐGK");
                HeaderCell(header.Cell(), "ĐCK");
                HeaderCell(header.Cell(), "ĐTBm");
                HeaderCell(header.Cell(), "Nhận xét");
            });

            foreach (var r in _model.Rows)
            {
                BodyCell(table.Cell(), r.StudentCode, center: true);
                BodyCell(table.Cell(), r.StudentName);
                for (var i = 0; i < _dtxSlots; i++)
                    BodyCell(table.Cell(), Fmt(r.RegularScores.ElementAtOrDefault(i)), center: true);
                BodyCell(table.Cell(), Fmt(r.MidTermScore), center: true);
                BodyCell(table.Cell(), Fmt(r.FinalScore), center: true);
                BodyCell(table.Cell(), Fmt(r.AverageScore), center: true, bold: true);
                BodyCell(table.Cell(), r.Remark ?? "");
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}")
               .FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                t.Span("Trang ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private static string Fmt(decimal? score) => score?.ToString("F1") ?? "—";

    private static void HeaderCell(IContainer cell, string text) =>
        cell.Background(Colors.Grey.Lighten2)
            .Border(0.5f).BorderColor(Colors.Grey.Medium)
            .PaddingVertical(4).PaddingHorizontal(3)
            .AlignCenter().AlignMiddle()
            .Text(text).Bold().FontSize(8.5f);

    private static void BodyCell(IContainer cell, string text, bool center = false, bool bold = false)
    {
        var styled = cell.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                         .PaddingVertical(3).PaddingHorizontal(3).AlignMiddle();
        if (center) styled = styled.AlignCenter();
        var span = styled.Text(text).FontSize(9);
        if (bold) span.Bold();
    }
}
