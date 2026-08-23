# Frontend — Registrar Evento (MVP)

Pantalla mínima en React 18 + TypeScript + Tailwind que consume `POST /events` de `EventService`.

## Ejecutar en local

```bash
cd frontend
cp .env.example .env      # completar VITE_API_BASE_URL y VITE_DEMO_JWT
npm install
npm run dev
```

Abrir http://localhost:5173

## Notas
- El token JWT es fijo (`VITE_DEMO_JWT`) para simplificar la demo — ver README raíz sobre cómo generar uno de prueba con rol `Admin`.
- Validación mínima en cliente (campos obligatorios, capacidad > 0, precio ≥ 0) — la validación autoritativa vive en el backend (FluentValidation).
