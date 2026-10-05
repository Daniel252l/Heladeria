import { useRef, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Plus, Trash2 } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import { Boton, Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { SeccionFormulario } from "../../components/ui/SeccionFormulario";
import { EstadoCargando } from "../../components/ui/Estados";
import type { RecetaConsulta, ReferenciasRecetas } from "../../types/produccion";
import { AvisoProduccion, ErrorProduccion, PieFormularioProduccion } from "./ComponentesProduccion";
import { useOperacionProduccion, usePresentacionesProduccion, useRecursoProduccion } from "./useDatosProduccion";
import { cantidadValida, fechaLocal, medidaProduccion, numeroProduccion as n, opcionesPresentacion } from "./utilidadesProduccion";

interface Linea { clave: number; productoInsumoId: number; productoPresentacionId: number | null; cantidad: number; observacion: string }
export function PaginaFormularioReceta() {
  const { id } = useParams();
  const receta = useRecursoProduccion<RecetaConsulta>(id ? `/api/recetas/${id}` : null);
  const refs = useRecursoProduccion<ReferenciasRecetas>("/api/recetas/referencias");
  if (receta.cargando || refs.cargando) return <EstadoCargando />;
  if (receta.error) return <ErrorProduccion error={receta.error} reintentar={receta.recargar} />;
  if (refs.error) return <ErrorProduccion error={refs.error} reintentar={refs.recargar} />;
  if (receta.datos && !receta.datos.encabezado.esVigente) return <AvisoProduccion>Esta versión es histórica y se conserva en modo consulta. <Link to={`/recetas/${id}`}>Ver receta</Link></AvisoProduccion>;
  return refs.datos ? <FormularioReceta key={`${id ?? "nueva"}:${receta.datos?.encabezado.versionFila}`} inicial={receta.datos} referencias={refs.datos} recargar={receta.recargar} /> : null;
}
function FormularioReceta({ inicial, referencias, recargar }: { inicial: RecetaConsulta | null; referencias: ReferenciasRecetas; recargar: () => void }) {
  const r = inicial?.encabezado;
  const navegar = useNavigate();
  const [producto, setProducto] = useState(r?.productoTerminadoId ?? 0);
  const [codigo, setCodigo] = useState(r?.codigo ?? "");
  const [nombre, setNombre] = useState(r?.nombre ?? "");
  const [fecha, setFecha] = useState(r?.vigenteDesde ?? fechaLocal().slice(0, 10));
  const [estadoId, setEstadoId] = useState(r?.estadoId ?? referencias.estados.find((e) => e.nombre.toLowerCase() === "activo")?.id ?? 0);
  const [rendimiento, setRendimiento] = useState(r?.rendimiento ?? 1);
  const [presentacion, setPresentacion] = useState<number | null>(r?.productoPresentacionRendimientoId ?? null);
  const [lineas, setLineas] = useState<Linea[]>(inicial?.detalles.map((d, i) => ({ clave: i, productoInsumoId: d.productoInsumoId, productoPresentacionId: d.productoPresentacionId, cantidad: d.cantidad, observacion: d.observacion ?? "" })) ?? [{ clave: 0, productoInsumoId: 0, productoPresentacionId: null, cantidad: 1, observacion: "" }]);
  const siguiente = useRef(lineas.length);
  const [validacion, setValidacion] = useState("");
  const operacion = useOperacionProduccion();
  const presentaciones = usePresentacionesProduccion([producto, ...lineas.map((l) => l.productoInsumoId)]);
  const medida = medidaProduccion(presentaciones.datos?.[producto], presentacion);
  const cambiarLinea = (clave: number, valores: Partial<Linea>) => setLineas((actuales) => actuales.map((l) => l.clave === clave ? { ...l, ...valores } : l));
  const guardar = async (e: FormEvent) => {
    e.preventDefault(); setValidacion("");
    if (!producto || !codigo.trim() || !nombre.trim() || !fecha || !estadoId || !cantidadValida(rendimiento)) { setValidacion("Complete los campos obligatorios y un rendimiento mayor que cero."); return; }
    if (!lineas.length) { setValidacion("La receta debe contener al menos un ingrediente."); return; }
    if (lineas.some((l) => l.productoInsumoId === producto)) { setValidacion("El producto terminado no puede ser un ingrediente."); return; }
    if (new Set(lineas.map((l) => l.productoInsumoId)).size !== lineas.length) { setValidacion("No se puede repetir un ingrediente dentro de la receta."); return; }
    if (!medida || lineas.some((l) => !cantidadValida(l.cantidad) || !medidaProduccion(presentaciones.datos?.[l.productoInsumoId], l.productoPresentacionId))) { setValidacion("Revise cantidades, unidades base y presentaciones de los ingredientes."); return; }
    const resultado = await operacion.ejecutar(() => solicitar<RecetaConsulta>(r ? `/api/recetas/${r.id}` : "/api/recetas", { method: r ? "PUT" : "POST", body: JSON.stringify({ productoTerminadoId: producto, codigo: codigo.trim(), nombre: nombre.trim(), rendimiento, productoPresentacionRendimientoId: presentacion, vigenteDesde: fecha, estadoId, ...(r ? { versionFila: r.versionFila } : {}), detalles: lineas.map(({ productoInsumoId, productoPresentacionId, cantidad, observacion }) => ({ productoInsumoId, productoPresentacionId, cantidad, observacion: observacion.trim() || null })) }) }));
    if (resultado) navegar(`/recetas/${resultado.encabezado.id}`);
  };
  return <form onSubmit={(e) => void guardar(e)} className="space-y-6">
    <EncabezadoPagina titulo={r ? "Editar receta" : "Nueva receta"} descripcion="Defina el rendimiento y las cantidades de cada ingrediente." />
    {inicial?.fueUtilizada && <AvisoProduccion>Esta receta ya fue utilizada en producción. Los cambios crearán una nueva versión y conservarán la versión histórica.</AvisoProduccion>}
    <fieldset disabled={operacion.procesando} className="min-w-0 space-y-6">
      <Tarjeta className="space-y-5 p-4 sm:p-6"><SeccionFormulario titulo="Información general">
        <SelectorEntidad etiqueta="Producto terminado *" opcionesLocales={referencias.productos} value={producto || ""} deshabilitado={!!r} onChange={(id) => { setProducto(Number(id)); setPresentacion(null); }} />
        <Entrada etiqueta="Código *" required maxLength={30} value={codigo} disabled={inicial?.fueUtilizada} onChange={(e) => setCodigo(e.target.value)} />
        <Entrada etiqueta="Nombre *" required maxLength={150} value={nombre} onChange={(e) => setNombre(e.target.value)} />
        <Entrada etiqueta="Vigente desde *" required type="date" value={fecha} onChange={(e) => setFecha(e.target.value)} />
        <Seleccion etiqueta="Estado *" required value={estadoId || ""} onChange={(e) => setEstadoId(Number(e.target.value))}><option value="">Seleccione</option>{referencias.estados.map((e) => <option key={e.id} value={e.id}>{e.nombre}</option>)}</Seleccion>
      </SeccionFormulario><SeccionFormulario titulo="Rendimiento">
        <Entrada etiqueta="Cantidad de rendimiento *" required type="number" min="0.000001" step="0.000001" value={rendimiento} onChange={(e) => setRendimiento(Number(e.target.value))} />
        <SelectorEntidad etiqueta="Presentación de rendimiento *" opcionesLocales={opcionesPresentacion(presentaciones.datos?.[producto])} value={presentacion ?? "base"} deshabilitado={!producto || presentaciones.cargando} onChange={(id) => setPresentacion(id === "base" ? null : Number(id))} />
        <p className="rounded-lg bg-teal-50 p-3 text-sm text-teal-800 sm:col-span-2">{medida ? `${n.format(rendimiento)} ${medida.unidad} = ${n.format(rendimiento * medida.factor)} ${presentaciones.datos?.[producto]?.unidadBaseAbreviatura} · Factor × ${n.format(medida.factor)}` : "Seleccione un producto con unidad base y una presentación válida."}</p>
      </SeccionFormulario></Tarjeta>
      <Tarjeta className="p-4 sm:p-6"><div className="mb-4 flex flex-wrap justify-between gap-3"><h2 className="text-lg font-bold">Ingredientes</h2><Boton type="button" variante="secundario" disabled={lineas.length >= 100} onClick={() => setLineas([...lineas, { clave: siguiente.current++, productoInsumoId: 0, productoPresentacionId: null, cantidad: 1, observacion: "" }])}><Plus className="h-4 w-4" /> Agregar ingrediente</Boton></div>
        <div className="space-y-4">{lineas.map((l, i) => { const datos = presentaciones.datos?.[l.productoInsumoId]; const m = medidaProduccion(datos, l.productoPresentacionId); return <section key={l.clave} aria-label={`Ingrediente ${i + 1}`} className="rounded-xl border border-slate-200 p-4"><div className="grid items-end gap-4 md:grid-cols-3">
          <SelectorEntidad etiqueta={`Ingrediente ${i + 1} *`} opcionesLocales={referencias.productos} value={l.productoInsumoId || ""} onChange={(id) => cambiarLinea(l.clave, { productoInsumoId: Number(id), productoPresentacionId: null })} />
          <SelectorEntidad etiqueta={`Presentación ingrediente ${i + 1}`} opcionesLocales={opcionesPresentacion(datos)} value={l.productoPresentacionId ?? "base"} deshabilitado={!datos} onChange={(id) => cambiarLinea(l.clave, { productoPresentacionId: id === "base" ? null : Number(id) })} />
          <Entrada etiqueta={`Cantidad ingrediente ${i + 1} *`} type="number" required min="0.000001" step="0.000001" value={l.cantidad} onChange={(e) => cambiarLinea(l.clave, { cantidad: Number(e.target.value) })} />
          <Entrada etiqueta={`Observación ingrediente ${i + 1}`} maxLength={300} value={l.observacion} onChange={(e) => cambiarLinea(l.clave, { observacion: e.target.value })} />
          <p className="py-3 text-sm text-teal-800">{m ? `${n.format(l.cantidad)} ${m.unidad} × ${n.format(m.factor)} = ${n.format(l.cantidad * m.factor)} ${datos?.unidadBaseAbreviatura}` : "Seleccione ingrediente y presentación."}</p>
          <Boton type="button" variante="sutil" onClick={() => setLineas(lineas.filter((item) => item.clave !== l.clave))} aria-label={`Eliminar ingrediente ${i + 1}`}><Trash2 className="h-4 w-4" /> Eliminar</Boton>
        </div></section>; })}</div>
      </Tarjeta>
    </fieldset>
    {presentaciones.cargando && <EstadoCargando />}{presentaciones.error && <ErrorProduccion error={presentaciones.error} reintentar={presentaciones.recargar} />}
    {validacion && <AvisoProduccion error>{validacion}</AvisoProduccion>}{operacion.error && <AvisoProduccion error>{operacion.error.message}{operacion.conflicto && <Boton type="button" variante="sutil" onClick={recargar}>Recargar receta</Boton>}</AvisoProduccion>}
    <PieFormularioProduccion volver={r ? `/recetas/${r.id}` : "/recetas"} procesando={operacion.procesando} disabled={presentaciones.cargando || !!presentaciones.error} texto={inicial?.fueUtilizada ? "Crear nueva versión" : "Guardar receta"} />
  </form>;
}
