import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { Rocket } from "lucide-react";
import type { AuthResultDto, Result } from "../types";

export default function Register() {
  const [ad, setAd] = useState("");
  const [soyad, setSoyad] = useState("");
  const [email, setEmail] = useState("");
  const [sifre, setSifre] = useState("");
  const [ekipAdi, setEkipAdi] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      // 1. Hesabı oluştur
      const registerRes = await api.post<Result>("/Auth/register", {
        ad,
        soyad,
        email,
        sifre,
      });
      if (!registerRes.data.isSuccess) {
        setError(registerRes.data.error || "Kayıt başarısız.");
        return;
      }

      // 2. Ekip oluşturmak için token gereklidir, geçici olarak giriş yap
      const gecici = await api.post<Result<AuthResultDto>>("/Auth/login", {
        email,
        sifre,
      });
      if (!gecici.data.isSuccess || !gecici.data.data) {
        setError(gecici.data.error || "Giriş başarısız.");
        return;
      }
      login(gecici.data.data.token, gecici.data.data.kullanici);

      // 3. Ekibi oluştur, kullanıcı Lider rolüne yükseltilir
      const ekipRes = await api.post<Result>("/Ekip/olustur", { ad: ekipAdi });
      if (!ekipRes.data.isSuccess) {
        setError(ekipRes.data.error || "Ekip oluşturulamadı.");
        return;
      }

      // 4. Rol ve ekip bilgisi token'a işlensin diye tekrar giriş yap
      const finalLogin = await api.post<Result<AuthResultDto>>("/Auth/login", {
        email,
        sifre,
      });
      if (finalLogin.data.isSuccess && finalLogin.data.data) {
        login(finalLogin.data.data.token, finalLogin.data.data.kullanici);
        navigate("/");
      } else {
        setError(
          "Ekip oluşturuldu ama oturum yenilenemedi, lütfen tekrar giriş yapın.",
        );
        navigate("/login");
      }
    } catch (err: any) {
      setError(
        err.response?.data?.error ||
          "Bir sunucu hatası oluştu. (Backend çalışmıyor olabilir)",
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
            <Rocket className="h-8 w-8 text-indigo-600 dark:text-indigo-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold tracking-tight text-gray-900 dark:text-white">
            Ekip Kur
          </h2>
          <p className="mt-2 text-center text-sm text-gray-500 dark:text-gray-400">
            Hesabınızı oluşturun ve yeni bir ekibin lideri olun
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          {error && (
            <div className="rounded-xl bg-red-50 p-4 dark:bg-red-900/30 border border-red-100 dark:border-red-800">
              <p className="text-sm font-medium text-red-800 dark:text-red-300">
                {error}
              </p>
            </div>
          )}

          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Ad
                </label>
                <Input
                  required
                  value={ad}
                  onChange={(e) => setAd(e.target.value)}
                  placeholder="Adınız"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Soyad
                </label>
                <Input
                  required
                  value={soyad}
                  onChange={(e) => setSoyad(e.target.value)}
                  placeholder="Soyadınız"
                />
              </div>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Email adresi
              </label>
              <Input
                type="email"
                autoComplete="email"
                required
                placeholder="ornek@prizma.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Şifre
              </label>
              <Input
                type="password"
                autoComplete="new-password"
                required
                minLength={6}
                placeholder="En az 6 karakter"
                value={sifre}
                onChange={(e) => setSifre(e.target.value)}
              />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Ekip Adı
              </label>
              <Input
                required
                placeholder="Örn: Prizma Yazılım"
                value={ekipAdi}
                onChange={(e) => setEkipAdi(e.target.value)}
              />
            </div>
          </div>

          <div>
            <Button
              type="submit"
              className="w-full shadow-lg shadow-indigo-500/30"
              size="lg"
              disabled={loading}
            >
              {loading ? "Oluşturuluyor..." : "Ekibi Oluştur"}
            </Button>
          </div>

          <p className="text-center text-sm text-gray-500 dark:text-gray-400">
            Zaten bir hesabınız var mı?{" "}
            <Link
              to="/login"
              className="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
            >
              Giriş yapın
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
