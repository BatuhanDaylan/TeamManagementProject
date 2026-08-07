import { Fragment, useEffect, useState } from "react";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import type { TimelineGorevDto, Result } from "../types";
import { format, parseISO, differenceInCalendarDays } from "date-fns";
import { tr } from "date-fns/locale";
import { signalRService } from "../api/signalr";

const ONCELIK_SIRASI: Record<string, number> = {
  Kritik: 0,
  Yuksek: 1,
  Orta: 2,
  Dusuk: 3,
};

const ONCELIK_ETIKET: Record<string, string> = {
  Kritik: "Kritik",
  Yuksek: "Yüksek",
  Orta: "Orta",
  Dusuk: "Düşük",
};

//Önceliğe göre renk ailesi sabit kalır, tarihe yaklaştıkça (0=çok uzak açık ton,
//4=bugün/geçmiş en koyu ton) sadece o rengin tonu koyulaşır. Tailwind derlemede
//sınıfları taradığı için tonlar burada tam metin olarak yazılmalı (dinamik ${} olmaz).
const ONCELIK_TON_PALETI: Record<string, string[]> = {
  Kritik: ["bg-red-300", "bg-red-400", "bg-red-500", "bg-red-600", "bg-red-700"],
  Yuksek: [
    "bg-amber-300",
    "bg-amber-400",
    "bg-amber-500",
    "bg-amber-600",
    "bg-amber-700",
  ],
  Orta: ["bg-blue-300", "bg-blue-400", "bg-blue-500", "bg-blue-600", "bg-blue-700"],
  Dusuk: ["bg-gray-300", "bg-gray-400", "bg-gray-500", "bg-gray-600", "bg-gray-700"],
};

//Görevin çubuk rengi önce durumuna (tamamlandı/iptal) sonra önceliğine ve
//teslim tarihine kalan güne göre belirlenir
function barRengi(task: TimelineGorevDto, bitis: Date, now: Date): string {
  if (task.durum === "Tamamlandi") return "bg-green-500";
  if (task.durum === "Iptal") return "bg-gray-300 dark:bg-gray-700";

  const gunKaldi = differenceInCalendarDays(bitis, now);
  const tonIndex =
    gunKaldi <= 0 ? 4 : gunKaldi <= 2 ? 3 : gunKaldi <= 7 ? 2 : gunKaldi <= 14 ? 1 : 0;

  const palet = ONCELIK_TON_PALETI[task.oncelik] ?? ONCELIK_TON_PALETI.Orta;
  return palet[tonIndex];
}

const LABEL_GENISLIGI = "14rem";
const TICK_SAYISI = 6;

