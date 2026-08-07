namespace TeamManagement.Application.DTOs.Ekipler
{
    //Liderin, davet/e-posta akışına gerek kalmadan bir üyeyi doğrudan kendi eliyle
    //kaydedebilmesi için kullanılır (ör. proje sadece yerel makinede çalışıp dışarıdan
    //erişilemediğinde, davet linki e-postayla gönderilemez/açılamaz)
    public class AdminKullaniciOlusturDto
    {
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}
