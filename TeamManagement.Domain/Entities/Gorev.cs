using System;
using System.Collections.Generic;
using TeamManagement.Domain.Enums;

namespace TeamManagement.Domain.Entities
{
    public class Gorev : BaseEntity
    {
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public GorevDurumu Durum { get; set; }
        public string Oncelik { get; set; }
        //Gorev zaman araligi timeline ve dashboard hesaplamalari icin kullanilir
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? Deadline { get; set; }

        //ikincil anahtarlar
        public Guid ProjeId { get; set; }
        public Proje Proje { get; set; }

        public Guid? AtananId { get; set; }
        public Kullanici AtananKullanici { get; set; }

        //Navigasyonlar
        public ICollection<Yorum> Yorumlar { get; set; }
        public ICollection<GorevIlerleme> Ilerlemeler { get; set; }
    }
}
