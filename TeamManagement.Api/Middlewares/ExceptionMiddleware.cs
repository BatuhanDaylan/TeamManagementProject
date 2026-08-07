using System.Net;
using System.Text.Json;

namespace TeamManagement.Api.Middlewares
{
    //Global hata yakalama middleware tüm istek pipeline inde oluşan hataları yakalar
    public class ExceptionMiddleware
    {
        //Sonraki middleware i çağırmak için delegate
        private readonly RequestDelegate _next;

        //Hata detaylarını kaydetmek için logger
        private readonly ILogger<ExceptionMiddleware> _logger;

        //Sadece development ortamında ham hata mesajı istemciye döndürülür
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        //Her HTTP isteği bu metot üzerinden geçer hata olursa yakalar
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                //Sonraki middleware i çağır hata yoksa normal akış devam eder
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                //Hata loglanır ve istemciye standart hata yanıtı döndürülür
                _logger.LogError($"Bir hata oluştu: {ex}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        //HTTP yanıtını 500 olarak ayarlar ve JSON formatında hata bilgisi döndürür
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Sunucu tarafında beklenmeyen bir hata oluştu.",
                //Ham hata mesajı (iç detaylar, stack bilgisi vb.) sadece development ortamında döndürülür,
                //production'da istemciye iç sistem detayı sızdırılmaz
                Detailed = _environment.IsDevelopment() ? exception.Message : null
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}
