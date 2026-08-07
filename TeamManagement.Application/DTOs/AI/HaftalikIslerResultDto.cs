namespace TeamManagement.Application.DTOs.AI
{
    //Haftalik mail parser sonuc DTO su, mail icindeki BIRDEN FAZLA projeyi ayri ayri tasir
    //(gercek kullanicilar tek mailde birden cok projenin haftalik islerini tek seferde yaziyor)
    public class HaftalikIslerResultDto
    {
        public List<HaftalikProjePlaniDto> Projeler { get; set; } = new();
    }

    //Mail icindeki tek bir projeye ait plan bolumunu temsil eder
    public class HaftalikProjePlaniDto
    {
        //Mailin gövdesinden cikan proje adi hangi projenin etkilenecegini belirler
        public string ProjeAdi { get; set; } = string.Empty;

        //Yeni eklenecek gorevler (serbest metinde acikca "iptal" veya "durum guncelleme" belirtilmeyen her madde buraya duser)
        public List<HaftalikGorevDto> EklenecekGorevler { get; set; } = new();

        //Iptal edildigi acikca belirtilen gorev basliklari Iptal durumuna alinir
        public List<string> IptalEdilenGorevler { get; set; } = new();

        //Durumu acikca guncellenen gorevlerin yeni durumu ve notu
        public List<HaftalikIlerlemeDto> GuncellenecekGorevler { get; set; } = new();

        //Durumu degil, sadece oncelik/tarihi degistirilmek istenen var olan gorevler
        public List<HaftalikDuzenlemeDto> DuzenlenecekGorevler { get; set; } = new();
    }

    //Haftalik mailde EKLE bolumundeki tek bir gorev satirini temsil eder
    public class HaftalikGorevDto
    {
        public string Baslik { get; set; } = string.Empty;
        //Baslangic tarihi opsiyoneldir yazilmazsa null kalir
        public DateTime? BaslangicTarihi { get; set; }
        //Bitis tarihi opsiyoneldir yazilmazsa null kalir
        public DateTime? BitisTarihi { get; set; }
        //Oncelik Yuksek Normal veya Dusuk olabilir
        public string Oncelik { get; set; } = "Normal";
        //Mailde gorevin atandigi belirtilen kisinin adi (varsa); arka plan servisi bunu
        //ekip uyeleriyle isim eslestirerek AtananId'ye cevirir, eslesme yoksa sahipsiz kalir
        public string? Atanan { get; set; }
    }

    //Haftalik mailde ILERLEME bolumundeki tek bir guncelleme satirini temsil eder
    public class HaftalikIlerlemeDto
    {
        public string GorevBasligi { get; set; } = string.Empty;
        //Gecerli degerler Backlog DevamEdiyor Test OnayBekliyor Tamamlandi Iptal
        public string YeniDurum { get; set; } = string.Empty;
        //Isteğe bagli not veya aciklama
        public string Not { get; set; } = string.Empty;
    }

    //Var olan bir gorevin durumu degil, oncelik/tarih alanlari degistirilmek istendiginde kullanilir
    //(ör. "X gorevinin tarihini 30 Agustos'a cekelim" gibi durum icermeyen bir duzenleme talebi)
    public class HaftalikDuzenlemeDto
    {
        public string GorevBasligi { get; set; } = string.Empty;
        //Belirtilmemisse null birakilir, mevcut deger degismez
        public string? YeniOncelik { get; set; }
        public DateTime? YeniBaslangicTarihi { get; set; }
        public DateTime? YeniBitisTarihi { get; set; }
    }
}