export default function Timeline() {
  const { user } = useAuthStore();
  const [tasks, setTasks] = useState<TimelineGorevDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchTimeline = async () => {
      if (!user?.ekipId) return;
      try {
        const response = await api.get<Result<TimelineGorevDto[]>>(
          `/Timeline/ekip/${user.ekipId}`,
        );
        if (response.data.isSuccess && response.data.data) {
          setTasks(response.data.data);
        }
      } catch (error) {
        console.error("Timeline yüklenirken hata:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchTimeline();

    signalRService.startConnection().then(() => {
      signalRService.on("GorevGuncellendi", (guncelGorev: TimelineGorevDto) => {
        setTasks((prev) => {
          const exists = prev.find((t) => t.id === guncelGorev.id);
          if (exists) {
            return prev.map((t) =>
              t.id === guncelGorev.id ? { ...t, ...guncelGorev } : t,
            );
          }
          return [...prev, guncelGorev];
        });
      });
    });

    return () => {
      signalRService.off("GorevGuncellendi", () => {});
    };
  }, [user?.ekipId]);

  if (loading) {
    return (
      <div className="p-8 text-center text-gray-500 dark:text-gray-400">
        Yükleniyor...
      </div>
    );
  }

  //Grafikte bir konum verilebilmesi için görevin en az bir tarihi olmalı
  const tarihliGorevler = tasks.filter((t) => t.baslangicTarihi || t.deadline);
  const tarihsizGorevler = tasks.filter(
    (t) => !t.baslangicTarihi && !t.deadline,
  );

  //Görevin çubuk başlangıcı: baslangicTarihi girilmemişse sisteme giriş tarihi taban alınır
  const cubukBaslangici = (t: TimelineGorevDto) =>
    new Date(t.baslangicTarihi ?? t.olusturulmaTarihi).getTime();

  //Tarihe, eşitlik durumunda önceliğe göre soldan sağa sıralanır
  const siraliGorevler = [...tarihliGorevler].sort((a, b) => {
    const aTarih = cubukBaslangici(a);
    const bTarih = cubukBaslangici(b);
    if (aTarih !== bTarih) return aTarih - bTarih;
    return (
      (ONCELIK_SIRASI[a.oncelik] ?? 9) - (ONCELIK_SIRASI[b.oncelik] ?? 9)
    );
  });

  const now = new Date();
  const tumTarihler = tarihliGorevler.flatMap((t) =>
    [t.baslangicTarihi ?? t.olusturulmaTarihi, t.deadline]
      .filter((d): d is string => !!d)
      .map((d) => parseISO(d)),
  );
  tumTarihler.push(now);

  let domainBaslangic = new Date(
    Math.min(...tumTarihler.map((d) => d.getTime())),
  );
  let domainBitis = new Date(
    Math.max(...tumTarihler.map((d) => d.getTime())),
  );

  const GUN_MS = 1000 * 60 * 60 * 24;
  const araligMs = Math.max(
    domainBitis.getTime() - domainBaslangic.getTime(),
    GUN_MS,
  );
  //Çubukların kenara yapışmaması için her iki tarafa da biraz boşluk bırakılır
  const paddingMs = Math.max(araligMs * 0.08, 2 * GUN_MS);
  domainBaslangic = new Date(domainBaslangic.getTime() - paddingMs);
  domainBitis = new Date(domainBitis.getTime() + paddingMs);
  const toplamMs = domainBitis.getTime() - domainBaslangic.getTime();

  const yuzdeyeCevir = (tarih: Date) =>
    ((tarih.getTime() - domainBaslangic.getTime()) / toplamMs) * 100;

  const ticks = Array.from({ length: TICK_SAYISI + 1 }, (_, i) => {
    const t =
      domainBaslangic.getTime() + (toplamMs * i) / TICK_SAYISI;
    return new Date(t);
  });

  const bugunYuzde = yuzdeyeCevir(now);
  const satirSayisi = siraliGorevler.length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
          Timeline
        </h1>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Görevler tarihlerine ve önceliklerine göre soldan sağa dizilir. Çubuk
          rengi önceliği, tonu ise teslim tarihine kalan süreyi gösterir
          (koyulaştıkça tarih yaklaşır).
        </p>
      </div>

      {/* Öncelik / durum lejantı */}
      <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-gray-600 dark:text-gray-400">
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-red-500" /> Kritik
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-amber-500" /> Yüksek
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-blue-500" /> Orta
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-gray-400" /> Düşük
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-green-500" /> Tamamlandı
        </span>
        <span className="flex items-center gap-1.5 ml-auto">
          <span className="inline-block h-3 w-0 border-l-2 border-red-400" />
          Bugün
        </span>
      </div>

      <div className="thin-scrollbar rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 overflow-x-auto">
        {satirSayisi === 0 ? (
          <p className="p-8 text-center text-sm text-gray-500">
            Grafikte gösterilecek tarihli görev bulunamadı.
          </p>
        ) : (
          <div
            className="grid min-w-[900px]"
            style={{ gridTemplateColumns: `${LABEL_GENISLIGI} 1fr` }}
          >
            {siraliGorevler.map((task, idx) => {
              //Çubuk, görevin sisteme giriş tarihinden (baslangicTarihi girilmemişse
              //olusturulmaTarihi taban alınır) teslim tarihine kadar uzanır
              const baslangic = task.baslangicTarihi
                ? parseISO(task.baslangicTarihi)
                : parseISO(task.olusturulmaTarihi);
              const bitis = task.deadline
                ? parseISO(task.deadline)
                : baslangic;
              const solYuzde = yuzdeyeCevir(baslangic);
              //Tek tarihli görevlerde baslangic===bitis olur; okunabilir bir çubuk
              //oluşması için minimum genişlik referans grafiklerdeki gibi büyük tutulur
              const genislikYuzde = Math.max(
                yuzdeyeCevir(bitis) - solYuzde,
                2.5,
              );
              const zebra = idx % 2 === 1;
              const detay = `${task.baslik} — ${task.projeAdi} — ${task.atananAdSoyad}${
                task.baslangicTarihi
                  ? ` — Başlangıç: ${format(parseISO(task.baslangicTarihi), "d MMM yyyy", { locale: tr })}`
                  : ""
              }${
                task.deadline
                  ? ` — Bitiş: ${format(parseISO(task.deadline), "d MMM yyyy", { locale: tr })}`
                  : ""
              } — ${ONCELIK_ETIKET[task.oncelik] ?? task.oncelik}`;

              return (
                <Fragment key={task.id}>
                  <div
                    className={`flex flex-col justify-center gap-0.5 border-b border-gray-100 px-4 py-2.5 dark:border-gray-800 ${zebra ? "bg-gray-50/60 dark:bg-gray-800/30" : ""}`}
                    style={{ gridColumn: 1, gridRow: idx + 1 }}
                  >
                    <p
                      className={`truncate text-sm font-medium text-gray-900 dark:text-gray-100 ${task.durum === "Iptal" ? "line-through opacity-60" : ""}`}
                      title={task.baslik}
                    >
                      {task.baslik}
                    </p>
                    <p className="truncate text-xs text-gray-500 dark:text-gray-400">
                      {task.projeAdi} · {task.atananAdSoyad}
                    </p>
                  </div>

                  <div
                    className={`relative border-b border-gray-100 dark:border-gray-800 ${zebra ? "bg-gray-50/60 dark:bg-gray-800/30" : ""}`}
                    style={{ gridColumn: 2, gridRow: idx + 1, minHeight: 52 }}
                  >
                    {/* Az sayıda görev olduğunda satırın boş kalan kısmı tamamen
                        boşluk gibi durmasın diye ince bir taban çizgisi eklenir */}
                    <div className="absolute top-1/2 left-0 right-0 h-px -translate-y-1/2 bg-gray-100 dark:bg-gray-800" />
                    <div
                      className={`absolute top-1/2 h-5 -translate-y-1/2 rounded-full ${barRengi(task, bitis, now)} shadow-sm`}
                      style={{ left: `${solYuzde}%`, width: `${genislikYuzde}%` }}
                      title={detay}
                    />
                  </div>
                </Fragment>
              );
            })}

            {/* Tarih ekseni (alt kısım) */}
            <div style={{ gridColumn: 1, gridRow: satirSayisi + 1 }} />
            <div
              className="relative"
              style={{ gridColumn: 2, gridRow: satirSayisi + 1, minHeight: 36 }}
            >
              {ticks.map((tick, i) => (
                <div
                  key={i}
                  className="absolute top-2 -translate-x-1/2 whitespace-nowrap text-[11px] text-gray-500 dark:text-gray-400"
                  style={{ left: `${yuzdeyeCevir(tick)}%` }}
                >
                  {format(tick, "d MMM", { locale: tr })}
                </div>
              ))}
            </div>

            {/* Dikey referans çizgileri + bugün çizgisi, tüm satırların üzerine (2. sütun) yayılır */}
            <div
              className="relative pointer-events-none"
              style={{ gridColumn: 2, gridRow: `1 / span ${satirSayisi + 1}` }}
            >
              {ticks.map((tick, i) => (
                <div
                  key={i}
                  className="absolute top-0 bottom-0 border-l border-dashed border-gray-200 dark:border-gray-800"
                  style={{ left: `${yuzdeyeCevir(tick)}%` }}
                />
              ))}
              {bugunYuzde >= 0 && bugunYuzde <= 100 && (
                <div
                  className="absolute top-0 bottom-0 border-l-2 border-red-400"
                  style={{ left: `${bugunYuzde}%` }}
                >
                  <span className="absolute -top-5 -translate-x-1/2 whitespace-nowrap rounded bg-red-50 px-1 text-[10px] font-bold text-red-500 dark:bg-red-900/30">
                    BUGÜN
                  </span>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {tarihsizGorevler.length > 0 && (
        <div>
          <h2 className="mb-2 text-sm font-semibold text-gray-700 dark:text-gray-300">
            Tarihi belirtilmemiş görevler
          </h2>
          <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 divide-y divide-gray-100 dark:divide-gray-800">
            {tarihsizGorevler.map((task) => (
              <div
                key={task.id}
                className="flex items-center justify-between gap-3 px-4 py-2.5 text-sm"
              >
                <span
                  className={`font-medium text-gray-900 dark:text-gray-100 ${task.durum === "Iptal" ? "line-through opacity-60" : ""}`}
                >
                  {task.baslik}
                </span>
                <span className="text-xs text-gray-500 dark:text-gray-400">
                  {task.projeAdi} · {task.atananAdSoyad}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
