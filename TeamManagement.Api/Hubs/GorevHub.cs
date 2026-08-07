using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TeamManagement.Api.Hubs
{
    //SignalR Hub — gerçek zamanlı görev ve bildirim yayınları için merkezi nokta
    [Authorize]
    public class GorevHub : Hub
    {
        //İstemci belirli bir projenin gerçek zamanlı güncellemelerini almak için gruba katılır
        public async Task ProjeGrubunuKatil(string projeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"proje_{projeId}");
        }

        //İstemci projeden ayrılırken gruptan çıkarılır gereksiz yayın almasın
        public async Task ProjeGrubundanAyril(string projeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"proje_{projeId}");
        }

        //Kullanıcı kendi bildirim grubuna katılır anlık bildirim almak için
        public async Task KullaniciGrubunuKatil(string kullaniciId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"kullanici_{kullaniciId}");
        }

        //Bağlantı kesilince loglama yapılır
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
