using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Infrastructure.Services;

/// <summary>
/// Nạp sẵn (warm-up) model Ollama vào RAM ngay khi ứng dụng khởi động, chạy nền
/// không chặn startup. Nhờ vậy học sinh gửi câu hỏi đầu tiên không phải chờ
/// cold-load model (~15s trở lên). Nếu Ollama chưa chạy, chỉ ghi cảnh báo và
/// bỏ qua — không làm ảnh hưởng tới việc khởi động app.
/// </summary>
public sealed class OllamaWarmupService : BackgroundService
{
    private readonly IHttpClientFactory              _httpClientFactory;
    private readonly ILogger<OllamaWarmupService>    _logger;
    private readonly string                          _ollamaUrl;
    private readonly string                          _model;

    public OllamaWarmupService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<OllamaWarmupService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
        _ollamaUrl         = config["Ollama:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:11434";
        _model             = config["Ollama:Model"] ?? "qwen2.5:7b";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("Ollama");

            // num_predict:1 + keep_alive:30m → chỉ nạp model vào RAM và giữ warm, không sinh câu trả lời dài.
            var warmupBody = new
            {
                model      = _model,
                messages   = new[] { new { role = "user", content = "ping" } },
                stream     = false,
                keep_alive = "30m",
                options    = new { num_predict = 1 }
            };

            _logger.LogInformation("Đang warm-up model Ollama '{Model}'...", _model);
            var response = await http.PostAsJsonAsync($"{_ollamaUrl}/api/chat", warmupBody, stoppingToken);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Warm-up Ollama thành công — model '{Model}' đã sẵn sàng.", _model);
            else
                _logger.LogWarning("Warm-up Ollama trả về {Status}. Bỏ qua.", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Không warm-up được Ollama tại {Url} (có thể chưa chạy). Gia Sư AI sẽ cold-load ở lần dùng đầu.",
                _ollamaUrl);
        }
    }
}
