import { useCallback, useEffect, useState, type FormEvent } from "react";
import { ClipboardList, Plus, RotateCcw, Search } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { BotonAccionTabla, GrupoAccionesTabla } from "../../components/ui/BotonAccionTabla";
import { Boton, Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoError, EstadoVacio } from "../../components/ui/Estados";
import { Paginacion } from "../../components/ui/Paginacion";
import { PanelFiltros } from "../../components/ui/PanelFiltros";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import type { Paginado } from "../../types/api";
import type { CompraConsulta, CompraResumen, ReferenciasCompras } from "../../types/compras";
import { esBorrador, EstadoCompraBadge, fechaCorta, formatoMoneda, normalizarEstadoCompra, puedeRecibirse } from "./utilidadesCompras";

interface ProgresoOrden { completas: number; pendientes: number; total: number }
interface AccionPendiente { compra: CompraResumen; tipo: "confirmar" | "anular" }

export function PaginaCompras() {
  const navegar = useNavigate();
  const { sucursalActiva, tienePermiso } = useAutenticacion();
  const sucursalId = sucursalActiva?.id ?? 0;
  const [datos, setDatos] = useState<Paginado<CompraResumen> | null>(null);
  const [referencias, setReferencias] = useState<ReferenciasCompras | null>(null);
  const [progreso, setProgreso] = useState<Record<number, ProgresoOrden>>({});
  const [pagina, setPagina] = useState(1);
  const [tamanio, setTamanio] = useState(10);
  const [texto, setTexto] = useState("");
  const [filtros, setFiltros] = useState({ busqueda: "", proveedorId: "", estadoId: "", fechaDesde: "", fechaHasta: "" });
  const [proveedorId, setProveedorId] = useState("");
  const [estadoId, setEstadoId] = useState("");
  const [fechaDesde, setFechaDesde] = useState("");
  const [fechaHasta, setFechaHasta] = useState("");
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [accion, setAccion] = useState<AccionPendiente | null>(null);
  const [procesando, setProcesando] = useState(false);
  const { busqueda: busquedaAplicada, proveedorId: proveedorAplicado, estadoId: estadoAplicado, fechaDesde: fechaDesdeAplicada, fechaHasta: fechaHastaAplicada } = filtros;

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      const parametros = new URLSearchParams({ pagina: String(pagina), tamanioPagina: String(tamanio) });
      if (busquedaAplicada) parametros.set("busqueda", busquedaAplicada);
      if (proveedorAplicado) parametros.set("proveedorId", proveedorAplicado);
      if (estadoAplicado) parametros.set("estadoId", estadoAplicado);
      if (fechaDesdeAplicada) parametros.set("fechaDesde", fechaDesdeAplicada);
      if (fechaHastaAplicada) parametros.set("fechaHasta", fechaHastaAplicada);
      if (sucursalId) parametros.set("sucursalId", String(sucursalId));
      const paginaCompras = await solicitar<Paginado<CompraResumen>>(`/api/compras?${parametros}`);
      setDatos(paginaCompras);
      const resultados = await Promise.allSettled(
        paginaCompras.elementos.map((item) => solicitar<CompraConsulta>(`/api/compras/${item.id}`)),
      );
      const nuevo: Record<number, ProgresoOrden> = {};
      resultados.forEach((resultado, indice) => {
        if (resultado.status !== "fulfilled") return;
        const lineas = resultado.value.detalles;
        nuevo[paginaCompras.elementos[indice].id] = {
          total: lineas.length,
          completas: lineas.filter((linea) => linea.cantidadPendiente <= 0).length,
          pendientes: lineas.filter((linea) => linea.cantidadPendiente > 0).length,
        };
      });
      setProgreso(nuevo);
    } catch (excepcion) {
      setError(excepcion instanceof Error ? excepcion.message : "No fue posible consultar las órdenes de compra.");
    } finally {
      setCargando(false);
    }
  }, [busquedaAplicada, estadoAplicado, fechaDesdeAplicada, fechaHastaAplicada, pagina, proveedorAplicado, sucursalId, tamanio]);

  useEffect(() => {
    // Sincroniza la tabla con filtros, página y sucursal operativa.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);
  useEffect(() => {
    let vigente = true;
    void solicitar<ReferenciasCompras>("/api/compras/referencias")
      .then((respuesta) => { if (vigente) setReferencias(respuesta); })
      .catch(() => { /* El listado puede seguir mostrándose sin referencias auxiliares. */ });
    return () => { vigente = false; };
  }, []);

  const buscar = (evento: FormEvent) => {
    evento.preventDefault();
    setPagina(1);
    setFiltros({ busqueda: texto.trim(), proveedorId, estadoId, fechaDesde, fechaHasta });
  };
  const limpiar = () => {
    setTexto(""); setProveedorId(""); setEstadoId(""); setFechaDesde(""); setFechaHasta(""); setPagina(1);
    setFiltros({ busqueda: "", proveedorId: "", estadoId: "", fechaDesde: "", fechaHasta: "" });
  };
  const ejecutarAccion = async () => {
    if (!accion) return;
    setProcesando(true);
    setError(null);
    try {
      if (accion.tipo === "confirmar") {
        await solicitar(`/api/compras/${accion.compra.id}/confirmar`, { method: "POST" });
      } else {
        await solicitar(`/api/compras/${accion.compra.id}/estado`, { method: "PUT", body: JSON.stringify({ estado: "ANULADA" }) });
      }
      setAccion(null);
      await cargar();
    } catch (excepcion) {
      setError(excepcion instanceof Error ? excepcion.message : "No fue posible cambiar el estado de la orden.");
      setAccion(null);
    } finally { setProcesando(false); }
  };

  const administra = tienePermiso(PERMISOS.comprasAdministrar);
  const recibe = tienePermiso(PERMISOS.comprasRecibir);
  return (
    <div className="space-y-6">
      <EncabezadoPagina titulo="Órdenes de compra" descripcion="Gestiona borradores, confirmaciones y recepciones con trazabilidad completa." icono={<ClipboardList className="h-7 w-7" />} acciones={administra ? <Boton onClick={() => navegar("/compras/nueva")}><Plus className="h-4 w-4" /> Nueva orden</Boton> : undefined} />
      <PanelFiltros descripcion="Encuentra órdenes por documento, proveedor, estado o rango de fechas." acciones={<><Boton type="submit" form="filtros-compras"><Search className="h-4 w-4" /> Buscar órdenes</Boton><Boton variante="secundario" onClick={limpiar}><RotateCcw className="h-4 w-4" /> Limpiar filtros</Boton></>}>
        <form id="filtros-compras" className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" onSubmit={buscar}>
          <Entrada etiqueta="Búsqueda" value={texto} onChange={(e) => setTexto(e.target.value)} placeholder="Número, código o proveedor" />
          <SelectorEntidad etiqueta="Proveedor" ruta="/api/compras/proveedores" opcional value={proveedorId} onChange={(v) => setProveedorId(v ?? "")} />
          <SelectorEntidad etiqueta="Sucursal" value={sucursalActiva?.id ?? ""} etiquetaActual={sucursalActiva?.nombre} onChange={() => undefined} deshabilitado />
          <Seleccion etiqueta="Estado" value={estadoId} onChange={(e) => setEstadoId(e.target.value)}><option value="">Todos</option>{referencias?.estados.map((estado) => <option key={estado.id} value={estado.id}>{estado.codigo.replaceAll("_", " ")}</option>)}</Seleccion>
          <Entrada etiqueta="Fecha desde" type="date" value={fechaDesde} onChange={(e) => setFechaDesde(e.target.value)} />
          <Entrada etiqueta="Fecha hasta" type="date" value={fechaHasta} onChange={(e) => setFechaHasta(e.target.value)} />
        </form>
      </PanelFiltros>
      {error && <EstadoError mensaje={error} reintentar={cargar} />}
      <Tarjeta className="overflow-hidden">
        <div className="border-b border-slate-200 px-5 py-4"><h2 className="font-bold text-slate-950">Lista de órdenes</h2><p className="text-sm text-slate-500">{datos?.totalRegistros ?? 0} órdenes encontradas</p></div>
        {cargando ? <EstadoCargando mensaje="Consultando órdenes…" /> : !datos?.elementos.length ? <EstadoVacio titulo="No hay órdenes de compra registradas" descripcion="Crea la primera orden o cambia los filtros de búsqueda." accion={administra ? <Boton onClick={() => navegar("/compras/nueva")}>Nueva orden</Boton> : undefined} /> : <>
          <div className="overflow-x-auto"><table className="min-w-[82rem] divide-y divide-slate-200 text-left text-sm"><thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500"><tr>{["Número","Proveedor","Sucursal","Fecha","Moneda","Total","Estado","Recibido","Pendiente","Acciones"].map((x) => <th className="px-4 py-3" key={x}>{x}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">{datos.elementos.map((compra) => {
            const moneda = referencias?.monedas.find((x) => x.id === compra.monedaId); const avance = progreso[compra.id]; const estado = normalizarEstadoCompra(compra.estado);
            return <tr key={compra.id} className="hover:bg-slate-50/70"><td className="px-4 py-4 font-semibold text-slate-900">{compra.numeroOrden}</td><td className="px-4 py-4 text-slate-700">{compra.proveedor}</td><td className="px-4 py-4 text-slate-600">{compra.sucursal}</td><td className="px-4 py-4 text-slate-600">{fechaCorta(compra.fechaEmision)}</td><td className="px-4 py-4"><span className="font-semibold">{moneda?.codigoISO ?? `#${compra.monedaId}`}</span><span className="block text-xs text-slate-400">{moneda?.nombre}</span></td><td className="px-4 py-4 font-semibold tabular-nums">{formatoMoneda(compra.totalEstimado, moneda)}</td><td className="px-4 py-4"><EstadoCompraBadge estado={compra.estado} /></td><td className="px-4 py-4 text-slate-600">{avance ? `${avance.completas}/${avance.total} líneas` : "—"}</td><td className="px-4 py-4 text-slate-600">{avance ? `${avance.pendientes} líneas` : "—"}</td><td className="px-4 py-4"><GrupoAccionesTabla><BotonAccionTabla tipo="ver" onClick={() => navegar(`/compras/${compra.id}`)} />{administra && esBorrador(compra.estado) && <BotonAccionTabla tipo="editar" onClick={() => navegar(`/compras/${compra.id}/editar`)} />}{administra && esBorrador(compra.estado) && <BotonAccionTabla tipo="confirmar" onClick={() => setAccion({ compra, tipo: "confirmar" })} />}{recibe && puedeRecibirse(compra.estado) && <BotonAccionTabla tipo="recibir" onClick={() => navegar(`/compras/${compra.id}/recibir`)} />}{administra && (estado === "BORRADOR" || estado === "CONFIRMADA") && <BotonAccionTabla tipo="anular" onClick={() => setAccion({ compra, tipo: "anular" })} />}</GrupoAccionesTabla></td></tr>;
          })}</tbody></table></div><Paginacion pagina={datos.pagina} totalPaginas={datos.totalPaginas} totalRegistros={datos.totalRegistros} tamanioPagina={tamanio} cambiar={setPagina} cambiarTamanio={(n) => { setTamanio(n); setPagina(1); }} entidad="orden" /></>}
      </Tarjeta>
      <DialogoConfirmacion abierto={Boolean(accion)} cerrar={() => setAccion(null)} confirmar={ejecutarAccion} cargando={procesando} peligro={accion?.tipo === "anular"} titulo={accion?.tipo === "confirmar" ? "Confirmar orden" : "Anular orden"} descripcion={accion?.tipo === "confirmar" ? "Después de confirmar la orden ya no podrá editar sus detalles." : "Esta acción no elimina la orden y quedará registrada en auditoría."} textoConfirmar={accion?.tipo === "confirmar" ? "Confirmar" : "Anular orden"}>{accion && <p className="rounded-lg bg-slate-50 p-3 text-sm text-slate-600">Orden <strong>{accion.compra.numeroOrden}</strong> · {accion.compra.proveedor}</p>}</DialogoConfirmacion>
    </div>
  );
}
