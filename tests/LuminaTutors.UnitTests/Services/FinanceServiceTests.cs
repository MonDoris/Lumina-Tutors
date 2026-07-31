using LuminaTutors.Application.DTOs.Finance;
using LuminaTutors.Application.Services;
using LuminaTutors.Domain.Entities.Finance;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.Services;

/// <summary>
/// Unit test cho <see cref="FinanceService"/> — cấu hình học phí, tạo hóa đơn,
/// ghi nhận thanh toán (thủ công + VNPay) và báo cáo tài chính tháng.
/// </summary>
public class FinanceServiceTests : ServiceTestBase
{
    private FinanceService CreateSut() => new(Uow.Object, Mapper, NullLogger<FinanceService>.Instance);

    // ══════════════════════════════════════════════════════════════════════════
    //  1. Cấu hình học phí
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFeeConfig_Trung_TraVeDuplicate()
    {
        Repo(c => c.TuitionFeeConfigs).SetupFind(Fake.FeeConfig());

        var result = await CreateSut().CreateFeeConfigAsync(1, 9,
            new CreateFeeConfigRequest("Học phí", 1, 1, 1_000_000));

        ShouldFail(result, "DUPLICATE");
    }

    [Fact]
    public async Task CreateFeeConfig_HopLe_LuuVaTraVeDto()
    {
        Repo(c => c.TuitionFeeConfigs).SetupFind();     // chưa có cấu hình
        var added = Repo(c => c.TuitionFeeConfigs).CaptureAdds();

        var result = await CreateSut().CreateFeeConfigAsync(1, 9,
            new CreateFeeConfigRequest("Học phí kỳ 1", 1, 1, 1_500_000));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Amount.Should().Be(1_500_000);
        added.Should().ContainSingle();
        ShouldHaveSaved();
    }

