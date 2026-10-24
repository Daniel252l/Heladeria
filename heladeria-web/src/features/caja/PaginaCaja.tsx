import { Link } from 'react-router-dom'
import { Wallet, ArrowDownLeft, ArrowUpRight, Calculator, LockKeyhole } from 'lucide-react'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'
import { Boton } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando } from '../../components/ui/Estados'
import type { Paginado } from '../../types/api'
import type { SesionCaja, SesionCajaResumen } from '../../types/caja'
import { useRecursoCaja } from './useDatosCaja'
import { AvisoCaja, ErrorCaja, EstadoCaja, NavegacionCaja, ResumenCaja, SeccionCaja } from './ComponentesCaja'
import { fechaCaja, enlaceCaja, enlaceCajaPrimario, operable } from './utilidadesCaja'

export function PaginaCaja() {
  const { tienePermiso } = useAutenticacion()
  const abiertas = useRecursoCaja<Paginado<SesionCajaResumen>>('/api/caja/sesiones?abierta=true&tamanioPagina=10')
  return <div className="caja-vista space-y-6"><EncabezadoPagina titulo="Caja" descripcion="Control operativo de efectivo, cobros y sesiones de la sucursal." icono={<Wallet />} /><NavegacionCaja />
    {abiertas.cargando ? <EstadoCargando /> : abiertas.error ? <ErrorCaja error={abiertas.error} recargar={abiertas.recargar} /> : !abiertas.datos?.elementos.length
      ? <section className="space-y-4 rounded-xl border border-slate-200 bg-white p-6"><EstadoCaja estado="CERRADA" /><h2 className="text-lg font-bold">No hay una sesión de caja abierta.</h2><p className="text-sm text-slate-600">Abra una caja para registrar cobros y movimientos. Los turnos cerrados se conservan en el histórico.</p>{tienePermiso(PERMISOS.cajaAbrir) && <Link className={enlaceCajaPrimario} to="/caja/abrir">Abrir caja ahora</Link>}</section>
      : <><p className="text-sm text-slate-600">{abiertas.datos.totalRegistros} sesión(es) abierta(s). Seleccione la caja que desea operar.</p>
        {abiertas.datos.elementos.map((s) => <SesionAbierta key={s.id} sesion={s} />)}
        {abiertas.datos.totalPaginas > 1 && <Link className={enlaceCajaPrimario} to="/caja/sesiones">Ver todas las sesiones</Link>}</>}
  </div>
}
function SesionAbierta({ sesion }: { sesion: SesionCajaResumen }) {
  const { tienePermiso, sucursalActiva } = useAutenticacion()
  const operar = tienePermiso(PERMISOS.cajaOperar)
  const manual = operar && tienePermiso(PERMISOS.cajaMovimientoManual)
  const datos = useRecursoCaja<SesionCaja>(`/api/caja/sesiones/${sesion.id}`)
  return <SeccionCaja titulo={`${sesion.caja} · ${sesion.numeroApertura ?? `Sesión #${sesion.id}`}`}><div className="space-y-4 p-4 sm:p-5">
    <div className="flex flex-wrap items-center gap-3"><EstadoCaja estado={sesion.estado} /><span className="font-semibold">{sucursalActiva?.nombre}</span><span className="text-sm text-slate-600">{sesion.usuarioApertura} · {fechaCaja(sesion.fechaApertura)}</span></div>
    {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCaja error={datos.error} recargar={datos.recargar} /> : datos.datos && <ResumenCaja sesion={datos.datos} />}
    {datos.datos && operable(datos.datos) && <div className="caja-acciones grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      {manual && <><Link className={enlaceCajaPrimario} to={`/caja/sesiones/${sesion.id}?accion=ingreso`}><ArrowDownLeft size={18} /> Registrar ingreso</Link><Link className={enlaceCaja} to={`/caja/sesiones/${sesion.id}?accion=egreso`}><ArrowUpRight size={18} /> Registrar egreso</Link></>}
      {operar && <Link className={enlaceCaja} to={`/caja/sesiones/${sesion.id}?accion=arqueo`}><Calculator size={18} /> Arqueo</Link>}
      {tienePermiso(PERMISOS.cajaCerrar) && (datos.datos.arqueos.some((a) => a.vigente) ? <Link className={enlaceCaja + ' border-red-200 text-red-700'} to={`/caja/sesiones/${sesion.id}?accion=cierre`}><LockKeyhole size={18} /> Cerrar caja</Link> : <Boton variante="peligro" disabled title="Registre primero un arqueo vigente"><LockKeyhole size={18} /> Cerrar caja</Boton>)}
    </div>}
    {datos.datos && !datos.datos.arqueos.some((a) => a.vigente) && tienePermiso(PERMISOS.cajaCerrar) && <AvisoCaja>Registre un arqueo vigente antes de cerrar.</AvisoCaja>}
    <Link className={enlaceCaja} to={`/caja/sesiones/${sesion.id}`}>Consultar / operar sesión</Link>
  </div></SeccionCaja>
}
