import axios from "axios";
import { useAuthStore } from "../store/authStore";

const api = axios.create({
  // Backend API URL.
  baseURL: "http://localhost:5292/api",
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor: Her isteğe token ekle
api.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().token;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor: 401 Unauthorized gelirse çıkış yap ve login'e yönlendir
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
      window.location.href = "/login";
    }
    return Promise.reject(error);
  },
);

export default api;
