import { leerPaginas } from '../../api/cartera'
import { ErrorApi, solicitar } from '../../api/clienteHttp'
import type { CompraCredito, CuentaCartera, DetalleCompraCredito } from '../../types/cartera'
import type { ReferenciasCompras } from '../../types/compras'

export const compraRecibida = (c: CompraCredito, empresa: number, sucursal: number) =>
  c.empresaId === empresa && c.sucursalId === sucursal && c.esCredito === true && c.estado === 'RECIBIDA' && Boolean(c.versionFila)
export function validarDetalleCompra(c: DetalleCompraCredito, empresa: number, sucursal: number) {
  if (!compraRecibida(c.encabezado, empresa, sucursal) || !c.detalles.length || c.detalles.some((d) => d.cantidadPendiente !== 0 || d.cantidadRecibida !== d.cantidad))
    throw new ErrorApi('Seleccione una compra a crédito totalmente recibida en la sucursal activa.', 400, { codigo: 'VALIDACION_CXP' })
}
export async function comprasFacturables(empresa: number, sucursal: number, signal?: AbortSignal) {
  const referencias = await solicitar<ReferenciasCompras>('/api/compras/referencias', { signal })
  const recibida = referencias.estados.find((e) => e.flujo === 'ORDENES_COMPRA' && e.codigo === 'RECIBIDA')
  if (!recibida) return { compras: [], referencias }
  const [compras, cuentas] = await Promise.all([
    leerPaginas<CompraCredito>(`/api/compras?sucursalId=${sucursal}&estadoId=${recibida.id}`, signal),
    leerPaginas<CuentaCartera>(`/api/cuentas-por-pagar?sucursalId=${sucursal}`, signal),
  ])
  const facturadas = new Set(cuentas.map((c) => c.compraId))
  return { compras: compras.filter((c) => compraRecibida(c, empresa, sucursal) && !facturadas.has(c.id)), referencias }
}
