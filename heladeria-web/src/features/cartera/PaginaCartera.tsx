import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ReceiptText } from 'lucide-react'
import { apiCartera, rutaCartera } from '../../api/cartera'
import { PERMISOS } from '../../auth/permisos'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton, Entrada, Seleccion } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import { PanelFiltros } from '../../components/ui/PanelFiltros'
import { Paginacion } from '../../components/ui/Paginacion'
import { SelectorEntidad } from '../../components/ui/SelectorEntidad'
import type { Paginado } from '../../types/api'
import type { CuentaCartera, ResumenCartera, TipoCartera } from '../../types/cartera'
import { mostrarFechaCivil } from '../../utils/fechas'
import { useRecursoVentas } from '../ventas/useDatosVentas'
import { dineroCaja, enlaceCaja, enlaceCajaPrimario } from '../caja/utilidadesCaja'
import { ErrorCartera, EstadoCartera, OrigenCartera, ResumenCarteraVista, SeccionCartera, TablaCartera } from './ComponentesCartera'

const inicial = { busqueda: '', terceroId: '', estado: '', vencimiento: '', fechaDesde: '', fechaHasta: '' }
export function PaginaCartera({ tipo }: { tipo: TipoCartera }) {
  const { tienePermiso, sucursalActiva } = useAutenticacion()
  const [filtros, setFiltros] = useState(inicial); const [aplicados, setAplicados] = useState(inicial)
  const [pagina, setPagina] = useState(1); const [tamanio, setTamanio] = useState(10)
  const tercero = tipo === 'cobrar' ? 'Cliente' : 'Proveedor'
  const criterios = new URLSearchParams({ sucursalId: String(sucursalActiva?.id ?? '') })
  Object.entries(aplicados).forEach(([k, v]) => { if (v) criterios.set(k, v) })
  // El resumen agrega todas las cuentas filtradas, independientemente de la página del listado.
  const p = new URLSearchParams(criterios)
  p.set('pagina', String(pagina))
  p.set('tamanioPagina', String(tamanio))
  const datos = useRecursoVentas<Paginado<CuentaCartera>>(`${apiCartera(tipo)}?${p}`)
  const resumen = useRecursoVentas<ResumenCartera[]>(`${apiCartera(tipo)}/resumen?${criterios}`)
  const campo = (k: keyof typeof inicial, v: string) => setFiltros({ ...filtros, [k]: v })
  const consultaTercero = tienePermiso(tipo === 'cobrar' ? PERMISOS.ventasConsultar : PERMISOS.proveedoresConsultar)
  return <div className="caja-vista space-y-6">
    <EncabezadoPagina titulo={`Cuentas por ${tipo}`} descripcion={`Saldos, vencimientos e historial de ${tipo === 'cobrar' ? 'clientes' : 'proveedores'} · ${sucursalActiva?.nombre ?? ''}.`} icono={<ReceiptText />}
      acciones={tipo === 'pagar' && tienePermiso(PERMISOS.cxpAdministrar) && tienePermiso(PERMISOS.comprasConsultar) ? <Link className={enlaceCajaPrimario} to="/cuentas-por-pagar/registrar-factura">Registrar factura de proveedor</Link> : undefined} />
    <PanelFiltros acciones={<><Boton form="filtros-cartera" type="submit">Buscar cuentas</Boton><Boton variante="secundario" onClick={() => { setFiltros(inicial); setAplicados(inicial); setPagina(1) }}>Limpiar filtros</Boton></>}>
      <form id="filtros-cartera" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" onSubmit={(e) => { e.preventDefault(); setAplicados({ ...filtros, busqueda: filtros.busqueda.trim() }); setPagina(1) }}>
        <Entrada etiqueta="Búsqueda" placeholder="Documento o nombre" maxLength={150} value={filtros.busqueda} onChange={(e) => campo('busqueda', e.target.value)} />
        {consultaTercero ? <SelectorEntidad etiqueta={tercero} ruta={tipo === 'cobrar' ? '/api/clientes' : '/api/compras/proveedores'} opcional placeholder={`Todos los ${tercero.toLowerCase()}s`} value={filtros.terceroId} onChange={(v) => campo('terceroId', v ?? '')} />
          : <Entrada etiqueta={`${tercero} (ID)`} type="number" min="1" step="1" ayuda="También puede buscar por nombre. El catálogo requiere su permiso de consulta." value={filtros.terceroId} onChange={(e) => campo('terceroId', e.target.value)} />}
        <Seleccion etiqueta="Estado" value={filtros.estado} onChange={(e) => campo('estado', e.target.value)}><option value="">Todos</option>{['PENDIENTE', 'PARCIAL', 'PAGADA', 'ANULADA'].map((v) => <option key={v}>{v}</option>)}</Seleccion>
        <Seleccion etiqueta="Vencimiento" value={filtros.vencimiento} onChange={(e) => campo('vencimiento', e.target.value)}><option value="">Todos</option>{['VIGENTE', 'VENCIDA', 'PAGADA'].map((v) => <option key={v}>{v}</option>)}</Seleccion>
        <Entrada etiqueta="Fecha desde" type="date" max={filtros.fechaHasta || undefined} value={filtros.fechaDesde} onChange={(e) => campo('fechaDesde', e.target.value)} />
        <Entrada etiqueta="Fecha hasta" type="date" min={filtros.fechaDesde || undefined} value={filtros.fechaHasta} onChange={(e) => campo('fechaHasta', e.target.value)} />
      </form>
    </PanelFiltros>
    {resumen.cargando ? <EstadoCargando mensaje="Consultando resumen…" /> : resumen.error ? <ErrorCartera error={resumen.error} recargar={resumen.recargar} /> : resumen.datos && <ResumenCarteraVista datos={resumen.datos} />}
    <SeccionCartera titulo="Cuentas de la sucursal">
      {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCartera error={datos.error} recargar={datos.recargar} /> : datos.datos && <>
        {!datos.datos.elementos.length ? <EstadoVacio titulo="No se encontraron cuentas" descripcion="Revise los filtros y la sucursal activa." /> : <TablaCartera etiqueta={`Cuentas por ${tipo}`} columnas={['Documento origen', tercero, ...(tipo === 'pagar' ? ['Compra origen'] : []), 'Emisión', 'Vencimiento', 'Importe original', 'Aplicado', 'Pendiente', 'Estado', 'Clasificación', 'Acciones']}>
          {datos.datos.elementos.map((c) => <tr key={c.id}>
            <td><OrigenCartera cuenta={c} tipo={tipo} /></td><td>{c.tercero}</td>{tipo === 'pagar' ? [<td key="compra">{c.compraId ? tienePermiso(PERMISOS.comprasConsultar) ? <Link className="text-teal-800 underline" to={`/compras/${c.compraId}`}>Compra #{c.compraId}</Link> : `Compra #${c.compraId}` : 'Sin compra vinculada'}</td>] : []}
            <td className="sm:whitespace-nowrap">{mostrarFechaCivil(c.fechaEmision)}</td><td className="sm:whitespace-nowrap">{mostrarFechaCivil(c.fechaVencimiento)}</td>
            <td className="sm:whitespace-nowrap">{dineroCaja(c.importeOriginal, c.moneda)}</td><td className="sm:whitespace-nowrap">{dineroCaja(c.totalAplicado, c.moneda)}</td><td className="font-bold sm:whitespace-nowrap">{dineroCaja(c.saldoPendiente, c.moneda)}</td>
            <td><EstadoCartera estado={c.estado} /></td><td><EstadoCartera estado={c.vencimiento} /></td><td><Link className={enlaceCaja} to={`${rutaCartera(tipo)}/${c.id}`} aria-label={`Ver detalle ${c.numeroDocumentoOrigen}`}>Ver detalle</Link></td>
          </tr>)}
        </TablaCartera>}
        <Paginacion {...datos.datos} cambiar={setPagina} cambiarTamanio={(n) => { setTamanio(n); setPagina(1) }} entidad="cuenta" />
      </>}
    </SeccionCartera>
  </div>
}
