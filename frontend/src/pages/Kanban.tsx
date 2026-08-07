import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import api from "../api/axios";
import type { UyeGorevOzetiDto, GorevDto, Result } from "../types";
import { Badge } from "../components/Badge";
import {
  DndContext,
  DragOverlay,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  PointerSensor,
  pointerWithin,
} from "@dnd-kit/core";
import type { DragEndEvent, DragStartEvent } from "@dnd-kit/core";
import { signalRService } from "../api/signalr";
import { Plus } from "lucide-react";
import { Button } from "../components/Button";
import { CreateGorevModal } from "../components/forms/CreateGorevModal";
import { TaskDetailsModal } from "../components/forms/TaskDetailsModal";

const SAHIPSIZ_ID = "Sahipsiz";

//Sütunların doğal ileri/geri sırası; hangi geçişin "ileri" hangisinin "geri" sayılacağını belirler
const STATUS_SIRASI = [
  "Backlog",
  "DevamEdiyor",
  "Test",
  "OnayBekliyor",
  "Tamamlandi",
  "Iptal",
];

//Üye sadece bu tek adımlık ileri geçişleri yapabilir (backend'deki UpdateGorevDurumWithRoleCheckAsync
//ile birebir aynı kural) — burada da kontrol edilir ki geçersiz bir sürüklemede önce ekrana yansıyıp
//sonra hatayla geri sıçramasın, daha baştan hiç kabul edilmesin
const UYE_GECERLI_GECISLER: Record<string, string> = {
  Backlog: "DevamEdiyor",
  DevamEdiyor: "Test",
  Test: "OnayBekliyor",
};

const COLUMNS = [
  { id: "Backlog", title: "Backlog" },
  { id: "DevamEdiyor", title: "Devam Ediyor" },
  { id: "Test", title: "Test" },
  { id: "OnayBekliyor", title: "Onay Bekliyor" },
  { id: "Tamamlandi", title: "Tamamlandı" },
  { id: "Iptal", title: "İptal Edildi" },
];

//Kartın görsel içeriği; hem normal (sürüklenebilir) haliyle hem de DragOverlay'deki
//taşınan kopyada birebir aynı görünsün diye ortak bileşende tutulur
function GorevKartIcerigi({
  task,
  ownerName,
}: {
  task: GorevDto;
  ownerName?: string;
}) {
  return (
    <>
      <div className="mb-2 flex items-start justify-between">
        <Badge
          variant={
            task.oncelik === "Kritik"
              ? "danger"
              : task.oncelik === "Yuksek"
                ? "warning"
                : "default"
          }
        >
          {task.oncelik}
        </Badge>
        {ownerName && (
          <span className="text-[11px] font-medium text-gray-500 dark:text-gray-400 truncate max-w-[100px]">
            {ownerName}
          </span>
        )}
      </div>
      <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 line-clamp-2 mb-1">
        {task.baslik}
      </h4>
      <p className="text-xs text-gray-500 line-clamp-2">{task.aciklama}</p>
    </>
  );
}

function DraggableCard({
  task,
  onClick,
  ownerName,
  isOwn,
}: {
  task: GorevDto;
  onClick: (task: GorevDto) => void;
  ownerName?: string;
  isOwn?: boolean;
}) {
  //Kartın kendisi artık sürükleme sırasında yer değiştirmiyor (transform uygulanmıyor); onun
  //yerine DragOverlay ile bir kopyası fare imlecini takip ediyor. Bu sayede kolonların kendi
  //içindeki "overflow-y-auto" kaydırma alanının dışına çıkınca kart kesilip kaybolmuyor —
  //DragOverlay document.body'ye portal ile render edildiği için hiçbir üst elemanın overflow'una
  //takılmıyor. Orijinal kart, sürükleme sırasında sadece soluk bir "yer tutucu" olarak kalıyor.
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: task.id,
    data: task,
  });

  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      onClick={() => {
        if (!isDragging) onClick(task);
      }}
      className={`cursor-grab rounded-lg p-4 shadow-sm border hover:shadow-md transition-shadow touch-none ${
        isOwn
          ? "bg-indigo-50 border-indigo-300 dark:bg-indigo-950/40 dark:border-indigo-700"
          : "bg-white border-gray-200 dark:border-gray-700 dark:bg-gray-900"
      } ${isDragging ? "opacity-40" : ""}`}
    >
      <GorevKartIcerigi task={task} ownerName={ownerName} />
    </div>
  );
}

