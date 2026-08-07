import { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { Bell, Check, BellRing } from "lucide-react";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";
import api from "../api/axios";
import type { Result, BildirimDto } from "../types";
import { useAuthStore } from "../store/authStore";

interface NotificationDropdownProps {
  yeniBildirimVar?: boolean;
  onOkundu?: () => void;
}

export function NotificationDropdown({ yeniBildirimVar = false, onOkundu }: NotificationDropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [bildirimler, setBildirimler] = useState<BildirimDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [panelStyle, setPanelStyle] = useState<{ top?: number; bottom?: number; left: number }>({ left: 0 });
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const { isAuthenticated } = useAuthStore();
  const navigate = useNavigate();

  //Zil, sidebar'daki daralıp genişleyen (overflow-hidden) bir kapsayıcının içinde olduğu için
  //panel absolute konumlandırılırsa görsel olarak kırpılıp hiç görünmüyordu. fixed + butonun
  //ekran koordinatları kullanılarak bu kapsayıcı tamamen es geçilir.
  //Zil sidebar'ın en altında olduğu için aşağı doğru açmak paneli ekranın dışına taşırıyordu;
  //altta yeterli yer yoksa panel yukarı doğru açılır.
  const PANEL_TAHMINI_YUKSEKLIK = 420;
  const togglePanel = () => {
    if (!isOpen && buttonRef.current) {
      const rect = buttonRef.current.getBoundingClientRect();
      const left = Math.max(8, rect.right - 320);
      const altBosluk = window.innerHeight - rect.bottom;
      if (altBosluk < PANEL_TAHMINI_YUKSEKLIK && rect.top > altBosluk) {
        setPanelStyle({ bottom: window.innerHeight - rect.top + 8, left });
      } else {
        setPanelStyle({ top: rect.bottom + 8, left });
      }
    }
    setIsOpen((prev) => !prev);
  };

  const fetchUnreadCount = async () => {
    try {
      const res = await api.get<{ okunmamisSayi: number }>("/Bildirim/sayac");
      if (res.status === 200) {
        setUnreadCount(res.data.okunmamisSayi);
      }
    } catch (error) {
      console.error("Bildirim sayacı alınamadı:", error);
    }
  };

  const fetchBildirimler = async () => {
    try {
      const res = await api.get<Result<BildirimDto[]>>("/Bildirim/listele");
      if (res.data.isSuccess && res.data.data) {
        setBildirimler(res.data.data);
        return res.data.data;
      }
    } catch (error) {
      console.error("Bildirimler alınamadı:", error);
    }
    return [];
  };

  //Panel açılıp bildirimler gözle görülünce hepsi okundu sayılır, aksi halde sayaç sürekli
  //takılı kalıyordu (sadece satırdaki küçük ✓ ikonuna tek tek basınca düşüyordu)
  const markAllAsRead = async (liste: BildirimDto[]) => {
    const okunmamislar = liste.filter((b) => !b.okundu);
    if (okunmamislar.length === 0) return;

    try {
      await api.patch("/Bildirim/tumunu-oku");
      setBildirimler((prev) => prev.map((b) => ({ ...b, okundu: true })));
      setUnreadCount(0);
    } catch (error) {
      console.error("Bildirimler okundu işaretlenemedi:", error);
    }
  };

  const handleBildirimClick = (bildirim: BildirimDto) => {
    if (!bildirim.gorevId) return;
    setIsOpen(false);
    navigate(`/pano?gorevId=${bildirim.gorevId}`);
  };

  const markAsRead = async (id: string) => {
    try {
      const res = await api.patch(`/Bildirim/${id}/oku`);
      if (res.status === 200) {
        setBildirimler((prev) =>
          prev.map((b) => (b.id === id ? { ...b, okundu: true } : b)),
        );
        setUnreadCount((prev) => Math.max(0, prev - 1));
      }
    } catch (error) {
      console.error("Okundu işaretleme hatası:", error);
    }
  };

  useEffect(() => {
    if (isAuthenticated) {
      fetchUnreadCount();
      // Basit polling for unread count every 30 seconds
      const interval = setInterval(fetchUnreadCount, 30000);
      return () => clearInterval(interval);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    if (isOpen) {
      (async () => {
        const liste = await fetchBildirimler();
        await markAllAsRead(liste);
        onOkundu?.(); // Layout'taki anlık bildirim badge'ini söndür
      })();
    }
  }, [isOpen]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        ref={buttonRef}
        onClick={togglePanel}
        className="relative rounded-full p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-500 dark:hover:bg-gray-800 dark:hover:text-gray-300 transition-colors"
        title="Bildirimler"
      >
        {yeniBildirimVar ? (
          <BellRing className="h-5 w-5 text-indigo-500 animate-bounce" />
        ) : (
          <Bell className="h-5 w-5" />
        )}
        {unreadCount > 0 && (
          <span className="absolute top-1.5 right-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[9px] font-bold text-white shadow-sm ring-2 ring-white dark:ring-gray-900">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          style={{ position: "fixed", ...panelStyle }}
          className="w-80 origin-top-right rounded-xl bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none dark:bg-gray-900 dark:ring-gray-800 z-50 overflow-hidden flex flex-col max-h-[400px]">
          <div className="border-b border-gray-100 dark:border-gray-800 px-4 py-3 flex items-center justify-between bg-gray-50/50 dark:bg-gray-800/50">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2">
              <BellRing className="h-4 w-4 text-indigo-500" /> Bildirimler
            </h3>
            {unreadCount > 0 && (
              <span className="text-xs text-indigo-600 dark:text-indigo-400 font-medium">
                {unreadCount} yeni
              </span>
            )}
          </div>

          <div className="flex-1 overflow-y-auto divide-y divide-gray-100 dark:divide-gray-800">
            {bildirimler.length === 0 ? (
              <div className="px-4 py-8 text-center text-sm text-gray-500">
                Hiç bildiriminiz yok.
              </div>
            ) : (
              bildirimler.map((bildirim) => (
                <div
                  key={bildirim.id}
                  onClick={() => handleBildirimClick(bildirim)}
                  className={`px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors flex gap-3 ${!bildirim.okundu ? "bg-indigo-50/30 dark:bg-indigo-900/10" : ""} ${bildirim.gorevId ? "cursor-pointer" : ""}`}
                >
                  <div className="flex-1 min-w-0">
                    <p
                      className={`text-sm ${!bildirim.okundu ? "font-medium text-gray-900 dark:text-gray-100" : "text-gray-600 dark:text-gray-400"}`}
                    >
                      {bildirim.icerik}
                    </p>
                    <p className="text-xs text-gray-400 mt-1">
                      {format(
                        parseISO(bildirim.olusturulmaTarihi),
                        "dd MMM HH:mm",
                        { locale: tr },
                      )}
                    </p>
                  </div>
                  {!bildirim.okundu && (
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        markAsRead(bildirim.id);
                      }}
                      className="flex-shrink-0 text-indigo-600 hover:text-indigo-800 dark:text-indigo-400 dark:hover:text-indigo-300 p-1"
                      title="Okundu olarak işaretle"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
