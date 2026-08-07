using TeamManagement.Application.DTOs.AI;
using TeamManagement.Application.Common.Results;

namespace TeamManagement.Application.Interfaces
{
    public interface IAiEmailParserService
    {
        //apiKey: cagiran taraf hangi Gemini anahtariyla calisilacagini belirler
        //(ortak bot kutusu icin appsettings'teki global anahtar, lider bazli akis icin liderin kendi anahtari)
        Task<Result<AiTaskUpdateResultDto>> ParseEmailContentAsync(string emailContent, string apiKey);
        //Haftalik Yapilacak Isler maillerini parse eden ozel metot
        //mevcutGorevlerBaglami: ekibin su an aktif gorev basliklari (proje bazinda); AI bir maddenin var olan
        //bir goreve mi (guncelleme/iptal/duzenleme) yoksa yeni bir goreve mi ait oldugunu bu listeye bakarak
        //ayirt eder ve varsa GorevBasligi'ni EXACT olarak buradan alir (basit metin eslestirmesi tutsun diye)
        Task<Result<HaftalikIslerResultDto>> ParseHaftalikIslerAsync(string emailContent, string apiKey, string? mevcutGorevlerBaglami = null);
    }
}
