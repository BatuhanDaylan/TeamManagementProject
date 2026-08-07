import React, { useState } from "react";
import { Modal } from "../Modal";
import { Button } from "../Button";
import { Input } from "../Input";
import api from "../../api/axios";
import { useAuthStore } from "../../store/authStore";

export function DavetGonderModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const { user } = useAuthStore();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user?.ekipId) return;
    setLoading(true);
    setErrorMsg("");
    setSuccessMsg("");
    try {
      await api.post("/Davet/gonder", { email, ekipId: user.ekipId });
      setSuccessMsg("Davet başarıyla gönderildi.");
      setEmail("");
      setTimeout(() => {
        setSuccessMsg("");
        onClose();
      }, 2000);
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error || "Davet gönderilirken hata oluştu.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Ekibe Davet Gönder">
      <form onSubmit={handleSubmit} className="space-y-4">
        {errorMsg && (
          <div className="text-sm text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400 p-3 rounded-md">
            {errorMsg}
          </div>
        )}
        {successMsg && (
          <div className="text-sm text-green-600 bg-green-50 dark:bg-green-900/30 dark:text-green-400 p-3 rounded-md">
            {successMsg}
          </div>
        )}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            E-posta Adresi
          </label>
          <Input
            required
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="isim@sirket.com"
          />
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            Sadece takım liderleri davet gönderebilir.
          </p>
        </div>
        <div className="flex justify-end gap-2 mt-6">
          <Button type="button" variant="ghost" onClick={onClose}>
            İptal
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? "Gönderiliyor..." : "Gönder"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
