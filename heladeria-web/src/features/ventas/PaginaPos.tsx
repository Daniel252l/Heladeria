import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { CakeSlice, Coffee, IceCreamBowl, Minus, Monitor, Package, Plus, Search, ShoppingCart, Sparkles, Trash2 } from "lucide-react";
import { CierrePos } from "./CierrePos";
import type { SesionCajaResumen } from "../../types/caja";
import { ventasApi } from "../../api/ventas";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { Boton, Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoVacio } from "../../components/ui/Estados";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { Paginacion } from "../../components/ui/Paginacion";
import { fechaHoraLocal } from "../../utils/fechas";
import type { Paginado } from "../../types/api";
import type { LineaCarrito, PresentacionPos, ProductoPos, ReferenciasVentas, VentaConsulta } from "../../types/ventas";
import { AvisoVenta, enlaceVenta, ErrorVenta, TotalesVenta } from "./ComponentesVentas";
import { DetalleVenta } from "./PaginaDetalleVenta";
import { useCargaVentas, useOperacionVenta, useRecursoVentas } from "./useDatosVentas";
import { agregarLinea, cantidadValida, claveLinea, clavePresentacion, descuentoValido, dineroVenta, esConcurrencia, nombrePresentacion, numeroVenta as n, precioUtilizable, previewCarrito, previewLinea } from "./utilidadesVentas";

export function PaginaPos({ nueva = false }: { nueva?: boolean }) {
  const { id } = useParams();
  const { usuario, sucursalActiva } = useAutenticacion();
  return <CargarPos key={`${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}:${id}:${nueva}`} id={id} nueva={nueva} />;
}
function CargarPos({ id, nueva }: { id?: string; nueva: boolean }) {
  const refs = useRecursoVentas<ReferenciasVentas>("/api/ventas/referencias");
  const original = useRecursoVentas<VentaConsulta>(id ? `/api/ventas/${id}` : null);
  const { sucursalActiva } = useAutenticacion();
  if (refs.cargando || original.cargando) return <EstadoCargando mensaje="Preparando punto de venta…" />;
  if (refs.error) return <ErrorVenta error={refs.error} recargar={refs.recargar} />;
  if (original.error) return <ErrorVenta error={original.error} recargar={original.recargar} />;
  if (!refs.datos) return null;
  if (refs.datos.sucursalId !== sucursalActiva?.id || refs.datos.empresaId !== sucursalActiva?.empresaId) return <AvisoVenta error>Las referencias no corresponden a la sucursal activa. Vuelva a cargar la página.</AvisoVenta>;
  if (original.datos && original.datos.encabezado.estado !== "BORRADOR") return <div className="space-y-4"><AvisoVenta>Esta venta no admite edición. Solo pueden editarse borradores.</AvisoVenta><Link className={enlaceVenta} to={`/ventas/${id}`}>Ver detalle</Link></div>;
  return <PuestoVenta referencias={refs.datos} inicial={original.datos} nueva={nueva} />;
}
function PuestoVenta({ referencias, inicial, nueva }: { referencias: ReferenciasVentas; inicial: VentaConsulta | null; nueva: boolean }) {
  const [venta, setVenta] = useState(inicial);
  const [verDetalle, setVerDetalle] = useState(false);
  const [revision, setRevision] = useState(0);
  if (verDetalle && venta) return <DetalleVenta key={venta.encabezado.id} inicial={venta}
    editar={(actual) => { setVenta(actual); setVerDetalle(false); }} nueva={() => { setVenta(null); setRevision((r) => r + 1); setVerDetalle(false); }} />;
  return <CargarEditor key={`${venta?.encabezado.id ?? "nueva"}:${revision}`} referencias={referencias} original={venta} nueva={nueva}
    reiniciar={() => { setVenta(null); setRevision((r) => r + 1); }} guardada={(actual) => { setVenta(actual); setVerDetalle(true); }} />;
}
function CargarEditor({ referencias, original, nueva, guardada, reiniciar }: { referencias: ReferenciasVentas; original: VentaConsulta | null; nueva: boolean; guardada: (venta: VentaConsulta) => void; reiniciar: () => void }) {
  const ids = [...new Set(original?.detalles.map((l) => l.productoId) ?? [])].sort((a, b) => a - b).join(",");
  const cargar = useCallback(async (signal: AbortSignal) => Promise.all(ids ? ids.split(",").map((id) => solicitar<ProductoPos>(`/api/ventas/productos/${id}/presentaciones`, { signal })) : []), [ids]);
  const productos = useCargaVentas(ids, cargar);
  if (productos.cargando) return <EstadoCargando />;
  if (productos.error) return <ErrorVenta error={productos.error} recargar={productos.recargar} />;
  const lineas: LineaCarrito[] = [];
  for (const d of original?.detalles ?? []) {
    const producto = productos.datos?.find((p) => p.id === d.productoId);
    const presentacion = producto?.presentaciones.find((p) => p.productoPresentacionId === d.productoPresentacionId);
    if (!producto || !presentacion) return <AvisoVenta error>Una presentación del borrador ya no está disponible. Consulte la venta y su configuración antes de editarla.</AvisoVenta>;
    lineas.push({ producto, presentacion, cantidad: String(d.cantidad), descuento: String(d.porcentajeDescuento) });
  }
  return <EditorVenta referencias={referencias} original={original} nueva={nueva} inicial={lineas} guardada={guardada} reiniciar={reiniciar} />;
}

