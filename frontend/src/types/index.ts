export interface Result<T = void> {
  isSuccess: boolean;
  isFailure: boolean;
  error?: string;
  data?: T;
}

export interface User {
  id: string;
  ad: string;
  soyad: string;
  email: string;
  rol: "Lider" | "Uye";
  ekipId: string;
}

export interface AuthResultDto {
  token: string;
  kullanici: User;
}

export interface GorevDto {
  id: string;
  baslik: string;
  aciklama?: string;
  durum: "Backlog" | "DevamEdiyor" | "Test" | "OnayBekliyor" | "Tamamlandi" | "Iptal";
  oncelik: "Dusuk" | "Orta" | "Yuksek" | "Kritik";
  baslangicTarihi?: string;
  deadline?: string;
  projeId: string;
  //Sadece Dashboard uç noktasından (Home sayfası) gelen görevlerde doldurulur
  projeAdi?: string | null;
  atananId?: string;
  oluşturulmaTarihi: string;
}

export interface ProjeDto {
  id: string;
  ad: string;
  aciklama?: string;
  ekipId: string;
  olusturanLiderId: string;
  olusturulmaTarihi: string;
}

export interface UyeGorevOzetiDto {
  kullaniciId: string;
  ad: string;
  soyad: string;
  email: string;
  rol: string;
  aktifGorevler: GorevDto[];
}

export interface TimelineGorevDto extends Omit<
  GorevDto,
  "oluşturulmaTarihi" | "aciklama"
> {
  projeAdi: string;
  atananAdSoyad: string;
  olusturulmaTarihi: string;
}

export interface DavetGonderDto {
  email: string;
  ekipId: string;
}

export interface CreateProjeDto {
  ad: string;
  aciklama?: string;
  ekipId: string;
}

export interface CreateGorevDto {
  baslik: string;
  aciklama?: string;
  oncelik: "Dusuk" | "Orta" | "Yuksek" | "Kritik";
  baslangicTarihi?: string;
  deadline?: string;
  projeId: string;
  atananId?: string | null;
}

export interface YorumDto {
  id: string;
  icerik: string;
  yazanId: string;
  yazanAdSoyad: string;
  gorevId: string;
  duzenlendi: boolean;
  olusturulmaTarihi: string;
}

export interface UpdateGorevDto {
  baslik?: string;
  aciklama?: string;
  oncelik?: "Dusuk" | "Orta" | "Yuksek" | "Kritik";
  baslangicTarihi?: string;
  deadline?: string;
  projeId?: string;
  atananId?: string | null;
}

export interface BildirimDto {
  id: string;
  icerik: string;
  okundu: boolean;
  gorevId?: string | null;
  olusturulmaTarihi: string;
}

export interface MailAyarlariDurumDto {
  mailYapilandirildiMi: boolean;
  aiApiKeyYapilandirildiMi: boolean;
  kayitliEmail: string;
}

export interface HataDto {
  id: string;
  gorevId: string;
  sorumluUyeId: string;
  modul: string;
  aciklama: string;
  siddet: string;
  durum: "Acik" | "Cozuldu";
  olusturulmaTarihi: string;
}

export interface HataOlusturDto {
  gorevId: string;
  modul: string;
  aciklama: string;
  siddet: string;
}

export interface HataIstatistikDto {
  modul: string;
  hataSayisi: number;
}

export interface KisiHataIstatistikDto {
  kullaniciId: string;
  hataSayisi: number;
}

export interface EkipOzetiGecmisDto {
  id: string;
  icerik: string;
  olusturulmaTarihi: string;
}

export interface GorevIlerlemeDto {
  id: string;
  tamamlananKisim: string;
  kaynakEmailMetni?: string;
  gorevId: string;
  ekleyenKullaniciId?: string;
  olusturulmaTarihi: string;
}
