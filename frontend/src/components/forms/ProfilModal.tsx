import React, { useState, useEffect } from "react";
import { Modal } from "../Modal";
import { Button } from "../Button";
import { Input } from "../Input";
import api from "../../api/axios";
import { useAuthStore } from "../../store/authStore";
import type { User, Result } from "../../types";

export function ProfilModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const { user, updateUser } = useAuthStore();

  const [ad, setAd] = useState("");
  const [soyad, setSoyad] = useState("");
  const [profilKaydediliyor, setProfilKaydediliyor] = useState(false);
  const [profilMesaj, setProfilMesaj] = useState<{ tip: "basari" | "hata"; metin: string } | null>(null);

  const [mevcutSifre, setMevcutSifre] = useState("");
  const [yeniSifre, setYeniSifre] = useState("");
  const [sifreKaydediliyor, setSifreKaydediliyor] = useState(false);
  const [sifreMesaj, setSifreMesaj] = useState<{ tip: "basari" | "hata"; metin: string } | null>(null);

  useEffect(() => {
    if (isOpen && user) {
      setAd(user.ad);
      setSoyad(user.soyad);
      setProfilMesaj(null);
      setSifreMesaj(null);
      setMevcutSifre("");
      setYeniSifre("");
    }
  }, [isOpen, user]);

  const handleProfilKaydet = async (e: React.FormEvent) => {
    e.preventDefault();
    setProfilKaydediliyor(true);
    setProfilMesaj(null);
    try {
      const res = await api.put<Result<User>>("/Auth/profil", { ad, soyad });
      if (res.data.isSuccess && res.data.data) {
        updateUser({ ad: res.data.data.ad, soyad: res.data.data.soyad });
        setProfilMesaj({ tip: "basari", metin: "Bilgileriniz güncellendi." });
      }
    } catch (err: any) {
      setProfilMesaj({
        tip: "hata",
        metin: err.response?.data?.error || "Güncellenirken bir hata oluştu.",
      });
    } finally {
      setProfilKaydediliyor(false);
    }
  };

  const handleSifreDegistir = async (e: React.FormEvent) => {
    e.preventDefault();
    setSifreKaydediliyor(true);
    setSifreMesaj(null);
    try {
      await api.put("/Auth/sifre-degistir", { mevcutSifre, yeniSifre });
      setSifreMesaj({ tip: "basari", metin: "Şifreniz güncellendi." });
      setMevcutSifre("");
      setYeniSifre("");
    } catch (err: any) {
      setSifreMesaj({
        tip: "hata",
        metin: err.response?.data?.error || "Şifre değiştirilirken bir hata oluştu.",
      });
    } finally {
      setSifreKaydediliyor(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Profil Ayarları">
      <div className="space-y-6">
        <form onSubmit={handleProfilKaydet} className="space-y-3">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
            Ad Soyad
          </h3>
          {profilMesaj && (
            <div
              className={`text-sm p-2.5 rounded-md ${
                profilMesaj.tip === "basari"
                  ? "text-green-600 bg-green-50 dark:bg-green-900/30 dark:text-green-400"
                  : "text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400"
              }`}
            >
              {profilMesaj.metin}
            </div>
          )}
          <div className="flex gap-3">
            <Input value={ad} onChange={(e) => setAd(e.target.value)} placeholder="Ad" required />
            <Input value={soyad} onChange={(e) => setSoyad(e.target.value)} placeholder="Soyad" required />
          </div>
          <div className="flex justify-end">
            <Button type="submit" size="sm" disabled={profilKaydediliyor}>
              {profilKaydediliyor ? "Kaydediliyor..." : "Kaydet"}
            </Button>
          </div>
        </form>

        <div className="border-t border-gray-100 dark:border-gray-800 pt-6">
          <form onSubmit={handleSifreDegistir} className="space-y-3">
            <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
              Şifre Değiştir
            </h3>
            {sifreMesaj && (
              <div
                className={`text-sm p-2.5 rounded-md ${
                  sifreMesaj.tip === "basari"
                    ? "text-green-600 bg-green-50 dark:bg-green-900/30 dark:text-green-400"
                    : "text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400"
                }`}
              >
                {sifreMesaj.metin}
              </div>
            )}
            <Input
              type="password"
              value={mevcutSifre}
              onChange={(e) => setMevcutSifre(e.target.value)}
              placeholder="Mevcut şifre"
              autoComplete="current-password"
              required
            />
            <Input
              type="password"
              value={yeniSifre}
              onChange={(e) => setYeniSifre(e.target.value)}
              placeholder="Yeni şifre (en az 6 karakter)"
              autoComplete="new-password"
              minLength={6}
              required
            />
            <div className="flex justify-end">
              <Button type="submit" size="sm" disabled={sifreKaydediliyor}>
                {sifreKaydediliyor ? "Kaydediliyor..." : "Şifreyi Değiştir"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </Modal>
  );
}
