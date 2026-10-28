import { useState } from 'react'
import { Link } from 'react-router-dom'
import { CalendarDays } from 'lucide-react'
import { rutaDiario } from '../../api/diario'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'
import { Boton, Entrada, Seleccion } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando } from '../../components/ui/Estados'
import { Paginacion } from '../../components/ui/Paginacion'
import type { FiltroDiario, ReporteDiario } from '../../types/diario'
import type { Paginado } from '../../types/api'
import { mostrarFechaCivil, mostrarInstante } from '../../utils/fechas'
import { useRecursoVentas } from '../ventas/useDatosVentas'
import { dineroVenta } from '../ventas/utilidadesVentas'
import { ErrorReporte, ResumenReporte, TablaReporte } from './ComponentesReportes'
import './reportes.css'
import './diario.css'

export function PaginaReporteDiario() {
  const { usuario, sucursalActiva } = useAutenticacion()
  return <Diario key={`${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}`} />
}
function Diario() {
  const { sucursalActiva, sucursales, tienePermiso } = useAutenticacion()
  const inicial = { fecha: null as string | null, sucursalId: String(sucursalActiva?.id ?? ''), cajaId: '', formaPagoId: '', modalidad: '' }
  const [filtros, setFiltros] = useState(inicial)
  const [aplicados, setAplicados] = useState(inicial)
  const [paginas, setPaginas] = useState({ pagina: 1, paginaOperaciones: 1, paginaCajas: 1 })
  const parametros: FiltroDiario = { ...paginas, tamanioPagina: 20, fecha: aplicados.fecha || undefined, sucursalId: Number(aplicados.sucursalId) || undefined,
    cajaId: Number(aplicados.cajaId) || undefined, formaPagoId: Number(aplicados.formaPagoId) || undefined, esCredito: aplicados.modalidad === '' ? undefined : aplicados.modalidad === 'credito' }
  const carga = useRecursoVentas<ReporteDiario>(rutaDiario(parametros)), d = carga.datos
  const campo = (clave: keyof typeof filtros, valor: string) => setFiltros({ ...filtros, [clave]: valor })
  const fechaVisible = filtros.fecha ?? (filtros.sucursalId === aplicados.sucursalId ? d?.periodo.fechaDesde : '') ?? ''
  const aplicar = () => { setAplicados({ ...filtros, fecha: fechaVisible }); setPaginas({ pagina: 1, paginaOperaciones: 1, paginaCajas: 1 }) }
  const paginar = (clave: keyof typeof paginas, pagina: number) => {
    // Fijar el día efectivo al paginar, incluso si la consulta inicial cruzó medianoche.
    if (!aplicados.fecha && d) setAplicados({ ...aplicados, fecha: d.periodo.fechaDesde })
    setPaginas({ ...paginas, [clave]: pagina })
  }
  const verVentas = tienePermiso(PERMISOS.ventasConsultar) && d?.periodo.sucursalId === sucursalActiva?.id
  return <div className="reportes-vista diario-vista space-y-6">
    <EncabezadoPagina titulo="Reporte diario" descripcion="Actividad comercial, cobros y caja de un día civil." icono={<CalendarDays />} acciones={<div className="diario-no-imprimir"><Boton disabled={!d || carga.cargando || !!carga.error} onClick={() => window.print()}>Imprimir reporte</Boton></div>} />
    <form className="diario-no-imprimir rounded-xl border border-slate-200 bg-white p-5" onSubmit={e => { e.preventDefault(); aplicar() }}>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <Entrada etiqueta="Fecha" aria-label="Fecha" type="date" value={fechaVisible} min="1900-01-01" max="9998-12-31" onChange={e => campo('fecha', e.target.value)} ayuda="Día actual calculado por el servidor si se deja vacío." />
        <Seleccion etiqueta="Sucursal" aria-label="Sucursal" value={filtros.sucursalId} onChange={e => campo('sucursalId', e.target.value)}>{sucursales.filter(s => s.empresaId === sucursalActiva?.empresaId).map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}</Seleccion>
        <Entrada etiqueta="Caja (ID opcional)" type="number" min={1} step={1} value={filtros.cajaId} onChange={e => campo('cajaId', e.target.value)} />
        <Entrada etiqueta="Forma de pago (ID opcional)" type="number" min={1} step={1} value={filtros.formaPagoId} onChange={e => campo('formaPagoId', e.target.value)} />
        <Seleccion etiqueta="Modalidad" aria-label="Modalidad" value={filtros.modalidad} onChange={e => campo('modalidad', e.target.value)}><option value="">Todas</option><option value="contado">Contado</option><option value="credito">Crédito</option></Seleccion>
      </div><div className="mt-4 flex flex-wrap gap-3"><Boton type="submit">Aplicar</Boton><Boton type="button" variante="secundario" onClick={() => { setFiltros(inicial); setAplicados(inicial); setPaginas({ pagina: 1, paginaOperaciones: 1, paginaCajas: 1 }) }}>Limpiar</Boton></div>
    </form>
    {carga.cargando ? <EstadoCargando mensaje="Consultando reporte diario…" /> : carga.error ? <ErrorReporte error={carga.error} recargar={carga.recargar} /> : d && <>
      <p className="rounded-xl border border-teal-200 bg-teal-50 p-4" aria-label="Día consultado"><strong>{mostrarFechaCivil(d.periodo.fechaDesde)} · {d.sucursal}</strong> · {d.periodo.zonaHoraria}<br />Generado: {mostrarInstante(d.generadoUtc, true, d.periodo.zonaHoraria)}<br />Caja: {aplicados.cajaId || 'Todas'} · Forma: {aplicados.formaPagoId || 'Todas'} · Modalidad: {aplicados.modalidad || 'Todas'}</p>
      <p className="text-sm text-slate-600">Ventas = actividad comercial. Cobros = pagos recibidos. Caja = movimientos financieros. Estas dimensiones no se suman entre sí. Las anulaciones se presentan separadas; el estado mostrado es el actual.</p>
      {tienePermiso(PERMISOS.ventasConsultar) && d.periodo.sucursalId !== sucursalActiva?.id && <p className="diario-no-imprimir text-sm text-slate-600">Para abrir ventas o comprobantes de esta sucursal, selecciónela como sucursal activa.</p>}
      {(aplicados.cajaId || aplicados.formaPagoId || aplicados.modalidad) && <p className="text-sm text-slate-600">Caja/forma solo atribuyen ventas con un pago asociado; excluyen crédito emitido sin cobro. Modalidad filtra operaciones a cobros de ventas o CxC; no incluye aperturas, pagos CxP ni movimientos manuales. Los saldos de turno conservan todas sus formas y operaciones.</p>}
      {!d.ventasTotales.length && <p>Sin ventas realizadas para este día y filtros.</p>}
      {d.ventasTotales.map(v => <ResumenReporte key={`${v.monedaId}:${v.estado}`} titulo={`Ventas ${v.estado} · ${v.moneda}`} valores={[
        ['Cantidad', v.cantidad], ['Total ventas', dineroVenta(v.total, v.moneda)], ['Descuentos', dineroVenta(v.descuentos, v.moneda)], ['Costo', dineroVenta(v.costo, v.moneda)], ['Margen bruto', dineroVenta(v.margenBruto, v.moneda)],
      ]} />)}
      <TablaReporte titulo="Cobros por forma de pago" filas={d.cobrosPorForma} columnas={[
        ['Forma', x => x.formaPago], ['Moneda', x => x.moneda], ['Aplicado', x => dineroVenta(x.aplicado, x.moneda)],
      ]} />
      <TablaReporte titulo="Crédito generado · importe original CxC" filas={d.creditosGenerados} columnas={[
        ['Moneda', x => x.moneda], ['Cuentas', x => x.cantidad], ['Importe original', x => dineroVenta(x.importeOriginal, x.moneda)],
      ]} />
      {d.finanzas.map((f, i) => <ResumenReporte key={i} titulo={`Movimientos del día · ${f.moneda ?? 'Moneda sin registrar'}`} valores={[
        ['Aperturas (no son ingreso)', dineroVenta(f.aperturas, f.moneda ?? '')], ['Ingresos operativos', dineroVenta(f.ingresos, f.moneda ?? '')], ['Egresos', dineroVenta(f.egresos, f.moneda ?? '')],
        ['De ingresos: cobros ventas', dineroVenta(f.cobrosVentas, f.moneda ?? '')], ['De ingresos: cobros CxC', dineroVenta(f.cobrosCxc, f.moneda ?? '')], ['De egresos: pagos CxP', dineroVenta(f.pagosCxp, f.moneda ?? '')], ['Movimientos sin clasificación completa', f.sinClasificar],
      ]} />)}
      <TablaReporte titulo="Ventas del día" filas={d.ventas.elementos} columnas={[
        ['Documento', v => <>{verVentas ? <Link to={`/ventas/${v.id}`}>{v.numero}</Link> : v.numero}{verVentas && v.tieneComprobante && <Link className="diario-no-imprimir block underline" to={`/ventas/${v.id}/comprobante`}>Ver comprobante</Link>}</>],
        ['Fecha / hora', v => mostrarInstante(v.fecha, true, d.periodo.zonaHoraria)], ['Cliente', v => v.cliente ?? 'Sin cliente identificado'], ['Modalidad', v => v.esCredito ? 'Crédito' : 'Contado'], ['Estado', v => v.estado], ['Total', v => dineroVenta(v.total, v.moneda)], ['Formas', v => v.formasPago.join(', ') || 'Sin cobro directo'], ['Usuario', v => v.usuario],
      ]} />
      <PaginaDiario titulo="ventas" datos={d.ventas} cambiar={p => paginar('pagina', p)} />
      <TablaReporte titulo="Operaciones financieras del día" filas={d.operaciones.elementos} columnas={[
        ['Fecha / hora', x => mostrarInstante(x.fecha, true, d.periodo.zonaHoraria)], ['Tipo', x => x.tipo.replaceAll('_', ' ')], ['Documento', x => x.documento ?? '—'], ['Referencia', x => x.referencia ?? '—'], ['Forma', x => x.formaPago ?? '—'], ['Apertura', x => dineroVenta(x.apertura, x.moneda ?? '')], ['Ingreso', x => dineroVenta(x.ingreso, x.moneda ?? '')], ['Egreso', x => dineroVenta(x.egreso, x.moneda ?? '')], ['Usuario', x => x.usuario],
      ]} />
      <PaginaDiario titulo="operaciones" datos={d.operaciones} cambiar={p => paginar('paginaOperaciones', p)} />
      <p className="text-sm text-slate-600">Turnos que cruzan el día: saldos completos hasta el cierre o fin del día consultado. No son solo los movimientos del día. Contado y diferencia aparecen únicamente con cierre registrado.</p>
      <TablaReporte titulo="Caja · saldos de turnos al corte" filas={d.cajas.elementos} columnas={[
        ['Caja / turno', x => `${x.caja} · #${x.id}`], ['Estado al corte', x => x.estado], ['Inicial', x => dineroVenta(x.saldoInicial, x.moneda ?? '')], ['Ingresos del turno', x => dineroVenta(x.ingresosOperativos, x.moneda ?? '')], ['Egresos del turno', x => dineroVenta(x.egresosOperativos, x.moneda ?? '')], ['Saldo teórico', x => dineroVenta(x.saldoTeorico, x.moneda ?? '')], ['Efectivo esperado', x => dineroVenta(x.efectivoEsperado, x.moneda ?? '')], ['Contado', x => dineroVenta(x.efectivoContado, x.moneda ?? '')], ['Diferencia', x => dineroVenta(x.diferencia, x.moneda ?? '')],
      ]} />
      <PaginaDiario titulo="turnos" datos={d.cajas} cambiar={p => paginar('paginaCajas', p)} />
      <p className="text-sm text-slate-600">Impresión: incluye los resúmenes completos y solo las páginas de detalle visibles. Cambie de página para consultar el resto.</p>
    </>}
  </div>
}
function PaginaDiario<T>({ titulo, datos, cambiar }: { titulo: string; datos: Paginado<T>; cambiar: (pagina: number) => void }) {
  return <section aria-label={`Paginación de ${titulo}`}><p className="text-xs text-slate-600">{titulo}: página {datos.pagina} de {Math.max(1, datos.totalPaginas)} · {datos.totalRegistros} registros.</p><div className="diario-no-imprimir"><Paginacion {...datos} cambiar={cambiar} /></div></section>
}
