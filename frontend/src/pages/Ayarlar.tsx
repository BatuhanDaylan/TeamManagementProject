import React, { useState, useEffect } from "react";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { Mail, KeyRound, Trash2, CheckCircle2, Settings } from "lucide-react";
import type { MailAyarlariDurumDto, Result } from "../types";

export default function Ayarlar() {
  const { user } = useAuthStore();
  const [durum, setDurum] = useState<MailAyarlariDurumDto | null>(null);
  const [loading, setLoading] = useState(true);

  const [uygulamaSifresi, setUygulamaSifresi] = useState("");
  const [aiApiKey, setAiApiKey] = useState("");
  const [saving, setSaving] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const [successMsg, setSuccessMsg] = useState("");

  const fetchDurum = async () => {
    try {
      const res = await api.get<Result<MailAyarlariDurumDto>>("/Ayarlar/mail");
      if (res.data.isSuccess && res.data.data) {
        setDurum(res.data.data);
      }
    } catch (error) {
      console.error("Mail ayarları alınamadı:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user?.rol === "Lider") {
      fetchDurum();
    } else {
      setLoading(false);
    }
  }, [user]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const sifreTrim = uygulamaSifresi.trim();
    const anahtarTrim = aiApiKey.trim();
    if (!sifreTrim && !anahtarTrim) return;

    setSaving(true);
    setErrorMsg("");
    setSuccessMsg("");
    try {
      await api.put("/Ayarlar/mail", {
        uygulamaSifresi: sifreTrim || undefined,
        aiApiKey: anahtarTrim || undefined,
      });
      setSuccessMsg("Ayarlar başarıyla kaydedildi.");
      setUygulamaSifresi("");
      setAiApiKey("");
      fetchDurum();
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error ||
          "Ayarlar kaydedilirken bir hata oluştu.",
      );
    } finally {
      setSaving(false);
    }
  };

  const handleKaldir = async () => {
    if (
      !confirm(
        "Mail entegrasyonu bağlantısını kaldırmak istediğinize emin misiniz? Haftalık plan mailleriniz artık otomatik işlenmeyecek.",
      )
    )
      return;

    setRemoving(true);
    setErrorMsg("");
    setSuccessMsg("");
    try {
      await api.delete("/Ayarlar/mail");
      setSuccessMsg("Bağlantı kaldırıldı.");
      fetchDurum();
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error || "Bağlantı kaldırılırken bir hata oluştu.",
      );
    } finally {
      setRemoving(false);
    }
  };

  if (user?.rol !== "Lider") {
    return (
      <div className="flex h-full items-center justify-center">
        <div className="text-center space-y-4">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-amber-100 dark:bg-amber-900/30">
            <Settings className="h-8 w-8 text-amber-600 dark:text-amber-400" />
          </div>
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Yetkisiz Erişim
          </h2>
          <p className="text-gray-500 dark:text-gray-400">
            Mail entegrasyonu ayarlarını sadece ekip liderleri yapılandırabilir.
          </p>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="p-8 text-center text-gray-500 dark:text-gray-400">
        Yükleniyor...
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
          Ayarlar
        </h1>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Haftalık plan e-postalarınızın otomatik işlenmesi için mail entegrasyonunu yapılandırın.
        </p>
      </div>

      <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm p-6">
        <div className="flex items-center gap-2 mb-4 pb-4 border-b border-gray-100 dark:border-gray-800">
          <div className="p-2 bg-indigo-50 dark:bg-indigo-900/30 rounded-lg text-indigo-600 dark:text-indigo-400">
            <Mail className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
              Haftalık Plan Mail Entegrasyonu
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Kayıtlı e-postanız: <span className="font-medium">{durum?.kayitliEmail}</span>
            </p>
          </div>
        </div>

        <div className="flex flex-col sm:flex-row gap-3 mb-6">
          <div
            className={`flex-1 flex items-center gap-2 text-sm px-3 py-2 rounded-md ${
              durum?.mailYapilandirildiMi
                ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400"
                : "bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400"
            }`}
          >
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            Uygulama Şifresi:{" "}
            {durum?.mailYapilandirildiMi ? "Yapılandırıldı" : "Yapılandırılmadı"}
          </div>
          <div
            className={`flex-1 flex items-center gap-2 text-sm px-3 py-2 rounded-md ${
              durum?.aiApiKeyYapilandirildiMi
                ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400"
                : "bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400"
            }`}
          >
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            AI API Anahtarı:{" "}
            {durum?.aiApiKeyYapilandirildiMi ? "Yapılandırıldı" : "Yapılandırılmadı"}
          </div>
        </div>

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
              Uygulama Şifresi
            </label>
            <Input
              type="password"
              value={uygulamaSifresi}
              onChange={(e) => setUygulamaSifresi(e.target.value)}
              placeholder={
                durum?.mailYapilandirildiMi
                  ? "Değiştirmek için yeni şifre girin"
                  : "16 haneli uygulama şifresi"
              }
              autoComplete="off"
            />
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Bu, Google hesap şifreniz DEĞİLDİR. Google Hesabı → Güvenlik →
              Uygulama Şifreleri bölümünden oluşturduğunuz 16 haneli özel şifredir.
            </p>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Gemini API Anahtarı
            </label>
            <Input
              type="password"
              value={aiApiKey}
              onChange={(e) => setAiApiKey(e.target.value)}
              placeholder={
                durum?.aiApiKeyYapilandirildiMi
                  ? "Değiştirmek için yeni anahtar girin"
                  : "Kendi Gemini API anahtarınız"
              }
              autoComplete="off"
            />
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Haftalık plan mailinizi yapay zekanın işleyebilmesi için gereklidir.
            </p>
          </div>

          <div className="flex items-center justify-between gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              className="text-red-600 border-red-200 hover:bg-red-50 dark:text-red-400 dark:border-red-900/50 dark:hover:bg-red-900/20"
              onClick={handleKaldir}
              disabled={removing || (!durum?.mailYapilandirildiMi && !durum?.aiApiKeyYapilandirildiMi)}
            >
              <Trash2 className="w-4 h-4 mr-2" />
              {removing ? "Kaldırılıyor..." : "Bağlantıyı Kaldır"}
            </Button>
            <Button
              type="submit"
              disabled={saving || (!uygulamaSifresi.trim() && !aiApiKey.trim())}
            >
              <KeyRound className="w-4 h-4 mr-2" />
              {saving ? "Test ediliyor ve kaydediliyor..." : "Kaydet"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
