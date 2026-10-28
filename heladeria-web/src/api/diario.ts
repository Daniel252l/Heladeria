import type { FiltroDiario } from '../types/diario'
export function rutaDiario(filtros: FiltroDiario) {
  const query = new URLSearchParams()
  for (const clave of ['fecha', 'sucursalId', 'cajaId', 'formaPagoId', 'esCredito', 'pagina', 'tamanioPagina', 'paginaOperaciones', 'paginaCajas'] as const) {
    const valor = filtros[clave]
    if (valor !== undefined && valor !== '') query.set(clave, String(valor))
  }
  return `/api/reportes/diario?${query}`
}
