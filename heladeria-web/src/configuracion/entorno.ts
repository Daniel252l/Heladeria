export const entorno = {
  apiUrl: import.meta.env.VITE_API_URL?.trim() || window.location.origin,
} as const
