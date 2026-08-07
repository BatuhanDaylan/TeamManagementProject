import { Outlet, Navigate, Link, useLocation } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import {
  LayoutDashboard,
  CalendarDays,
  LogOut,
  Menu,
  Plus,
  UserPlus,
  Users,
  BarChart,
  ChevronLeft,
  KanbanSquare,
  Settings,
  UserX,
} from "lucide-react";
import { cn } from "../utils/cn";
import { useState, useEffect } from "react";
import { Button } from "./Button";
import { CreateProjeModal } from "./forms/CreateProjeModal";
import { DavetGonderModal } from "./forms/DavetGonderModal";
import { ProfilModal } from "./forms/ProfilModal";
import { NotificationDropdown } from "./NotificationDropdown";
import { signalRService } from "../api/signalr";
import api from "../api/axios";

export default function Layout() {
  const { isAuthenticated, logout, user } = useAuthStore();
  const location = useLocation();
  const [isProjeModalOpen, setProjeModalOpen] = useState(false);
  const [isDavetModalOpen, setDavetModalOpen] = useState(false);
  const [isProfilModalOpen, setProfilModalOpen] = useState(false);
  const [isSidebarOpen, setSidebarOpen] = useState(true);
  const [yeniBildirimVar, setYeniBildirimVar] = useState(false);

  const handleEkiptenAyril = async () => {
    if (
      !confirm(
        "Ekipten ayrılmak istediğinize emin misiniz? Üzerinizdeki görevler sahipsiz kalacak ve tekrar bir davet almadan bu ekibe erişemeyeceksiniz.",
      )
    )
      return;

    try {
      await api.post("/Ekip/ayril");
      alert("Ekipten ayrıldınız. Oturum kapatılıyor.");
      logout();
    } catch (error: any) {
      console.error("Ekipten ayrılınamadı:", error);
      alert(error?.response?.data?.error || "Ekipten ayrılırken bir hata oluştu.");
    }
  };

  // SignalR: kullanıcı kendi bildirim grubuna join olur ve anlık bildirimleri alır
  useEffect(() => {
    if (!user?.id) return;

    // Aynı fonksiyon referansı hem on hem off'a verilmeli, aksi halde temizlenmede
    // dinleyici gerçekten kaldırılmaz ve her yeniden bağlanışta birikir
    const handleYeniBildirim = () => setYeniBildirimVar(true);

    signalRService.startConnection().then(async () => {
      // Kullanıcıya özel SignalR grubuna katıl — backend bu gruba yazar
      await signalRService.invoke("KullaniciGrubunuKatil", user.id);

      // YeniBildirim eventi gelince badge'i yak
      signalRService.on("YeniBildirim", handleYeniBildirim);
    });

    return () => {
      signalRService.off("YeniBildirim", handleYeniBildirim);
    };
  }, [user?.id]);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const navigation = [
    { name: "Dashboard", href: "/", icon: LayoutDashboard },
    { name: "Kanban Panosu", href: "/pano", icon: KanbanSquare },
    { name: "Timeline", href: "/timeline", icon: CalendarDays },
    { name: "Raporlar", href: "/raporlar", icon: BarChart, isLiderOnly: true },
    { name: "Ekip Yönetimi", href: "/ekip", icon: Users, isLiderOnly: true },
    { name: "Ayarlar", href: "/ayarlar", icon: Settings, isLiderOnly: true },
  ];

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50 dark:bg-gray-950">
      <CreateProjeModal
        isOpen={isProjeModalOpen}
        onClose={() => setProjeModalOpen(false)}
      />
      <DavetGonderModal
        isOpen={isDavetModalOpen}
        onClose={() => setDavetModalOpen(false)}
      />
      <ProfilModal
        isOpen={isProfilModalOpen}
        onClose={() => setProfilModalOpen(false)}
      />

      {/* Sidebar */}
      <div
        className={`hidden flex-col border-r border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-900 md:flex transition-all duration-300 ease-in-out ${isSidebarOpen ? "w-64" : "w-20"}`}
      >
        <div
          className={cn(
            "flex h-16 shrink-0 items-center overflow-hidden transition-all duration-300",
            isSidebarOpen ? "justify-between px-5" : "justify-center px-0",
          )}
        >
          <span
            className={cn(
              "text-lg font-bold tracking-tight text-gray-800 dark:text-gray-100 whitespace-nowrap overflow-hidden transition-all duration-300",
              isSidebarOpen ? "opacity-100 max-w-[200px]" : "opacity-0 max-w-0",
            )}
          >
            Prizma Pano
          </span>
          <button
            onClick={() => setSidebarOpen(!isSidebarOpen)}
            className="p-1.5 rounded-md text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800 shrink-0"
          >
            {isSidebarOpen ? (
              <ChevronLeft className="w-5 h-5" />
            ) : (
              <Menu className="w-5 h-5" />
            )}
          </button>
        </div>
        <div className="flex flex-1 flex-col overflow-y-auto overflow-x-hidden px-3 py-4">
          <nav className="flex-1 space-y-2">
            {navigation
              .filter((item) => !item.isLiderOnly || user?.rol === "Lider")
              .map((item) => {
                const isActive = location.pathname === item.href;
                return (
                  <Link
                    key={item.name}
                    to={item.href}
                    title={!isSidebarOpen ? item.name : undefined}
                    className={cn(
                      "group flex items-center rounded-md py-2 transition-all duration-300 overflow-hidden whitespace-nowrap",
                      isSidebarOpen ? "px-3" : "px-0 justify-center",
                      isActive
                        ? "bg-white shadow-sm text-blue-700 dark:bg-gray-800 dark:text-blue-400 font-semibold border border-gray-200 dark:border-gray-700"
                        : "text-gray-600 hover:bg-gray-200/50 hover:text-gray-900 dark:text-gray-400 dark:hover:bg-gray-800/50 dark:hover:text-white",
                    )}
                  >
                    <item.icon
                      className={cn(
                        "h-5 w-5 shrink-0 transition-all duration-300",
                        isSidebarOpen ? "mr-3" : "mx-auto",
                        isActive
                          ? "text-blue-600 dark:text-blue-400"
                          : "text-gray-400 group-hover:text-gray-600 dark:text-gray-500 dark:group-hover:text-gray-300",
                      )}
                      aria-hidden="true"
                    />
                    <span
                      className={cn(
                        "text-sm font-medium transition-all duration-300",
                        isSidebarOpen
                          ? "opacity-100 max-w-[200px]"
                          : "opacity-0 max-w-0",
                      )}
                    >
                      {item.name}
                    </span>
                  </Link>
                );
              })}
          </nav>
        </div>

        <div className="px-3 py-2 mb-4 overflow-hidden space-y-2 flex flex-col items-center whitespace-nowrap">
          {user?.rol === "Lider" && (
            <>
              <Button
                variant="outline"
                className={cn(
                  "justify-start border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 shadow-sm transition-all duration-300",
                  isSidebarOpen
                    ? "w-full px-4"
                    : "w-10 h-10 p-0 justify-center",
                )}
                onClick={() => setProjeModalOpen(true)}
                title={!isSidebarOpen ? "Yeni Proje" : undefined}
              >
                <Plus
                  className={cn(
                    "shrink-0 transition-all duration-300",
                    isSidebarOpen ? "mr-2 h-4 w-4" : "h-5 w-5",
                  )}
                />
                <span
                  className={cn(
                    "transition-all duration-300",
                    isSidebarOpen
                      ? "opacity-100 max-w-[150px]"
                      : "opacity-0 max-w-0",
                  )}
                >
                  Yeni Proje
                </span>
              </Button>
              <Button
                variant="outline"
                className={cn(
                  "justify-start border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 shadow-sm transition-all duration-300",
                  isSidebarOpen
                    ? "w-full px-4"
                    : "w-10 h-10 p-0 justify-center",
                )}
                onClick={() => setDavetModalOpen(true)}
                title={!isSidebarOpen ? "Ekibe Davet Et" : undefined}
              >
                <UserPlus
                  className={cn(
                    "shrink-0 transition-all duration-300",
                    isSidebarOpen ? "mr-2 h-4 w-4" : "h-5 w-5",
                  )}
                />
                <span
                  className={cn(
                    "transition-all duration-300",
                    isSidebarOpen
                      ? "opacity-100 max-w-[150px]"
                      : "opacity-0 max-w-0",
                  )}
                >
                  Davet Et
                </span>
              </Button>
            </>
          )}
        </div>

        <div className="border-t border-gray-200 p-4 dark:border-gray-800 overflow-hidden whitespace-nowrap">
          <div
            className={cn(
              "flex transition-all duration-300",
              isSidebarOpen
                ? "items-center gap-3 flex-row"
                : "flex-col items-center gap-4",
            )}
          >
            <button
              onClick={() => setProfilModalOpen(true)}
              className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-blue-600 text-white dark:bg-blue-500 shadow-sm font-bold hover:ring-2 hover:ring-blue-300 dark:hover:ring-blue-700 transition-all"
              title="Profil Ayarları"
            >
              {user?.ad?.charAt(0) || "U"}
            </button>

            <button
              onClick={() => setProfilModalOpen(true)}
              className={cn(
                "flex-1 overflow-hidden text-left transition-all duration-300",
                isSidebarOpen
                  ? "opacity-100 max-w-[150px]"
                  : "opacity-0 max-w-0 hidden",
              )}
              title="Profil Ayarları"
            >
              <p className="truncate text-sm font-medium text-gray-700 dark:text-gray-200 hover:text-blue-600 dark:hover:text-blue-400">
                {user?.ad} {user?.soyad}
              </p>
              <p className="truncate text-xs text-gray-500 dark:text-gray-400">
                {user?.rol}
              </p>
            </button>

            <div
              className={cn(
                "flex transition-all duration-300",
                isSidebarOpen
                  ? "items-center gap-1 flex-row"
                  : "flex-col items-center gap-3",
              )}
            >
              <NotificationDropdown
                yeniBildirimVar={yeniBildirimVar}
                onOkundu={() => setYeniBildirimVar(false)}
              />
              {user?.rol === "Uye" && (
                <button
                  onClick={handleEkiptenAyril}
                  className="rounded-md p-2 shrink-0 text-gray-400 hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-900/20 dark:hover:text-red-400"
                  title="Ekipten Ayrıl"
                >
                  <UserX className="h-5 w-5" />
                </button>
              )}
              <button
                onClick={() => logout()}
                className="rounded-md p-2 shrink-0 text-gray-400 hover:bg-gray-100 hover:text-gray-500 dark:hover:bg-gray-800 dark:hover:text-gray-300"
                title="Çıkış Yap"
              >
                <LogOut className="h-5 w-5" />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Mobile top bar */}
        <div className="flex h-16 shrink-0 items-center border-b border-gray-200 bg-white px-4 dark:border-gray-800 dark:bg-gray-900 md:hidden">
          <button
            type="button"
            className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
          >
            <Menu className="h-6 w-6" />
          </button>
          <div className="ml-4 flex-1">
            <span className="text-lg font-bold text-gray-800 dark:text-gray-100">
              Prizma Pano
            </span>
          </div>
        </div>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
