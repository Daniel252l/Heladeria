import { useEffect, useMemo, useState, type FormEvent } from "react";
import { ArrowLeft, CalendarDays, Coins, PackagePlus, Plus, Save, Trash2, Truck } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { Boton, Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoError } from "../../components/ui/Estados";
import { SeccionFormulario } from "../../components/ui/SeccionFormulario";
import { SelectorEntidad, type OpcionEntidad } from "../../components/ui/SelectorEntidad";
import type { CompraConsulta, GuardarCompra, PresentacionCompra, ProductoPresentacionesCompra, ReferenciasCompras } from "../../types/compras";
import { formatoNumero } from "../../utils/formato";
import { fechaHoraLocal } from "../../utils/fechas";
import { formatoCostoBase, formatoMoneda } from "./utilidadesCompras";

interface LineaFormulario {
  clave: string;
  productoId: string;
  producto: string;
  presentacionId: string;
  presentacion: string;
  cantidad: string;
  precioUnitario: string;
  porcentajeDescuento: string;
  factor: number;
  unidadBase: string;
  unidadCompra: string;
  presentaciones: PresentacionCompra[];
  cargandoPresentaciones: boolean;
  errorPresentaciones: string | null;
}

const PRESENTACION_BASE = "base";
const nuevaLinea = (): LineaFormulario => ({ clave: crypto.randomUUID(), productoId: "", producto: "", presentacionId: "", presentacion: "", cantidad: "", precioUnitario: "", porcentajeDescuento: "0", factor: 0, unidadBase: "", unidadCompra: "", presentaciones: [], cargandoPresentaciones: false, errorPresentaciones: null });
const fechaLocal = () => fechaHoraLocal().slice(0, 10);

