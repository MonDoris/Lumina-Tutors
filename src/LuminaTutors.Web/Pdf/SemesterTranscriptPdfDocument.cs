using LuminaTutors.Application.DTOs.Grading;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LuminaTutors.Web.Pdf;

/// <summary>
/// Dựng PDF "Bảng điểm học kỳ" từ dữ liệu thật <see cref="StudentSemesterSummaryDto"/>.
/// Khớp với màn hình StudentGrades/Index: điểm TB từng môn, xếp loại, nhận xét + tổng kết học kỳ.
/// </summary>
public sealed class SemesterTranscriptPdfDocument : IDocument
{
    private readonly StudentSemesterSummaryDto _model;

    public SemesterTranscriptPdfDocument(StudentSemesterSummaryDto model) => _model = model;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("BẢNG ĐIỂM HỌC KỲ").FontSize(16).Bold();
            col.Item().AlignCenter().Text(_model.SemesterName).FontSize(12).SemiBold();
            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("Học sinh: ").SemiBold();
                t.Span(_model.StudentName);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            // ── Khối tổng kết học kỳ ──
            col.Item().Background(Colors.Grey.Lighten3).Padding(8).Row(row =>
            {
                SummaryItem(row.RelativeItem(), "ĐTB học kỳ",
                    _model.SemesterGpa.HasValue ? _model.SemesterGpa.Value.ToString("F1") : "—");
                SummaryItem(row.RelativeItem(), "Số môn", _model.SubjectAverages.Count.ToString());
                SummaryItem(row.RelativeItem(), "Vắng mặt", _model.AbsenceCount.ToString());
                SummaryItem(row.RelativeItem(), "Xếp loại", Remark(_model.SemesterGpa));
            });

            if (!string.IsNullOrEmpty(_model.SemesterRemark))
                col.Item().PaddingTop(6).Text(t =>
                {
                    t.Span("Nhận xét: ").SemiBold();
                    t.Span(_model.SemesterRemark);
                });

            // ── Bảng điểm từng môn ──
            col.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);    // Môn học
                    columns.ConstantColumn(70);   // Điểm TB
                    columns.ConstantColumn(70);   // Xếp loại
                    columns.RelativeColumn(3);    // Nhận xét
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Môn học");
                    HeaderCell(header.Cell(), "Điểm TB");
                    HeaderCell(header.Cell(), "Xếp loại");
                    HeaderCell(header.Cell(), "Nhận xét");
                });

                if (_model.SubjectAverages.Count == 0)
                {
                    table.Cell().ColumnSpan(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                         .Padding(8).AlignCenter()
                         .Text("Chưa có điểm nào được nhập cho học kỳ này.").Italic();
                }
                else
                {
                    foreach (var s in _model.SubjectAverages.OrderBy(x => x.SubjectName))
                    {
                        BodyCell(table.Cell(), s.SubjectName);
                        BodyCell(table.Cell(), s.AverageScore?.ToString("F1") ?? "—", center: true);
                        BodyCell(table.Cell(), Remark(s.AverageScore), center: true);
                        BodyCell(table.Cell(), string.IsNullOrEmpty(s.Remark) ? "—" : s.Remark);
                    }
                }
            });
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

    // Xếp loại theo điểm — khớp logic Remark() trong StudentGrades/Index.cshtml.
    private static string Remark(decimal? score) => score switch
    {
        >= 8.0m  => "Giỏi",
        >= 6.5m  => "Khá",
        >= 5.0m  => "TB",
        not null => "Yếu",
        _        => "—"
    };

    private static void SummaryItem(IContainer container, string label, string value) =>
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(value).FontSize(13).Bold();
            col.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
        });

    private static void HeaderCell(IContainer cell, string text) =>
        cell.Background(Colors.Grey.Lighten2)
            .Border(0.5f).BorderColor(Colors.Grey.Medium)
            .PaddingVertical(5).PaddingHorizontal(6)
            .Text(text).Bold().FontSize(9.5f);

    private static void BodyCell(IContainer cell, string text, bool center = false)
    {
        var styled = cell.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                         .PaddingVertical(4).PaddingHorizontal(6).AlignMiddle();
        if (center) styled = styled.AlignCenter();
        styled.Text(text).FontSize(10);
    }
}
