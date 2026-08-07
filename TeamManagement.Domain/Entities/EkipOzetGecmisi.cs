namespace TeamManagement.Domain.Entities
{
    //Gönderilen her haftalık ekip özetinin bir kopyası, uygulama içinde daha sonra
    //incelenebilmesi için burada arşivlenir (mail sadece anlık bildirim amaçlıdır)
    public class EkipOzetGecmisi : BaseEntity
    {
        public string Icerik { get; set; } = string.Empty;

        //ikincil anahtar
        public Guid EkipId { get; set; }
        public Ekip Ekip { get; set; }
    }
}
