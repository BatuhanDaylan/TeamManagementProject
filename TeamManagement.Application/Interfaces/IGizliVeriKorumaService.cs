namespace TeamManagement.Application.Interfaces
{
    //Uygulama şifresi ve API anahtarı gibi hassas verileri veritabanına yazmadan önce şifreler/çözer
    public interface IGizliVeriKorumaService
    {
        string Sifrele(string duzMetin);
        string? SifreCoz(string? sifreliMetin);
    }
}
