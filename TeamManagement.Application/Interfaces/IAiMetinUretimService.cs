using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    //AiEmailParserService'ten farklı olarak JSON şeması zorlamaz; serbest metin üretimi
    //(haftalık özet, hata analizi gibi) için kullanılır
    public interface IAiMetinUretimService
    {
        Task<Result<string>> MetinUretAsync(string prompt, string apiKey);
    }
}
