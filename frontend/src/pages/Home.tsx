import { useState, useEffect } from "react";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import type { UyeGorevOzetiDto, GorevDto, Result, BildirimDto } from "../types";
import { format, parseISO, isToday, isTomorrow, isPast } from "date-fns";
import { tr } from "date-fns/locale";
import {
  CheckCircle2,
  Clock,
  CalendarDays,
  AlertTriangle,
  ChevronRight,
  Bell,
  Users,
} from "lucide-react";
import { Badge } from "../components/Badge";
import { Link } from "react-router-dom";
import { TaskDetailsModal } from "../components/forms/TaskDetailsModal";

export default function Home() {
  const { user } = useAuthStore();
  const [myTasks, setMyTasks] = useState<GorevDto[]>([]);
  const [teamMembers, setTeamMembers] = useState<UyeGorevOzetiDto[]>([]);
  const [bildirimler, setBildirimler] = useState<BildirimDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedTask, setSelectedTask] = useState<GorevDto | null>(null);
  const [genisletilenUyeler, setGenisletilenUyeler] = useState<Set<string>>(
    new Set(),
  );

  const uyeGenisletmeyiDegistir = (kullaniciId: string) => {
    setGenisletilenUyeler((prev) => {
      const yeni = new Set(prev);
      if (yeni.has(kullaniciId)) yeni.delete(kullaniciId);
      else yeni.add(kullaniciId);
      return yeni;
    });
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        if (!user?.ekipId) return;

        // 1. Ekip verisinden kullanıcının kendi görevlerini bul ve tüm ekibi kaydet
        const dashRes = await api.get<Result<UyeGorevOzetiDto[]>>(
          `/Dashboard/ekip/${user.ekipId}`,
        );
        if (dashRes.data.isSuccess && dashRes.data.data) {
          const myProfile = dashRes.data.data.find(
            (u) => u.kullaniciId === user.id,
          );
          if (myProfile && myProfile.aktifGorevler) {
            setMyTasks(myProfile.aktifGorevler);
          }

          setTeamMembers(dashRes.data.data);
        }

        // 2. Bildirimlerin son 5 tanesini al
        const bildirimRes =
          await api.get<Result<BildirimDto[]>>("/Bildirim/listele");
        if (bildirimRes.data.isSuccess && bildirimRes.data.data) {
          setBildirimler(bildirimRes.data.data.slice(0, 5));
        }
      } catch (error) {
        console.error("Anasayfa verileri alınamadı:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user]);

  if (loading) {
    return (
      <div className="p-8 text-center text-gray-500 dark:text-gray-400">
        Yükleniyor...
      </div>
    );
  }

  // İstatistikler
  const pendingTasks = myTasks.filter((t) => t.durum !== "Tamamlandi");
  const criticalTasks = pendingTasks.filter((t) => t.oncelik === "Kritik");

  // Yaklaşan görevleri deadline'a göre sırala
  const upcomingTasks = [...pendingTasks].sort((a, b) => {
    if (!a.deadline) return 1;
    if (!b.deadline) return -1;
    return new Date(a.deadline).getTime() - new Date(b.deadline).getTime();
  });

  const getDeadlineText = (deadlineStr: string | null | undefined) => {
    if (!deadlineStr) return <span className="text-gray-400">Tarih yok</span>;
    const date = parseISO(deadlineStr);
    if (isPast(date) && !isToday(date))
      return <span className="text-red-500 font-medium">Gecikti</span>;
    if (isToday(date))
      return <span className="text-amber-500 font-medium">Bugün</span>;
    if (isTomorrow(date))
      return <span className="text-blue-500 font-medium">Yarın</span>;
    return (
      <span className="text-gray-600 dark:text-gray-400">
        {format(date, "dd MMM", { locale: tr })}
      </span>
    );
  };

  return (
    <div className="space-y-6 max-w-6xl mx-auto">
      {/* Karşılama Alanı */}
      <div className="bg-gray-900 dark:bg-gray-900 rounded-xl p-6 sm:p-8 text-white shadow-xl relative overflow-hidden border border-gray-800">
        <div className="relative z-10">
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight mb-2 text-gray-50">
            Günaydın, {user?.ad}! 👋
          </h1>
          <p className="text-indigo-100 max-w-xl text-sm sm:text-base">
            Bugün senin üzerinde bekleyen toplam{" "}
            <strong className="text-white">{pendingTasks.length}</strong> görev
            var.
            {criticalTasks.length > 0 && (
              <span>
                {" "}
                Bunlardan{" "}
                <strong className="text-white bg-red-500/20 px-2 py-0.5 rounded-full">
                  {criticalTasks.length} tanesi kritik
                </strong>{" "}
                önceliğe sahip, lütfen onlara öncelik ver.
              </span>
            )}
          </p>
        </div>
        {/* Dekoratif Çizgiler */}
        <div className="absolute top-0 right-0 w-1/2 h-full bg-gradient-to-l from-blue-500/10 to-transparent pointer-events-none"></div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Sol Kolon: Görevlerim */}
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm overflow-hidden flex flex-col">
            <div className="p-5 border-b border-gray-100 dark:border-gray-800 flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                <CheckCircle2 className="w-5 h-5 text-blue-600 dark:text-blue-500" /> Bana Atanan
                Görevler
              </h2>
              <Link
                to="/pano"
                className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-800 flex items-center transition-colors"
              >
                Panoya Git <ChevronRight className="w-4 h-4 ml-1" />
              </Link>
            </div>

            <div className="p-5 divide-y divide-gray-100 dark:divide-gray-800">
              {upcomingTasks.length === 0 ? (
                <div className="text-center py-8 text-gray-500 flex flex-col items-center">
                  <div className="bg-gray-100 dark:bg-gray-800 p-3 rounded-full mb-3">
                    <CheckCircle2 className="w-6 h-6 text-emerald-500" />
                  </div>
                  Üzerinizde bekleyen görev yok. Harika iş çıkardınız!
                </div>
              ) : (
                upcomingTasks.map((task) => (
                  <div
                    key={task.id}
                    onClick={() => setSelectedTask(task)}
                    className="py-4 first:pt-0 last:pb-0 flex items-start sm:items-center justify-between gap-4 flex-col sm:flex-row group cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 rounded-lg px-2 transition-colors"
                  >
                    <div className="flex-1 min-w-0">
                      <h3 className="text-sm font-medium text-gray-900 dark:text-gray-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors truncate">
                        {task.baslik}
                      </h3>
                      {task.projeAdi && (
                        <p className="text-xs text-gray-400 dark:text-gray-500 truncate">
                          {task.projeAdi}
                        </p>
                      )}
                      <div className="mt-1 flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
                        <span className="flex items-center gap-1">
                          <CalendarDays className="w-3.5 h-3.5" />
                          {getDeadlineText(task.deadline)}
                        </span>
                        <span className="flex items-center gap-1">
                          <Badge
                            variant="default"
                            className="text-[10px] px-1.5 py-0"
                          >
                            {task.durum}
                          </Badge>
                        </span>
                      </div>
                    </div>
                    <div className="shrink-0 flex items-center gap-2">
                      {task.oncelik === "Kritik" && (
                        <Badge variant="danger" className="animate-pulse">
                          <AlertTriangle className="w-3 h-3 mr-1 inline" />{" "}
                          Kritik
                        </Badge>
                      )}
                      {task.oncelik === "Yuksek" && (
                        <Badge variant="warning">Yüksek</Badge>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Sağ Kolon: Bildirimler & Hızlı İstatistik */}
        <div className="space-y-6">
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm p-5">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2 mb-4">
              <Bell className="w-5 h-5 text-amber-500" /> Son Bildirimler
            </h2>
            <div className="space-y-4">
              {bildirimler.length === 0 ? (
                <div className="text-sm text-gray-500 text-center py-4">
                  Yeni bildiriminiz yok.
                </div>
              ) : (
                bildirimler.map((b) => (
                  <div key={b.id} className="flex gap-3">
                    <div
                      className={`mt-0.5 shrink-0 w-2 h-2 rounded-full ${!b.okundu ? "bg-indigo-500" : "bg-gray-300 dark:bg-gray-700"}`}
                    ></div>
                    <div>
                      <p
                        className={`text-sm ${!b.okundu ? "text-gray-900 dark:text-gray-100 font-medium" : "text-gray-600 dark:text-gray-400"}`}
                      >
                        {b.icerik}
                      </p>
                      <p className="text-[10px] text-gray-400 mt-0.5">
                        <Clock className="w-3 h-3 inline mr-1" />
                        {format(parseISO(b.olusturulmaTarihi), "dd MMM HH:mm", {
                          locale: tr,
                        })}
                      </p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* İstatistik Widget'ı */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm p-5">
            <h2 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
              Performans Özeti
            </h2>
            <div className="space-y-3">
              <div className="flex justify-between items-center p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Tamamlanan
                </span>
                <span className="font-bold text-emerald-600 dark:text-emerald-400">
                  {myTasks.filter((t) => t.durum === "Tamamlandi").length}
                </span>
              </div>
              <div className="flex justify-between items-center p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Bekleyen
                </span>
                <span className="font-bold text-indigo-600 dark:text-indigo-400">
                  {pendingTasks.length}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Ekip Üyeleri Görevleri */}
      {teamMembers.length > 0 && (
        <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl shadow-sm overflow-hidden mt-6">
          <div className="p-5 border-b border-gray-100 dark:border-gray-800 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
              <Users className="w-5 h-5 text-blue-600 dark:text-blue-500" /> Tüm Ekibin Görev
              Yükü
            </h2>
          </div>

          <div className="p-5 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {teamMembers.map((member) => (
              <div
                key={member.kullaniciId}
                className="border border-gray-100 dark:border-gray-800 rounded-xl p-4 bg-gray-50 dark:bg-gray-800/30 shadow-sm"
              >
                <div className="flex items-center gap-3 mb-4">
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-blue-600 text-white dark:bg-blue-500 shadow-sm font-bold shrink-0">
                    {member.ad?.charAt(0) || "U"}
                  </div>
                  <div>
                    <h3 className="font-semibold text-sm text-gray-900 dark:text-gray-100">
                      {member.ad} {member.soyad}
                    </h3>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      Bekleyen:{" "}
                      {member.aktifGorevler?.filter(
                        (t) => t.durum !== "Tamamlandi",
                      ).length || 0}
                    </p>
                  </div>
                </div>

                <div className="space-y-3">
                  {!member.aktifGorevler ||
                  member.aktifGorevler.filter((t) => t.durum !== "Tamamlandi")
                    .length === 0 ? (
                    <div className="text-xs text-gray-500 italic py-2">
                      Üzerinde aktif görev bulunmuyor.
                    </div>
                  ) : (
                    member.aktifGorevler
                      .filter((t) => t.durum !== "Tamamlandi")
                      .slice(
                        0,
                        genisletilenUyeler.has(member.kullaniciId)
                          ? undefined
                          : 3,
                      )
                      .map((task) => (
                        <div
                          key={task.id}
                          onClick={() => setSelectedTask(task)}
                          className="text-sm bg-white dark:bg-gray-900 p-2.5 rounded-lg border border-gray-100 dark:border-gray-800 shadow-sm flex flex-col gap-1.5 cursor-pointer hover:border-indigo-300 dark:hover:border-indigo-700 transition-colors"
                        >
                          <span
                            className="font-medium text-gray-800 dark:text-gray-200 truncate"
                            title={task.baslik}
                          >
                            {task.baslik}
                          </span>
                          {task.projeAdi && (
                            <span className="text-[10px] text-gray-400 dark:text-gray-500 truncate -mt-1">
                              {task.projeAdi}
                            </span>
                          )}
                          <div className="flex items-center justify-between">
                            <Badge
                              variant="default"
                              className="text-[10px] px-1.5 py-0"
                            >
                              {task.durum}
                            </Badge>
                            <span className="text-[10px] text-gray-500 flex items-center gap-1">
                              <CalendarDays className="w-3 h-3" />{" "}
                              {getDeadlineText(task.deadline)}
                            </span>
                          </div>
                        </div>
                      ))
                  )}
                  {member.aktifGorevler &&
                    member.aktifGorevler.filter((t) => t.durum !== "Tamamlandi")
                      .length > 3 && (
                      <button
                        type="button"
                        onClick={() =>
                          uyeGenisletmeyiDegistir(member.kullaniciId)
                        }
                        className="w-full text-xs text-indigo-600 dark:text-indigo-400 text-center font-medium mt-2 pt-2 border-t border-gray-100 dark:border-gray-800 hover:text-indigo-800 dark:hover:text-indigo-300 cursor-pointer"
                      >
                        {genisletilenUyeler.has(member.kullaniciId)
                          ? "Daha az göster"
                          : `+ ${
                              member.aktifGorevler.filter(
                                (t) => t.durum !== "Tamamlandi",
                              ).length - 3
                            } görev daha`}
                      </button>
                    )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {selectedTask && (
        <TaskDetailsModal
          isOpen={!!selectedTask}
          onClose={() => setSelectedTask(null)}
          task={selectedTask}
          members={teamMembers}
          onTaskUpdated={() => window.location.reload()}
        />
      )}
    </div>
  );
}