export function PaginaFormularioCompra() {
  const { id } = useParams();
  const editando = Boolean(id);
  const navegar = useNavigate();
  const { sucursalActiva } = useAutenticacion();
  const [referencias, setReferencias] = useState<ReferenciasCompras | null>(null);
  const [proveedorId, setProveedorId] = useState("");
  const [proveedor, setProveedor] = useState("");
  const [monedaId, setMonedaId] = useState("");
  const [fechaEmision, setFechaEmision] = useState(fechaLocal());
  const [fechaEntrega, setFechaEntrega] = useState("");
  const [tipoCambio, setTipoCambio] = useState("1");
  const [esCredito, setEsCredito] = useState<boolean | null>(false);
  const [edicionBloqueada, setEdicionBloqueada] = useState<string | null>(null);
  const [lineas, setLineas] = useState<LineaFormulario[]>([nuevaLinea()]);
  const [cargando, setCargando] = useState(true);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [noEncontrado, setNoEncontrado] = useState(false);

  useEffect(() => {
    let vigente = true;
    const cargar = async () => {
      setCargando(true);
      setEdicionBloqueada(null);
      try {
        const [refs, orden] = await Promise.all([
          solicitar<ReferenciasCompras>("/api/compras/referencias"),
          id ? solicitar<CompraConsulta>(`/api/compras/${id}`) : Promise.resolve(null),
        ]);
        if (!vigente) return;
        setReferencias(refs);
        if (!orden) {
          const base = refs.monedas.find((moneda) => moneda.esMonedaBase) ?? refs.monedas[0];
          setMonedaId(base ? String(base.id) : "");
          setTipoCambio("1");
          setEsCredito(false);
          return;
        }
        if (orden.encabezado.estado.toUpperCase() !== "BORRADOR"
          || !refs.estados.find((estado) => estado.id === orden.encabezado.estadoId)?.permiteEdicion
          || orden.recepciones.length > 0) {
          setEdicionBloqueada("Solo una orden en estado BORRADOR editable y sin recepciones puede modificarse.");
          return;
        }
        // NULL histórico es modalidad desconocida; no convertirlo a contado al abrir o guardar.
        setEsCredito(orden.encabezado.esCredito ?? null);
        setProveedorId(String(orden.encabezado.proveedorId)); setProveedor(orden.encabezado.proveedor);
        setMonedaId(String(orden.encabezado.monedaId)); setTipoCambio(String(orden.encabezado.tipoCambio));
        setFechaEmision(orden.encabezado.fechaEmision.slice(0, 10)); setFechaEntrega(orden.fechaEntregaEsperada?.slice(0, 10) ?? "");
        const preparadas = await Promise.all(orden.detalles.map(async (detalle): Promise<LineaFormulario> => {
          const bruto = detalle.cantidad * detalle.precioUnitario;
          const comun = { clave: String(detalle.id), productoId: String(detalle.productoId), producto: detalle.producto, cantidad: String(detalle.cantidad), precioUnitario: String(detalle.precioUnitario), porcentajeDescuento: bruto > 0 ? String((detalle.descuento / bruto) * 100) : "0", cargandoPresentaciones: false };
          try {
            const catalogo = await solicitar<ProductoPresentacionesCompra>(`/api/compras/productos/${detalle.productoId}/presentaciones`);
            const elegida = detalle.productoPresentacionId == null ? null : catalogo.presentaciones.find((item) => item.productoPresentacionId === detalle.productoPresentacionId);
            const invalida = detalle.productoPresentacionId != null && !elegida;
            return { ...comun, presentacionId: detalle.productoPresentacionId == null ? PRESENTACION_BASE : String(detalle.productoPresentacionId), presentacion: elegida?.presentacionNombre ?? (detalle.productoPresentacionId == null ? "Unidad base" : detalle.presentacion ?? "Presentación no disponible"), factor: invalida ? 0 : elegida?.factorConversion ?? 1, unidadBase: catalogo.unidadBaseAbreviatura || catalogo.unidadBaseCodigo, unidadCompra: elegida?.unidadAbreviatura || elegida?.unidadCodigo || catalogo.unidadBaseAbreviatura || catalogo.unidadBaseCodigo, presentaciones: catalogo.presentaciones, errorPresentaciones: invalida ? "La presentación guardada ya no está disponible para compra. Seleccione otra." : null };
          } catch (excepcion) {
            return { ...comun, presentacionId: detalle.productoPresentacionId == null ? PRESENTACION_BASE : String(detalle.productoPresentacionId), presentacion: detalle.presentacion ?? "Unidad base", factor: 0, unidadBase: detalle.unidadBase, unidadCompra: "", presentaciones: [], errorPresentaciones: excepcion instanceof Error ? excepcion.message : "No fue posible cargar las presentaciones de compra." };
          }
        }));
        if (vigente) setLineas(preparadas);
      } catch (excepcion) {
        if (!vigente) return;
        if (excepcion instanceof ErrorApi && excepcion.estado === 404) setNoEncontrado(true);
        else setError(excepcion instanceof Error ? excepcion.message : "No fue posible preparar la orden.");
      } finally { if (vigente) setCargando(false); }
    };
    void cargar();
    return () => { vigente = false; };
  }, [id]);

  const moneda = referencias?.monedas.find((item) => String(item.id) === monedaId);
  const subtotal = useMemo(() => lineas.reduce((total, linea) => {
    const bruto = Number(linea.cantidad) * Number(linea.precioUnitario);
    return total + bruto * (1 - Number(linea.porcentajeDescuento || 0) / 100);
  }, 0), [lineas]);
  const opcionesMoneda: OpcionEntidad[] = useMemo(() => referencias?.monedas.map((item) => ({ id: item.id, codigo: item.codigoISO, nombre: item.nombre })) ?? [], [referencias]);

  const actualizarLinea = (clave: string, cambio: Partial<LineaFormulario>) => setLineas((actuales) => actuales.map((linea) => linea.clave === clave ? { ...linea, ...cambio } : linea));
  const elegirProducto = async (linea: LineaFormulario, valor: string | null) => {
    if (!valor) { actualizarLinea(linea.clave, { productoId: "", producto: "", presentacionId: "", presentacion: "", presentaciones: [], factor: 0, unidadBase: "", unidadCompra: "", cargandoPresentaciones: false, errorPresentaciones: null }); return; }
    actualizarLinea(linea.clave, { productoId: valor, producto: "", presentacionId: "", presentacion: "", presentaciones: [], factor: 0, unidadBase: "", unidadCompra: "", cargandoPresentaciones: true, errorPresentaciones: null });
    try {
      const catalogo = await solicitar<ProductoPresentacionesCompra>(`/api/compras/productos/${valor}/presentaciones`);
      setLineas((actuales) => actuales.map((actual) => actual.clave === linea.clave && actual.productoId === valor ? { ...actual, producto: catalogo.producto, presentacionId: PRESENTACION_BASE, presentacion: "Unidad base", presentaciones: catalogo.presentaciones, factor: 1, unidadBase: catalogo.unidadBaseAbreviatura || catalogo.unidadBaseCodigo, unidadCompra: catalogo.unidadBaseAbreviatura || catalogo.unidadBaseCodigo, cargandoPresentaciones: false, errorPresentaciones: null } : actual));
    } catch (excepcion) {
      setLineas((actuales) => actuales.map((actual) => actual.clave === linea.clave && actual.productoId === valor ? { ...actual, cargandoPresentaciones: false, factor: 0, errorPresentaciones: excepcion instanceof Error ? excepcion.message : "No fue posible cargar las presentaciones de compra." } : actual));
    }
  };
  const elegirPresentacion = (linea: LineaFormulario, valor: string | null) => {
    if (valor === PRESENTACION_BASE) {
      actualizarLinea(linea.clave, { presentacionId: PRESENTACION_BASE, presentacion: "Unidad base", factor: 1, unidadCompra: linea.unidadBase });
      return;
    }
    const presentacion = linea.presentaciones.find((item) => String(item.productoPresentacionId) === valor);
    actualizarLinea(linea.clave, { presentacionId: valor ?? "", presentacion: presentacion?.presentacionNombre ?? "", factor: presentacion?.factorConversion ?? 0, unidadCompra: presentacion?.unidadAbreviatura || presentacion?.unidadCodigo || "" });
  };
  const guardar = async (evento: FormEvent) => {
    evento.preventDefault(); setError(null);
    if (guardando || edicionBloqueada) return;
    if (!sucursalActiva?.id || !proveedorId || !monedaId || !fechaEmision) { setError("Complete los datos generales obligatorios."); return; }
    if (!lineas.length || lineas.some((linea) => !linea.productoId || !linea.presentacionId || linea.cargandoPresentaciones || linea.errorPresentaciones || linea.factor <= 0 || Number(linea.cantidad) <= 0 || Number(linea.precioUnitario) < 0 || Number(linea.porcentajeDescuento) < 0 || Number(linea.porcentajeDescuento) > 100)) { setError("Revise los productos, presentaciones, cantidades, precios y descuentos de todas las líneas."); return; }
    const repetidas = new Set<string>();
    for (const linea of lineas) { const llave = `${linea.productoId}:${linea.presentacionId || "base"}`; if (repetidas.has(llave)) { setError("No puede repetir el mismo producto y presentación en la orden."); return; } repetidas.add(llave); }
    const cuerpo: GuardarCompra = { sucursalId: sucursalActiva.id, proveedorId: Number(proveedorId), fechaEmision, fechaEntregaEsperada: fechaEntrega || null, monedaId: Number(monedaId), tipoCambio: Number(tipoCambio), esCredito, detalles: lineas.map((linea) => ({ productoId: Number(linea.productoId), productoPresentacionId: linea.presentacionId === PRESENTACION_BASE ? null : Number(linea.presentacionId), cantidad: Number(linea.cantidad), precioUnitario: Number(linea.precioUnitario), porcentajeDescuento: Number(linea.porcentajeDescuento) })) };
    if (cuerpo.tipoCambio <= 0) { setError("El tipo de cambio debe ser mayor que cero."); return; }
    setGuardando(true);
    try {
      const resultado = await solicitar<CompraConsulta>(id ? `/api/compras/${id}` : "/api/compras", { method: id ? "PUT" : "POST", body: JSON.stringify(cuerpo) });
      navegar(`/compras/${resultado.encabezado.id}`);
    } catch (excepcion) { setError(excepcion instanceof Error ? excepcion.message : "No fue posible guardar la orden."); }
    finally { setGuardando(false); }
  };

  if (cargando) return <EstadoCargando mensaje="Preparando orden de compra…" />;
  if (noEncontrado) return <EstadoError mensaje="No se encontró el recurso solicitado." />;
  if (edicionBloqueada) return <div className="space-y-4"><EstadoError mensaje={edicionBloqueada} /><Boton variante="secundario" onClick={() => navegar(`/compras/${id}`)}>Volver a la orden</Boton></div>;
  return <div className="space-y-6">
    <EncabezadoPagina titulo={editando ? "Editar orden de compra" : "Nueva orden de compra"} descripcion={editando ? "Actualiza los datos de la orden mientras permanezca en borrador." : "Registra la solicitud al proveedor con cantidades y equivalencias reales."} icono={<PackagePlus className="h-7 w-7" />} acciones={<Boton variante="secundario" onClick={() => navegar(id ? `/compras/${id}` : "/compras")}><ArrowLeft className="h-4 w-4" /> Volver</Boton>} />
    <form className="space-y-5" onSubmit={guardar}>
      <Tarjeta className="space-y-4 p-5 sm:p-6">
        <SeccionFormulario titulo="Datos generales" descripcion="Sucursal y fechas de la orden." icono={<CalendarDays className="h-4 w-4" />}>
          <SelectorEntidad etiqueta="Sucursal *" opcionesLocales={sucursalActiva ? [{ id: sucursalActiva.id, nombre: sucursalActiva.nombre }] : []} value={sucursalActiva?.id ?? ""} etiquetaActual={sucursalActiva?.nombre} onChange={() => undefined} deshabilitado />
          <Entrada etiqueta="Fecha de emisión *" type="date" value={fechaEmision} onChange={(e) => setFechaEmision(e.target.value)} />
          <Entrada etiqueta="Fecha de entrega esperada" type="date" value={fechaEntrega} min={fechaEmision} onChange={(e) => setFechaEntrega(e.target.value)} />
        </SeccionFormulario>
        <SeccionFormulario titulo="Proveedor" descripcion="Proveedor al que se emitirá la orden." icono={<Truck className="h-4 w-4" />}>
          <div className="sm:col-span-2"><SelectorEntidad etiqueta="Proveedor *" ruta="/api/compras/proveedores" value={proveedorId} etiquetaActual={proveedor} onChange={(valor) => setProveedorId(valor ?? "")} /></div>
        </SeccionFormulario>
        <SeccionFormulario titulo="Moneda y modalidad" descripcion="Moneda, conversión y modalidad aplicables a la compra." icono={<Coins className="h-4 w-4" />}>
          <SelectorEntidad etiqueta="Moneda *" opcionesLocales={opcionesMoneda} value={monedaId} etiquetaActual={moneda ? `${moneda.codigoISO} · ${moneda.nombre}` : undefined} onChange={(valor) => { setMonedaId(valor ?? ""); const elegida = referencias?.monedas.find((item) => String(item.id) === valor); if (elegida?.esMonedaBase) setTipoCambio("1"); }} />
          <Entrada etiqueta="Tipo de cambio *" type="number" min="0.00000001" step="0.00000001" value={tipoCambio} disabled={moneda?.esMonedaBase} onChange={(e) => setTipoCambio(e.target.value)} ayuda={moneda?.esMonedaBase ? "La moneda base utiliza tipo de cambio 1." : undefined} />
          <Seleccion etiqueta="Modalidad" aria-label="Modalidad" name="esCredito" value={esCredito === null ? "" : esCredito ? "CREDITO" : "CONTADO"} disabled={guardando} onChange={(e) => setEsCredito(e.target.value === "CREDITO")}
            ayuda={esCredito === null ? "Modalidad histórica sin definir. Se conserva si no elige Contado o Crédito." : esCredito ? "La factura y su vencimiento se registran posteriormente en Cuentas por pagar." : undefined}>
            {esCredito === null && <option value="" disabled>Sin definir</option>}
            <option value="CONTADO">Contado</option><option value="CREDITO">Crédito</option>
          </Seleccion>
        </SeccionFormulario>
      </Tarjeta>
      <Tarjeta className="overflow-hidden"><header className="flex items-center justify-between gap-3 border-b border-slate-200 px-5 py-4"><div><h2 className="font-bold text-slate-950">Productos / detalles</h2><p className="text-sm text-slate-500">Agrega hasta 100 líneas comprables.</p></div><Boton type="button" variante="secundario" onClick={() => setLineas((x) => [...x, nuevaLinea()])} disabled={lineas.length >= 100}><Plus className="h-4 w-4" /> Agregar línea</Boton></header><div className="space-y-4 p-5">{lineas.map((linea, indice) => {
        const opcionesPresentacion = linea.presentaciones.map((item) => ({ id: item.productoPresentacionId, nombre: `${item.presentacionNombre || item.presentacionCodigo} · ${item.unidadCodigo} · × ${formatoNumero.format(item.factorConversion)}` }));
        const costoBase = linea.factor > 0 && linea.precioUnitario !== "" ? Number(linea.precioUnitario) / linea.factor : null;
        return <section key={linea.clave} className="rounded-xl border border-slate-200 p-4"><div className="mb-4 flex items-center justify-between"><h3 className="font-semibold text-slate-900">Línea {indice + 1}</h3><button type="button" className="rounded-lg p-2 text-rose-600 hover:bg-rose-50 disabled:opacity-40" disabled={lineas.length === 1} onClick={() => setLineas((x) => x.filter((item) => item.clave !== linea.clave))} aria-label={`Eliminar línea ${indice + 1}`} title="Eliminar línea"><Trash2 className="h-4 w-4" /></button></div><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3"><SelectorEntidad etiqueta="Producto *" ruta="/api/productos" value={linea.productoId} etiquetaActual={linea.producto} onChange={(valor) => void elegirProducto(linea, valor)} /><SelectorEntidad etiqueta="Presentación *" opcionesLocales={linea.unidadBase ? [{ id: PRESENTACION_BASE, nombre: `Unidad base · ${linea.unidadBase} · × 1` }, ...opcionesPresentacion] : []} value={linea.presentacionId} etiquetaActual={linea.presentacion} onChange={(valor) => elegirPresentacion(linea, valor)} deshabilitado={!linea.productoId || linea.cargandoPresentaciones || Boolean(linea.errorPresentaciones)} cargandoExterno={linea.cargandoPresentaciones} error={linea.errorPresentaciones ?? undefined} placeholder={linea.cargandoPresentaciones ? "Cargando presentaciones…" : linea.errorPresentaciones ? "Presentaciones no disponibles" : "Seleccione una presentación"} /><Entrada etiqueta="Cantidad *" type="number" min="0.000001" step="0.000001" value={linea.cantidad} onChange={(e) => actualizarLinea(linea.clave, { cantidad: e.target.value })} /><Entrada etiqueta="Precio unitario *" type="number" min="0" step="0.01" value={linea.precioUnitario} onChange={(e) => actualizarLinea(linea.clave, { precioUnitario: e.target.value })} /><Entrada etiqueta="Descuento (%) *" type="number" min="0" max="100" step="0.01" value={linea.porcentajeDescuento} onChange={(e) => actualizarLinea(linea.clave, { porcentajeDescuento: e.target.value })} /><div className="rounded-lg bg-teal-50 p-3 text-sm text-teal-900"><span className="block text-xs font-semibold uppercase tracking-wide text-teal-700">Equivalencia estimada</span>{linea.cantidad && linea.unidadBase && linea.unidadCompra && linea.factor > 0 ? <><strong>{formatoNumero.format(Number(linea.cantidad))} {linea.unidadCompra}</strong><span className="mx-2">=</span><strong>{formatoNumero.format(Number(linea.cantidad) * linea.factor)} {linea.unidadBase}</strong><span className="mt-1 block text-xs">Factor: {formatoNumero.format(linea.factor)}</span>{costoBase != null && <span className="mt-1 block text-xs text-slate-600">Precio: {formatoMoneda(Number(linea.precioUnitario), moneda)} / {linea.unidadCompra} · Costo base est.: {formatoCostoBase(costoBase, moneda)} / {linea.unidadBase}</span>}</> : linea.cargandoPresentaciones ? "Cargando equivalencias…" : linea.errorPresentaciones ? "No hay una equivalencia válida disponible." : "Selecciona un producto, presentación y cantidad."}</div></div></section>;
      })}</div></Tarjeta>
      <Tarjeta className="p-5"><h2 className="font-bold text-slate-950">Resumen</h2><dl className="mt-4 ml-auto grid max-w-xl gap-3 text-sm"><div className="flex justify-between"><dt>Subtotal antes de impuestos</dt><dd className="font-semibold">{formatoMoneda(subtotal, moneda)}</dd></div><div className="flex justify-between text-slate-500"><dt>Impuestos</dt><dd>Calculados por el servidor al guardar</dd></div><div className="flex justify-between border-t border-slate-200 pt-3 text-base"><dt className="font-bold">Total estimado sin impuestos</dt><dd className="font-bold text-teal-800">{formatoMoneda(subtotal, moneda)}</dd></div>{moneda && <p className="text-right text-xs text-slate-500">{moneda.codigoISO} / {moneda.nombre}</p>}</dl></Tarjeta>
      {error && <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">{error}</div>}
      <div className="flex justify-end gap-3"><Boton type="button" variante="secundario" onClick={() => navegar(id ? `/compras/${id}` : "/compras")}>Cancelar</Boton><Boton type="submit" cargando={guardando}><Save className="h-4 w-4" /> Guardar borrador</Boton></div>
    </form>
  </div>;
}
