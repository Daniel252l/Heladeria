import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { solicitar } from "../../api/clienteHttp";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando } from "../../components/ui/Estados";
import type { OrdenProduccionConsulta, RecetaConsulta, ReferenciasProduccion } from "../../types/produccion";
import { AvisoProduccion, ErrorProduccion, IngredientesReceta, PieFormularioProduccion } from "./ComponentesProduccion";
import { useOperacionProduccion, usePresentacionesProduccion, useRecursoProduccion } from "./useDatosProduccion";
import { cantidadValida, fechaLocal, medidaProduccion, numeroProduccion as n, opcionesPresentacion } from "./utilidadesProduccion";

export function PaginaFormularioProduccion() {
  const { id } = useParams();
  const refs = useRecursoProduccion<ReferenciasProduccion>("/api/produccion/referencias");
  const orden = useRecursoProduccion<OrdenProduccionConsulta>(id ? `/api/produccion/${id}` : null);
  if (refs.cargando || orden.cargando) return <EstadoCargando />;
  if (refs.error) return <ErrorProduccion error={refs.error} reintentar={refs.recargar} />;
  if (orden.error) return <ErrorProduccion error={orden.error} reintentar={orden.recargar} />;
  if (orden.datos && orden.datos.encabezado.estado !== "BORRADOR") return <AvisoProduccion>Esta orden está {orden.datos.encabezado.estado.toLowerCase()} y no admite edición. <Link to={`/produccion/${id}`}>Ver orden</Link></AvisoProduccion>;
  return refs.datos ? <FormularioOrden key={`${id ?? "nueva"}:${orden.datos?.encabezado.versionFila}`} inicial={orden.datos} referencias={refs.datos} /> : null;
}
function FormularioOrden({ inicial, referencias }: { inicial: OrdenProduccionConsulta | null; referencias: ReferenciasProduccion }) {
  const { sucursalActiva } = useAutenticacion();
  const navegar = useNavigate();
  const o = inicial?.encabezado;
  const [recetaId, setRecetaId] = useState(o?.recetaId ?? 0);
  const [bodegaId, setBodegaId] = useState(o?.bodegaProduccionId ?? 0);
  const [presentacionId, setPresentacionId] = useState<number | null>(o?.productoPresentacionId ?? null);
  const [cantidad, setCantidad] = useState(o?.cantidadPlanificada ?? 1);
  const [fecha, setFecha] = useState(o ? fechaLocal(new Date(o.fechaInicio)) : fechaLocal());
  const [validacion, setValidacion] = useState("");
  const operacion = useOperacionProduccion();
  const receta = useRecursoProduccion<RecetaConsulta>(recetaId ? `/api/produccion/recetas/${recetaId}` : null);
  const productoId = receta.datos?.encabezado.productoTerminadoId ?? 0;
  const presentaciones = usePresentacionesProduccion([productoId], "/api/produccion");
  const medida = medidaProduccion(presentaciones.datos?.[productoId], presentacionId);
  const escala = medida && receta.datos?.encabezado.rendimientoBase ? cantidad * medida.factor / receta.datos.encabezado.rendimientoBase : 0;
  const bodegas = [...new Map(referencias.almacenes.map((a) => [a.bodegaId, { id: a.bodegaId, nombre: a.bodega }])).values()];
  const guardar = async (e: FormEvent) => {
    e.preventDefault(); setValidacion("");
    if (!sucursalActiva || !bodegaId || !receta.datos || !medida || !cantidadValida(cantidad) || !fecha || !referencias.recetasVigentes.some((r) => r.id === recetaId)) { setValidacion("Seleccione una bodega, receta vigente, presentación y cantidad válida."); return; }
    // Hora civil planificada; el servidor resuelve la zona de la sucursal autenticada.
    const resultado = await operacion.ejecutar(() => solicitar<OrdenProduccionConsulta>(o ? `/api/produccion/${o.id}` : "/api/produccion", { method: o ? "PUT" : "POST", body: JSON.stringify({ sucursalId: sucursalActiva.id, bodegaProduccionId: bodegaId, recetaId, productoPresentacionId: presentacionId, cantidadPlanificada: cantidad, fechaInicio: fecha, ...(o ? { versionFila: o.versionFila } : {}) }) }));
    if (resultado) navegar(`/produccion/${resultado.encabezado.id}`);
  };
  return <form className="space-y-6" onSubmit={(e) => void guardar(e)}><EncabezadoPagina titulo={o ? "Editar orden de producción" : "Nueva orden de producción"} descripcion="Planifique cantidades a partir de una receta vigente." />
    <Tarjeta className="p-4 sm:p-6"><fieldset disabled={operacion.procesando} className="grid min-w-0 gap-5 sm:grid-cols-2">
      <Entrada etiqueta="Sucursal" readOnly value={sucursalActiva?.nombre ?? ""} ayuda="Se utiliza la sucursal activa. Puede cambiarla desde el encabezado." />
      <SelectorEntidad etiqueta="Bodega de producción *" opcionesLocales={bodegas} value={bodegaId || ""} onChange={(id) => setBodegaId(Number(id))} />
      <Seleccion etiqueta="Receta vigente *" required value={recetaId || ""} onChange={(e) => { setRecetaId(Number(e.target.value)); setPresentacionId(null); }}><option value="">Seleccione una receta</option>{referencias.recetasVigentes.map((r) => <option key={r.id} value={r.id}>{r.codigo} · {r.producto} · v{r.version}</option>)}</Seleccion>
      <SelectorEntidad etiqueta="Presentación del producto terminado *" value={presentacionId ?? "base"} opcionesLocales={opcionesPresentacion(presentaciones.datos?.[productoId])} deshabilitado={!productoId || presentaciones.cargando} onChange={(id) => setPresentacionId(id === "base" ? null : Number(id))} />
      <Entrada etiqueta="Cantidad planificada *" type="number" min="0.000001" step="0.000001" required value={cantidad} onChange={(e) => setCantidad(Number(e.target.value))} />
      <Entrada etiqueta="Fecha inicio *" type="datetime-local" required value={fecha} onChange={(e) => setFecha(e.target.value)} />
    </fieldset></Tarjeta>
    {receta.cargando || presentaciones.cargando ? <EstadoCargando /> : receta.error ? <ErrorProduccion error={receta.error} reintentar={receta.recargar} /> : presentaciones.error ? <ErrorProduccion error={presentaciones.error} reintentar={presentaciones.recargar} /> : receta.datos && <><AvisoProduccion>{receta.datos.encabezado.productoTerminado} · v{receta.datos.encabezado.version}. Rendimiento: {n.format(receta.datos.encabezado.rendimiento)} {receta.datos.encabezado.abreviaturaRendimiento}.{medida && <p>{n.format(cantidad)} {medida.unidad} = {n.format(cantidad * medida.factor)} {receta.datos.abreviaturaUnidadBase} · Escala × {n.format(escala)}</p>}</AvisoProduccion><IngredientesReceta detalles={receta.datos.detalles} escala={escala} /></>}
    {validacion && <AvisoProduccion error>{validacion}</AvisoProduccion>}{operacion.error && <AvisoProduccion error>{operacion.error.message}</AvisoProduccion>}
    <PieFormularioProduccion volver={o ? `/produccion/${o.id}` : "/produccion"} procesando={operacion.procesando} texto="Guardar orden" disabled={receta.cargando || presentaciones.cargando || !!receta.error || !!presentaciones.error} />
  </form>;
}
