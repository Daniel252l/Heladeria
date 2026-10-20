import { parametrosReporte } from '../../api/reportes'
import { PERMISOS } from '../../auth/permisos'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton, Entrada, Seleccion } from '../../components/ui/Controles'
import { PanelFiltros } from '../../components/ui/PanelFiltros'
import { SelectorEntidad } from '../../components/ui/SelectorEntidad'
import type { TipoReporte } from '../../types/reportes'
import { estadosReporte } from './configuracionReportes'
import type { CampoFiltro, FiltrosEditables } from './utilidadesReportes'

export function FiltrosReportes({ tipo, filtros, cambiar, aplicar, limpiar }: {
  tipo: TipoReporte; filtros: FiltrosEditables; cambiar: (f: FiltrosEditables) => void; aplicar: () => void; limpiar: () => void
}) {
  const { sucursales, tienePermiso } = useAutenticacion()
  const campos: readonly string[] = parametrosReporte[tipo]
  const tiene = (campo: CampoFiltro) => campos.includes(campo)
  const valor = (campo: CampoFiltro) => filtros[campo] ?? ''
  const poner = (campo: CampoFiltro, v: string) => cambiar({ ...filtros, [campo]: v })
  const campoId = (campo: CampoFiltro, etiqueta: string, ruta?: string, permiso?: string) => !tiene(campo) ? null
    : ruta && permiso && tienePermiso(permiso)
      ? <SelectorEntidad key={campo} etiqueta={etiqueta} ruta={ruta} opcional placeholder="Todos" value={valor(campo)} onChange={(v) => poner(campo, v ?? '')} />
      : <Entrada key={campo} etiqueta={`${etiqueta} (ID)`} aria-label={`${etiqueta} (ID)`} type="number" min="1" max={['clienteId', 'terceroId', 'proveedorId', 'usuarioId', 'monedaId'].includes(campo) ? 2147483647 : Number.MAX_SAFE_INTEGER} step="1"
        ayuda="Opcional. Identificador del registro; vacío no filtra." value={valor(campo)} onChange={(e) => poner(campo, e.target.value)} />
  return <PanelFiltros descripcion="Complete los criterios y aplíquelos. Los filtros no se envían mientras escribe."
    acciones={<><Boton type="submit" form="filtros-reportes">Aplicar filtros</Boton><Boton variante="secundario" onClick={limpiar}>Limpiar</Boton></>}>
    <form id="filtros-reportes" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" onSubmit={(e) => { e.preventDefault(); aplicar() }}>
      <Seleccion etiqueta="Sucursal" aria-label="Sucursal" required value={valor('sucursalId')} onChange={(e) => {
        // No conservar ubicaciones/cajas de otra sucursal. Aún no envía la consulta.
        cambiar({ ...filtros, sucursalId: e.target.value, bodegaId: '', ubicacionId: '', cajaId: '' })
      }}>{sucursales.map((s) => <option key={s.id} value={s.id}>{s.nombre}</option>)}</Seleccion>
      <Entrada etiqueta="Búsqueda" maxLength={150} value={valor('busqueda')} onChange={(e) => poner('busqueda', e.target.value)} placeholder="Buscar en el reporte" />
      {tiene('fechaDesde') && <>
        <Entrada etiqueta="Fecha desde" type="date" max={valor('fechaHasta') || undefined} value={valor('fechaDesde')} onChange={(e) => poner('fechaDesde', e.target.value)} />
        <Entrada etiqueta="Fecha hasta" type="date" min={valor('fechaDesde') || undefined} value={valor('fechaHasta')} onChange={(e) => poner('fechaHasta', e.target.value)} />
      </>}
      {campoId('productoId', 'Producto', '/api/productos', PERMISOS.productosConsultar)}
      {campoId('clienteId', 'Cliente', '/api/clientes', PERMISOS.ventasConsultar)}
      {campoId('proveedorId', 'Proveedor', '/api/compras/proveedores', PERMISOS.proveedoresConsultar)}
      {campoId('terceroId', tipo === 'cuentas-por-pagar' ? 'Proveedor' : 'Cliente', tipo === 'cuentas-por-pagar' ? '/api/compras/proveedores' : '/api/clientes', tipo === 'cuentas-por-pagar' ? PERMISOS.proveedoresConsultar : PERMISOS.ventasConsultar)}
      {campoId('monedaId', 'Moneda')}
      {campoId('bodegaId', 'Bodega')}
      {campoId('ubicacionId', 'Ubicación')}
      {campoId('cajaId', 'Caja', `/api/caja/cajas?sucursalId=${valor('sucursalId')}`, PERMISOS.cajaConsultar)}
      {campoId('usuarioId', 'Usuario')}
      {tiene('estado') && <Seleccion etiqueta="Estado" aria-label="Estado" value={valor('estado')} onChange={(e) => poner('estado', e.target.value)}>
        <option value="">{tipo === 'ventas' ? 'Predeterminado: COMPLETADA' : 'Todos'}</option>
        {estadosReporte[tipo]?.map((e) => <option key={e}>{e}</option>)}
      </Seleccion>}
      {tiene('esCredito') && <Seleccion etiqueta="Modalidad" aria-label="Modalidad" value={valor('esCredito')} onChange={(e) => poner('esCredito', e.target.value)}><option value="">Todas</option><option value="false">Contado</option><option value="true">Crédito</option></Seleccion>}
      {tiene('vencimiento') && <Seleccion etiqueta="Clasificación de vencimiento" aria-label="Clasificación de vencimiento" value={valor('vencimiento')} onChange={(e) => poner('vencimiento', e.target.value)}><option value="">Todas</option>{['VIGENTE', 'VENCIDA', 'PAGADA'].map((v) => <option key={v}>{v}</option>)}</Seleccion>}
      {tiene('tipo') && <Seleccion etiqueta="Tipo de movimiento" aria-label="Tipo de movimiento" value={valor('tipo')} onChange={(e) => poner('tipo', e.target.value)}><option value="">Todos</option><option>ENTRADA</option><option>SALIDA</option></Seleccion>}
      {(['concepto', 'modulo', 'accion', 'entidad'] as const).filter(tiene).map((campo) => <Entrada key={campo}
        etiqueta={{ concepto: 'Concepto', modulo: 'Módulo', accion: 'Acción', entidad: 'Entidad' }[campo]}
        maxLength={campo === 'concepto' ? 30 : campo === 'modulo' ? 50 : 100} value={valor(campo)} onChange={(e) => poner(campo, e.target.value)} />)}
    </form>
  </PanelFiltros>
}
