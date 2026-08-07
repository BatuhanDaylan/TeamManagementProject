import { useState, useEffect } from "react";
import api from "../api/axios";
import { useAuthStore } from "../store/authStore";
import type {
  Result,
  HataIstatistikDto,
  KisiHataIstatistikDto,
  UyeGorevOzetiDto,
  EkipOzetiGecmisDto,
} from "../types";
import { BarChart3, Users, Sparkles, ChevronDown, ChevronUp, Archive, Trash2 } from "lucide-react";
import { Button } from "../components/Button";
import { Badge } from "../components/Badge";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";

export default function Raporlar() {
  const { user } = useAuthStore();
  const [modulIstatistik, setModulIstatistik] = useState<HataIstatistikDto[]>(
    [],
  );
  const [kisiIstatistik, setKisiIstatistik] = useState<KisiHataIstatistikDto[]>(
    [],
  );
  const [members, setMembers] = useState<UyeGorevOzetiDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [ozetGonderiliyor, setOzetGonderiliyor] = useState(false);
  const [ozetMesaj, setOzetMesaj] = useState<{ tip: "basari" | "hata"; metin: string } | null>(null);
  const [gecmisOzetler, setGecmisOzetler] = useState<EkipOzetiGecmisDto[]>([]);
  const [acikOzetId, setAcikOzetId] = useState<string | null>(null);

  const fetchGecmisOzetler = async () => {
    try {
      const res = await api.get<Result<EkipOzetiGecmisDto[]>>("/Ozet/gecmis");
      if (res.data.isSuccess && res.data.data) {
        setGecmisOzetler(res.data.data);
      }
    } catch (error) {
      console.error("Geçmiş özetler alınamadı:", error);
    }
  };

  const handleOzetSil = async (ozetId: string) => {
    if (!confirm("Bu özeti kalıcı olarak silmek istediğinize emin misiniz?")) return;
    try {
      await api.delete(`/Ozet/gecmis/${ozetId}`);
      setGecmisOzetler((prev) => prev.filter((o) => o.id !== ozetId));
      if (acikOzetId === ozetId) setAcikOzetId(null);
    } catch (err: any) {
      setOzetMesaj({
        tip: "hata",
        metin: err.response?.data?.error || "Özet silinirken bir hata oluştu.",
      });
    }
  };

  const handleOzetGonder = async () => {
    setOzetGonderiliyor(true);
    setOzetMesaj(null);
    try {
      await api.post("/Ozet/gonder");
      setOzetMesaj({ tip: "basari", metin: "Haftalık özet mail olarak gönderildi." });
      fetchGecmisOzetler();
    } catch (err: any) {
      setOzetMesaj({
        tip: "hata",
        metin: err.response?.data?.error || "Özet gönderilirken bir hata oluştu.",
      });
    } finally {
      setOzetGonderiliyor(false);
    }
  };

  useEffect(() => {
    const fetchIstatistikler = async () => {
      if (!user?.ekipId || user?.rol !== "Lider") return;

      try {
        const [modulRes, kisiRes, ekipRes, gecmisRes] = await Promise.all([
          api.get<Result<HataIstatistikDto[]>>(
            `/Hata/istatistik/modul/${user.ekipId}`,
          ),
          api.get<Result<KisiHataIstatistikDto[]>>(
            `/Hata/istatistik/kisi/${user.ekipId}`,
          ),
          api.get<Result<UyeGorevOzetiDto[]>>(`/Dashboard/ekip/${user.ekipId}`),
          api.get<Result<EkipOzetiGecmisDto[]>>("/Ozet/gecmis"),
        ]);

        if (modulRes.data.isSuccess && modulRes.data.data) {
          setModulIstatistik(modulRes.data.data);
        }
        if (kisiRes.data.isSuccess && kisiRes.data.data) {
          setKisiIstatistik(kisiRes.data.data);
        }
        if (ekipRes.data.isSuccess && ekipRes.data.data) {
          setMembers(ekipRes.data.data);
        }
        if (gecmisRes.data.isSuccess && gecmisRes.data.data) {
          setGecmisOzetler(gecmisRes.data.data);
        }
      } catch (error) {
        console.error("İstatistikler alınamadı:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchIstatistikler();
  }, [user]);

  if (loading) {
    return (
      <div className="p-8 text-center text-gray-500 dark:text-gray-400">
        Yükleniyor...
      </div>
    );
  }

  if (user?.rol !== "Lider") {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center space-y-4">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-amber-100 dark:bg-amber-900/30">
            <BarChart3 className="h-8 w-8 text-amber-600 dark:text-amber-400" />
          </div>
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Yetkisiz Erişim
          </h2>
          <p className="text-gray-500 dark:text-gray-400">
            Ekip raporlarını sadece ekip liderleri görüntüleyebilir.
          </p>
        </div>
      </div>
    );
  }

  const maxModulHata = Math.max(...modulIstatistik.map((m) => m.hataSayisi), 1);
  const maxKisiHata = Math.max(...kisiIstatistik.map((k) => k.hataSayisi), 1);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
          Ekip Raporları
        </h1>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Projedeki hata (bug) dağılımları ve ekip performansı.
        </p>
      </div>

      <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm p-5">
        <div className="flex items-center gap-2 mb-4 pb-3 border-b border-gray-100 dark:border-gray-800">
          <div className="p-2 bg-indigo-50 dark:bg-indigo-900/30 rounded-lg text-indigo-600 dark:text-indigo-400">
            <Sparkles className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
              Haftalık Ekip Özeti
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Kişi bazlı çalışma özeti ve hata/yorum analizi kendi e-postanıza gönderilir. Ayrıca
              her Cuma otomatik olarak da gönderilir.
            </p>
          </div>
        </div>

        {ozetMesaj && (
          <div
            className={`text-sm p-3 rounded-md mb-4 ${
              ozetMesaj.tip === "basari"
                ? "text-green-600 bg-green-50 dark:bg-green-900/30 dark:text-green-400"
                : "text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400"
            }`}
          >
            {ozetMesaj.metin}
          </div>
        )}

        <Button onClick={handleOzetGonder} disabled={ozetGonderiliyor}>
          <Sparkles className="w-4 h-4 mr-2" />
          {ozetGonderiliyor ? "Hazırlanıyor ve gönderiliyor..." : "Özet Gönder"}
        </Button>
      </div>

      {/* Geçmiş Özetler Arşivi */}
      <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm p-5">
        <div className="flex items-center gap-2 mb-4 pb-3 border-b border-gray-100 dark:border-gray-800">
          <div className="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg text-gray-600 dark:text-gray-400">
            <Archive className="w-5 h-5" />
          </div>
          <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
            Geçmiş Özetler
          </h2>
          <Badge>{gecmisOzetler.length}</Badge>
        </div>

        {gecmisOzetler.length === 0 ? (
          <div className="text-sm text-gray-500 text-center py-4">
            Henüz gönderilmiş bir özet yok.
          </div>
        ) : (
          <div className="divide-y divide-gray-100 dark:divide-gray-800">
            {gecmisOzetler.map((ozet) => {
              const acik = acikOzetId === ozet.id;
              return (
                <div key={ozet.id} className="py-3">
                  <div className="w-full flex items-center justify-between gap-2">
                    <button
                      onClick={() => setAcikOzetId(acik ? null : ozet.id)}
                      className="flex-1 flex items-center justify-between text-left"
                    >
                      <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        {format(parseISO(ozet.olusturulmaTarihi), "dd MMMM yyyy, HH:mm", { locale: tr })}
                      </span>
                      {acik ? (
                        <ChevronUp className="w-4 h-4 text-gray-400 shrink-0" />
                      ) : (
                        <ChevronDown className="w-4 h-4 text-gray-400 shrink-0" />
                      )}
                    </button>
                    <button
                      onClick={() => handleOzetSil(ozet.id)}
                      title="Özeti sil"
                      className="shrink-0 p-1.5 rounded-md text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/30 dark:hover:text-red-400 transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                  {acik && (
                    <div className="mt-2 text-sm text-gray-600 dark:text-gray-400 whitespace-pre-wrap bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3">
                      {ozet.icerik}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Modül Bazlı Hatalar */}
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-5 shadow-sm">
          <div className="flex items-center gap-2 mb-6 border-b border-gray-100 dark:border-gray-800 pb-3">
            <div className="p-2 bg-indigo-50 dark:bg-indigo-900/30 rounded-lg text-indigo-600 dark:text-indigo-400">
              <BarChart3 className="w-5 h-5" />
            </div>
            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
              Modül Bazlı Hatalar
            </h2>
          </div>

          <div className="space-y-4">
            {modulIstatistik.length === 0 ? (
              <div className="text-sm text-gray-500 text-center py-4">
                Kayıtlı hata bulunmuyor.
              </div>
            ) : (
              modulIstatistik.map((stat, idx) => {
                const yuzde = (stat.hataSayisi / maxModulHata) * 100;
                return (
                  <div key={idx}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="font-medium text-gray-700 dark:text-gray-300">
                        {stat.modul}
                      </span>
                      <span className="text-gray-500">
                        {stat.hataSayisi} hata
                      </span>
                    </div>
                    <div className="w-full bg-gray-100 dark:bg-gray-800 rounded-full h-2.5">
                      <div
                        className="bg-indigo-500 h-2.5 rounded-full transition-all duration-500"
                        style={{ width: `${yuzde}%` }}
                      ></div>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* Kişi Bazlı Hatalar */}
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-5 shadow-sm">
          <div className="flex items-center gap-2 mb-6 border-b border-gray-100 dark:border-gray-800 pb-3">
            <div className="p-2 bg-rose-50 dark:bg-rose-900/30 rounded-lg text-rose-600 dark:text-rose-400">
              <Users className="w-5 h-5" />
            </div>
            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
              Kişi Bazlı Hatalar (Sorumlu)
            </h2>
          </div>

          <div className="space-y-4">
            {kisiIstatistik.length === 0 ? (
              <div className="text-sm text-gray-500 text-center py-4">
                Kayıtlı hata bulunmuyor.
              </div>
            ) : (
              kisiIstatistik.map((stat, idx) => {
                const uye = members.find(
                  (m) => m.kullaniciId === stat.kullaniciId,
                );
                const uyeAd = uye ? `${uye.ad} ${uye.soyad}` : "Bilinmeyen Üye";
                const yuzde = (stat.hataSayisi / maxKisiHata) * 100;

                return (
                  <div key={idx}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="font-medium text-gray-700 dark:text-gray-300">
                        {uyeAd}
                      </span>
                      <span className="text-gray-500">
                        {stat.hataSayisi} hata
                      </span>
                    </div>
                    <div className="w-full bg-gray-100 dark:bg-gray-800 rounded-full h-2.5">
                      <div
                        className="bg-rose-500 h-2.5 rounded-full transition-all duration-500"
                        style={{ width: `${yuzde}%` }}
                      ></div>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