    [Fact]
    public async Task DeactivateFeeConfig_KhongTonTai_TraVeNotFound()
    {
        Repo(c => c.TuitionFeeConfigs).SetupGetById(null);

        var result = await CreateSut().DeactivateFeeConfigAsync(1);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateFeeConfig_HopLe_TatHieuLuc()
    {
        var config = Fake.FeeConfig();
        Repo(c => c.TuitionFeeConfigs).SetupGetById(config);

        var result = await CreateSut().DeactivateFeeConfigAsync(config.Id);

        result.IsSuccess.Should().BeTrue();
        config.IsActive.Should().BeFalse();
        ShouldHaveSaved();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. Hóa đơn
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetInvoice_KhongTonTai_TraVeNotFound()
    {
        Repo(i => i.TuitionInvoices).SetupFindWithInclude();

        var result = await CreateSut().GetInvoiceAsync(1);

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task GetInvoice_TonTai_TraVeDto()
    {
        Repo(i => i.TuitionInvoices).SetupFindWithInclude(Fake.Invoice(id: 5, amount: 1_200_000));

        var result = await CreateSut().GetInvoiceAsync(5);

        result.IsSuccess.Should().BeTrue();
        result.Data!.InvoiceId.Should().Be(5);
        result.Data.FinalAmount.Should().Be(1_200_000);
    }

    [Fact]
    public async Task CreateInvoice_LoaiPhiKhongTonTai_TraVeCfgNotFound()
    {
        Repo(c => c.TuitionFeeConfigs).SetupGetById(null);

        var result = await CreateSut().CreateInvoiceAsync(1, 9,
            new CreateInvoiceRequest(100, 1, "2026-01", 1_000_000, 0, DateOnly.FromDateTime(DateTime.UtcNow)));

        ShouldFail(result, "CFG_NOT_FOUND");
    }

    [Fact]
    public async Task CreateInvoice_HocSinhKhongTonTai_TraVeStuNotFound()
    {
        Repo(c => c.TuitionFeeConfigs).SetupGetById(Fake.FeeConfig());
        Repo(u => u.Users).SetupGetById(null);

        var result = await CreateSut().CreateInvoiceAsync(1, 9,
            new CreateInvoiceRequest(100, 1, "2026-01", 1_000_000, 0, DateOnly.FromDateTime(DateTime.UtcNow)));

        ShouldFail(result, "STU_NOT_FOUND");
    }

    [Fact]
    public async Task CreateInvoice_Trung_TraVeDuplicate()
    {
        Repo(c => c.TuitionFeeConfigs).SetupGetById(Fake.FeeConfig());
        Repo(u => u.Users).SetupGetById(Fake.User(id: 100));
        Repo(i => i.TuitionInvoices).SetupAny(true);

        var result = await CreateSut().CreateInvoiceAsync(1, 9,
            new CreateInvoiceRequest(100, 1, "2026-01", 1_000_000, 0, DateOnly.FromDateTime(DateTime.UtcNow)));

        ShouldFail(result, "DUPLICATE");
    }

    [Fact]
    public async Task CreateInvoice_HopLe_TaoHoaDon()
    {
        Repo(c => c.TuitionFeeConfigs).SetupGetById(Fake.FeeConfig());
        Repo(u => u.Users).SetupGetById(Fake.User(id: 100));
        Repo(i => i.TuitionInvoices).SetupAny(false);
        Repo(i => i.TuitionInvoices).SetupCount(0);
        Repo(i => i.TuitionInvoices).SetupFindWithInclude(Fake.Invoice(id: 7)); // GetInvoice sau khi tạo
        var added = Repo(i => i.TuitionInvoices).CaptureAdds();

        var result = await CreateSut().CreateInvoiceAsync(1, 9,
            new CreateInvoiceRequest(100, 1, "2026-01", 1_000_000, 0, DateOnly.FromDateTime(DateTime.UtcNow)));

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].StudentId.Should().Be(100);
        ShouldHaveSaved();
    }

    [Fact]
    public async Task GenerateInvoices_TraVeSoLuong()
    {
        Repo(i => i.TuitionInvoices).SetupFind(Fake.Invoice(id: 1), Fake.Invoice(id: 2), Fake.Invoice(id: 3));

        var result = await CreateSut().GenerateInvoicesAsync(1, 9,
            new GenerateInvoicesRequest(1, "2026-01", DateOnly.FromDateTime(DateTime.UtcNow)));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(3);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. Ghi nhận thanh toán (thủ công)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordPayment_HoaDonKhongTonTai_TraVeNotFound()
    {
        Repo(i => i.TuitionInvoices).SetupGetById(null);

        var result = await CreateSut().RecordPaymentAsync(1, 9,
            new RecordPaymentRequest(1, 500_000, PaymentMethod.Cash));

        ShouldFail(result, "NOT_FOUND");
    }

    [Fact]
    public async Task RecordPayment_DaThanhToan_TraVeAlreadyPaid()
    {
        Repo(i => i.TuitionInvoices).SetupGetById(Fake.Invoice(status: InvoiceStatus.Paid));

        var result = await CreateSut().RecordPaymentAsync(1, 9,
            new RecordPaymentRequest(1, 500_000, PaymentMethod.Cash));

        ShouldFail(result, "ALREADY_PAID");
    }

    [Fact]
    public async Task RecordPayment_SoTienKhongHopLe_TraVeInvalidAmount()
    {
        Repo(i => i.TuitionInvoices).SetupGetById(Fake.Invoice(status: InvoiceStatus.Pending));

        var result = await CreateSut().RecordPaymentAsync(1, 9,
            new RecordPaymentRequest(1, 0, PaymentMethod.Cash));

        ShouldFail(result, "INVALID_AMOUNT");
    }

    [Fact]
    public async Task RecordPayment_VuotCongNo_TraVeAmountExceeds()
    {
        // FinalAmount = 1.000.000 (amount) - 200.000 (discount) = 800.000; nộp 1.000.000 → vượt.
        var invoice = Fake.Invoice(amount: 1_000_000, discount: 200_000, status: InvoiceStatus.Pending);
        Repo(i => i.TuitionInvoices).SetupGetById(invoice);

        var result = await CreateSut().RecordPaymentAsync(1, 9,
            new RecordPaymentRequest(1, 1_000_000, PaymentMethod.Cash));

        ShouldFail(result, "AMOUNT_EXCEEDS");
    }

    [Fact]
    public async Task RecordPayment_TraDu_DanhDauDaThanhToan()
    {
        var invoice = Fake.Invoice(amount: 1_000_000, status: InvoiceStatus.Pending);
        Repo(i => i.TuitionInvoices).SetupGetById(invoice);
        var added = Repo(p => p.TuitionPayments).CaptureAdds();

        var result = await CreateSut().RecordPaymentAsync(1, 9,
            new RecordPaymentRequest(1, 1_000_000, PaymentMethod.Cash));

        result.IsSuccess.Should().BeTrue();
        result.Data!.AmountPaid.Should().Be(1_000_000);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        added.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordPayment_TraMotPhan_DanhDauPartial()
    {
        var invoice = Fake.Invoice(amount: 1_000_000, status: InvoiceStatus.Pending);
        Repo(i => i.TuitionInvoices).SetupGetById(invoice);
        Repo(p => p.TuitionPayments).CaptureAdds();

        var result = await CreateSut().RecordPaymentAsync(1, 9,
            new RecordPaymentRequest(1, 400_000, PaymentMethod.Cash));

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Partial);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. Xác nhận thanh toán VNPay (idempotent)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConfirmVnPay_KhongTonTai_TraVeNotFound()
    {
        Repo(i => i.TuitionInvoices).SetupGetById(null);

        var result = await CreateSut().ConfirmVnPayPaymentAsync(1, 1_000_000, "TXN1", null);

        result.Should().Be(VnPayConfirmResult.NotFound);
    }

    [Fact]
    public async Task ConfirmVnPay_DaThanhToan_TraVeAlreadyConfirmed()
    {
        Repo(i => i.TuitionInvoices).SetupGetById(Fake.Invoice(status: InvoiceStatus.Paid));

        var result = await CreateSut().ConfirmVnPayPaymentAsync(1, 1_000_000, "TXN1", null);

        result.Should().Be(VnPayConfirmResult.AlreadyConfirmed);
    }

    [Fact]
    public async Task ConfirmVnPay_SaiSoTien_TraVeInvalidAmount()
    {
        Repo(i => i.TuitionInvoices).SetupGetById(Fake.Invoice(amount: 1_000_000, status: InvoiceStatus.Pending));

        var result = await CreateSut().ConfirmVnPayPaymentAsync(1, 999_000, "TXN1", null);

        result.Should().Be(VnPayConfirmResult.InvalidAmount);
    }

    [Fact]
    public async Task ConfirmVnPay_DungSoTien_TraVeOk_VaDanhDauPaid()
    {
        var invoice = Fake.Invoice(amount: 1_000_000, status: InvoiceStatus.Pending);
        Repo(i => i.TuitionInvoices).SetupGetById(invoice);
        var added = Repo(p => p.TuitionPayments).CaptureAdds();

        var result = await CreateSut().ConfirmVnPayPaymentAsync(1, 1_000_000, "TXN123", "{raw}");

        result.Should().Be(VnPayConfirmResult.Ok);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        added.Should().ContainSingle();
        added[0].PaymentMethod.Should().Be(PaymentMethod.VnPay);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. Báo cáo tài chính tháng
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMonthlyReport_TinhTongVaTyLeThu()
    {
        // 2 hóa đơn tổng 3.000.000, đã thu 1.500.000 ⇒ tỷ lệ thu 50%
        Repo(s => s.Schools).SetupGetById(Fake.School());
        Repo(i => i.TuitionInvoices).SetupFind(
            Fake.Invoice(id: 1, amount: 1_000_000, status: InvoiceStatus.Paid),
            Fake.Invoice(id: 2, amount: 2_000_000, status: InvoiceStatus.Pending));
        Repo(p => p.TuitionPayments).SetupFind(Fake.Payment(amountPaid: 1_500_000));

        var result = await CreateSut().GetMonthlyReportAsync(1, month: 1, year: 2026);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalBilled.Should().Be(3_000_000);
        result.Data.TotalCollected.Should().Be(1_500_000);
        result.Data.CollectionRate.Should().Be(50m);
    }
}
