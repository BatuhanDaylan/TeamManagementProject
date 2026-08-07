import React, { useState, useEffect, useRef } from "react";
import api from "../api/axios";
import { useAuthStore } from "../store/authStore";
import type { Result, YorumDto } from "../types";
import { Button } from "./Button";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";
import { Send, MessageSquare, Pencil, Trash2, Check, X } from "lucide-react";
import { signalRService } from "../api/signalr";

export function TaskComments({ gorevId }: { gorevId: string }) {
  const { user } = useAuthStore();
  const [yorumlar, setYorumlar] = useState<YorumDto[]>([]);
  const [yeniYorum, setYeniYorum] = useState("");
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editText, setEditText] = useState("");
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    fetchYorumlar();

    // YorumEklendi SignalR eventini dinle — polling yerine real-time
    const handler = (yorum: YorumDto) => {
      if (yorum.gorevId === gorevId) {
        setYorumlar((prev) => {
          // Aynı yorum tekrar eklenmesini önle
          if (prev.find((y) => y.id === yorum.id)) return prev;
          return [...prev, yorum];
        });
      }
    };

    signalRService.startConnection().then(() => {
      signalRService.on("YorumEklendi", handler);
    });

    return () => {
      signalRService.off("YorumEklendi", handler);
    };
  }, [gorevId]);

  const fetchYorumlar = async () => {
    try {
      const res = await api.get<Result<YorumDto[]>>(`/Yorum/gorev/${gorevId}`);
      if (res.data.isSuccess && res.data.data) {
        setYorumlar(res.data.data);
        setTimeout(() => bottomRef.current?.scrollIntoView({ behavior: "smooth" }), 50);
      }
    } catch (error) {
      console.error("Yorumlar alınamadı:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!yeniYorum.trim()) return;

    setSending(true);
    try {
      await api.post("/Yorum/ekle", {
        gorevId,
        icerik: yeniYorum,
      });
      setYeniYorum("");
      fetchYorumlar(); // Hemen yenile
    } catch (error) {
      console.error("Yorum eklenemedi:", error);
    } finally {
      setSending(false);
    }
  };

  const handleEditStart = (yorum: YorumDto) => {
    setEditingId(yorum.id);
    setEditText(yorum.icerik);
  };

  const handleEditCancel = () => {
    setEditingId(null);
    setEditText("");
  };

  const handleEditSave = async (yorumId: string) => {
    if (!editText.trim()) return;
    try {
      await api.put(`/Yorum/${yorumId}`, { icerik: editText });
      setYorumlar((prev) =>
        prev.map((y) =>
          y.id === yorumId ? { ...y, icerik: editText, duzenlendi: true } : y,
        ),
      );
      setEditingId(null);
      setEditText("");
    } catch (error) {
      console.error("Yorum düzenlenemedi:", error);
    }
  };

  const handleDelete = async (yorumId: string) => {
    if (!confirm("Bu yorumu silmek istediğinize emin misiniz?")) return;
    try {
      await api.delete(`/Yorum/${yorumId}`);
      setYorumlar((prev) => prev.filter((y) => y.id !== yorumId));
    } catch (error) {
      console.error("Yorum silinemedi:", error);
    }
  };

  return (
    <div className="flex flex-col h-[400px] border rounded-lg bg-gray-50/50 dark:bg-gray-900/50 dark:border-gray-800">
      <div className="p-3 border-b dark:border-gray-800 flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300">
        <MessageSquare className="h-4 w-4" /> Yorumlar ({yorumlar.length})
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4" id="yorum-listesi">
        {loading && yorumlar.length === 0 ? (
          <div className="text-center text-sm text-gray-500 dark:text-gray-400 py-4">
            Yükleniyor...
          </div>
        ) : yorumlar.length === 0 ? (
          <div className="text-center text-sm text-gray-500 dark:text-gray-400 py-8">
            Henüz yorum yapılmamış. İlk yorumu siz yapın!
          </div>
        ) : (
          yorumlar.map((yorum) => {
            const isMe = yorum.yazanId === user?.id;
            const isEditing = editingId === yorum.id;
            return (
              <div
                key={yorum.id}
                className={`flex ${isMe ? "justify-end" : "justify-start"}`}
              >
                <div
                  className={`group max-w-[85%] rounded-lg p-3 ${isMe ? "bg-indigo-600 text-white rounded-br-none" : "bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-bl-none"}`}
                >
                  <div className="flex items-center justify-between mb-1 gap-4">
                    <span
                      className={`text-xs font-semibold ${isMe ? "text-indigo-100" : "text-gray-900 dark:text-gray-200"}`}
                    >
                      {isMe ? "Siz" : yorum.yazanAdSoyad}
                    </span>
                    <div className="flex items-center gap-2">
                      <span
                        className={`text-[10px] ${isMe ? "text-indigo-200" : "text-gray-500"}`}
                      >
                        {format(
                          parseISO(yorum.olusturulmaTarihi),
                          "dd MMM HH:mm",
                          { locale: tr },
                        )}
                        {yorum.duzenlendi && " (düzenlendi)"}
                      </span>
                      {isMe && !isEditing && (
                        <div className="hidden group-hover:flex items-center gap-1">
                          <button
                            onClick={() => handleEditStart(yorum)}
                            className={isMe ? "text-indigo-200 hover:text-white" : "text-gray-400 hover:text-gray-700"}
                            title="Düzenle"
                          >
                            <Pencil className="h-3 w-3" />
                          </button>
                          <button
                            onClick={() => handleDelete(yorum.id)}
                            className={isMe ? "text-indigo-200 hover:text-white" : "text-gray-400 hover:text-red-600"}
                            title="Sil"
                          >
                            <Trash2 className="h-3 w-3" />
                          </button>
                        </div>
                      )}
                    </div>
                  </div>

                  {isEditing ? (
                    <div className="space-y-2">
                      <textarea
                        value={editText}
                        onChange={(e) => setEditText(e.target.value)}
                        className="w-full rounded-md border border-gray-300 bg-white px-2 py-1.5 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-900 dark:text-white"
                        rows={2}
                        autoFocus
                      />
                      <div className="flex justify-end gap-1">
                        <button
                          onClick={handleEditCancel}
                          className={`p-1 rounded ${isMe ? "text-indigo-100 hover:bg-indigo-700" : "text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"}`}
                          title="İptal"
                        >
                          <X className="h-3.5 w-3.5" />
                        </button>
                        <button
                          onClick={() => handleEditSave(yorum.id)}
                          className={`p-1 rounded ${isMe ? "text-indigo-100 hover:bg-indigo-700" : "text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"}`}
                          title="Kaydet"
                        >
                          <Check className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </div>
                  ) : (
                    <p
                      className={`text-sm whitespace-pre-wrap ${isMe ? "text-white" : "text-gray-700 dark:text-gray-300"}`}
                    >
                      {yorum.icerik}
                    </p>
                  )}
                </div>
              </div>
            );
          })
        )}
        <div ref={bottomRef} />
      </div>

      <div className="p-3 bg-white dark:bg-gray-900 border-t dark:border-gray-800 rounded-b-lg">
        <form onSubmit={handleSend} className="flex gap-2">
          <input
            type="text"
            placeholder="Bir yorum yazın..."
            value={yeniYorum}
            onChange={(e) => setYeniYorum(e.target.value)}
            className="flex-1 rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
            disabled={sending}
          />
          <Button type="submit" disabled={sending || !yeniYorum.trim()}>
            <Send className="h-4 w-4" />
          </Button>
        </form>
      </div>
    </div>
  );
}
