import { useState, useEffect } from "react";
import { Modal } from "../Modal";
import { Button } from "../Button";
import { Input } from "../Input";
import { Badge } from "../Badge";
import { TaskComments } from "../TaskComments";
import { TaskBugs } from "../TaskBugs";
import { GorevIlerlemeler } from "../GorevIlerlemeler";
import api from "../../api/axios";
import { useAuthStore } from "../../store/authStore";
import type { GorevDto, UyeGorevOzetiDto } from "../../types";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";
import { CalendarIcon, User, Tag, Trash2, Edit2, Save, X } from "lucide-react";

interface TaskDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  task: GorevDto | null;
  members: UyeGorevOzetiDto[];
  onTaskUpdated?: (updatedTask: GorevDto) => void;
  onTaskDeleted?: (taskId: string) => void;
}

export function TaskDetailsModal({
  isOpen,
  onClose,
  task,
  members,
  onTaskUpdated,
  onTaskDeleted,
}: TaskDetailsModalProps) {
  const { user } = useAuthStore();
  const [isEditing, setIsEditing] = useState(false);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const [activeTab, setActiveTab] = useState<
    "detaylar" | "yorumlar" | "hatalar" | "ilerlemeler"
  >("detaylar");

  const [baslik, setBaslik] = useState("");
  const [aciklama, setAciklama] = useState("");
  const [oncelik, setOncelik] = useState("Orta");
  const [atananId, setAtananId] = useState("");
  const [deadline, setDeadline] = useState("");

  const isLider = user?.rol === "Lider";

  useEffect(() => {
    if (task && isOpen) {
      setBaslik(task.baslik);
      setAciklama(task.aciklama || "");
      setOncelik(task.oncelik);
      setAtananId(task.atananId || "atanmamis");
      setDeadline(
        task.deadline
          ? format(parseISO(task.deadline), "yyyy-MM-dd'T'HH:mm")
          : "",
      );
      setIsEditing(false);
      setErrorMsg("");
    }
  }, [task, isOpen]);

  const handleUpdate = async () => {
    if (!task) return;
    setLoading(true);
    setErrorMsg("");
    try {
      const payload = {
        baslik,
        aciklama,
        oncelik,
        atananId: atananId === "atanmamis" ? null : atananId,
        deadline: deadline ? new Date(deadline).toISOString() : null,
      };
      const res = await api.put(`/Gorev/${task.id}`, payload);

      if (res.data.isSuccess) {
        setIsEditing(false);
        // Optimistic UI update or signalR will handle it
        if (onTaskUpdated) {
          onTaskUpdated({ ...task, ...payload } as GorevDto);
        }
      }
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.error || "Görev güncellenirken bir hata oluştu.",
      );
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!task || !confirm("Bu görevi silmek istediğinize emin misiniz?"))
      return;
    setLoading(true);
    try {
      const res = await api.delete(`/Gorev/sil/${task.id}`);
      if (res.status === 200) {
        if (onTaskDeleted) onTaskDeleted(task.id);
        onClose();
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error || "Silme işlemi başarısız.");
    } finally {
      setLoading(false);
    }
  };

  if (!task) return null;

  const atananUye = members.find((m) => m.kullaniciId === task.atananId);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEditing ? "Görevi Düzenle" : "Görev Detayları"}
      className="max-w-3xl"
    >
      <div className="space-y-4">
        {errorMsg && (
          <div className="text-sm text-red-600 bg-red-50 dark:bg-red-900/30 dark:text-red-400 p-3 rounded-md">
            {errorMsg}
          </div>
        )}

        {/* Sekmeler (Tabs) */}
        {!isEditing && (
          <div className="flex border-b border-gray-200 dark:border-gray-800">
            <button
              onClick={() => setActiveTab("detaylar")}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "detaylar" ? "border-indigo-500 text-indigo-600 dark:text-indigo-400" : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"}`}
            >
              Detaylar
            </button>
            <button
              onClick={() => setActiveTab("yorumlar")}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "yorumlar" ? "border-indigo-500 text-indigo-600 dark:text-indigo-400" : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"}`}
            >
              Yorumlar
            </button>
            <button
              onClick={() => setActiveTab("hatalar")}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "hatalar" ? "border-red-500 text-red-600 dark:text-red-400" : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"}`}
            >
              Hatalar (Bugs)
            </button>
            <button
              onClick={() => setActiveTab("ilerlemeler")}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors flex items-center gap-1 ${activeTab === "ilerlemeler" ? "border-indigo-500 text-indigo-600 dark:text-indigo-400" : "border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"}`}
            >
              İlerlemeler (AI)
            </button>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-2">
          {/* Sol Kısım: Aktif Sekme İçeriği */}
          <div className="md:col-span-2 space-y-4">
            {isEditing ? (
              <div className="space-y-3">
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                    Başlık
                  </label>
                  <Input
                    value={baslik}
                    onChange={(e) => setBaslik(e.target.value)}
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                    Açıklama
                  </label>
                  <textarea
                    value={aciklama}
                    onChange={(e) => setAciklama(e.target.value)}
                    className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white min-h-[100px]"
                  />
                </div>
              </div>
            ) : (
              <>
                {activeTab === "detaylar" ? (
                  <div>
                    <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-2">
                      {task.baslik}
                    </h3>
                    <div className="prose prose-sm dark:prose-invert max-w-none text-gray-600 dark:text-gray-300 whitespace-pre-wrap">
                      {task.aciklama || (
                        <span className="italic text-gray-400">
                          Açıklama eklenmemiş.
                        </span>
                      )}
                    </div>
                  </div>
                ) : activeTab === "yorumlar" ? (
                  <TaskComments gorevId={task.id} />
                ) : activeTab === "hatalar" ? (
                  <TaskBugs gorevId={task.id} />
                ) : activeTab === "ilerlemeler" ? (
                  <GorevIlerlemeler gorevId={task.id} />
                ) : null}
              </>
            )}
          </div>

          {/* Sağ Kısım: Meta Bilgiler (Her zaman görünür) */}
          <div className="space-y-4 bg-gray-50 dark:bg-gray-800/50 p-4 rounded-xl border border-gray-100 dark:border-gray-800 h-fit">
            <div>
              <label className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1 flex items-center gap-1">
                <Tag className="w-3 h-3" /> Durum & Öncelik
              </label>
              {!isEditing ? (
                <div className="flex gap-2 mt-1">
                  <Badge>{task.durum}</Badge>
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
                </div>
              ) : (
                <select
                  value={oncelik}
                  onChange={(e) => setOncelik(e.target.value)}
                  className="mt-1 flex h-9 w-full rounded-md border border-gray-300 bg-white px-3 py-1 text-sm focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900"
                >
                  <option value="Dusuk">Düşük</option>
                  <option value="Orta">Orta</option>
                  <option value="Yuksek">Yüksek</option>
                  <option value="Kritik">Kritik</option>
                </select>
              )}
            </div>

            <div>
              <label className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1 flex items-center gap-1">
                <User className="w-3 h-3" /> Sorumlu
              </label>
              {!isEditing ? (
                <div className="mt-1 font-medium text-sm text-gray-900 dark:text-gray-200">
                  {atananUye ? (
                    `${atananUye.ad} ${atananUye.soyad}`
                  ) : (
                    <span className="text-gray-400">Atanmamış</span>
                  )}
                </div>
              ) : (
                <select
                  value={atananId}
                  onChange={(e) => setAtananId(e.target.value)}
                  className="mt-1 flex h-9 w-full rounded-md border border-gray-300 bg-white px-3 py-1 text-sm focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900"
                >
                  <option value="atanmamis">-- Atanmamış --</option>
                  {members.map((m) => (
                    <option key={m.kullaniciId} value={m.kullaniciId}>
                      {m.ad} {m.soyad}
                    </option>
                  ))}
                </select>
              )}
            </div>

            <div>
              <label className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1 flex items-center gap-1">
                <CalendarIcon className="w-3 h-3" /> Bitiş Tarihi
              </label>
              {!isEditing ? (
                <div className="mt-1 text-sm font-medium text-gray-900 dark:text-gray-200">
                  {task.deadline ? (
                    format(parseISO(task.deadline), "dd MMM yyyy HH:mm", {
                      locale: tr,
                    })
                  ) : (
                    <span className="text-gray-400">Belirtilmemiş</span>
                  )}
                </div>
              ) : (
                <Input
                  type="datetime-local"
                  value={deadline}
                  onChange={(e) => setDeadline(e.target.value)}
                  className="mt-1 h-9"
                />
              )}
            </div>

            {!isEditing && (
              <div className="pt-2">
                <div className="text-[10px] text-gray-400">
                  Oluşturulma:{" "}
                  {format(parseISO(task.oluşturulmaTarihi), "dd MMM yyyy")}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Aksiyon Butonları */}
        {isLider && (
          <div className="flex items-center justify-end gap-2 pt-4 border-t dark:border-gray-800">
            {isEditing ? (
              <>
                <Button
                  variant="ghost"
                  onClick={() => setIsEditing(false)}
                  disabled={loading}
                >
                  <X className="w-4 h-4 mr-2" /> İptal
                </Button>
                <Button onClick={handleUpdate} disabled={loading}>
                  <Save className="w-4 h-4 mr-2" /> Kaydet
                </Button>
              </>
            ) : (
              <>
                <Button
                  variant="outline"
                  className="text-red-600 border-red-200 hover:bg-red-50 dark:text-red-400 dark:border-red-900/50 dark:hover:bg-red-900/20"
                  onClick={handleDelete}
                  disabled={loading}
                >
                  <Trash2 className="w-4 h-4 mr-2" /> Sil
                </Button>
                <Button
                  variant="outline"
                  onClick={() => {
                    setIsEditing(true);
                    setActiveTab("detaylar");
                  }}
                >
                  <Edit2 className="w-4 h-4 mr-2" /> Düzenle
                </Button>
              </>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}
