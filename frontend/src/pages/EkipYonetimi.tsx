import { useEffect, useState } from "react";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import type { Result, UyeGorevOzetiDto, ProjeDto } from "../types";
import { Users, Mail, Clock, RefreshCw, Folder, Trash2, UserMinus, Pencil, Check, X, UserPlus } from "lucide-react";
import { Badge } from "../components/Badge";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { DavetGonderModal } from "../components/forms/DavetGonderModal";
import { CreateUyeModal } from "../components/forms/CreateUyeModal";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";

interface DavetListeDto {
  id: string;
  email: string;
  sonKullanmaTarihi: string;
  kullanildi: boolean;
  ekipId: string;
}

export default function EkipYonetimi() {
  const { user } = useAuthStore();
  const [uyeler, setUyeler] = useState<UyeGorevOzetiDto[]>([]);
  const [davetler, setDavetler] = useState<DavetListeDto[]>([]);
  const [projeler, setProjeler] = useState<ProjeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isDavetModalOpen, setDavetModalOpen] = useState(false);
  const [isUyeModalOpen, setUyeModalOpen] = useState(false);
  const [editingProjeId, setEditingProjeId] = useState<string | null>(null);
  const [editAd, setEditAd] = useState("");
  const [editAciklama, setEditAciklama] = useState("");

  const fetchData = async () => {
    setLoading(true);
    try {
      if (!user?.ekipId) return;

      const [uyelerRes, davetlerRes, projelerRes] = await Promise.all([
        api.get<Result<UyeGorevOzetiDto[]>>(`/Dashboard/ekip/${user.ekipId}`),
        api.get<Result<DavetListeDto[]>>(`/Davet/liste/${user.ekipId}`),
        api.get<Result<ProjeDto[]>>(`/Proje/ekip/${user.ekipId}`),
      ]);

      if (uyelerRes.data.isSuccess && uyelerRes.data.data) {
        setUyeler(uyelerRes.data.data);
      }
      if (davetlerRes.data.isSuccess && davetlerRes.data.data) {
        setDavetler(davetlerRes.data.data);
      }
      if (projelerRes.data.isSuccess && projelerRes.data.data) {
        setProjeler(projelerRes.data.data);
      }
    } catch (error) {
      console.error("Veriler alınırken hata oluştu:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [user?.ekipId]);

  const handleProjeSil = async (projeId: string, projeAd: string) => {
    if (
      !confirm(
        `'${projeAd}' projesini ve içindeki TÜM görevleri silmek istediğinize emin misiniz? Bu işlem geri alınamaz.`,
      )
    )
      return;

    try {
      await api.delete(`/Proje/sil/${projeId}`);
      fetchData();
    } catch (error) {
      console.error("Proje silinemedi:", error);
      alert("Proje silinirken bir hata oluştu.");
    }
  };

  const handleProjeDuzenleStart = (proje: ProjeDto) => {
    setEditingProjeId(proje.id);
    setEditAd(proje.ad);
    setEditAciklama(proje.aciklama || "");
  };

  const handleProjeDuzenleIptal = () => {
    setEditingProjeId(null);
    setEditAd("");
    setEditAciklama("");
  };

  const handleProjeDuzenleKaydet = async (projeId: string) => {
    if (!editAd.trim()) return;
    try {
      await api.put(`/Proje/${projeId}`, { ad: editAd, aciklama: editAciklama });
      setEditingProjeId(null);
      fetchData();
    } catch (error: any) {
      console.error("Proje güncellenemedi:", error);
      alert(error?.response?.data?.error || "Proje güncellenirken bir hata oluştu.");
    }
  };

  const handleDavetSil = async (davetId: string, email: string) => {
    if (!confirm(`'${email}' adresine gönderilen daveti silmek istediğinize emin misiniz?`))
      return;

    try {
      await api.delete(`/Davet/${davetId}`);
      fetchData();
    } catch (error) {
      console.error("Davet silinemedi:", error);
      alert("Davet silinirken bir hata oluştu.");
    }
  };

  const handleUyeCikar = async (uyeId: string, adSoyad: string) => {
    if (
      !confirm(
        `'${adSoyad}' adlı üyeyi ekipten çıkarmak istediğinize emin misiniz? Üzerindeki görevler sahipsiz kalacak.`,
      )
    )
      return;

    try {
      await api.delete(`/Ekip/uye/${uyeId}`);
      fetchData();
    } catch (error: any) {
      console.error("Üye çıkarılamadı:", error);
      alert(error?.response?.data?.error || "Üye çıkarılırken bir hata oluştu.");
    }
  };

  const handleEkipSil = async () => {
    if (!user?.ekipId) return;
    if (
      !confirm(
        "Ekibi silmek, tüm üyeleri, projeleri ve görevleri kalıcı olarak kaldıracaktır. Devam etmek istiyor musunuz?",
      )
    )
      return;

    try {
      await api.delete(`/Ekip/sil/${user.ekipId}`);
      alert("Ekip başarıyla silindi. Oturum kapatılıyor.");
      window.location.href = "/login";
    } catch (error: any) {
      console.error("Ekip silinemedi:", error);
      alert(error?.response?.data?.error || "Ekip silinirken bir hata oluştu.");
    }
  };

  if (user?.rol !== "Lider") {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center space-y-4">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/30">
            <Users className="h-8 w-8 text-red-600 dark:text-red-400" />
          </div>
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Yetkisiz Erişim
          </h2>
          <p className="text-gray-500 dark:text-gray-400">
            Bu sayfayı sadece ekip liderleri görüntüleyebilir.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <DavetGonderModal
        isOpen={isDavetModalOpen}
        onClose={() => {
          setDavetModalOpen(false);
          fetchData(); // Davet sonrası listeyi yenile
        }}
      />
      <CreateUyeModal
        isOpen={isUyeModalOpen}
        onClose={() => {
          setUyeModalOpen(false);
          fetchData();
        }}
      />

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
            Ekip Yönetimi
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Ekibinizdeki üyeleri ve gönderilen davetleri yönetin.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="outline" onClick={fetchData} title="Yenile">
            <RefreshCw className={`h-4 w-4 ${loading ? "animate-spin" : ""}`} />
          </Button>
          <Button variant="outline" onClick={() => setUyeModalOpen(true)}>
            <UserPlus className="mr-2 h-4 w-4" /> Kullanıcı Kaydet
          </Button>
          <Button onClick={() => setDavetModalOpen(true)}>
            <Mail className="mr-2 h-4 w-4" /> Yeni Davet Gönder
          </Button>
          <Button
            variant="outline"
            className="text-red-600 border-red-200 hover:bg-red-50 dark:text-red-400 dark:border-red-900/50 dark:hover:bg-red-900/20"
            onClick={handleEkipSil}
            title="Ekibi kalıcı olarak sil"
          >
            <Trash2 className="mr-2 h-4 w-4" /> Ekibi Sil
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Üyeler Listesi */}
        <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="border-b border-gray-200 px-6 py-4 dark:border-gray-800">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                <Users className="h-5 w-5 text-indigo-500" /> Mevcut Üyeler
              </h2>
              <Badge>{uyeler.length}</Badge>
            </div>
          </div>
          <div className="divide-y divide-gray-100 dark:divide-gray-800">
            {loading && uyeler.length === 0 ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</div>
            ) : uyeler.length === 0 ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                Henüz ekibinizde üye bulunmuyor.
              </div>
            ) : (
              //Lider listede her zaman en üstte gösterilir
              [...uyeler]
                .sort((a, b) => (a.rol === "Lider" ? -1 : b.rol === "Lider" ? 1 : 0))
                .map((uye) => (
                <div
                  key={uye.kullaniciId}
                  className="flex items-center justify-between px-6 py-4 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                >
                  <div className="flex items-center gap-4">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 font-bold dark:bg-indigo-900/50 dark:text-indigo-400">
                      {uye.ad.charAt(0)}
                      {uye.soyad.charAt(0)}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-900 dark:text-white">
                        {uye.ad} {uye.soyad}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        {uye.email}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        Aktif Görev: {uye.aktifGorevler?.length || 0}
                      </p>
                    </div>
                  </div>
                  {uye.kullaniciId === user.id ? (
                    <Badge
                      variant="default"
                      className="bg-indigo-50 text-indigo-700"
                    >
                      Sen (Lider)
                    </Badge>
                  ) : (
                    <button
                      onClick={() =>
                        handleUyeCikar(uye.kullaniciId, `${uye.ad} ${uye.soyad}`)
                      }
                      className="text-red-400 hover:text-red-600 dark:text-red-500 dark:hover:text-red-400 transition-colors p-1"
                      title="Ekipten çıkar"
                    >
                      <UserMinus className="h-4 w-4" />
                    </button>
                  )}
                </div>
              ))
            )}
          </div>
        </div>

        {/* Bekleyen Davetler */}
        <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="border-b border-gray-200 px-6 py-4 dark:border-gray-800">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                <Mail className="h-5 w-5 text-amber-500" /> Gönderilen Davetler
              </h2>
              <Badge variant="warning">
                {davetler.filter((d) => !d.kullanildi).length}
              </Badge>
            </div>
          </div>
          <div className="divide-y divide-gray-100 dark:divide-gray-800 max-h-[500px] overflow-y-auto">
            {loading && davetler.length === 0 ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</div>
            ) : davetler.length === 0 ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                Henüz davet göndermediniz.
              </div>
            ) : (
              davetler.map((davet) => {
                const isExpired =
                  new Date(davet.sonKullanmaTarihi) < new Date();
                return (
                  <div
                    key={davet.id}
                    className="flex items-center justify-between px-6 py-4 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                  >
                    <div>
                      <p className="text-sm font-medium text-gray-900 dark:text-white">
                        {davet.email}
                      </p>
                      <div className="flex items-center gap-2 mt-1 text-xs text-gray-500 dark:text-gray-400">
                        <Clock className="h-3 w-3" />
                        <span>
                          Bitiş:{" "}
                          {format(
                            parseISO(davet.sonKullanmaTarihi),
                            "dd MMM yyyy HH:mm",
                            { locale: tr },
                          )}
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      {davet.kullanildi ? (
                        <Badge variant="success">Kabul Edildi</Badge>
                      ) : (
                        <>
                          {isExpired ? (
                            <Badge variant="danger">Süresi Doldu</Badge>
                          ) : (
                            <Badge variant="warning">Bekliyor</Badge>
                          )}
                          <button
                            onClick={() => handleDavetSil(davet.id, davet.email)}
                            className="text-red-400 hover:text-red-600 dark:text-red-500 dark:hover:text-red-400 transition-colors p-1"
                            title="Daveti sil"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      </div>

      {/* Projeler Listesi (Alt Kısım, Tam Genişlik) */}
      <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 mt-6">
        <div className="border-b border-gray-200 px-6 py-4 dark:border-gray-800">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
              <Folder className="h-5 w-5 text-indigo-500" /> Ekip Projeleri
            </h2>
            <Badge>{projeler.length}</Badge>
          </div>
        </div>
        <div className="p-6">
          {loading && projeler.length === 0 ? (
            <div className="text-center text-gray-500 dark:text-gray-400 py-4">Yükleniyor...</div>
          ) : projeler.length === 0 ? (
            <div className="text-center text-gray-500 dark:text-gray-400 py-8">
              Henüz hiçbir proje oluşturmadınız. "Yeni Proje" butonu ile
              oluşturabilirsiniz.
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {projeler.map((proje) => {
                const isEditing = editingProjeId === proje.id;
                return (
                  <div
                    key={proje.id}
                    className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:border-indigo-300 dark:hover:border-indigo-700 transition-colors bg-gray-50/50 dark:bg-gray-800/30"
                  >
                    {isEditing ? (
                      <div className="space-y-2">
                        <Input
                          value={editAd}
                          onChange={(e) => setEditAd(e.target.value)}
                          className="h-8 text-sm font-semibold"
                          placeholder="Proje adı"
                          autoFocus
                        />
                        <textarea
                          value={editAciklama}
                          onChange={(e) => setEditAciklama(e.target.value)}
                          className="w-full rounded-md border border-gray-300 bg-white px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white h-16"
                          placeholder="Açıklama"
                        />
                        <div className="flex justify-end gap-1">
                          <button
                            onClick={handleProjeDuzenleIptal}
                            className="p-1.5 rounded text-gray-500 hover:bg-gray-200 dark:hover:bg-gray-700"
                            title="İptal"
                          >
                            <X className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => handleProjeDuzenleKaydet(proje.id)}
                            className="p-1.5 rounded text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20"
                            title="Kaydet"
                          >
                            <Check className="h-4 w-4" />
                          </button>
                        </div>
                      </div>
                    ) : (
                      <>
                        <div className="flex justify-between items-start mb-2">
                          <h3
                            className="font-semibold text-gray-900 dark:text-white line-clamp-1"
                            title={proje.ad}
                          >
                            {proje.ad}
                          </h3>
                          <div className="flex items-center gap-1 shrink-0">
                            <button
                              onClick={() => handleProjeDuzenleStart(proje)}
                              className="text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors p-1"
                              title="Projeyi düzenle"
                            >
                              <Pencil className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => handleProjeSil(proje.id, proje.ad)}
                              className="text-red-400 hover:text-red-600 dark:text-red-500 dark:hover:text-red-400 transition-colors p-1"
                              title="Proje ve içindeki tüm görevleri sil"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        </div>
                        <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2 mb-4 h-10">
                          {proje.aciklama || "Açıklama yok."}
                        </p>
                        <div className="text-xs text-gray-400 flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          {proje.olusturulmaTarihi
                            ? format(
                                parseISO(proje.olusturulmaTarihi),
                                "dd MMM yyyy",
                                { locale: tr },
                              )
                            : "Tarih yok"}
                        </div>
                      </>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
