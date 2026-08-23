import type { CreateEventPayload, CreateEventResponse } from "../types/event";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

// Para la demo del reto, el token JWT es fijo (ver README: cómo generar uno de prueba
// con rol "Admin"). En un entorno real este token vendría de un login OIDC/OAuth2.
const DEMO_JWT = import.meta.env.VITE_DEMO_JWT ?? "";

export class ApiError extends Error {
  constructor(message: string, public status?: number) {
    super(message);
  }
}

export async function createEvent(payload: CreateEventPayload): Promise<CreateEventResponse> {
  const response = await fetch(`${API_BASE_URL}/events`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(DEMO_JWT ? { Authorization: `Bearer ${DEMO_JWT}` } : {})
    },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    // No se exponen detalles internos del servidor; se muestra un mensaje genérico por status.
    if (response.status === 401 || response.status === 403) {
      throw new ApiError("No tiene permisos para crear eventos. Verifique el token de acceso.", response.status);
    }
    if (response.status === 400) {
      const body = await response.json().catch(() => null);
      throw new ApiError(body?.title ?? "Los datos del formulario no son válidos.", response.status);
    }
    throw new ApiError("No se pudo crear el evento. Intente nuevamente.", response.status);
  }

  return response.json();
}
