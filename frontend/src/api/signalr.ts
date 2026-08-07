import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { useAuthStore } from "../store/authStore";

class SignalRService {
  private connection: HubConnection | null = null;
  private isConnecting = false;

  async startConnection() {
    if (this.connection?.state === "Connected" || this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    try {
      const token = useAuthStore.getState().token;

      this.connection = new HubConnectionBuilder()
        .withUrl("http://localhost:5292/hubs/gorev", {
          accessTokenFactory: () => token || "",
        })
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();

      await this.connection.start();
      console.log("SignalR Hub bağlantısı başarılı.");

      this.connection.onclose(() => {
        console.log("SignalR bağlantısı kapandı.");
      });
    } catch (err) {
      console.error("SignalR bağlantı hatası:", err);
    } finally {
      this.isConnecting = false;
    }
  }

  stopConnection() {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
    }
  }

  // Event dinleyici ekleme
  on(eventName: string, callback: (...args: any[]) => void) {
    if (this.connection) {
      this.connection.on(eventName, callback);
    }
  }

  // Event dinleyici kaldırma
  off(eventName: string, callback: (...args: any[]) => void) {
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

  // Hub sunucu metodunu çağırma (örn: ProjeGrubunuKatil)
  async invoke(methodName: string, ...args: any[]) {
    if (this.connection?.state === "Connected") {
      try {
        await this.connection.invoke(methodName, ...args);
      } catch (err) {
        console.error(`SignalR invoke hatası [${methodName}]:`, err);
      }
    }
  }
}

export const signalRService = new SignalRService();
