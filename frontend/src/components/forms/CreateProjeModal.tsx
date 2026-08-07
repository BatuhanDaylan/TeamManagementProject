import React, { useState } from "react";
import { Modal } from "../Modal";
import { Button } from "../Button";
import { Input } from "../Input";
import api from "../../api/axios";
import { useAuthStore } from "../../store/authStore";

export function CreateProjeModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const [ad, setAd] = useState("");
  const [aciklama, setAciklama] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const { user } = useAuthStore();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user?.ekipId) return;
    setLoading(true);
    setErrorMsg("");
    try {
      await api.post("/Proje/olustur", { ad, aciklama, ekipId: user.ekipId });
      onClose();
      setAd("");
      setAciklama("");
      window.location.reload(); // Şimdilik sayfayı yeniliyoruz, ileride state'e bağlanabilir
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error || "Proje eklenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Yeni Proje Ekle">
      <form onSubmit={handleSubmit} className="space-y-4">
        {errorMsg && (
          <div className="text-sm text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400 p-3 rounded-md">
            {errorMsg}
          </div>
        )}

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Proje Adı
          </label>
          <Input
            required
            value={ad}
            onChange={(e) => setAd(e.target.value)}
            placeholder="Örn: Yeni Web Sitesi"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Açıklama
          </label>
          <textarea
            value={aciklama}
            onChange={(e) => setAciklama(e.target.value)}
            placeholder="İsteğe bağlı açıklama"
            className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white min-h-[100px]"
          />
        </div>

        <div className="flex justify-end gap-2 mt-6">
          <Button type="button" variant="ghost" onClick={onClose}>
            İptal
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? "Ekleniyor..." : "Ekle"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
