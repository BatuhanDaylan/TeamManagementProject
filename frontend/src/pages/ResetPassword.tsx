import { useState } from "react";
import type React from "react";
import { useSearchParams, useNavigate, Link } from "react-router-dom";
import api from "../api/axios";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { KeyRound, CheckCircle2 } from "lucide-react";

export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");
  const navigate = useNavigate();

  const [yeniSifre, setYeniSifre] = useState("");
  const [sifreTekrar, setSifreTekrar] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const [basarili, setBasarili] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg("");

    if (yeniSifre !== sifreTekrar) {
      setErrorMsg("Şifreler eşleşmiyor.");
      return;
    }
    if (!token) {
      setErrorMsg("Geçersiz veya eksik sıfırlama bağlantısı.");
      return;
    }

    setLoading(true);
    try {
      await api.post("/Auth/sifre-sifirla", { token, yeniSifre });
      setBasarili(true);
      setTimeout(() => navigate("/login"), 2500);
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error ||
          "Şifre sıfırlanırken bir hata oluştu. Bağlantının süresi dolmuş olabilir.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8 dark:bg-gray-950">
      <div className="w-full max-w-md space-y-8 rounded-3xl bg-white p-10 shadow-2xl shadow-indigo-500/5 dark:bg-gray-900 border border-gray-100 dark:border-gray-800">
        <div>
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-indigo-50 dark:bg-indigo-900/30">
            <KeyRound className="h-8 w-8 text-indigo-600 dark:text-indigo-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold tracking-tight text-gray-900 dark:text-white">
            Yeni Şifre Belirle
          </h2>
        </div>

        {basarili ? (
          <div className="rounded-xl bg-emerald-50 p-4 dark:bg-emerald-900/30 border border-emerald-100 dark:border-emerald-800 flex items-start gap-3">
            <CheckCircle2 className="h-5 w-5 text-emerald-600 dark:text-emerald-400 shrink-0 mt-0.5" />
            <p className="text-sm font-medium text-emerald-800 dark:text-emerald-300">
              Şifreniz güncellendi. Giriş sayfasına yönlendiriliyorsunuz...
            </p>
          </div>
        ) : !token ? (
          <div className="rounded-xl bg-red-50 p-4 dark:bg-red-900/30 border border-red-100 dark:border-red-800">
            <p className="text-sm font-medium text-red-800 dark:text-red-300">
              Geçersiz veya eksik sıfırlama bağlantısı. Lütfen yeni bir talepte
              bulunun.
            </p>
          </div>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
            {errorMsg && (
              <div className="rounded-xl bg-red-50 p-4 dark:bg-red-900/30 border border-red-100 dark:border-red-800">
                <p className="text-sm font-medium text-red-800 dark:text-red-300">
                  {errorMsg}
                </p>
              </div>
            )}

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Yeni Şifre
              </label>
              <Input
                type="password"
                required
                minLength={6}
                placeholder="••••••••"
                value={yeniSifre}
                onChange={(e) => setYeniSifre(e.target.value)}
                autoComplete="new-password"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Yeni Şifre (Tekrar)
              </label>
              <Input
                type="password"
                required
                minLength={6}
                placeholder="••••••••"
                value={sifreTekrar}
                onChange={(e) => setSifreTekrar(e.target.value)}
                autoComplete="new-password"
              />
            </div>

            <Button
              type="submit"
              className="w-full shadow-lg shadow-indigo-500/30"
              size="lg"
              disabled={loading}
            >
              {loading ? "Kaydediliyor..." : "Şifreyi Güncelle"}
            </Button>
          </form>
        )}

        <p className="text-center text-sm text-gray-500 dark:text-gray-400">
          <Link
            to="/login"
            className="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
          >
            Giriş sayfasına dön
          </Link>
        </p>
      </div>
    </div>
  );
}
