import { useState, useEffect } from "react";
import api from "../api/axios";
import type { Result, GorevIlerlemeDto } from "../types";
import { format, parseISO } from "date-fns";
import { tr } from "date-fns/locale";
import { Bot, Mail, ChevronDown, ChevronUp, Inbox, Sparkles } from "lucide-react";

export function GorevIlerlemeler({ gorevId }: { gorevId: string }) {
  const [ilerlemeler, setIlerlemeler] = useState<GorevIlerlemeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [acikEmailId, setAcikEmailId] = useState<string | null>(null);

  useEffect(() => {
    const fetchIlerlemeler = async () => {
      try {
        const res = await api.get<Result<GorevIlerlemeDto[]>>(
          `/GorevIlerleme/gorev/${gorevId}`,
        );
        if (res.data.isSuccess && res.data.data) {
          setIlerlemeler(res.data.data);
        }
      } catch (error) {
        console.error("İlerlemeler alınamadı:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchIlerlemeler();
  }, [gorevId]);

  //TaskComments/TaskBugs ile aynı sabit yükseklikte kapsayıcı kullanılır; aksi halde
  //bu sekmenin içeriği diğerlerinden kısa/uzun olduğunda modal sekmeler arası zıplıyordu
  return (
    <div className="flex flex-col h-[400px] border rounded-lg bg-gray-50/50 dark:bg-gray-900/50 dark:border-gray-800">
      <div className="p-3 border-b dark:border-gray-800 flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300">
        <Sparkles className="h-4 w-4 text-indigo-500" /> İlerlemeler (
        {ilerlemeler.length})
      </div>

      <div className="thin-scrollbar flex-1 overflow-y-auto p-4 space-y-3">
        {loading ? (
          <div className="py-8 text-center text-sm text-gray-500 dark:text-gray-400">
            Yükleniyor...
          </div>
        ) : ilerlemeler.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full gap-3 text-center text-sm text-gray-500 dark:text-gray-400">
            <div className="bg-gray-100 dark:bg-gray-800 rounded-full p-3">
              <Inbox className="h-6 w-6 text-gray-400" />
            </div>
            <p className="font-medium text-gray-700 dark:text-gray-300">
              Henüz AI ilerleme kaydı yok.
            </p>
            <p className="text-xs max-w-xs">
              Bir ekip üyesi e-posta ile görev güncellemesi gönderdiğinde AI bu
              bilgileri otomatik olarak buraya kaydeder.
            </p>
          </div>
        ) : (
          ilerlemeler.map((ilerleme) => {
            const isAcik = acikEmailId === ilerleme.id;
            return (
              <div
                key={ilerleme.id}
                className="rounded-lg border border-indigo-100 bg-indigo-50/50 dark:border-indigo-900/30 dark:bg-indigo-950/20 overflow-hidden"
              >
                {/* Başlık satırı */}
                <div className="flex items-start gap-3 p-3">
                  <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-indigo-100 dark:bg-indigo-900/50">
                    <Bot className="h-4 w-4 text-indigo-600 dark:text-indigo-400" />
                  </div>

                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-800 dark:text-gray-200 leading-snug">
                      {ilerleme.tamamlananKisim}
                    </p>
                    <p className="text-[11px] text-gray-400 mt-1">
                      {format(
                        parseISO(ilerleme.olusturulmaTarihi),
                        "dd MMM yyyy • HH:mm",
                        { locale: tr },
                      )}
                    </p>
                  </div>

                  {/* Kaynak e-posta açma butonu */}
                  {ilerleme.kaynakEmailMetni && (
                    <button
                      onClick={() => setAcikEmailId(isAcik ? null : ilerleme.id)}
                      className="flex-shrink-0 flex items-center gap-1 text-[11px] text-indigo-500 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300 mt-0.5"
                      title="Kaynak e-postayı göster"
                    >
                      <Mail className="h-3.5 w-3.5" />
                      {isAcik ? (
                        <ChevronUp className="h-3 w-3" />
                      ) : (
                        <ChevronDown className="h-3 w-3" />
                      )}
                    </button>
                  )}
                </div>

                {/* Kaynak e-posta içeriği — toggle */}
                {isAcik && ilerleme.kaynakEmailMetni && (
                  <div className="border-t border-indigo-100 dark:border-indigo-900/30 bg-white dark:bg-gray-900 px-4 py-3">
                    <p className="text-[10px] font-semibold uppercase tracking-wider text-gray-400 mb-1">
                      Kaynak E-posta
                    </p>
                    <pre className="thin-scrollbar text-xs text-gray-600 dark:text-gray-400 whitespace-pre-wrap font-mono leading-relaxed max-h-[200px] overflow-y-auto">
                      {ilerleme.kaynakEmailMetni}
                    </pre>
                  </div>
                )}
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
