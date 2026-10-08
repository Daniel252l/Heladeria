import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Boton, Entrada, Seleccion } from '../../components/ui/Controles'
import { SelectorEntidad } from '../../components/ui/SelectorEntidad'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import { Paginacion } from '../../components/ui/Paginacion'
import type { Paginado } from '../../types/api'
import type { SesionCajaResumen } from '../../types/caja'
import { useRecursoCaja } from './useDatosCaja'
import { ErrorCaja, EstadoCaja, NavegacionCaja, SeccionCaja, TablaCaja } from './ComponentesCaja'
import { dineroCaja, fechaCaja, enlaceCaja } from './utilidadesCaja'

const inicial = { busqueda: '', cajaId: '', abierta: '' }
export function PaginaSesionesCaja() {
  const [filtros, setFiltros] = useState(inicial); const [aplicados, setAplicados] = useState(inicial); const [pagina, setPagina] = useState(1)
  const p = new URLSearchParams({ pagina: String(pagina), tamanioPagina: '10' }); Object.entries(aplicados).forEach(([k, v]) => { if (v) p.set(k, v) })
  const datos = useRecursoCaja<Paginado<SesionCajaResumen>>(`/api/caja/sesiones?${p}`)
  return <div className="caja-vista space-y-6"><EncabezadoPagina titulo="Sesiones de caja" descripcion="Aperturas, cierres e histórico de la sucursal activa." /><NavegacionCaja />
    <form className="space-y-3 rounded-xl border border-slate-200 bg-white p-4" onSubmit={(e) => { e.preventDefault(); setAplicados(filtros); setPagina(1) }}><div className="grid gap-4 sm:grid-cols-3">
      <Entrada etiqueta="Buscar sesión" value={filtros.busqueda} maxLength={150} onChange={(e) => setFiltros({ ...filtros, busqueda: e.target.value })} />
      <SelectorEntidad etiqueta="Caja" ruta="/api/caja/cajas" value={filtros.cajaId} opcional onChange={(id) => setFiltros({ ...filtros, cajaId: id ?? '' })} />
      <Seleccion etiqueta="Estado" value={filtros.abierta} onChange={(e) => setFiltros({ ...filtros, abierta: e.target.value })}><option value="">Todas</option><option value="true">Abierta</option><option value="false">Cerrada</option></Seleccion>
    </div><div className="flex flex-wrap gap-3"><Boton type="submit">Buscar sesiones</Boton><Boton variante="secundario" type="button" onClick={() => { setFiltros(inicial); setAplicados(inicial); setPagina(1) }}>Limpiar filtros</Boton></div></form>
    {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCaja error={datos.error} recargar={datos.recargar} /> : datos.datos && <SeccionCaja titulo="Histórico de sesiones">
      {!datos.datos.elementos.length ? <EstadoVacio titulo="No se encontraron sesiones" descripcion="Cambie los filtros o abra una caja." /> : <TablaCaja etiqueta="Listado de sesiones" columnas={['Apertura', 'Caja', 'Usuario', 'Fecha apertura', 'Estado', 'Saldo inicial', 'Fecha cierre', 'Cierre', 'Acciones']}>
        {datos.datos.elementos.map((s) => <tr key={s.id}><td>{s.numeroApertura ?? `Sesión #${s.id}`}</td><td>{s.caja}</td><td>{s.usuarioApertura}</td><td>{fechaCaja(s.fechaApertura)}</td><td><EstadoCaja estado={s.estado} /></td><td>{dineroCaja(s.saldoInicial, s.moneda)}</td><td>{s.fechaCierre ? fechaCaja(s.fechaCierre) : '—'}</td><td>{s.numeroCierre ?? '—'}</td><td><Link className={enlaceCaja} to={`/caja/sesiones/${s.id}`}>Ver sesión</Link></td></tr>)}
      </TablaCaja>}<Paginacion {...datos.datos} cambiar={setPagina} entidad="sesión" /></SeccionCaja>}
  </div>
}
