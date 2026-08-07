import React, { useState } from "react";
import { Modal } from "../Modal";
import { Button } from "../Button";
import { Input } from "../Input";
import api from "../../api/axios";

export function CreateUyeModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const [ad, setAd] = useState("");
  const [soyad, setSoyad] = useState("");
  const [email, setEmail] = useState("");
  const [sifre, setSifre] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const [successMsg, setSuccessMsg] = useState("");

  const resetVeKapat = () => {
    setAd("");
    setSoyad("");
    setEmail("");
    setSifre("");
    setErrorMsg("");
    setSuccessMsg("");
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg("");
    setSuccessMsg("");
    try {
      await api.post("/Ekip/uye-olustur", { ad, soyad, email, sifre });
      setSuccessMsg(
        `Hesap oluşturuldu. '${email}' adresi ve belirlediğiniz şifreyle giriş yapabilirler.`,
      );
      setAd("");
      setSoyad("");
      setEmail("");
      setSifre("");
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error || "Kullanıcı oluşturulurken hata oluştu.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={resetVeKapat} title="Kullanıcı Kaydet">
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Davet e-postası göndermeden, kişinin bilgilerini ve girişte
          kullanacağı şifreyi doğrudan siz belirleyerek hesabı ekibinize
          eklersiniz.
        </p>

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

        <div className="flex gap-3">
          <div className="flex-1">
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Ad
            </label>
            <Input required value={ad} onChange={(e) => setAd(e.target.value)} />
          </div>
          <div className="flex-1">
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Soyad
            </label>
            <Input
              required
              value={soyad}
              onChange={(e) => setSoyad(e.target.value)}
            />
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            E-posta
          </label>
          <Input
            required
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="isim@sirket.com"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Şifre
          </label>
          <Input
            required
            type="password"
            minLength={6}
            value={sifre}
            onChange={(e) => setSifre(e.target.value)}
            placeholder="En az 6 karakter"
            autoComplete="new-password"
          />
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            Bu şifreyi kişiye ayrıca (sözlü, mesajla vb.) iletmeniz gerekir.
          </p>
        </div>

        <div className="flex justify-end gap-2 mt-6">
          <Button type="button" variant="ghost" onClick={resetVeKapat}>
            Kapat
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? "Kaydediliyor..." : "Kullanıcıyı Kaydet"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