function DroppableColumn({
  colId,
  title,
  tasks,
  onTaskClick,
  disabled,
  getOwnerName,
  currentUserId,
}: {
  colId: string;
  title: string;
  tasks: GorevDto[];
  onTaskClick: (task: GorevDto) => void;
  disabled?: boolean;
  getOwnerName?: (kullaniciId?: string) => string | undefined;
  currentUserId?: string;
}) {
  const { isOver, setNodeRef } = useDroppable({
    id: colId,
    disabled,
  });

  // Kullanıcının kendi görevleri her zaman kolonun en üstünde gösterilir
  const sortedTasks = [...tasks].sort((a, b) => {
    const aOwn = currentUserId && a.atananId === currentUserId ? 1 : 0;
    const bOwn = currentUserId && b.atananId === currentUserId ? 1 : 0;
    return bOwn - aOwn;
  });

  return (
    <div
      className={`flex w-80 shrink-0 flex-col rounded-xl p-4 border transition-colors ${isOver ? "bg-indigo-50 border-indigo-200 dark:bg-indigo-900/20 dark:border-indigo-800" : "bg-gray-100/50 border-gray-200 dark:bg-gray-800/50 dark:border-gray-700"}`}
    >
      <div className="mb-4 flex items-center justify-between">
        <h3 className="font-semibold text-gray-700 dark:text-gray-200">
          {title}
        </h3>
        <Badge variant="default">{tasks.length}</Badge>
      </div>
      {/* Bir kolona çok sayıda görev düşerse (ör. mail akışından toplu görev gelmesi) kolon
          sonsuza kadar uzayıp sayfayı bozmasın diye kendi içinde ayrı kaydırılır, başlık sabit kalır */}
      <div
        ref={setNodeRef}
        className="thin-scrollbar flex-1 space-y-3 min-h-[150px] max-h-[calc(100vh-280px)] overflow-y-auto pr-1"
      >
        {sortedTasks.map((task) => (
          <DraggableCard
            key={task.id}
            task={task}
            onClick={onTaskClick}
            ownerName={getOwnerName?.(task.atananId)}
            isOwn={!!currentUserId && task.atananId === currentUserId}
          />
        ))}
        {tasks.length === 0 && (
          <div className="flex h-20 items-center justify-center rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-700">
            <span className="text-xs text-gray-400">Görev yok</span>
          </div>
        )}
      </div>
    </div>
  );
}

