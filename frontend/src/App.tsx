import {
  createBrowserRouter,
  RouterProvider,
  Navigate,
} from "react-router-dom";
import Layout from "./components/Layout";
import Login from "./pages/Login";
import Register from "./pages/Register";
import DavetKabul from "./pages/DavetKabul";
import ForgotPassword from "./pages/ForgotPassword";
import ResetPassword from "./pages/ResetPassword";
import Home from "./pages/Home";
import Kanban from "./pages/Kanban";
import Timeline from "./pages/Timeline";
import EkipYonetimi from "./pages/EkipYonetimi";
import Raporlar from "./pages/Raporlar";
import Ayarlar from "./pages/Ayarlar";

const router = createBrowserRouter([
  {
    path: "/login",
    element: <Login />,
  },
  {
    path: "/register",
    element: <Register />,
  },
  {
    path: "/davet-kabul",
    element: <DavetKabul />,
  },
  {
    path: "/sifremi-unuttum",
    element: <ForgotPassword />,
  },
  {
    path: "/sifre-sifirla",
    element: <ResetPassword />,
  },
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Home />,
      },
      {
        path: "pano",
        element: <Kanban />,
      },
      {
        path: "timeline",
        element: <Timeline />,
      },
      {
        path: "ekip",
        element: <EkipYonetimi />,
      },
      {
        path: "raporlar",
        element: <Raporlar />,
      },
      {
        path: "ayarlar",
        element: <Ayarlar />,
      },
    ],
  },
  {
    path: "*",
    element: <Navigate to="/" replace />,
  },
]);

function App() {
  return <RouterProvider router={router} />;
}

export default App;
