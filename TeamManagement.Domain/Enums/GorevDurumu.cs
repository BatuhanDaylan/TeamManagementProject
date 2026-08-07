namespace TeamManagement.Domain.Enums
{
    public enum GorevDurumu
    {
        Backlog = 0,
        DevamEdiyor = 1,
        Test = 2,
        OnayBekliyor = 3,
        Tamamlandi = 4,
        //Haftalik mail parser veya lider tarafindan iptal edilen gorevler icin
        Iptal = 5
    }
}