function EditorVenta({ referencias: refs, original, nueva, inicial, guardada, reiniciar }: { referencias: ReferenciasVentas; original: VentaConsulta | null; nueva: boolean; inicial: LineaCarrito[]; guardada: (venta: VentaConsulta) => void; reiniciar: () => void }) {
  const { tienePermiso, sucursalActiva, usuario } = useAutenticacion();
  const bodegas = [...new Map(refs.almacenes.map((a) => [a.bodegaId, { id: a.bodegaId, nombre: a.bodega }])).values()];
  const bodegaInicial = original?.encabezado.bodegaId ? String(original.encabezado.bodegaId) : bodegas.length === 1 ? String(bodegas[0].id) : "";
  const ubicacionesIniciales = refs.almacenes.filter((a) => String(a.bodegaId) === bodegaInicial);
  const [bodega, setBodega] = useState(bodegaInicial);
  const [ubicacion, setUbicacion] = useState(original?.encabezado.ubicacionId ? String(original.encabezado.ubicacionId) : ubicacionesIniciales.length === 1 ? String(ubicacionesIniciales[0].ubicacionId) : "");
  const [cliente, setCliente] = useState(original?.encabezado.clienteId ? String(original.encabezado.clienteId) : "");
  const [esCredito, setEsCredito] = useState(original?.encabezado.esCredito ?? false);
  const [vencimiento, setVencimiento] = useState(original?.encabezado.fechaVencimientoCredito ?? "");
  const [lineas, setLineas] = useState(inicial);
  const persistida = useRef(original);
  const [cierre, setCierre] = useState<VentaConsulta | null>(null);
  const [errorLocal, setErrorLocal] = useState("");
  const [buscar, setBuscar] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [categoria, setCategoria] = useState("");
  const [pagina, setPagina] = useState(1);
  const operacion = useOperacionVenta();
  const administrar = tienePermiso(PERMISOS.ventasAdministrar);
  const consultar = tienePermiso(PERMISOS.ventasConsultar);
  const vender = administrar && consultar && tienePermiso(PERMISOS.ventasVender) && tienePermiso(PERMISOS.posOperar);
  const cobrar = [PERMISOS.cajaConsultar, PERMISOS.cajaOperar, PERMISOS.cajaCobrar].every(tienePermiso);
  const sesiones = useRecursoVentas<Paginado<SesionCajaResumen>>(cobrar ? "/api/caja/sesiones?abierta=true&pagina=1&tamanioPagina=20" : null);
  const turnoAbierto = sesiones.datos?.elementos.some((x) => x.esFase8 && x.estado === "ABIERTA" && x.monedaId === refs.monedaId) ?? false;
  const ubicaciones = refs.almacenes.filter((a) => String(a.bodegaId) === bodega);
  useEffect(() => {
    const timer = window.setTimeout(() => { setBusqueda(buscar.trim()); setPagina(1); }, 300);
    return () => window.clearTimeout(timer);
  }, [buscar]);
  const params = new URLSearchParams({ pagina: String(pagina), tamanioPagina: "12" });
  if (busqueda) params.set("busqueda", busqueda);
  if (categoria) params.set("categoriaId", categoria);
  if (bodega) params.set("bodegaId", bodega);
  if (ubicacion && bodega) params.set("ubicacionId", ubicacion);
  const catalogo = useRecursoVentas<Paginado<ProductoPos>>(`/api/ventas/productos?${params}`);
  const agregar = (producto: ProductoPos, presentacion: PresentacionPos) => {
    if (operacion.procesando || !precioUtilizable(presentacion)) return;
    setErrorLocal("");
    setLineas((actuales) => agregarLinea(actuales, producto, presentacion));
  };
  const actualizar = (clave: string, cambio: Partial<LineaCarrito>) => setLineas((ls) => ls.map((l) => claveLinea(l) === clave ? { ...l, ...cambio } : l));
  const guardar = async (finalizar = false) => {
    if (!administrar || operacion.procesando || esConcurrencia(operacion.error)) return;
    setErrorLocal("");
    if (!refs.almacenes.some((a) => String(a.bodegaId) === bodega && String(a.ubicacionId) === ubicacion)) { setErrorLocal("Seleccione una bodega y ubicación válidas."); return; }
    if (esCredito && !cliente) { setErrorLocal("Seleccione un cliente para la venta a crédito."); return; }
    if (esCredito && !vencimiento) { setErrorLocal("Indique la fecha de vencimiento del crédito."); return; }
    if (esCredito && vencimiento < fechaHoraLocal().slice(0, 10)) { setErrorLocal("El vencimiento no puede ser anterior a hoy en la sucursal activa."); return; }
    if (cliente && !refs.clientes.some((c) => String(c.id) === cliente)) { setErrorLocal("Seleccione un cliente disponible o venta sin cliente identificado."); return; }
    if (!lineas.length || lineas.length > 100) { setErrorLocal("La venta debe contener entre 1 y 100 líneas."); return; }
    if (lineas.some((l) => !cantidadValida(l.cantidad) || !descuentoValido(l.descuento) || !precioUtilizable(l.presentacion) || (!l.producto.permiteDescuento && Number(l.descuento) !== 0))) { setErrorLocal("Revise cantidades (hasta 6 decimales), descuentos (0–100 %, hasta 2 decimales) y precios de las líneas."); return; }
    const resultado = await operacion.ejecutar(() => ventasApi.guardar({ sucursalId: refs.sucursalId, bodegaId: Number(bodega), ubicacionId: Number(ubicacion), clienteId: cliente ? Number(cliente) : null, monedaId: refs.monedaId,
      esCredito, fechaVencimientoCredito: esCredito ? vencimiento : null,
      detalles: lineas.map((l) => ({ productoId: l.producto.id, productoPresentacionId: l.presentacion.productoPresentacionId, cantidad: Number(l.cantidad), porcentajeDescuento: Number(l.descuento) })) }, persistida.current?.encabezado));
    if (resultado) { persistida.current = resultado; if (finalizar) setCierre(resultado); else guardada(resultado); }
  };
  const total = previewCarrito(lineas);
  const datosValidos = !!bodega && !!ubicacion && lineas.length > 0 && lineas.length <= 100
    && lineas.every((l) => cantidadValida(l.cantidad) && descuentoValido(l.descuento) && precioUtilizable(l.presentacion))
    && (!esCredito || (!!cliente && !!vencimiento && vencimiento >= fechaHoraLocal().slice(0, 10)));
  if (operacion.error instanceof ErrorApi && [403, 404].includes(operacion.error.estado)) return <ErrorVenta error={operacion.error} />;
  return <div className="ventas-vista pos-puesto space-y-5">
    <EncabezadoPagina titulo={original ? `Editar ${original.encabezado.numeroVenta}` : nueva ? "Nueva venta" : "Punto de venta"} descripcion={`${sucursalActiva?.nombre} · ${usuario?.nombreUsuario ?? ""} · ${refs.moneda}`} icono={<Monitor />} acciones={<div className="flex flex-wrap gap-2">{consultar && <Link className={enlaceVenta} to="/ventas">Historial de ventas</Link>}<a className={`${enlaceVenta} xl:hidden`} href="#carrito-pos"><ShoppingCart className="h-4 w-4" /> Ver carrito ({lineas.length})</a></div>} />
    <div className="pos-layout">
      <section aria-label="Catálogo POS" className="min-w-0 space-y-4">
        <Tarjeta className="space-y-4 p-4 sm:p-5">
          <div className="pos-buscador"><Search className="h-5 w-5 text-teal-700" aria-hidden="true" /><Entrada etiqueta="Buscar productos" placeholder="Buscar producto" value={buscar} onChange={(e) => setBuscar(e.target.value)} maxLength={150} type="search" /></div>
          <div className="pos-categorias" role="group" aria-label="Categorías">
            {[{ id: "", nombre: "Todos" }, ...refs.categorias].map((c) => <button key={c.id} type="button" aria-pressed={categoria === String(c.id)} onClick={() => { setCategoria(String(c.id)); setPagina(1); }}>{c.nombre}</button>)}
          </div>
          <details className="pos-almacen"><summary>Ubicación de venta · {ubicaciones.find((u) => String(u.ubicacionId) === ubicacion)?.ubicacion ?? "Seleccione ubicación"}</summary><div className="mt-4 grid gap-3 sm:grid-cols-2">
      <Seleccion etiqueta="Bodega" value={bodega} disabled={operacion.procesando} onChange={(e) => { const v = e.target.value; setBodega(v); const opciones = refs.almacenes.filter((a) => String(a.bodegaId) === v); setUbicacion(opciones.length === 1 ? String(opciones[0].ubicacionId) : ""); setPagina(1); }}><option value="">Seleccione bodega</option>{bodegas.map((b) => <option key={b.id} value={b.id}>{b.nombre}</option>)}</Seleccion>
      <Seleccion etiqueta="Ubicación" value={ubicacion} disabled={!bodega || operacion.procesando} onChange={(e) => { setUbicacion(e.target.value); setPagina(1); }}><option value="">Seleccione ubicación</option>{ubicaciones.map((u) => <option key={u.ubicacionId} value={u.ubicacionId}>{u.ubicacion}</option>)}</Seleccion>
          </div></details>
        </Tarjeta>
        {catalogo.cargando ? <EstadoCargando mensaje="Buscando productos…" /> : catalogo.error ? <ErrorVenta error={catalogo.error} recargar={catalogo.recargar} /> : <>
          {!catalogo.datos?.elementos.length ? <Tarjeta><EstadoVacio titulo="No se encontraron productos" descripcion="Pruebe otra búsqueda o categoría." /></Tarjeta> : <div className="pos-catalogo">{catalogo.datos.elementos.map((p) => <TarjetaProductoPos key={p.id} producto={p} agregar={agregar} ocupado={operacion.procesando} />)}</div>}
          {catalogo.datos && <Tarjeta><Paginacion {...catalogo.datos} cambiar={setPagina} entidad="producto" /></Tarjeta>}
        </>}
      </section>
      <aside id="carrito-pos" aria-label="Carrito de venta" className="pos-carrito"><Tarjeta className="pos-orden overflow-hidden">
        <div className="flex items-center justify-between gap-3 border-b border-slate-200 p-5"><h2 className="flex items-center gap-2 text-lg font-bold"><ShoppingCart className="h-5 w-5 text-teal-700" /> Orden actual</h2><span className="text-sm text-slate-500" aria-live="polite">{lineas.length} {lineas.length === 1 ? "línea" : "líneas"}</span></div>
        <div className="pos-orden-cuerpo">
        <div className="space-y-4 border-b border-slate-100 p-4">
          <fieldset><legend className="mb-2 text-sm font-semibold">Modalidad</legend><div className="pos-modalidad">
            <button type="button" aria-pressed={!esCredito} disabled={operacion.procesando} onClick={() => { setEsCredito(false); setVencimiento(""); setErrorLocal(""); }}>Contado</button>
            <button type="button" aria-pressed={esCredito} disabled={operacion.procesando} onClick={() => { setEsCredito(true); setErrorLocal(""); }}>Crédito</button>
          </div></fieldset>
      <SelectorEntidad etiqueta={esCredito ? "Cliente *" : "Cliente (opcional)"} opcionesLocales={refs.clientes} opcional={!esCredito} value={cliente} onChange={(id) => { setCliente(id ?? ""); setErrorLocal(""); }} placeholder={esCredito ? "Seleccione un cliente para crédito" : "Venta sin cliente identificado"} deshabilitado={operacion.procesando} etiquetaActual={original?.encabezado.cliente ?? undefined} />
      {esCredito && <Entrada etiqueta="Fecha de vencimiento *" name="fechaVencimientoCredito" type="date" required min={fechaHoraLocal().slice(0, 10)} value={vencimiento} onChange={(e) => { setVencimiento(e.target.value); setErrorLocal(""); }} disabled={operacion.procesando} ayuda="Obligatoria para crédito. Fecha civil de la sucursal, sin hora." />}

          {!esCredito && <p className="text-xs text-slate-500">Efectivo, tarjeta o pago mixto al finalizar, según las formas disponibles.</p>}
        </div>
        {!lineas.length ? <EstadoVacio titulo="Su carrito está vacío" descripcion="Seleccione un producto y su presentación para comenzar." /> : <ul className="divide-y divide-slate-100">{lineas.map((l) => <li key={claveLinea(l)} className="min-w-0 space-y-3 p-4" aria-label={`${l.producto.nombre} · ${l.presentacion.nombre}`}>
          <div className="flex items-start justify-between gap-2"><div className="min-w-0"><h3 className="break-words font-semibold text-slate-900">{l.producto.nombre}</h3><p className="mt-1 text-xs text-slate-500">{l.presentacion.nombre} · {l.presentacion.abreviatura} · × {n.format(l.presentacion.factorConversion)}</p></div><Boton variante="sutil" className="shrink-0 px-2" disabled={operacion.procesando} aria-label={`Eliminar ${l.producto.nombre} ${l.presentacion.nombre}`} onClick={() => setLineas((ls) => ls.filter((x) => claveLinea(x) !== claveLinea(l)))}><Trash2 className="h-4 w-4" /></Boton></div>
          <div className="flex min-w-0 items-end gap-2"><Boton variante="secundario" className="h-11 shrink-0 px-2" aria-label={`Disminuir ${l.producto.nombre} ${l.presentacion.nombre}`} disabled={operacion.procesando || Number(l.cantidad) <= 1} onClick={() => actualizar(claveLinea(l), { cantidad: String(Number((Number(l.cantidad) - 1).toFixed(6))) })}><Minus className="h-4 w-4" /></Boton>
            <div className="min-w-0 flex-1"><Entrada etiqueta={`Cantidad · ${l.presentacion.nombre}`} aria-label={`Cantidad ${l.producto.nombre} ${l.presentacion.nombre}`} type="number" min="0.000001" max="9999999999999.999999" step="0.000001" value={l.cantidad} disabled={operacion.procesando} onChange={(e) => actualizar(claveLinea(l), { cantidad: e.target.value })} error={!cantidadValida(l.cantidad) ? "Cantidad positiva, hasta 6 decimales." : undefined} /></div>
            <Boton variante="secundario" className="h-11 shrink-0 px-2" disabled={operacion.procesando} aria-label={`Incrementar ${l.producto.nombre} ${l.presentacion.nombre}`} onClick={() => actualizar(claveLinea(l), { cantidad: String(Number((Number(l.cantidad || 0) + 1).toFixed(6))) })}><Plus className="h-4 w-4" /></Boton>
          </div>
          <p className="rounded-lg bg-teal-50 px-3 py-2 text-xs font-medium text-teal-800">{n.format(Number(l.cantidad || 0))} {l.presentacion.abreviatura} = {n.format(Number(l.cantidad || 0) * l.presentacion.factorConversion)} {l.producto.abreviaturaBase}</p>
          {!precioUtilizable(l.presentacion) && <p role="alert" className="text-sm text-rose-700">Precio no configurado</p>}
          {l.producto.permiteDescuento ? <Entrada etiqueta={`Descuento % · ${l.producto.nombre} ${l.presentacion.nombre}`} type="number" min="0" max="100" step="0.01" value={l.descuento} disabled={operacion.procesando} onChange={(e) => actualizar(claveLinea(l), { descuento: e.target.value })} error={!descuentoValido(l.descuento) ? "Ingrese de 0 a 100, hasta 2 decimales." : undefined} /> : <p className="text-xs text-slate-500">Este producto no permite descuentos.</p>}
          <div className="flex flex-wrap justify-between gap-2 text-xs text-slate-500"><span>{dineroVenta(l.presentacion.precio, refs.moneda)} / {l.presentacion.abreviatura}</span><span>Subtotal: {dineroVenta(previewLinea(l).subtotal, refs.moneda)}</span></div><p className="text-right text-sm font-semibold">Total línea: {dineroVenta(previewLinea(l).total, refs.moneda)}</p>
        </li>)}</ul>}
        </div>
        <div className="pos-orden-resumen space-y-3 border-t border-slate-200 bg-slate-50/70 p-4"><TotalesVenta {...total} moneda={refs.moneda} preview />
          <p className="text-xs leading-5 text-slate-500">Importes sujetos a validación al guardar. El borrador no descuenta inventario.</p>
          {errorLocal && <AvisoVenta error>{errorLocal}</AvisoVenta>}{operacion.error && <AvisoVenta error><p>{operacion.error.message}</p>{esConcurrencia(operacion.error) && <p className="mt-2">La venta cambió desde que la abriste. Recarga la información.</p>}{original && consultar && <Link className="mt-2 inline-block font-semibold underline" to={`/ventas/${original.encabezado.id}`}>Consultar la venta actual antes de reintentar</Link>}</AvisoVenta>}
          {vender && <><Boton className="pos-finalizar w-full" onClick={() => void guardar(true)} cargando={operacion.procesando} disabled={!datosValidos || (!esCredito && total.total > 0 && (!cobrar || !turnoAbierto)) || esConcurrencia(operacion.error)}>Finalizar venta</Boton>
            {!esCredito && total.total > 0 && !turnoAbierto && <p role="status" className="text-sm text-amber-800">{sesiones.cargando ? "Consultando caja…" : sesiones.error ? "No se pudo verificar la caja. Vuelva a consultar." : "Se requiere un turno de caja abierto para cobrar."}{sesiones.error && <button className="ml-2 underline" onClick={sesiones.recargar}>Reintentar</button>}</p>}</>}
          {administrar ? <Boton className="w-full" variante="secundario" onClick={() => void guardar()} cargando={operacion.procesando} disabled={!lineas.length || !refs.almacenes.length || esConcurrencia(operacion.error)}>{original ? "Guardar cambios del borrador" : "Guardar borrador"}</Boton> : <p className="text-sm text-slate-600">Puede consultar el catálogo. Se requiere VENTAS.ADMINISTRAR para guardar una venta.</p>}
        </div>
      </Tarjeta></aside>
    </div>
    <a className="pos-acceso-carrito" href="#carrito-pos"><ShoppingCart className="h-5 w-5" aria-hidden="true" /> Orden actual ({lineas.length}) <strong>{dineroVenta(total.total, refs.moneda)}</strong></a>
    {cierre && <CierrePos inicial={cierre} nueva={reiniciar} cerrar={(actual) => { persistida.current = actual; setCierre(null); if (actual.encabezado.estado !== "BORRADOR") guardada(actual); }} />}
  </div>;
}

