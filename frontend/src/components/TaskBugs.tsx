import React, { useState, useEffect } from "react";
import api from "../api/axios";
import type { Result, HataDto } from "../types";
import { Button } from "./Button";
import { Input } from "./Input";
import { Badge } from "./Badge";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";
import { Bug, Plus, Clock, CheckCircle2, RotateCcw } from "lucide-react";

export function TaskBugs({ gorevId }: { gorevId: string }) {
  const [hatalar, setHatalar] = useState<HataDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [isAdding, setIsAdding] = useState(false);
  const [modul, setModul] = useState("");
  const [aciklama, setAciklama] = useState("");
  const [siddet, setSiddet] = useState("Orta");
  const [saving, setSaving] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");

  useEffect(() => {
    fetchHatalar();
  }, [gorevId]);

  const fetchHatalar = async () => {
    try {
      const res = await api.get<Result<HataDto[]>>(`/Hata/gorev/${gorevId}`);
      if (res.data.isSuccess && res.data.data) {
        setHatalar(res.data.data);
      }
    } catch (error) {
      console.error("Hatalar alınamadı:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleDurumDegistir = async (hata: HataDto) => {
    const yeniDurum = hata.durum === "Acik" ? "Cozuldu" : "Acik";
    //İyimser güncelleme: sunucu yanıtını beklemeden liste hemen güncellenir
    setHatalar((prev) =>
      prev.map((h) => (h.id === hata.id ? { ...h, durum: yeniDurum } : h)),
    );
    try {
      await api.patch(`/Hata/${hata.id}/durum`, { yeniDurum });
    } catch (error) {
      console.error("Hata durumu güncellenemedi:", error);
      //Başarısız olursa eski duruma geri dön
      setHatalar((prev) =>
        prev.map((h) => (h.id === hata.id ? { ...h, durum: hata.durum } : h)),
      );
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!modul.trim() || !aciklama.trim()) return;

    setSaving(true);
    setErrorMsg("");
    try {
      await api.post("/Hata/kaydet", {
        gorevId,
        modul,
        aciklama,
        siddet,
      });
      setIsAdding(false);
      setModul("");
      setAciklama("");
      setSiddet("Orta");
      fetchHatalar();
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error || "Hata kaydedilirken bir sorun oluştu.",
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="flex flex-col h-[400px] border rounded-lg bg-gray-50/50 dark:bg-gray-900/50 dark:border-gray-800">
      <div className="p-3 border-b dark:border-gray-800 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300">
          <Bug className="h-4 w-4 text-red-500" /> Hata Takibi ({hatalar.length}
          )
        </div>
        {!isAdding && (
          <Button
            variant="outline"
            size="sm"
            className="h-7 text-xs"
            onClick={() => setIsAdding(true)}
          >
            <Plus className="h-3 w-3 mr-1" /> Yeni Hata Bildir
          </Button>
        )}
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {isAdding ? (
          <div className="bg-white dark:bg-gray-800 p-4 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm">
            <h4 className="text-sm font-medium mb-3">Yeni Hata Kaydı</h4>
            {errorMsg && (
              <div className="text-xs text-red-500 mb-2">{errorMsg}</div>
            )}
            <form onSubmit={handleSave} className="space-y-3">
              <div>
                <label className="text-xs font-medium text-gray-700 dark:text-gray-300">
                  Modül/Bileşen Adı
                </label>
                <Input
                  required
                  value={modul}
                  onChange={(e) => setModul(e.target.value)}
                  placeholder="Örn: Login Ekranı"
                  className="h-8 text-sm mt-1"
                />
              </div>
              <div>
                <label className="text-xs font-medium text-gray-700 dark:text-gray-300">
                  Şiddet (Öncelik)
                </label>
                <select
                  value={siddet}
                  onChange={(e) => setSiddet(e.target.value)}
                  className="mt-1 flex h-8 w-full rounded-md border border-gray-300 bg-white px-2 py-1 text-sm focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900"
                >
                  <option value="Dusuk">Düşük</option>
                  <option value="Orta">Orta</option>
                  <option value="Yuksek">Yüksek</option>
                  <option value="Kritik">Kritik</option>
                </select>
              </div>
              <div>
                <label className="text-xs font-medium text-gray-700 dark:text-gray-300">
                  Açıklama / Nasıl Tekrar Üretilir?
                </label>
                <textarea
                  required
                  value={aciklama}
                  onChange={(e) => setAciklama(e.target.value)}
                  className="w-full rounded-md border border-gray-300 bg-white px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white mt-1 h-20"
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setIsAdding(false)}
                >
                  İptal
                </Button>
                <Button type="submit" size="sm" disabled={saving}>
                  {saving ? "Kaydediliyor..." : "Kaydet"}
                </Button>
              </div>
            </form>
          </div>
        ) : loading && hatalar.length === 0 ? (
          <div className="text-center text-sm text-gray-500 dark:text-gray-400 py-4">
            Yükleniyor...
          </div>
        ) : hatalar.length === 0 ? (
          <div className="text-center text-sm text-gray-500 dark:text-gray-400 py-8 flex flex-col items-center">
            <div className="bg-gray-100 dark:bg-gray-800 rounded-full p-3 mb-2">
              <Bug className="h-6 w-6 text-gray-400" />
            </div>
            Bu göreve ait kayıtlı hata bulunmuyor.
          </div>
        ) : (
          hatalar.map((hata) => {
            const cozuldu = hata.durum === "Cozuldu";
            return (
              <div
                key={hata.id}
                className={`bg-white dark:bg-gray-800 border rounded-lg p-3 shadow-sm transition-colors ${
                  cozuldu
                    ? "border-gray-200 dark:border-gray-700 opacity-60"
                    : "border-gray-200 dark:border-gray-700 hover:border-red-300 dark:hover:border-red-900/50"
                }`}
              >
                <div className="flex items-start justify-between mb-2">
                  <div className="flex items-center gap-2 flex-wrap">
                    <Badge
                      variant={
                        hata.siddet === "Kritik"
                          ? "danger"
                          : hata.siddet === "Yuksek"
                            ? "warning"
                            : "default"
                      }
                    >
                      {hata.siddet}
                    </Badge>
                    <Badge variant={cozuldu ? "success" : "default"}>
                      {cozuldu ? "Çözüldü" : "Açık"}
                    </Badge>
                    <span
                      className={`text-sm font-semibold text-gray-900 dark:text-white ${cozuldu ? "line-through" : ""}`}
                    >
                      {hata.modul}
                    </span>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <div className="flex items-center text-[10px] text-gray-400">
                      <Clock className="w-3 h-3 mr-1" />
                      {format(parseISO(hata.olusturulmaTarihi), "dd MMM HH:mm", {
                        locale: tr,
                      })}
                    </div>
                    <button
                      onClick={() => handleDurumDegistir(hata)}
                      className={`p-1 rounded-full transition-colors ${
                        cozuldu
                          ? "text-gray-400 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20"
                          : "text-gray-400 hover:text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20"
                      }`}
                      title={cozuldu ? "Tekrar aç" : "Çözüldü olarak işaretle"}
                    >
                      {cozuldu ? (
                        <RotateCcw className="w-4 h-4" />
                      ) : (
                        <CheckCircle2 className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                </div>
                <p className="text-sm text-gray-600 dark:text-gray-300 whitespace-pre-wrap">
                  {hata.aciklama}
                </p>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
