import React, { useState, useEffect } from "react";
import { Modal } from "../Modal";
import { Button } from "../Button";
import { Input } from "../Input";
import api from "../../api/axios";
import { useAuthStore } from "../../store/authStore";
import type { UyeGorevOzetiDto } from "../../types";

export function CreateGorevModal({
  isOpen,
  onClose,
  members = [],
}: {
  isOpen: boolean;
  onClose: () => void;
  members?: UyeGorevOzetiDto[];
}) {
  const { user } = useAuthStore();
  const [baslik, setBaslik] = useState("");
  const [aciklama, setAciklama] = useState("");
  const [projeId, setProjeId] = useState("");
  const [oncelik, setOncelik] = useState("Orta");
  const [atananId, setAtananId] = useState("");
  const [projeler, setProjeler] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");

  const [deadline, setDeadline] = useState("");

  useEffect(() => {
    if (isOpen) {
      if (user?.id) setAtananId(user.id);
      setBaslik("");
      setAciklama("");
      setDeadline("");
      setOncelik("Orta");
    }
  }, [isOpen, user?.id]);

  useEffect(() => {
    if (isOpen && user?.ekipId) {
      api.get(`/Proje/ekip/${user.ekipId}`).then((res) => {
        if (res.data.isSuccess) {
          setProjeler(res.data.data);
          if (res.data.data.length > 0) setProjeId(res.data.data[0].id);
        }
      });
    }
  }, [isOpen, user?.ekipId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg("");
    try {
      await api.post("/Gorev/olustur", {
        baslik,
        aciklama,
        projeId,
        oncelik,
        atananId: atananId === "atanmamis" ? null : atananId,
        deadline: deadline ? new Date(deadline).toISOString() : null,
      });
      onClose();
      setBaslik("");
      setAciklama("");
      setDeadline("");
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error || "Görev eklenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Yeni Görev Ekle">
      <form onSubmit={handleSubmit} className="space-y-4">
        {errorMsg && (
          <div className="text-sm text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400 p-3 rounded-md">
            {errorMsg}
          </div>
        )}
        {projeler.length === 0 && (
          <div className="text-sm text-amber-600 bg-amber-50 dark:bg-amber-900/30 dark:text-amber-400 p-3 rounded-md">
            Henüz ekibinizde hiçbir proje yok. Görev eklemek için önce proje
            oluşturmalısınız.
          </div>
        )}

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Görev Başlığı
          </label>
          <Input
            required
            value={baslik}
            onChange={(e) => setBaslik(e.target.value)}
            disabled={projeler.length === 0}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Açıklama
          </label>
          <textarea
            value={aciklama}
            onChange={(e) => setAciklama(e.target.value)}
            disabled={projeler.length === 0}
            className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white min-h-[100px]"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Proje
            </label>
            <select
              value={projeId}
              onChange={(e) => setProjeId(e.target.value)}
              disabled={projeler.length === 0}
              className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white"
            >
              {projeler.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.ad}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Kime Atanacak?
            </label>
            <select
              value={atananId}
              onChange={(e) => setAtananId(e.target.value)}
              disabled={projeler.length === 0}
              className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white"
            >
              <option value="atanmamis">-- Atanmamış (Boş Bırak) --</option>
              {members
                .filter(
                  (m) =>
                    m.kullaniciId !== "00000000-0000-0000-0000-000000000000",
                )
                .map((m) => (
                  <option key={m.kullaniciId} value={m.kullaniciId}>
                    {m.ad} {m.soyad}
                  </option>
                ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Öncelik
            </label>
            <select
              value={oncelik}
              onChange={(e) => setOncelik(e.target.value)}
              disabled={projeler.length === 0}
              className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white"
            >
              <option value="Dusuk">Düşük</option>
              <option value="Orta">Orta</option>
              <option value="Yuksek">Yüksek</option>
              <option value="Kritik">Kritik</option>
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Bitiş Tarihi
            </label>
            <Input
              type="datetime-local"
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              disabled={projeler.length === 0}
            />
          </div>
        </div>

        <div className="flex justify-end gap-2 mt-6">
          <Button type="button" variant="ghost" onClick={onClose}>
            İptal
          </Button>
          <Button type="submit" disabled={loading || projeler.length === 0}>
            {loading ? "Ekleniyor..." : "Ekle"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