function TarjetaProductoPos({ producto: p, agregar, ocupado }: { producto: ProductoPos; agregar: (p: ProductoPos, opcion: PresentacionPos) => void; ocupado: boolean }) {
  const [seleccion, setSeleccion] = useState("");
  const opcion = p.presentaciones.find((x) => clavePresentacion(x) === seleccion) ?? p.presentaciones.find(precioUtilizable) ?? p.presentaciones[0];
  const [imagenRota, setImagenRota] = useState(false);
  const sinStock = !!opcion && p.manejaExistencia && p.disponibleBase !== null && p.disponibleBase < opcion.factorConversion;
  const categoria = p.categoria.toLocaleLowerCase("es");
  const Icono = /helad/.test(categoria) ? IceCreamBowl : /bebida/.test(categoria) ? Coffee : /postre/.test(categoria) ? CakeSlice : /especial/.test(categoria) ? Sparkles : Package;
  return <article aria-label={p.nombre} className={`pos-producto ${sinStock ? "pos-producto-agotado" : ""}`}>
    <div className="pos-producto-cabecera"><div className="pos-producto-imagen">{p.imagen && !imagenRota ? <img src={p.imagen} alt="" className="h-full w-full object-cover" loading="lazy" onError={() => setImagenRota(true)} /> : <Icono className="h-12 w-12" aria-hidden="true" />}</div><div className="min-w-0"><p className="text-[11px] font-semibold uppercase tracking-wide text-teal-700">{p.categoria}</p><h2 className="mt-1 break-words font-bold text-slate-900">{p.nombre}</h2><p className="mt-1 text-xs text-slate-500">{p.sku}</p></div></div>
    <div className="flex flex-1 flex-col gap-3 p-4">
      {sinStock && <p className="pos-sin-stock">Sin existencias</p>}<p className="text-xs text-slate-500">{p.disponibleBase === null ? "Disponibilidad no calculada" : `${n.format(p.disponibleBase)} ${p.abreviaturaBase} disponibles`}</p>
      <Seleccion etiqueta={`Presentación · ${p.nombre}`} value={opcion ? clavePresentacion(opcion) : ""} onChange={(e) => setSeleccion(e.target.value)} disabled={ocupado || !opcion}>{p.presentaciones.map((s) => <option key={clavePresentacion(s)} value={clavePresentacion(s)}>{nombrePresentacion(s)}{!precioUtilizable(s) ? " · Precio no configurado" : ""}</option>)}</Seleccion>
      <div className="mt-auto">{opcion && precioUtilizable(opcion) ? <><p className="text-xl font-bold text-slate-900">{dineroVenta(opcion.precio, p.moneda)} <span className="text-xs font-normal text-slate-500">/ {opcion.abreviatura}</span></p><p className="mt-1 text-xs text-slate-500">1 {opcion.abreviatura} = {n.format(opcion.factorConversion)} {p.abreviaturaBase}</p></> : <p className="font-semibold text-amber-700">Precio no configurado</p>}</div>
      <Boton variante="secundario" disabled={ocupado || sinStock || !opcion || !precioUtilizable(opcion)} onClick={() => opcion && agregar(p, opcion)} aria-label={`Agregar ${p.nombre} ${opcion?.nombre ?? ""}`}><Plus className="h-4 w-4" /> Agregar</Boton>
    </div>
  </article>;
}
