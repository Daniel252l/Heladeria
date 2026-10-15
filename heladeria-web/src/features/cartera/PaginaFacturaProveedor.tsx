import { useCallback, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { carteraApi, leerPaginas } from '../../api/cartera'
import { ErrorApi, solicitar } from '../../api/clienteHttp'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton, Entrada } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { SelectorEntidad } from '../../components/ui/SelectorEntidad'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import type { CuentaCartera, DetalleCartera, DetalleCompraCredito, SolicitudFacturaCartera } from '../../types/cartera'
import { fechaHoraLocal, mostrarFechaCivil } from '../../utils/fechas'
import { dineroCaja, enlaceCaja } from '../caja/utilidadesCaja'
import { useCargaVentas, useRecursoVentas } from '../ventas/useDatosVentas'
import { AvisoCartera, ErrorCartera, SeccionCartera } from './ComponentesCartera'
import { comprasFacturables, validarDetalleCompra } from './comprasFacturables'
import { useIntentoCartera } from './useIntentoCartera'

export function PaginaFacturaProveedor() {
  const { sucursalActiva } = useAutenticacion()
  const empresa = sucursalActiva?.empresaId ?? 0; const sucursal = sucursalActiva?.id ?? 0
  const intento = useIntentoCartera<SolicitudFacturaCartera>('factura')
  const cargar = useCallback((signal: AbortSignal) => comprasFacturables(empresa, sucursal, signal), [empresa, sucursal])
  const opciones = useCargaVentas('compras-facturables', cargar)
  const [compraId, setCompraId] = useState(String(intento.payload?.ordenCompraId ?? ''))
  const compra = useRecursoVentas<DetalleCompraCredito>(compraId ? `/api/compras/${compraId}` : null)
  const [serie, setSerie] = useState(intento.payload?.serie ?? '')
  const [numero, setNumero] = useState(intento.payload?.numeroDocumento ?? '')
  const [emision, setEmision] = useState(intento.payload?.fechaDocumento ?? fechaHoraLocal().slice(0, 10))
  const [vence, setVence] = useState(intento.payload?.fechaVencimiento ?? '')
  const [confirmar, setConfirmar] = useState(false); const [validacion, setValidacion] = useState('')
  const [resultado, setResultado] = useState<DetalleCartera | null>(null); const [mensaje, setMensaje] = useState('')
  const encabezado = compra.datos?.encabezado
  const moneda = opciones.datos?.referencias.monedas.find((m) => m.id === encabezado?.monedaId)?.codigoISO ?? null
  let elegible = false
  if (compra.datos && opciones.datos?.compras.some((c) => String(c.id) === compraId)) {
    try { validarDetalleCompra(compra.datos, empresa, sucursal); elegible = true } catch { /* Solo lectura: no se ofrece enviar una compra no elegible. */ }
  }
  function actualizar() { compra.recargar(); opciones.recargar() }
  async function enviar(e?: FormEvent) {
    e?.preventDefault()
    if (!intento.pendiente) {
      if (!elegible || !numero.trim() || !emision || !vence || emision > fechaHoraLocal().slice(0, 10) || vence < emision) { setValidacion('Revise compra elegible, número de factura, emisión no futura y vencimiento igual o posterior a emisión.'); return }
      if (!confirmar) { setValidacion(''); setConfirmar(true); return }
    }
    const r = await intento.enviar(async () => {
      const [actual, cuentas] = await Promise.all([
        solicitar<DetalleCompraCredito>(`/api/compras/${compraId}`),
        leerPaginas<CuentaCartera>(`/api/cuentas-por-pagar?sucursalId=${sucursal}`),
      ])
      validarDetalleCompra(actual, empresa, sucursal)
      if (cuentas.some((c) => c.compraId === actual.encabezado.id)) throw new ErrorApi('Compra facturada', 409, { codigo: 'DOCUMENTO_FINANCIERO_DUPLICADO' })
      if (actual.encabezado.versionFila !== encabezado?.versionFila) throw new ErrorApi('La compra cambió', 409, { codigo: 'CONCURRENCIA_CXP' })
      return { claveOperacion: crypto.randomUUID(), ordenCompraId: actual.encabezado.id, versionCompra: actual.encabezado.versionFila,
        serie: serie.trim() || null, numeroDocumento: numero.trim(), fechaDocumento: emision, fechaVencimiento: vence }
    }, carteraApi.facturar)
    if (r) { setResultado(r.cuenta); setMensaje(r.reintento ? 'Factura ya registrada; resultado recuperado sin duplicarla.' : 'Factura registrada. Cuenta por pagar creada.'); actualizar() }
    else { setConfirmar(false); actualizar() }
  }
  return <div className="caja-vista space-y-6">
    <EncabezadoPagina titulo="Registrar factura de proveedor" descripcion="Documento real de una compra a crédito totalmente recibida en la sucursal activa." acciones={<Link className={enlaceCaja} to="/cuentas-por-pagar">Volver a cuentas</Link>} />
    {resultado ? <SeccionCartera titulo="Factura registrada"><div className="space-y-4 p-5"><AvisoCartera>{mensaje}</AvisoCartera>
      <p>{resultado.encabezado.numeroDocumentoOrigen} · {resultado.encabezado.tercero} · {dineroCaja(resultado.encabezado.saldoPendiente, resultado.encabezado.moneda)}</p>
      <Link className={enlaceCaja} to={`/cuentas-por-pagar/${resultado.encabezado.id}`}>Ver cuenta por pagar</Link>
    </div></SeccionCartera> : <SeccionCartera titulo="Datos de la factura"><form className="space-y-5 p-4 sm:p-6" onSubmit={(e) => void enviar(e)}>
      {intento.pendiente && <AvisoCartera>Existe una factura pendiente de confirmar. No cambie sus datos ni genere otra UUID. {intento.payload ? 'Puede reintentar exactamente la misma solicitud.' : 'El formulario ya no está disponible tras recargar; consulte las cuentas y concilie con administración antes de otra operación.'}</AvisoCartera>}
      {opciones.cargando ? <EstadoCargando /> : opciones.error ? <ErrorCartera error={opciones.error} recargar={opciones.recargar} /> : <>
        {!opciones.datos?.compras.length && <EstadoVacio titulo="No hay compras candidatas" descripcion="Se requieren compras explícitamente a crédito, recibidas y sin CxP vinculada." />}
        <fieldset disabled={intento.procesando || intento.pendiente || confirmar} className="grid gap-4 sm:grid-cols-2">
          <div className="sm:col-span-2"><SelectorEntidad etiqueta="Compra recibida a crédito" value={compraId} onChange={(v) => setCompraId(v ?? '')} opcionesLocales={opciones.datos?.compras.map((c) => ({ id: c.id, codigo: c.numeroOrden, nombre: c.proveedor })) ?? []} /></div>
          <Entrada etiqueta="Serie (opcional)" maxLength={30} value={serie} onChange={(e) => setSerie(e.target.value)} />
          <Entrada etiqueta="Número de factura" required maxLength={50} value={numero} onChange={(e) => setNumero(e.target.value)} />
          <Entrada etiqueta="Fecha documento" type="date" required max={fechaHoraLocal().slice(0, 10)} value={emision} onChange={(e) => setEmision(e.target.value)} />
          <Entrada etiqueta="Fecha vencimiento" type="date" required min={emision} value={vence} onChange={(e) => setVence(e.target.value)} />
        </fieldset>
      </>}
      {compraId && (compra.cargando ? <EstadoCargando mensaje="Verificando la compra…" /> : compra.error ? <ErrorCartera error={compra.error} recargar={compra.recargar} /> : encabezado && <AvisoCartera error={!elegible && !intento.pendiente}>
        {encabezado.numeroOrden} · {encabezado.proveedor} · {mostrarFechaCivil(encabezado.fechaEmision)} · {dineroCaja(encabezado.totalEstimado, moneda)}.
        {!elegible && !intento.pendiente ? ' Esta compra no es elegible o ya tiene una cuenta vinculada.' : ' Recepción completa. El servidor verificará operatividad, importes y ausencia de factura previa.'}
      </AvisoCartera>)}
      {confirmar && <AvisoCartera>Confirme el registro del documento {serie} {numero}. No se modificará la recepción de inventario.</AvisoCartera>}
      {validacion && <AvisoCartera error>{validacion}</AvisoCartera>}{intento.error && <ErrorCartera error={intento.error} accion />}
      <div className="flex flex-wrap justify-end gap-3">
        {intento.pendiente ? <>{intento.payload && <Boton type="button" cargando={intento.procesando} onClick={() => void enviar()}>Reintentar misma factura</Boton>}<Link className={enlaceCaja} to="/cuentas-por-pagar">Consultar cuentas registradas</Link></>
          : <>{confirmar && <Boton type="button" variante="secundario" disabled={intento.procesando} onClick={() => setConfirmar(false)}>Editar datos</Boton>}<Boton type="submit" cargando={intento.procesando} disabled={!elegible || compra.cargando || opciones.cargando || intento.almacenamientoFallido}>{confirmar ? 'Confirmar factura' : 'Revisar factura'}</Boton></>}
      </div>
    </form></SeccionCartera>}
  </div>
}
