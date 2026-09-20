using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MarauderStudio.Core.Flashing;

/// <summary>
/// Загрузка прошивки на устройство через Web OTA (MarauderOTA AP).
/// Endpoint: POST http://192.168.4.1/update (multipart/form-data, поле "firmware").
/// Ответ: "OK" или "FAIL" (text/plain). После успеха устройство делает ESP.restart().
/// </summary>
public sealed class WebOtaUploader
{
    public const string DefaultApSsid     = "MarauderOTA";
    public const string DefaultApPassword = "justcallmekoko";
    public const string DefaultEndpoint   = "http://192.168.4.1/update";

    private readonly HttpClient _http;
    private readonly string _endpoint;

    public WebOtaUploader(HttpClient? httpClient = null, string endpoint = DefaultEndpoint)
    {
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _endpoint = endpoint;
    }

    /// <summary>
    /// Загружает .bin на устройство. Возвращает true при OK-ответе.
    /// </summary>
    public async Task<WebOtaResult> UploadAsync(
        string binPath,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(binPath))
            return new WebOtaResult(false, $"Файл не найден: {binPath}", 0);

        try
        {
            await using var src = File.OpenRead(binPath);
            using var form = new MultipartFormDataContent("----MarauderStudioBoundary");
            var stream = new StreamContent(src);
            stream.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(stream, "firmware", Path.GetFileName(binPath));

            using var response = await _http.PostAsync(_endpoint, form, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var ok = response.IsSuccessStatusCode && body.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase);
            return new WebOtaResult(ok,
                ok ? "OK" : $"FAIL ({response.StatusCode}): {body}",
                (int)response.StatusCode);
        }
        catch (TaskCanceledException)
        {
            return new WebOtaResult(false, "Таймаут загрузки", 0);
        }
        catch (HttpRequestException ex)
        {
            return new WebOtaResult(false, $"Не удалось подключиться к {_endpoint}: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return new WebOtaResult(false, ex.Message, 0);
        }
    }
}

public sealed record WebOtaResult(bool Success, string Message, int StatusCode);
