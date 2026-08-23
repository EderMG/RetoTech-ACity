import { useState } from "react";
import type { ZoneForm } from "../types/event";
import { createEvent, ApiError } from "../services/api";

function newZone(): ZoneForm {
  return { id: crypto.randomUUID(), name: "", price: "", capacity: "" };
}

type FormErrors = Partial<Record<"name" | "date" | "venue" | "zones", string>>;

export default function RegisterEventForm() {
  const [name, setName] = useState("");
  const [date, setDate] = useState("");
  const [venue, setVenue] = useState("");
  const [zones, setZones] = useState<ZoneForm[]>([newZone()]);

  const [errors, setErrors] = useState<FormErrors>({});
  const [status, setStatus] = useState<"idle" | "loading" | "success" | "error">("idle");
  const [feedback, setFeedback] = useState<string | null>(null);

  function updateZone(id: string, field: keyof ZoneForm, value: string) {
    setZones((prev) => prev.map((z) => (z.id === id ? { ...z, [field]: value } : z)));
  }

  function addZone() {
    setZones((prev) => [...prev, newZone()]);
  }

  function removeZone(id: string) {
    setZones((prev) => (prev.length > 1 ? prev.filter((z) => z.id !== id) : prev));
  }

  function validate(): boolean {
    const next: FormErrors = {};

    if (!name.trim()) next.name = "El nombre del evento es obligatorio.";
    if (!date) next.date = "La fecha es obligatoria.";
    else if (new Date(date) <= new Date()) next.date = "La fecha debe ser futura.";
    if (!venue.trim()) next.venue = "El lugar es obligatorio.";

    const zoneInvalid = zones.some((z) => {
      const price = Number(z.price);
      const capacity = Number(z.capacity);
      return !z.name.trim() || Number.isNaN(price) || price < 0 || !Number.isInteger(capacity) || capacity <= 0;
    });
    if (zoneInvalid) next.zones = "Cada zona necesita nombre, precio ≥ 0 y aforo > 0 (entero).";

    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFeedback(null);

    if (!validate()) return;

    setStatus("loading");
    try {
      const result = await createEvent({
        name: name.trim(),
        date: new Date(date).toISOString(),
        venue: venue.trim(),
        zones: zones.map((z) => ({
          name: z.name.trim(),
          price: Number(z.price),
          capacity: Number(z.capacity)
        }))
      });

      setStatus("success");
      setFeedback(`Evento "${result.name}" creado correctamente (id: ${result.id}).`);
      setName("");
      setDate("");
      setVenue("");
      setZones([newZone()]);
    } catch (err) {
      setStatus("error");
      setFeedback(err instanceof ApiError ? err.message : "Ocurrió un error inesperado.");
    }
  }

  return (
    <div className="min-h-screen flex items-start justify-center py-12 px-4">
      <div className="w-full max-w-2xl bg-white rounded-xl shadow-sm border border-stone-200 p-8">
        <h1 className="text-2xl font-semibold text-ink mb-1">Registrar Evento</h1>
        <p className="text-stone-500 mb-6 text-sm">
          Crea un evento con sus zonas. Se publicará de forma asíncrona hacia el servicio de notificaciones.
        </p>

        <form onSubmit={handleSubmit} noValidate className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-stone-700 mb-1" htmlFor="name">
              Nombre del evento
            </label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full rounded-md border border-stone-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-clay/50"
              placeholder="Ej. Concierto de Rock — Lima"
            />
            {errors.name && <p className="text-red-600 text-xs mt-1">{errors.name}</p>}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-stone-700 mb-1" htmlFor="date">
                Fecha y hora
              </label>
              <input
                id="date"
                type="datetime-local"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full rounded-md border border-stone-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-clay/50"
              />
              {errors.date && <p className="text-red-600 text-xs mt-1">{errors.date}</p>}
            </div>

            <div>
              <label className="block text-sm font-medium text-stone-700 mb-1" htmlFor="venue">
                Lugar
              </label>
              <input
                id="venue"
                type="text"
                value={venue}
                onChange={(e) => setVenue(e.target.value)}
                className="w-full rounded-md border border-stone-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-clay/50"
                placeholder="Ej. Estadio Nacional, Lima"
              />
              {errors.venue && <p className="text-red-600 text-xs mt-1">{errors.venue}</p>}
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <span className="block text-sm font-medium text-stone-700">Zonas</span>
              <button
                type="button"
                onClick={addZone}
                className="text-sm text-clay hover:underline"
              >
                + Agregar zona
              </button>
            </div>

            <div className="space-y-3">
              {zones.map((zone, idx) => (
                <div key={zone.id} className="grid grid-cols-12 gap-2 items-center">
                  <input
                    className="col-span-5 rounded-md border border-stone-300 px-3 py-2 text-sm"
                    placeholder={`Zona ${idx + 1} (ej. VIP)`}
                    value={zone.name}
                    onChange={(e) => updateZone(zone.id, "name", e.target.value)}
                  />
                  <input
                    className="col-span-3 rounded-md border border-stone-300 px-3 py-2 text-sm"
                    placeholder="Precio"
                    type="number"
                    min={0}
                    step="0.01"
                    value={zone.price}
                    onChange={(e) => updateZone(zone.id, "price", e.target.value)}
                  />
                  <input
                    className="col-span-3 rounded-md border border-stone-300 px-3 py-2 text-sm"
                    placeholder="Aforo"
                    type="number"
                    min={1}
                    value={zone.capacity}
                    onChange={(e) => updateZone(zone.id, "capacity", e.target.value)}
                  />
                  <button
                    type="button"
                    onClick={() => removeZone(zone.id)}
                    className="col-span-1 text-stone-400 hover:text-red-600 text-sm"
                    aria-label="Eliminar zona"
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
            {errors.zones && <p className="text-red-600 text-xs mt-2">{errors.zones}</p>}
          </div>

          <button
            type="submit"
            disabled={status === "loading"}
            className="w-full rounded-md bg-ink text-white py-2.5 font-medium hover:bg-stone-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {status === "loading" ? "Guardando…" : "Guardar"}
          </button>

          {feedback && (
            <p className={`text-sm ${status === "success" ? "text-green-700" : "text-red-600"}`} role="status">
              {feedback}
            </p>
          )}
        </form>
      </div>
    </div>
  );
}
