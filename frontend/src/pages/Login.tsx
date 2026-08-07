import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import { Button } from "../components/Button";
import { Input } from "../components/Input";
import { LogIn } from "lucide-react";
import type { AuthResultDto, Result } from "../types";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      // Backend api/auth/login adresine istek at
      const response = await api.post<Result<AuthResultDto>>("/Auth/login", {
        email,
        sifre: password,
      });

      if (response.data.isSuccess && response.data.data) {
        login(response.data.data.token, response.data.data.kullanici);
        navigate("/");
      } else {
        setError(response.data.error || "Giriş başarısız.");
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
            <LogIn className="h-8 w-8 text-indigo-600 dark:text-indigo-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold tracking-tight text-gray-900 dark:text-white">
            Prizma Pano
          </h2>
          <p className="mt-2 text-center text-sm text-gray-500 dark:text-gray-400">
            Ekip yönetimi ve görev takip sistemi
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
            <div>
              <label
                className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                htmlFor="email-address"
              >
                Email adresi
              </label>
              <Input
                id="email-address"
                name="email"
                type="email"
                autoComplete="email"
                required
                placeholder="ornek@prizma.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div>
              <div className="flex items-center justify-between mb-1">
                <label
                  className="block text-sm font-medium text-gray-700 dark:text-gray-300"
                  htmlFor="password"
                >
                  Şifre
                </label>
                <Link
                  to="/sifremi-unuttum"
                  className="text-xs font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
                >
                  Şifremi Unuttum
                </Link>
              </div>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                required
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
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
              {loading ? "Giriş yapılıyor..." : "Giriş Yap"}
            </Button>
          </div>

          <p className="text-center text-sm text-gray-500 dark:text-gray-400">
            Hesabınız yok mu?{" "}
            <Link
              to="/register"
              className="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
            >
              Ekip oluşturun
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
