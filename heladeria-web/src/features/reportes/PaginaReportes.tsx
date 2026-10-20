import { useState } from 'react'
import { ChartNoAxesCombined, ShieldCheck } from 'lucide-react'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando } from '../../components/ui/Estados'
import { Paginacion } from '../../components/ui/Paginacion'
import type { ResultadoReporte, TipoReporte } from '../../types/reportes'
import { mostrarFechaCivil } from '../../utils/fechas'
import { descripcionReporte, tituloReporte } from './configuracionReportes'
import { ErrorReporte, PeriodoConsultado } from './ComponentesReportes'
import { FiltrosReportes } from './FiltrosReportes'
import type { FiltrosEditables } from './utilidadesReportes'
import { ResultadoAuditoria } from './ResultadoAuditoria'
import { ResultadoCompras, ResultadoProductos, ResultadoVentas } from './ResultadosComerciales'
import { ResultadoInventario, ResultadoKardex } from './ResultadosExistencias'
import { ResultadoCaja, ResultadoCartera } from './ResultadosFinancieros'
import { paginadoReporte, periodoReporte, useReporte } from './useReporte'
import './reportes.css'

export function PaginaReportes({ tipo }: { tipo: TipoReporte }) {
  const { sucursalActiva, sucursales } = useAutenticacion()
  const inicial: FiltrosEditables = { sucursalId: String(sucursalActiva?.id ?? '') }
  const [filtros, setFiltros] = useState(inicial)
  const [aplicados, setAplicados] = useState(inicial)
  const [pagina, setPagina] = useState(1)
  const [tamanio, setTamanio] = useState(20)
  const carga = useReporte(tipo, aplicados, pagina, tamanio)
  const resultado = carga.datos
  const periodo = resultado && periodoReporte(resultado)
  const sucursalId = periodo?.sucursalId ?? (resultado?.tipo === 'inventario' ? resultado.respuesta.sucursalId : Number(aplicados.sucursalId))
  const sucursal = sucursales.find((s) => s.id === sucursalId)
  const nombreSucursal = sucursal?.nombre ?? `Sucursal #${sucursalId}`
  const zona = periodo?.zonaHoraria ?? sucursal?.zonaHoraria ?? 'UTC'
  const paginado = resultado && paginadoReporte(resultado)
  return <div className="reportes-vista space-y-6">
    <EncabezadoPagina titulo={tituloReporte(tipo)} descripcion={descripcionReporte[tipo]} icono={tipo === 'auditoria' ? <ShieldCheck /> : <ChartNoAxesCombined />} />
    <FiltrosReportes tipo={tipo} filtros={filtros} cambiar={setFiltros}
      aplicar={() => { setAplicados({ ...filtros }); setPagina(1) }}
      limpiar={() => { setFiltros(inicial); setAplicados(inicial); setPagina(1) }} />
    {carga.cargando ? <EstadoCargando mensaje="Consultando reporte…" /> : carga.error ? <ErrorReporte error={carga.error} recargar={carga.recargar} /> : resultado && <>
      {periodo ? <PeriodoConsultado periodo={periodo} sucursal={nombreSucursal} /> : tipo !== 'inventario' &&
        <p className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-600" aria-label="Rango solicitado">
          Rango solicitado: {aplicados.fechaDesde ? mostrarFechaCivil(aplicados.fechaDesde) : 'inicio predeterminado del servidor'}
          {' — '}{aplicados.fechaHasta ? mostrarFechaCivil(aplicados.fechaHasta) : 'fin predeterminado del servidor'} · {nombreSucursal} · {zona}.
          {' '}Este endpoint no devuelve el período efectivo.
        </p>}
      <Resultados resultado={resultado} zona={zona} sucursal={nombreSucursal} />
      {paginado && <Paginacion {...paginado} cambiar={setPagina} cambiarTamanio={(n) => { setTamanio(n); setPagina(1) }} />}
    </>}
  </div>
}
function Resultados({ resultado: r, zona, sucursal }: { resultado: ResultadoReporte; zona: string; sucursal: string }) {
  switch (r.tipo) {
    case 'ventas': return <ResultadoVentas datos={r.respuesta} zona={zona} />
    case 'productos-vendidos': return <ResultadoProductos datos={r.respuesta} />
    case 'inventario': return <ResultadoInventario datos={r.respuesta} sucursal={sucursal} />
    case 'kardex': return <ResultadoKardex datos={r.respuesta} zona={zona} />
    case 'compras': return <ResultadoCompras datos={r.respuesta} />
    case 'caja': return <ResultadoCaja datos={r.respuesta} zona={zona} />
    case 'cuentas-por-cobrar': return <ResultadoCartera datos={r.respuesta} pagar={false} />
    case 'cuentas-por-pagar': return <ResultadoCartera datos={r.respuesta} pagar />
    case 'auditoria': return <ResultadoAuditoria datos={r.respuesta} zona={zona} />
  }
}
