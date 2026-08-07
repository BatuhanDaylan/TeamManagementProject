using System;
using System.Collections.Generic;
using TeamManagement.Domain.Enums;

namespace TeamManagement.Domain.Entities
{
    public class Kullanici : BaseEntity
    {
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Email { get; set; }
        public string SifreHash { get; set; }
        public Rol Rol { get; set; } //Enum

        //ikincil anahtar
        public Guid? EkipId { get; set; }

        //Haftalık plan mailini kendi Gönderilenler klasöründen okuyabilmek için lider'in girdiği
        //şifrelenmiş IMAP uygulama şifresi ve kendi Gemini API anahtarı, ikisi de null ise özellik kapalı sayılır
        public string? MailUygulamaSifresiSifreli { get; set; }
        public string? AiApiKeySifreli { get; set; }

        //Gönderilenler klasöründe artımlı tarama için son işlenen mail işaretçisi
        //(IMAP \Seen bayrağı Sent klasöründe güvenilir değil, kullanıcı kendi gönderdiği postayı zaten okunmuş görür)
        public long? SonIslenenMailUidValidity { get; set; }
        public long? SonIslenenMailUid { get; set; }

        //Liderin kendi Gelen Kutusu'nda (üyelerden gelen durum güncelleme mailleri için) artımlı
        //tarama yapabilmek için ayrı bir işaretçi; Gönderilenler klasörüyle karışmasın diye farklı alan
        public long? SonIslenenInboxUidValidity { get; set; }
        public long? SonIslenenInboxUid { get; set; }

        //Şifre sıfırlama talebi token'ı ve son kullanma tarihi (talep yoksa ikisi de null)
        public string? SifreSifirlamaToken { get; set; }
        public DateTime? SifreSifirlamaSonKullanma { get; set; }

        //navigasyon
        public Ekip Ekip { get; set; }
    }
}