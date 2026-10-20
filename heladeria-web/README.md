# Heladería Web

Interfaz administrativa de Heladería construida con React, TypeScript, Vite y Tailwind CSS.

## Requisitos

- Node.js compatible con Vite 8.
- API de Heladería disponible por HTTPS.

## Configuración local

1. Copie `.env.example` a `.env.local`.
2. Ajuste `VITE_API_URL` con el origen de la API, sin una barra final.
3. Instale las dependencias con `npm install`.
4. Inicie la aplicación con `npm run dev`.

El origen de desarrollo esperado por la configuración CORS de la API es `http://localhost:5173`.

## Comandos

```text
npm run dev
npm run build
npm run lint
npm test
npm run preview
```

## Seguridad de sesión

El access token se conserva únicamente en memoria. La renovación utiliza la cookie HttpOnly emitida por la API y todas las solicitudes incluyen credenciales. El cliente intenta renovar una sola vez ante una respuesta 401 y nunca almacena contraseñas ni tokens en `localStorage` o `sessionStorage`.

En desarrollo, la API HTTPS configura la cookie con `SameSite=None` y `Secure` para aceptar solicitudes desde `http://localhost:5173`. La configuración base utilizada en producción conserva `SameSite=Strict`.
# Entrega de producción

Las visuales 1–8 están aprobadas por el propietario. Para instalación, publicación, seguridad y cierre final consultar [README principal](../README.md) y [DEPLOYMENT](../docs/DEPLOYMENT.md).

Usar `npm ci` con el lockfile. `VITE_API_URL` es pública y queda compilada en JavaScript: no admite secretos. `.env.production.example` contiene solo un dominio de ejemplo. `.env.local` también afecta builds productivos; el script `scripts/Publicar.ps1` exige una elección explícita y la sobrescribe en el proceso de build sin editarla. No usar `vite preview` como servidor productivo.