export default function Kanban() {
  const { user } = useAuthStore();
  //Varsayılan PointerSensor mesafe eşiği olmadan en ufak tıklamayı bile sürükleme sayıyor;
  //bu yüzden kart üzerindeki onClick (detay penceresini açan) hiç tetiklenmiyordu. 8px'lik bir
  //eşik konunca, imleç gerçekten hareket etmeden bırakılan tıklamalar sürükleme başlatmıyor.
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
  );
  const [searchParams, setSearchParams] = useSearchParams();
  const [tasks, setTasks] = useState<GorevDto[]>([]);
  const [members, setMembers] = useState<UyeGorevOzetiDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState("");
  const [isGorevModalOpen, setGorevModalOpen] = useState(false);

  const [selectedTask, setSelectedTask] = useState<GorevDto | null>(null);
  const [isTaskDetailsOpen, setTaskDetailsOpen] = useState(false);
  const [activeTask, setActiveTask] = useState<GorevDto | null>(null);

  const handleDragStart = (event: DragStartEvent) => {
    setActiveTask(event.active.data.current as GorevDto);
  };

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        if (!user?.ekipId) return;
        // Üye listesi (atama dropdown'ı için) Dashboard'tan, görev listesi ise ayrı
        // uçtan alınır: Dashboard sadece bir üyeye atanmış görevleri döndürür, ama
        // mail akışından eklenen görevlerin ataması olmayabilir — o yüzden Kanban
        // panosu atanmış/atanmamış fark etmeksizin ekibin TÜM görevlerini göstermeli.
        const [membersRes, tasksRes] = await Promise.all([
          api.get<Result<UyeGorevOzetiDto[]>>(`/Dashboard/ekip/${user.ekipId}`),
          api.get<Result<GorevDto[]>>(`/Gorev/ekip/${user.ekipId}`),
        ]);

        if (membersRes.data.isSuccess && membersRes.data.data) {
          setMembers(membersRes.data.data);
        }

        if (tasksRes.data.isSuccess && tasksRes.data.data) {
          setTasks(tasksRes.data.data);
        } else {
          setErrorMsg(tasksRes.data.error || "Veriler alınamadı.");
        }
      } catch (err) {
        console.error("Pano verileri alınamadı:", err);
        setErrorMsg("Sunucuya bağlanılamadı.");
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();

    // SignalR bağlantısını başlat, proje gruplarına katıl ve GorevGuncellendi olayını dinle
    signalRService.startConnection().then(async () => {
      signalRService.on("GorevGuncellendi", (guncelGorev: GorevDto) => {
        setTasks((prev) => {
          const exists = prev.find((t) => t.id === guncelGorev.id);
          if (exists) {
            return prev.map((t) => (t.id === guncelGorev.id ? guncelGorev : t));
          }
          return [...prev, guncelGorev];
        });

        // Eğer açık olan görevin detayı güncellendiyse onu da yansıt
        setSelectedTask((prev) =>
          prev?.id === guncelGorev.id ? guncelGorev : prev,
        );
      });
    });

    return () => {
      signalRService.off("GorevGuncellendi", () => {});
    };
  }, [user?.ekipId]);

  const handleDragEnd = async (event: DragEndEvent) => {
    setActiveTask(null);
    const { active, over } = event;
    if (!over) return;

    const taskId = active.id as string;
    const newStatus = over.id as string;
    const activeTask = active.data.current as GorevDto;

    // Sahipsiz (atanmamış) bir görev: sadece Backlog'a bırakılarak üstlenilebilir,
    // bu görev bir daha sahipsiz gruba geri döndürülemez.
    if (!activeTask.atananId) {
      if (newStatus !== "Backlog") {
        setErrorMsg("Sahipsiz bir görevi üstlenmek için Backlog'a bırakmalısınız.");
        setTimeout(() => setErrorMsg(""), 4000);
        return;
      }

      const previousTasks = [...tasks];
      setTasks(
        tasks.map((t) =>
          t.id === taskId ? { ...t, atananId: user?.id } : t,
        ),
      );
      setErrorMsg("");

      try {
        await api.patch(`/Gorev/${taskId}/ustlen`);
      } catch (err: any) {
        setTasks(previousTasks);
        setErrorMsg(
          err.response?.data?.error || "Görev üstlenilirken bir hata oluştu.",
        );
        setTimeout(() => setErrorMsg(""), 4000);
      }
      return;
    }

    if (activeTask.durum === newStatus) return; // Didn't change column

    const isLider = user?.rol === "Lider";

    //Üye için: geçerli tek adımlık ileri geçiş dışındaki her sürükleme daha baştan reddedilir,
    //ekrana hiç yansıtılmaz (kabul edilip sonra hatayla geri sıçraması yerine)
    if (!isLider && UYE_GECERLI_GECISLER[activeTask.durum] !== newStatus) {
      setErrorMsg(
        "Sadece ileri yönlü, tek adımlık durum geçişlerine izin veriliyor. Bir görevi geri almak için ekip liderinize danışın.",
      );
      setTimeout(() => setErrorMsg(""), 4000);
      return;
    }

    const mevcutIndex = STATUS_SIRASI.indexOf(activeTask.durum);
    const yeniIndex = STATUS_SIRASI.indexOf(newStatus);
    const ileriYonde = yeniIndex > mevcutIndex;

    //Geri alma sadece Lider'in ayrıcalığı olduğu için onay istenmeden direkt uygulanır;
    //ileri yöndeki her geçişte (üye ya da lider fark etmeksizin) önce onay istenir
    if (ileriYonde) {
      const yeniDurumBaslik =
        COLUMNS.find((c) => c.id === newStatus)?.title || newStatus;
      const onaylandi = window.confirm(
        `Bu görevi '${yeniDurumBaslik}' durumuna taşımak istediğinize emin misiniz?`,
      );
      if (!onaylandi) return;
    }

    // Optimistic UI update (Anında ekrana yansıt)
    const previousTasks = [...tasks];
    setTasks(
      tasks.map((t) =>
        t.id === taskId ? { ...t, durum: newStatus as any } : t,
      ),
    );
    setErrorMsg("");

    try {
      await api.patch(`/Gorev/${taskId}/durum`, {
        yeniDurum: newStatus,
      });
      // API call başarılı, SignalR ayrıca bu olayı herkese (bize de) yayınlayacak.
    } catch (err: any) {
      // Hata gelirse geri al (Rollback)
      setTasks(previousTasks);
      setErrorMsg(
        err.response?.data?.error ||
          "Durum güncellenirken bir yetki hatası oluştu.",
      );
      setTimeout(() => setErrorMsg(""), 4000);
    }
  };

  const handleTaskClick = (task: GorevDto) => {
    setSelectedTask(task);
    setTaskDetailsOpen(true);
  };

  //Bildirimden "gorevId" parametresiyle gelindiyse, görev listesi yüklendikten sonra
  //o görevin detay modalı otomatik açılır
  useEffect(() => {
    const gorevId = searchParams.get("gorevId");
    if (!gorevId || tasks.length === 0) return;

    const hedefGorev = tasks.find((t) => t.id === gorevId);
    if (hedefGorev) {
      handleTaskClick(hedefGorev);
      const yeniParams = new URLSearchParams(searchParams);
      yeniParams.delete("gorevId");
      setSearchParams(yeniParams, { replace: true });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tasks, searchParams]);

  const getOwnerName = (kullaniciId?: string) => {
    if (!kullaniciId) return undefined;
    const uye = members.find((m) => m.kullaniciId === kullaniciId);
    return uye ? `${uye.ad} ${uye.soyad}` : undefined;
  };

  if (loading) {
    return (
      <div className="p-8 text-center text-gray-500 dark:text-gray-400">
        Yükleniyor...
      </div>
    );
  }

  return (
    <div className="space-y-6 h-full flex flex-col">
      <CreateGorevModal
        isOpen={isGorevModalOpen}
        onClose={() => setGorevModalOpen(false)}
        members={members}
      />

      <TaskDetailsModal
        isOpen={isTaskDetailsOpen}
        onClose={() => {
          setTaskDetailsOpen(false);
          setTimeout(() => setSelectedTask(null), 200); // animasyon süresi
        }}
        task={selectedTask}
        members={members}
        onTaskUpdated={(updatedTask) => {
          setTasks((prev) =>
            prev.map((t) => (t.id === updatedTask.id ? updatedTask : t)),
          );
          setSelectedTask(updatedTask);
        }}
        onTaskDeleted={(taskId) => {
          setTasks((prev) => prev.filter((t) => t.id !== taskId));
        }}
      />

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
            Ekip Panosu
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Tüm projelerdeki görevlerin güncel durumu. Görevleri sürükleyip
            bırakabilirsiniz.
          </p>
        </div>
        {user?.rol === "Lider" && (
          <Button onClick={() => setGorevModalOpen(true)}>
            <Plus className="mr-2 h-4 w-4" /> Görev Ekle
          </Button>
        )}
      </div>

      {errorMsg && (
        <div className="rounded-md bg-red-50 p-4 border border-red-200 dark:bg-red-900/30 dark:border-red-800 transition-all">
          <p className="text-sm text-red-700 dark:text-red-300 font-medium">
            {errorMsg}
          </p>
        </div>
      )}

      <DndContext
        sensors={sensors}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        onDragCancel={() => setActiveTask(null)}
        collisionDetection={pointerWithin}
      >
        <div className="thin-scrollbar flex flex-1 gap-6 overflow-x-auto pb-4">
          <DroppableColumn
            colId={SAHIPSIZ_ID}
            title="Sahipsiz Görevler"
            tasks={tasks.filter((t) => !t.atananId)}
            onTaskClick={handleTaskClick}
            disabled
          />
          {COLUMNS.map((col) => {
            const columnTasks = tasks.filter(
              (t) => t.durum === col.id && !!t.atananId,
            );
            return (
              <DroppableColumn
                key={col.id}
                colId={col.id}
                title={col.title}
                tasks={columnTasks}
                onTaskClick={handleTaskClick}
                getOwnerName={getOwnerName}
                currentUserId={user?.id}
              />
            );
          })}
        </div>

        <DragOverlay>
          {activeTask && (
            <div className="cursor-grabbing rounded-lg p-4 shadow-xl ring-2 ring-indigo-500 border bg-white border-gray-200 dark:border-gray-700 dark:bg-gray-900 w-72">
              <GorevKartIcerigi
                task={activeTask}
                ownerName={getOwnerName(activeTask.atananId)}
              />
            </div>
          )}
        </DragOverlay>
      </DndContext>
    </div>
  );
}
