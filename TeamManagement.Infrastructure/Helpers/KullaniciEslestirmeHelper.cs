using System.Globalization;

namespace TeamManagement.Infrastructure.Helpers
{
    //Mail-tabanlı AI akışlarında (haftalık plan) serbest metinde geçen bir ismi ekip üyeleriyle
    //eşleştirmek için HaftalikPlanMailListenerBackgroundService tarafından kullanılır
    public static class KullaniciEslestirmeHelper
    {
        //atananAdi tam ad, sadece ad veya sadece soyad olabilir; eşleşme bulunamazsa VEYA birden
        //fazla kişiyle eşleşiyorsa (ör. ekipte aynı adı taşıyan iki kişi varsa) null döner —
        //belirsiz durumda rastgele/ilk bulunan kişiye yanlış atama yapmaktansa görev sahipsiz bırakılır
        public static Guid? EslesenKullaniciId(List<(Guid Id, string Ad, string Soyad)> ekipUyeleri, string? atananAdi)
        {
            if (string.IsNullOrWhiteSpace(atananAdi)) return null;

            var turkce = new CultureInfo("tr-TR");
            var aranan = atananAdi.Trim().ToLower(turkce);

            // 1) En güvenilir: "Ad Soyad" birebir eşleşmesi
            var tamEslesenler = ekipUyeleri.Where(u => $"{u.Ad} {u.Soyad}".ToLower(turkce) == aranan).ToList();
            if (tamEslesenler.Count == 1) return tamEslesenler[0].Id;
            if (tamEslesenler.Count > 1) return null;

            // 2) Sadece ad ya da sadece soyad birebir eşleşmesi — TEK kişide geçerliyse kullanılır;
            //    ekipte aynı adı/soyadı taşıyan birden fazla kişi varsa (ör. iki "Batuhan") belirsizdir
            var kismiEslesenler = ekipUyeleri
                .Where(u => u.Ad.ToLower(turkce) == aranan || u.Soyad.ToLower(turkce) == aranan)
                .ToList();
            if (kismiEslesenler.Count == 1) return kismiEslesenler[0].Id;
            if (kismiEslesenler.Count > 1) return null;

            // 3) En gevşek eşleşme (serbest metin içinde geçiyor mu) — yine sadece TEK kişide geçerliyse
            var genisEslesenler = ekipUyeleri
                .Where(u => $"{u.Ad} {u.Soyad}".ToLower(turkce).Contains(aranan) || aranan.Contains(u.Ad.ToLower(turkce)))
                .ToList();
            if (genisEslesenler.Count == 1) return genisEslesenler[0].Id;

            return null;
        }
    }
}
