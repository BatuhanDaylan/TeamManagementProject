import React, { useState, useEffect } from "react";
import { useSearchParams, useNavigate, Link } from "react-router-dom";
import api from "../api/axios";
import { Input } from "../components/Input";
import { Button } from "../components/Button";
import { CheckCircle2, XCircle, UserPlus } from "lucide-react";

export default function DavetKabul() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [isValidating, setIsValidating] = useState(true);
  const [errorMsg, setErrorMsg] = useState("");
  const [successMsg, setSuccessMsg] = useState("");

  const [email, setEmail] = useState("");
  const [ad, setAd] = useState("");
  const [soyad, setSoyad] = useState("");
  const [sifre, setSifre] = useState("");

  // 1. Token Doğrulama
  useEffect(() => {
    if (!token) {
      setErrorMsg("Geçersiz veya eksik davet bağlantısı.");
      setIsValidating(false);
      setLoading(false);
      return;
    }

    const verifyToken = async () => {
      try {
        const res = await api.get(
          `/Davet/dogrula?token=${encodeURIComponent(token)}`,
        );
        if (res.data.isSuccess && res.data.data.gecerliMi) {
          setEmail(res.data.data.email);
        } else {
          setErrorMsg(
            res.data.error || "Davet bağlantısı geçersiz veya süresi dolmuş.",
          );
        }
      } catch (err: any) {
        setErrorMsg(
          err.response?.data?.error ||
            "Davet doğrulanırken bir hata oluştu. Bağlantı süresi dolmuş olabilir.",
        );
      } finally {
        setIsValidating(false);
        setLoading(false);
      }
    };

    verifyToken();
  }, [token]);

  // 2. Form Gönderimi (Daveti Kabul Etme)
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg("");

    try {
      await api.post("/Davet/kabul", {
        token,
        ad,
        soyad,
        yeniSifre: sifre,
      });
      setSuccessMsg(
        "Hesabınız başarıyla oluşturuldu! Şimdi giriş yapabilirsiniz.",
      );
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error ||
          "Hesap oluşturulurken bir hata meydana geldi.",
      );
    } finally {
      setLoading(false);
    }
  };

  if (isValidating) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50 dark:bg-gray-950">
        <div className="text-center text-gray-500 dark:text-gray-400">
          <div className="mb-4 h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent mx-auto"></div>
          Davet bağlantınız doğrulanıyor...
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-gray-50 px-4 py-12 dark:bg-gray-950 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8 rounded-3xl bg-white p-10 shadow-2xl shadow-indigo-500/5 dark:bg-gray-900 border border-gray-100 dark:border-gray-800">
        <div>
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-indigo-50 dark:bg-indigo-900/30">
            <UserPlus className="h-8 w-8 text-indigo-600 dark:text-indigo-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold tracking-tight text-gray-900 dark:text-white">
            Prizma Pano
          </h2>
          <p className="mt-2 text-center text-sm text-gray-500 dark:text-gray-400">
            Ekibinize katılmak için hesabınızı oluşturun
          </p>
        </div>

        {errorMsg && !successMsg && (
          <div className="rounded-lg bg-red-50 p-4 border border-red-200 dark:bg-red-900/30 dark:border-red-800">
            <div className="flex items-center gap-3">
              <XCircle className="h-5 w-5 text-red-500" />
              <p className="text-sm font-medium text-red-800 dark:text-red-300">
                {errorMsg}
              </p>
            </div>
            {!token || errorMsg.includes("geçersiz") ? (
              <div className="mt-4 text-center">
                <Link
                  to="/login"
                  className="text-sm font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
                >
                  Giriş sayfasına dön &rarr;
                </Link>
              </div>
            ) : null}
          </div>
        )}

        {successMsg && (
          <div className="rounded-lg bg-green-50 p-6 border border-green-200 dark:bg-green-900/30 dark:border-green-800 text-center space-y-4">
            <CheckCircle2 className="h-12 w-12 text-green-500 mx-auto" />
            <h3 className="text-lg font-medium text-green-800 dark:text-green-300">
              Harika!
            </h3>
            <p className="text-sm text-green-700 dark:text-green-400">
              {successMsg}
            </p>
            <Button className="w-full mt-4" onClick={() => navigate("/login")}>
              Giriş Yap
            </Button>
          </div>
        )}

        {!errorMsg && !successMsg && (
          <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
            <div className="space-y-4 rounded-md shadow-sm">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                  E-posta Adresi (Davet Edilen)
                </label>
                <Input
                  type="email"
                  required
                  disabled
                  value={email}
                  className="bg-gray-100 dark:bg-gray-800 text-gray-500 cursor-not-allowed"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                    Adınız
                  </label>
                  <Input
                    type="text"
                    required
                    value={ad}
                    onChange={(e) => setAd(e.target.value)}
                    placeholder="Adınız"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                    Soyadınız
                  </label>
                  <Input
                    type="text"
                    required
                    value={soyad}
                    onChange={(e) => setSoyad(e.target.value)}
                    placeholder="Soyadınız"
                  />
                </div>
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Yeni Şifre Belirleyin
                </label>
                <Input
                  type="password"
                  required
                  value={sifre}
                  onChange={(e) => setSifre(e.target.value)}
                  placeholder="En az 6 karakter"
                  minLength={6}
                />
              </div>
            </div>

            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? "Hesap Oluşturuluyor..." : "Hesabı Oluştur ve Katıl"}
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}
