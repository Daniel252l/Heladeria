import { useCallback, useEffect, useId, useState, type FormEvent } from "react";
import { ArrowLeft, Building2, Link2, Plus, Save } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { BotonAccionTabla, GrupoAccionesTabla } from "../../components/ui/BotonAccionTabla";
import { Boton, Entrada, Insignia, Tarjeta } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoError, EstadoVacio } from "../../components/ui/Estados";
import { Modal } from "../../components/ui/Modal";
import { Paginacion } from "../../components/ui/Paginacion";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { SwitchCampo } from "../../components/ui/SwitchCampo";
import type { Estado, Paginado } from "../../types/api";
import type { GuardarProveedorProducto, Proveedor, ProveedorProducto, ReferenciasCompras } from "../../types/compras";
import { formatoCosto } from "../../utils/formato";

interface FormularioAsociacion { productoId: string; producto: string; codigoProveedor: string; precioUltimaCompra: string; esPreferido: boolean; estadoId: string }
const asociacionInicial: FormularioAsociacion = { productoId: "", producto: "", codigoProveedor: "", precioUltimaCompra: "", esPreferido: false, estadoId: "" };

export function PaginaDetalleProveedor() {
  const { id } = useParams(); const navegar = useNavigate(); const formularioId = useId();
  const { tienePermiso } = useAutenticacion();
  const [proveedor, setProveedor] = useState<Proveedor | null>(null);
  const [productos, setProductos] = useState<Paginado<ProveedorProducto> | null>(null);
  const [referencias, setReferencias] = useState<ReferenciasCompras | null>(null);
  const [estados, setEstados] = useState<Estado[]>([]);
  const [pagina, setPagina] = useState(1); const [tamanio, setTamanio] = useState(10);
  const [cargando, setCargando] = useState(true); const [error, setError] = useState<string | null>(null); const [noEncontrado, setNoEncontrado] = useState(false);
  const [modal, setModal] = useState(false); const [editando, setEditando] = useState<ProveedorProducto | null>(null); const [formulario, setFormulario] = useState<FormularioAsociacion>(asociacionInicial); const [errorFormulario, setErrorFormulario] = useState<string | null>(null); const [guardando, setGuardando] = useState(false);
  const [cambio, setCambio] = useState<{ asociacion: ProveedorProducto; estadoId: number; nombre: string } | null>(null);

  const cargar = useCallback(async () => {
    if (!id) return;
    setCargando(true); setError(null); setNoEncontrado(false);
    try {
      const [detalle, lista, refs, estadosRespuesta] = await Promise.all([
        solicitar<Proveedor>(`/api/compras/proveedores/${id}`),
        solicitar<Paginado<ProveedorProducto>>(`/api/compras/proveedores/${id}/productos?pagina=${pagina}&tamanioPagina=${tamanio}`),
        solicitar<ReferenciasCompras>("/api/compras/referencias").catch(() => null),
        solicitar<Paginado<Estado>>("/api/configuracion/estados?pagina=1&tamanioPagina=100"),
      ]);
      setProveedor(detalle); setProductos(lista); if (refs) setReferencias(refs); setEstados(estadosRespuesta.elementos);
    } catch (excepcion) {
      if (excepcion instanceof ErrorApi && excepcion.estado === 404) setNoEncontrado(true);
      else setError(excepcion instanceof Error ? excepcion.message : "No fue posible consultar el proveedor.");
    } finally { setCargando(false); }
  }, [id, pagina, tamanio]);
  useEffect(() => {
    // Sincroniza el proveedor y sus asociaciones con la ruta y la página.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);
  const puedeAdministrar = tienePermiso(PERMISOS.proveedoresAdministrar);
  const abrir = (item?: ProveedorProducto) => {
    setEditando(item ?? null); setErrorFormulario(null);
    if (item) setFormulario({ productoId: String(item.productoId), producto: item.producto, codigoProveedor: item.codigoProveedor ?? "", precioUltimaCompra: item.precioUltimaCompra == null ? "" : String(item.precioUltimaCompra), esPreferido: item.esPreferido, estadoId: String(item.estadoId) });
    else { const activo = estados.find((e) => e.nombre.toLowerCase() === "activo"); setFormulario({ ...asociacionInicial, estadoId: activo ? String(activo.id) : "" }); }
    setModal(true);
  };
  const guardar = async (evento: FormEvent) => {
    evento.preventDefault(); setErrorFormulario(null);
    if (!id || !formulario.productoId || !formulario.estadoId) { setErrorFormulario("Seleccione producto y estado."); return; }
    const cuerpo: GuardarProveedorProducto = { productoId: Number(formulario.productoId), codigoProveedor: formulario.codigoProveedor.trim() || null, precioUltimaCompra: formulario.precioUltimaCompra === "" ? null : Number(formulario.precioUltimaCompra), esPreferido: formulario.esPreferido, estadoId: Number(formulario.estadoId) };
    setGuardando(true);
    try { await solicitar(editando ? `/api/compras/proveedores/${id}/productos/${editando.id}` : `/api/compras/proveedores/${id}/productos`, { method: editando ? "PUT" : "POST", body: JSON.stringify(cuerpo) }); setModal(false); await cargar(); }
    catch (excepcion) { setErrorFormulario(excepcion instanceof Error ? excepcion.message : "No fue posible guardar la asociación."); }
    finally { setGuardando(false); }
  };
  const prepararEstado = (asociacion: ProveedorProducto) => {
    const activo = asociacion.estado.toLowerCase() === "activo";
    const destino = estados.find((e) => e.nombre.toLowerCase() === (activo ? "inactivo" : "activo"));
    if (!destino) { setError(`No se encontró el estado ${activo ? "Inactivo" : "Activo"}.`); return; }
    setCambio({ asociacion, estadoId: destino.id, nombre: destino.nombre });
  };
  const cambiarEstado = async () => {
    if (!id || !cambio) return;
    setGuardando(true);
    try { await solicitar(`/api/compras/proveedores/${id}/productos/${cambio.asociacion.id}/estado`, { method: "PUT", body: JSON.stringify({ estadoId: cambio.estadoId }) }); setCambio(null); await cargar(); }
    catch (excepcion) { setCambio(null); setError(excepcion instanceof Error ? excepcion.message : "No fue posible cambiar el estado."); }
    finally { setGuardando(false); }
  };

  if (cargando) return <EstadoCargando mensaje="Consultando proveedor…" />;
  if (noEncontrado) return <EstadoError mensaje="No se encontró el recurso solicitado." />;
  if (!proveedor) return <EstadoError mensaje={error ?? "No se encontró el recurso solicitado."} reintentar={cargar} />;
  const moneda = referencias?.monedas.find((m) => m.id === proveedor.monedaId);
  return <div className="space-y-6">
    <EncabezadoPagina titulo={proveedor.nombreProveedor} descripcion={`${proveedor.codigoProveedor} · Gestión de productos asociados`} icono={<Building2 className="h-7 w-7" />} acciones={<Boton variante="secundario" onClick={() => navegar("/proveedores")}><ArrowLeft className="h-4 w-4" /> Volver</Boton>} />
    {error && <EstadoError mensaje={error} reintentar={cargar} />}
    <Tarjeta className="p-5 sm:p-6"><div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4"><Dato etiqueta="Estado"><Insignia tono={proveedor.estado.toLowerCase() === "activo" ? "positivo" : "negativo"}>{proveedor.estado}</Insignia></Dato><Dato etiqueta="NIT" valor={proveedor.nit || "—"} /><Dato etiqueta="Teléfono" valor={proveedor.telefono || "—"} /><Dato etiqueta="Correo" valor={proveedor.correo || "—"} /><Dato etiqueta="Dirección" valor={proveedor.direccion || "—"} /><Dato etiqueta="Días crédito" valor={String(proveedor.diasCredito)} /><Dato etiqueta="Límite crédito" valor={proveedor.limiteCredito == null ? "—" : formatoCosto.format(proveedor.limiteCredito)} /><Dato etiqueta="Moneda" valor={moneda ? `${moneda.codigoISO} / ${moneda.nombre}` : "Sin asignar"} /></div></Tarjeta>
    <Tarjeta className="overflow-hidden"><header className="flex items-center justify-between gap-4 border-b border-slate-200 px-5 py-4"><div><h2 className="flex items-center gap-2 font-bold"><Link2 className="h-5 w-5 text-teal-700" /> Productos asociados</h2><p className="text-sm text-slate-500">Código y costo de referencia acordados con este proveedor.</p></div>{puedeAdministrar && <Boton onClick={() => abrir()}><Plus className="h-4 w-4" /> Asociar producto</Boton>}</header>{!productos?.elementos.length ? <EstadoVacio titulo="No hay productos asociados" descripcion="Agrega una asociación lógica entre el proveedor y un producto comprable." accion={puedeAdministrar ? <Boton onClick={() => abrir()}>Asociar producto</Boton> : undefined} /> : <><div className="overflow-x-auto"><table className="min-w-[55rem] w-full divide-y divide-slate-200 text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr>{["Producto","Código proveedor","Costo referencial","Preferido","Estado","Acciones"].map((x) => <th key={x} className="px-4 py-3">{x}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">{productos.elementos.map((item) => { const activo = item.estado.toLowerCase() === "activo"; return <tr key={item.id}><td className="px-4 py-4 font-semibold">{item.producto}</td><td className="px-4 py-4 font-mono text-xs">{item.codigoProveedor || "—"}</td><td className="px-4 py-4">{item.precioUltimaCompra == null ? "—" : formatoCosto.format(item.precioUltimaCompra)}</td><td className="px-4 py-4"><Insignia tono={item.esPreferido ? "positivo" : "neutro"}>{item.esPreferido ? "Sí" : "No"}</Insignia></td><td className="px-4 py-4"><Insignia tono={activo ? "positivo" : "negativo"}>{item.estado}</Insignia></td><td className="px-4 py-4"><GrupoAccionesTabla>{puedeAdministrar && <><BotonAccionTabla tipo="editar" onClick={() => abrir(item)} /><BotonAccionTabla tipo={activo ? "desactivar" : "activar"} onClick={() => prepararEstado(item)} /></>}</GrupoAccionesTabla></td></tr>; })}</tbody></table></div><Paginacion pagina={productos.pagina} totalPaginas={productos.totalPaginas} totalRegistros={productos.totalRegistros} tamanioPagina={tamanio} cambiar={setPagina} cambiarTamanio={(n) => { setTamanio(n); setPagina(1); }} entidad="producto asociado" /></>}</Tarjeta>
    <p className="rounded-xl border border-sky-200 bg-sky-50 p-4 text-sm text-sky-800">El contrato actual de productos por proveedor no expone presentación ni unidad de compra; esta vista muestra únicamente los datos reales disponibles y no los infiere.</p>
    <Modal abierto={modal} cerrar={() => !guardando && setModal(false)} titulo={editando ? "Editar producto asociado" : "Asociar producto"} descripcion="Define los datos comerciales específicos de este proveedor." icono={<Link2 className="h-5 w-5" />} footer={<><Boton variante="secundario" onClick={() => setModal(false)} disabled={guardando}>Cancelar</Boton><Boton type="submit" form={formularioId} cargando={guardando}><Save className="h-4 w-4" /> Guardar</Boton></>}><form id={formularioId} className="space-y-4 p-5" onSubmit={guardar}><SelectorEntidad etiqueta="Producto *" ruta="/api/productos" value={formulario.productoId} etiquetaActual={formulario.producto} deshabilitado={Boolean(editando)} onChange={(v) => setFormulario((x) => ({ ...x, productoId: v ?? "" }))} /><Entrada etiqueta="Código del proveedor" maxLength={50} value={formulario.codigoProveedor} onChange={(e) => setFormulario((x) => ({ ...x, codigoProveedor: e.target.value }))} /><Entrada etiqueta="Costo referencial" type="number" min="0" step="0.01" value={formulario.precioUltimaCompra} onChange={(e) => setFormulario((x) => ({ ...x, precioUltimaCompra: e.target.value }))} /><SwitchCampo etiqueta="Producto preferido" descripcion="Identifica la relación comercial preferente." activo={formulario.esPreferido} cambiar={(v) => setFormulario((x) => ({ ...x, esPreferido: v }))} /><SelectorEntidad etiqueta="Estado *" ruta="/api/configuracion/estados" value={formulario.estadoId} etiquetaActual={editando?.estado} onChange={(v) => setFormulario((x) => ({ ...x, estadoId: v ?? "" }))} />{errorFormulario && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{errorFormulario}</p>}</form></Modal>
    <DialogoConfirmacion abierto={Boolean(cambio)} cerrar={() => setCambio(null)} confirmar={cambiarEstado} cargando={guardando} peligro={cambio?.nombre.toLowerCase() === "inactivo"} titulo={`${cambio?.nombre.toLowerCase() === "inactivo" ? "Desasociar" : "Reactivar"} producto`} descripcion="La relación cambiará de estado sin eliminarse físicamente." textoConfirmar="Confirmar"><p className="font-semibold">{cambio?.asociacion.producto}</p></DialogoConfirmacion>
  </div>;
}

function Dato({ etiqueta, valor, children }: { etiqueta: string; valor?: string; children?: React.ReactNode }) { return <div><dt className="text-xs font-semibold uppercase text-slate-400">{etiqueta}</dt><dd className="mt-1.5 font-medium text-slate-800">{children ?? valor}</dd></div>; }
