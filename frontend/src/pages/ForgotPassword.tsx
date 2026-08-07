import { useState } from "react";
import type React from "react";
import { Link } from "react-router-dom";
import api from "../api/axios";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { KeyRound } from "lucide-react";

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [gonderildi, setGonderildi] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await api.post("/Auth/sifre-sifirlama-talep", { email });
      //Backend e-posta kayıtlı olsun ya da olmasın her zaman başarılı döner
      //(hangi e-postaların sistemde kayıtlı olduğunun dışarıdan anlaşılmasını önler)
      setGonderildi(true);
    } catch {
      //Talep uç noktası zaten her zaman 200 döner, ama ağ hatası ihtimaline karşı yine de
      //kullanıcıya aynı nötr mesaj gösterilir
      setGonderildi(true);
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
            Şifremi Unuttum
          </h2>
          <p className="mt-2 text-center text-sm text-gray-500 dark:text-gray-400">
            E-posta adresinizi girin, size şifre sıfırlama bağlantısı gönderelim.
          </p>
        </div>

        {gonderildi ? (
          <div className="rounded-xl bg-emerald-50 p-4 dark:bg-emerald-900/30 border border-emerald-100 dark:border-emerald-800">
            <p className="text-sm font-medium text-emerald-800 dark:text-emerald-300">
              Eğer bu e-posta adresi sistemde kayıtlıysa, şifre sıfırlama bağlantısı
              gönderildi. Gelen kutunuzu (ve spam klasörünü) kontrol edin.
            </p>
          </div>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
            <div>
              <label
                className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                htmlFor="email-address"
              >
                Email adresi
              </label>
              <Input
                id="email-address"
                type="email"
                autoComplete="email"
                required
                placeholder="ornek@prizma.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>

            <Button
              type="submit"
              className="w-full shadow-lg shadow-indigo-500/30"
              size="lg"
              disabled={loading}
            >
              {loading ? "Gönderiliyor..." : "Sıfırlama Bağlantısı Gönder"}
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
