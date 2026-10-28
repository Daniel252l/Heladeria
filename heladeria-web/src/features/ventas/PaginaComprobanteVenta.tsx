import { Link, useParams } from 'react-router-dom'
import { Boton } from '../../components/ui/Controles'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import type { VentaConsulta } from '../../types/ventas'
import { mostrarInstante } from '../../utils/fechas'
import { enlaceVenta, ErrorVenta } from './ComponentesVentas'
import { useRecursoVentas } from './useDatosVentas'
import { dineroVenta, numeroVenta } from './utilidadesVentas'
import './comprobante.css'

export function PaginaComprobanteVenta() {
  const { id } = useParams()
  const carga = useRecursoVentas<VentaConsulta>(`/api/ventas/${id}`)
  return <main className="comprobante-pagina">
    <div className="comprobante-controles"><Link className={enlaceVenta} to={`/ventas/${id}`}>Volver a venta</Link></div>
    {carga.cargando ? <EstadoCargando /> : carga.error ? <ErrorVenta error={carga.error} recargar={carga.recargar} /> : carga.datos && <ComprobanteVenta venta={carga.datos} />}
  </main>
}

export function ComprobanteVenta({ venta }: { venta: VentaConsulta }) {
  const o = venta.encabezado, c = venta.comprobante
  if (!c || !o.fechaCompletada || !['COMPLETADA', 'ANULADA'].includes(o.estado)) return <EstadoVacio titulo="Comprobante no disponible" descripcion="Solo las ventas realizadas con detalle histórico completo tienen comprobante." />
  const dinero = (valor: number | null) => dineroVenta(valor, o.moneda)
  const empresa = datoComercial(c.empresa), razonSocial = datoComercial(c.razonSocial), sucursal = datoComercial(o.sucursal)
  const direccion = datoComercial(c.direccionSucursal), telefono = datoComercial(c.telefonoSucursal)
  return <>
    <div className="comprobante-controles"><Boton onClick={() => window.print()}>Imprimir comprobante</Boton></div>
    <article className="comprobante-ticket" aria-label="Comprobante de venta">
      <header>{empresa && <h1>{empresa}</h1>}{razonSocial && razonSocial !== empresa && <p>{razonSocial}</p>}{sucursal && <p>{sucursal}</p>}{direccion && <p>{direccion}</p>}{telefono && <p>{telefono}</p>}
        <h2>COMPROBANTE DE VENTA</h2><strong>{o.numeroVenta}</strong>
        {o.estado === 'ANULADA' && <p className="comprobante-anulada">ANULADA</p>}
      </header>
      <dl><Fila etiqueta="Fecha">{mostrarInstante(o.fechaCompletada, true, c.zonaHoraria)}</Fila><Fila etiqueta="Usuario">{c.usuarioFinalizacion ?? o.usuario}</Fila><Fila etiqueta="Moneda">{o.moneda}</Fila>
        <Fila etiqueta="Cliente">{o.cliente ?? 'Venta sin cliente identificado'}</Fila>{c.codigoCliente && <Fila etiqueta="Código">{c.codigoCliente}</Fila>}
        <Fila etiqueta="Modalidad">{o.esCredito ? 'Crédito' : 'Contado'}</Fila></dl>
      <section aria-label="Productos del comprobante">{venta.detalles.map(d => <div className="comprobante-linea" key={d.id}>
        <strong>{d.producto}</strong><p>{d.presentacion ?? 'Unidad base'}</p>
        <div className="comprobante-fila"><span>{numeroVenta.format(d.cantidad)} {d.unidad} × {dinero(d.precioUnitario)}</span><strong>{dinero(d.totalLinea)}</strong></div>
        {d.montoDescuento > 0 && <p>Descuento: {dinero(d.montoDescuento)}</p>}
      </div>)}</section>
      <dl><Fila etiqueta="Subtotal">{dinero(o.subtotal)}</Fila><Fila etiqueta="Descuento">{dinero(o.descuento)}</Fila><Fila etiqueta="Impuesto">{dinero(o.impuesto)}</Fila><Fila etiqueta="TOTAL"><strong>{dinero(o.total)}</strong></Fila></dl>
      {o.esCredito ? <dl>{c.saldoInicialCredito !== null && <Fila etiqueta="Saldo inicial CxC">{dinero(c.saldoInicialCredito)}</Fila>}<Fila etiqueta="Cobro al emitir">No aplica · venta a crédito</Fila></dl>
        : <section aria-label="Pagos del comprobante"><h3>Pago</h3>{c.pagos.length ? c.pagos.map((p, i) => <dl key={i}><Fila etiqueta={p.formaPago}>{dinero(p.aplicado)}</Fila><Fila etiqueta={p.esEfectivo ? 'Efectivo recibido' : 'Recibido'}>{p.recibido === null ? 'No registrado' : dinero(p.recibido)}</Fila>{p.esEfectivo && <Fila etiqueta="Vuelto">{p.vuelto === null ? 'No registrado' : dinero(p.vuelto)}</Fila>}</dl>) : <p>{o.total === 0 ? 'Sin importe a cobrar' : 'Sin pago registrado'}</p>}</section>}
      {o.estado === 'ANULADA' && <p>Anulación: {mostrarInstante(o.fechaAnulacion, true, c.zonaHoraria)}<br />{o.motivoAnulacion}</p>}
      <footer><strong>Comprobante interno — no constituye documento tributario</strong><p>Importes históricos. Nombres y datos comerciales del catálogo actual.</p></footer>
    </article>
  </>
}
function Fila({ etiqueta, children }: { etiqueta: string; children: React.ReactNode }) { return <div className="comprobante-fila"><dt>{etiqueta}</dt><dd>{children}</dd></div> }

// Solo presentación comercial del encabezado. Coincidencia completa para conservar nombres/direcciones reales.
function datoComercial(valor: string | null | undefined) {
  const texto = valor?.trim()
  if (!texto || /^(?:pendiente(?: de (?:configurar|configuración))?|(?:sin|por) configurar)[.!…]*$/iu.test(texto.normalize('NFKC').replace(/\s+/g, ' '))) return null
  return texto
}
