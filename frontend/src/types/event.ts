export interface ZoneForm {
  id: string; // id local (uuid del navegador) solo para manejar la lista editable en UI
  name: string;
  price: string; // como string para permitir input controlado; se castea al enviar
  capacity: string;
}

export interface CreateEventPayload {
  name: string;
  date: string; // ISO-8601
  venue: string;
  zones: { name: string; price: number; capacity: number }[];
}

export interface CreateEventResponse {
  id: string;
  name: string;
  date: string;
  venue: string;
  status: string;
  createdAt: string;
  zones: { id: string; name: string; price: number; capacity: number }[];
}
