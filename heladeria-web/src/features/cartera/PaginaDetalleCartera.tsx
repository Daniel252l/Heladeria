import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiCartera, rutaCartera } from '../../api/cartera'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'
import { Boton } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import { PaginaNoEncontrada } from '../../pages/PaginaNoEncontrada'
import type { DetalleCartera, SolicitudPagoCartera, TipoCartera } from '../../types/cartera'
import { mostrarFechaCivil, mostrarInstante } from '../../utils/fechas'
import { dineroCaja, enlaceCaja } from '../caja/utilidadesCaja'
import { useRecursoVentas } from '../ventas/useDatosVentas'
import { AvisoCartera, DatoCartera, ErrorCartera, EstadoCartera, OrigenCartera, SeccionCartera, TablaCartera } from './ComponentesCartera'
import { FormularioPagoCartera } from './FormularioPagoCartera'
import { admitePago, permitePago } from './utilidadesCartera'
import { useIntentoCartera } from './useIntentoCartera'

export function PaginaDetalleCartera({ tipo }: { tipo: TipoCartera }) {
  const { id = '' } = useParams()
  if (!/^\d+$/.test(id) || !Number.isSafeInteger(Number(id)) || Number(id) <= 0) return <PaginaNoEncontrada />
  return <Detalle key={`${tipo}:${id}`} tipo={tipo} id={Number(id)} />
}
function Detalle({ tipo, id }: { tipo: TipoCartera; id: number }) {
  const { tienePermiso } = useAutenticacion()
  const datos = useRecursoVentas<DetalleCartera>(`${apiCartera(tipo)}/${id}`)
  const intento = useIntentoCartera<SolicitudPagoCartera>(`${tipo}:${id}`)
  const [modal, setModal] = useState(false); const [mensaje, setMensaje] = useState('')
  const c = datos.datos?.encabezado
  return <div className="caja-vista space-y-6">
    <EncabezadoPagina titulo={tipo === 'cobrar' ? 'Detalle de cuenta por cobrar' : 'Detalle de cuenta por pagar'} descripcion="Documento, saldo e historial financiero." acciones={<Link className={enlaceCaja} to={rutaCartera(tipo)}>Volver a cuentas</Link>} />
    {mensaje && <AvisoCartera>{mensaje}</AvisoCartera>}
    {permitePago(tipo, tienePermiso) && intento.pendiente && <Boton onClick={() => setModal(true)}>Revisar operación pendiente</Boton>}
    {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCartera error={datos.error} recargar={datos.recargar} /> : c && <>
      <SeccionCartera titulo={c.numeroDocumentoOrigen}><dl className="grid gap-5 p-5 sm:grid-cols-2 lg:grid-cols-4">
        <DatoCartera etiqueta="Documento origen"><OrigenCartera cuenta={c} tipo={tipo} /></DatoCartera><DatoCartera etiqueta={tipo === 'cobrar' ? 'Cliente' : 'Proveedor'}>{c.tercero}</DatoCartera>
        <DatoCartera etiqueta="Moneda">{c.moneda}</DatoCartera><DatoCartera etiqueta="Estado"><EstadoCartera estado={c.estado} /></DatoCartera>
        <DatoCartera etiqueta="Fecha emisión">{mostrarFechaCivil(c.fechaEmision)}</DatoCartera><DatoCartera etiqueta="Fecha vencimiento">{mostrarFechaCivil(c.fechaVencimiento)}</DatoCartera>
        <DatoCartera etiqueta="Clasificación"><EstadoCartera estado={c.vencimiento} /></DatoCartera>
        {tipo === 'pagar' && <DatoCartera etiqueta="Compra origen">{c.compraId ? tienePermiso(PERMISOS.comprasConsultar) ? <Link className="text-teal-800 underline" to={`/compras/${c.compraId}`}>Compra #{c.compraId}</Link> : `Compra #${c.compraId}` : 'Sin compra vinculada'}</DatoCartera>}
        <DatoCartera etiqueta="Importe original">{dineroCaja(c.importeOriginal, c.moneda)}</DatoCartera><DatoCartera etiqueta="Total aplicado">{dineroCaja(c.totalAplicado, c.moneda)}</DatoCartera><DatoCartera etiqueta="Saldo pendiente">{dineroCaja(c.saldoPendiente, c.moneda)}</DatoCartera>
      </dl></SeccionCartera>
      {!c.operable ? <AvisoCartera>Cuenta histórica: solo consulta hasta conciliación explícita.</AvisoCartera> : c.estado === 'PAGADA' || c.saldoPendiente === 0 ? <AvisoCartera>Cuenta pagada. No admite nuevos pagos.</AvisoCartera> : null}
      {admitePago(c) && permitePago(tipo, tienePermiso) && !intento.pendiente && <Boton onClick={() => { intento.limpiarError(); setModal(true) }}>{tipo === 'cobrar' ? 'Registrar abono' : 'Registrar pago'}</Boton>}
      <SeccionCartera titulo={tipo === 'cobrar' ? 'Historial de abonos' : 'Historial de pagos'}>
        {!datos.datos?.historial.length ? <EstadoVacio titulo="Sin pagos registrados" descripcion="Los pagos confirmados aparecerán aquí con su trazabilidad." /> : <TablaCartera etiqueta="Historial financiero" columnas={['Pago', 'Fecha', 'Monto', 'Forma de pago', 'Usuario', 'Estado', 'Observación', 'Movimientos']}>
          {datos.datos.historial.map((a) => <tr key={a.id}><td>Pago #{a.pagoId}</td><td>{mostrarInstante(a.fecha)}</td><td>{dineroCaja(a.monto, c.moneda)}</td>
            <td>{a.detalles.length > 1 ? <ul>{a.detalles.map((d) => <li key={d.id}>{d.formaPago} · {dineroCaja(d.monto, c.moneda)}</li>)}</ul> : a.formaPago}</td>
            <td>{a.usuario}</td><td><EstadoCartera estado={a.estado} /></td><td>{a.observacion ?? '—'}</td>
            <td>{a.movimientoIds.length ? a.movimientoIds.map((m) => <span className="block" key={m}>Movimiento #{m}</span>) : 'Sin movimientos vinculados'}{a.sesionCajaId && tienePermiso(PERMISOS.cajaConsultar) && <Link className="text-teal-800 underline" to={`/caja/sesiones/${a.sesionCajaId}`}>Ver sesión de caja</Link>}</td>
          </tr>)}
        </TablaCartera>}
      </SeccionCartera>
    </>}
    {modal && permitePago(tipo, tienePermiso) && <FormularioPagoCartera tipo={tipo} id={id} intento={intento} cerrar={() => setModal(false)} actualizada={(_, texto) => { setModal(false); setMensaje(texto); datos.recargar() }} />}
  </div>
}
