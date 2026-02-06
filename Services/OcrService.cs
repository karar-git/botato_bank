namespace CoreBank.Services;

public interface IOcrService
{
    /// <summary>
    /// Sends the ID card image to the external Python OCR service for validation.
    /// Returns whether the card is a valid البطاقة الوطنية and the extracted ID number.
    /// </summary>
    Task<OcrResult> ValidateNationalIdAsync(Stream imageStream, string fileName);
}

public class OcrResult
{
    public bool IsValid { get; set; }
    public string? NationalIdNumber { get; set; }
    public string? Error { get; set; }
}

public class OcrService : IOcrService
{
    private readonly HttpClient _httpClient;
    private readonly string _ocrApiUrl;
    private readonly ILogger<OcrService> _logger;

    public OcrService(HttpClient httpClient, IConfiguration config, ILogger<OcrService> logger)
    {
        _httpClient = httpClient;
        _ocrApiUrl = config["Ocr:ApiUrl"] ?? "http://localhost:8000/validate-id";
        _logger = logger;
    }

    public async Task<OcrResult> ValidateNationalIdAsync(Stream imageStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageStream);
            content.Add(streamContent, "image", fileName);

            var response = await _httpClient.PostAsync(_ocrApiUrl, content);
            var json = await response.Content.ReadFromJsonAsync<PythonOcrResponse>();

            if (!response.IsSuccessStatusCode || json == null)
            {
                _logger.LogWarning("OCR API returned {StatusCode}", response.StatusCode);
                return new OcrResult
                {
                    IsValid = false,
                    Error = "ID verification service is temporarily unavailable. Please try again later."
                };
            }

            return new OcrResult
            {
                IsValid = json.Valid,
                NationalIdNumber = json.IdNumber,
                Error = json.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call OCR API at {Url}", _ocrApiUrl);
            return new OcrResult
            {
                IsValid = false,
                Error = "ID verification service is temporarily unavailable. Please try again later."
            };
        }
    }

    /// <summary>
    /// Expected JSON response from the Python OCR service.
    /// </summary>
    private class PythonOcrResponse
    {
        public bool Valid { get; set; }
        public string? IdNumber { get; set; }
        public string? Error { get; set; }
    }
}
