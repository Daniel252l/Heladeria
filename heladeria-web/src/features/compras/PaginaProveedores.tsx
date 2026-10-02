import { useCallback, useEffect, useId, useMemo, useState, type FormEvent } from "react";
import { Building2, Mail, Plus, RotateCcw, Save, Search, WalletCards } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { BotonAccionTabla, GrupoAccionesTabla } from "../../components/ui/BotonAccionTabla";
import { AreaTexto, Boton, Entrada, Insignia, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoError, EstadoVacio } from "../../components/ui/Estados";
import { Modal } from "../../components/ui/Modal";
import { Paginacion } from "../../components/ui/Paginacion";
import { PanelFiltros } from "../../components/ui/PanelFiltros";
import { SeccionFormulario } from "../../components/ui/SeccionFormulario";
import { SelectorEntidad, type OpcionEntidad } from "../../components/ui/SelectorEntidad";
import type { Estado, Paginado } from "../../types/api";
import type { GuardarProveedor, Proveedor, ReferenciasCompras } from "../../types/compras";
import { formatoCosto } from "../../utils/formato";

interface FormularioProveedor { codigoProveedor: string; nombreProveedor: string; nit: string; telefono: string; correo: string; direccion: string; diasCredito: string; limiteCredito: string; monedaId: string; estadoId: string }
const formularioInicial: FormularioProveedor = { codigoProveedor: "", nombreProveedor: "", nit: "", telefono: "", correo: "", direccion: "", diasCredito: "0", limiteCredito: "", monedaId: "", estadoId: "" };

export function PaginaProveedores() {
  const formularioId = useId();
  const navegar = useNavigate();
  const { tienePermiso } = useAutenticacion();
  const [datos, setDatos] = useState<Paginado<Proveedor> | null>(null);
  const [referencias, setReferencias] = useState<ReferenciasCompras | null>(null);
  const [estados, setEstados] = useState<Estado[]>([]);
  const [pagina, setPagina] = useState(1); const [tamanio, setTamanio] = useState(10);
  const [borrador, setBorrador] = useState({ busqueda: "", nit: "", correo: "", estadoId: "" });
  const [filtros, setFiltros] = useState({ busqueda: "", nit: "", correo: "", estadoId: "" });
  const [cargando, setCargando] = useState(true); const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState(false); const [editando, setEditando] = useState<Proveedor | null>(null);
  const [formulario, setFormulario] = useState<FormularioProveedor>(formularioInicial);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null); const [guardando, setGuardando] = useState(false);
  const [cambio, setCambio] = useState<{ proveedor: Proveedor; estadoId: number; nombre: string } | null>(null);
  const { busqueda: busquedaAplicada, nit: nitAplicado, correo: correoAplicado, estadoId: estadoAplicado } = filtros;

  const cargarProveedores = useCallback(async () => {
    setCargando(true); setError(null);
    try {
      const parametros = new URLSearchParams({ pagina: String(pagina), tamanioPagina: String(tamanio) });
      if (busquedaAplicada) parametros.set("busqueda", busquedaAplicada);
      if (nitAplicado) parametros.set("nit", nitAplicado);
      if (correoAplicado) parametros.set("correo", correoAplicado);
      if (estadoAplicado) parametros.set("estadoId", estadoAplicado);
      setDatos(await solicitar<Paginado<Proveedor>>(`/api/compras/proveedores?${parametros}`));
    } catch (excepcion) { setError(excepcion instanceof Error ? excepcion.message : "No fue posible consultar proveedores."); }
    finally { setCargando(false); }
  }, [busquedaAplicada, correoAplicado, estadoAplicado, nitAplicado, pagina, tamanio]);
  useEffect(() => {
    // Solo el listado depende de página, tamaño y filtros ya aplicados.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargarProveedores();
  }, [cargarProveedores]);
  useEffect(() => {
    let vigente = true;
    void Promise.all([
      solicitar<ReferenciasCompras>("/api/compras/referencias").catch(() => null),
      solicitar<Paginado<Estado>>("/api/configuracion/estados?pagina=1&tamanioPagina=100"),
    ]).then(([refs, estadosRespuesta]) => {
      if (!vigente) return;
      if (refs) setReferencias(refs);
      setEstados(estadosRespuesta.elementos);
    }).catch(() => {
      // El listado sigue siendo utilizable si una referencia auxiliar no está disponible.
    });
    return () => { vigente = false; };
  }, []);

  const puedeAdministrar = tienePermiso(PERMISOS.proveedoresAdministrar);
  const opcionesMoneda: OpcionEntidad[] = useMemo(() => referencias?.monedas.map((m) => ({ id: m.id, codigo: m.codigoISO, nombre: m.nombre })) ?? [], [referencias]);
  const abrir = (proveedor?: Proveedor) => {
    setEditando(proveedor ?? null); setErrorFormulario(null);
    if (proveedor) setFormulario({ codigoProveedor: proveedor.codigoProveedor, nombreProveedor: proveedor.nombreProveedor, nit: proveedor.nit ?? "", telefono: proveedor.telefono ?? "", correo: proveedor.correo ?? "", direccion: proveedor.direccion ?? "", diasCredito: String(proveedor.diasCredito), limiteCredito: proveedor.limiteCredito == null ? "" : String(proveedor.limiteCredito), monedaId: proveedor.monedaId == null ? "" : String(proveedor.monedaId), estadoId: String(proveedor.estadoId) });
    else { const activo = estados.find((e) => e.nombre.trim().toLowerCase() === "activo"); setFormulario({ ...formularioInicial, estadoId: activo ? String(activo.id) : "" }); }
    setModal(true);
  };
  const guardar = async (evento: FormEvent) => {
    evento.preventDefault(); setErrorFormulario(null);
    if (!formulario.codigoProveedor.trim() || !formulario.nombreProveedor.trim() || !formulario.estadoId) { setErrorFormulario("Complete código, nombre y estado."); return; }
    const cuerpo: GuardarProveedor = { codigoProveedor: formulario.codigoProveedor.trim(), nombreProveedor: formulario.nombreProveedor.trim(), nit: formulario.nit.trim() || null, telefono: formulario.telefono.trim() || null, correo: formulario.correo.trim() || null, direccion: formulario.direccion.trim() || null, diasCredito: Number(formulario.diasCredito), limiteCredito: formulario.limiteCredito === "" ? null : Number(formulario.limiteCredito), monedaId: formulario.monedaId ? Number(formulario.monedaId) : null, estadoId: Number(formulario.estadoId) };
    setGuardando(true);
    try { await solicitar(editando ? `/api/compras/proveedores/${editando.id}` : "/api/compras/proveedores", { method: editando ? "PUT" : "POST", body: JSON.stringify(cuerpo) }); setModal(false); await cargarProveedores(); }
    catch (excepcion) { setErrorFormulario(excepcion instanceof Error ? excepcion.message : "No fue posible guardar el proveedor."); }
    finally { setGuardando(false); }
  };
  const prepararEstado = (proveedor: Proveedor) => {
    const estaActivo = proveedor.estado.trim().toLowerCase() === "activo";
    const objetivo = estados.find((e) => e.nombre.trim().toLowerCase() === (estaActivo ? "inactivo" : "activo"));
    if (!objetivo) { setError(`No se encontró el estado semántico ${estaActivo ? "Inactivo" : "Activo"} en las referencias.`); return; }
    setCambio({ proveedor, estadoId: objetivo.id, nombre: objetivo.nombre });
  };
  const cambiarEstado = async () => {
    if (!cambio) return;
    setGuardando(true);
    try { await solicitar(`/api/compras/proveedores/${cambio.proveedor.id}/estado`, { method: "PUT", body: JSON.stringify({ estadoId: cambio.estadoId }) }); setCambio(null); await cargarProveedores(); }
    catch (excepcion) { setCambio(null); setError(excepcion instanceof Error ? excepcion.message : "No fue posible actualizar el estado."); }
    finally { setGuardando(false); }
  };

  return <div className="space-y-6">
    <EncabezadoPagina titulo="Proveedores" descripcion="Administra contactos, condiciones comerciales y productos asociados." icono={<Building2 className="h-7 w-7" />} acciones={puedeAdministrar ? <Boton onClick={() => abrir()}><Plus className="h-4 w-4" /> Nuevo proveedor</Boton> : undefined} />
    <form onSubmit={(e) => { e.preventDefault(); setPagina(1); setFiltros({ ...borrador }); }}><PanelFiltros descripcion="Encuentra proveedores por código, nombre, NIT, correo o estado." acciones={<><Boton type="submit"><Search className="h-4 w-4" /> Buscar proveedores</Boton><Boton type="button" variante="secundario" onClick={() => { setBorrador({ busqueda: "", nit: "", correo: "", estadoId: "" }); setFiltros({ busqueda: "", nit: "", correo: "", estadoId: "" }); setPagina(1); }}><RotateCcw className="h-4 w-4" /> Limpiar filtros</Boton></>}><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4"><Entrada etiqueta="Código o nombre" value={borrador.busqueda} onChange={(e) => setBorrador((x) => ({ ...x, busqueda: e.target.value }))} /><Entrada etiqueta="NIT" value={borrador.nit} onChange={(e) => setBorrador((x) => ({ ...x, nit: e.target.value }))} /><Entrada etiqueta="Correo" type="email" value={borrador.correo} onChange={(e) => setBorrador((x) => ({ ...x, correo: e.target.value }))} /><Seleccion etiqueta="Estado" value={borrador.estadoId} onChange={(e) => setBorrador((x) => ({ ...x, estadoId: e.target.value }))}><option value="">Todos</option>{estados.map((estado) => <option key={estado.id} value={estado.id}>{estado.nombre}</option>)}</Seleccion></div></PanelFiltros></form>
    {error && <EstadoError mensaje={error} reintentar={cargarProveedores} />}
    <Tarjeta className="overflow-hidden"><header className="border-b border-slate-200 px-5 py-4"><h2 className="font-bold">Lista de proveedores</h2><p className="text-sm text-slate-500">{datos?.totalRegistros ?? 0} proveedores encontrados</p></header>{cargando ? <EstadoCargando /> : !datos?.elementos.length ? <EstadoVacio titulo="No hay proveedores registrados" descripcion="Agrega el primer proveedor o cambia los filtros." accion={puedeAdministrar ? <Boton onClick={() => abrir()}>Nuevo proveedor</Boton> : undefined} /> : <><div className="overflow-x-auto"><table className="min-w-[82rem] divide-y divide-slate-200 text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr>{["Código","Nombre","NIT","Teléfono","Correo","Días crédito","Límite crédito","Moneda","Estado","Acciones"].map((x) => <th key={x} className="px-4 py-3">{x}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">{datos.elementos.map((proveedor) => { const moneda = referencias?.monedas.find((m) => m.id === proveedor.monedaId); const activo = proveedor.estado.trim().toLowerCase() === "activo"; return <tr key={proveedor.id}><td className="px-4 py-4 font-mono text-xs">{proveedor.codigoProveedor}</td><td className="px-4 py-4 font-semibold">{proveedor.nombreProveedor}</td><td className="px-4 py-4 text-slate-600">{proveedor.nit || "—"}</td><td className="px-4 py-4 text-slate-600">{proveedor.telefono || "—"}</td><td className="px-4 py-4 text-slate-600">{proveedor.correo || "—"}</td><td className="px-4 py-4 text-right">{proveedor.diasCredito}</td><td className="px-4 py-4 text-right">{proveedor.limiteCredito == null ? "—" : formatoCosto.format(proveedor.limiteCredito)}</td><td className="px-4 py-4">{moneda?.codigoISO ?? "—"}</td><td className="px-4 py-4"><Insignia tono={activo ? "positivo" : "negativo"}>{proveedor.estado}</Insignia></td><td className="px-4 py-4"><GrupoAccionesTabla><BotonAccionTabla tipo="ver" onClick={() => navegar(`/proveedores/${proveedor.id}`)} />{puedeAdministrar && <><BotonAccionTabla tipo="editar" onClick={() => abrir(proveedor)} /><BotonAccionTabla tipo={activo ? "desactivar" : "activar"} onClick={() => prepararEstado(proveedor)} /></>}</GrupoAccionesTabla></td></tr>; })}</tbody></table></div><Paginacion pagina={datos.pagina} totalPaginas={datos.totalPaginas} totalRegistros={datos.totalRegistros} tamanioPagina={tamanio} cambiar={setPagina} cambiarTamanio={(n) => { setTamanio(n); setPagina(1); }} entidad="proveedor" /></>}</Tarjeta>
    <Modal abierto={modal} cerrar={() => !guardando && setModal(false)} titulo={editando ? "Editar proveedor" : "Nuevo proveedor"} descripcion="Completa únicamente los datos contemplados por el contrato de compras." ancho="max-w-3xl" icono={<Building2 className="h-5 w-5" />} footer={<><Boton variante="secundario" onClick={() => setModal(false)} disabled={guardando}>Cancelar</Boton><Boton type="submit" form={formularioId} cargando={guardando}><Save className="h-4 w-4" /> Guardar</Boton></>}><form id={formularioId} className="space-y-4 p-5" onSubmit={guardar}><SeccionFormulario titulo="Información general" icono={<Building2 className="h-4 w-4" />}><Entrada etiqueta="Código proveedor *" maxLength={20} value={formulario.codigoProveedor} onChange={(e) => setFormulario((x) => ({ ...x, codigoProveedor: e.target.value }))} /><Entrada etiqueta="Nombre *" maxLength={150} value={formulario.nombreProveedor} onChange={(e) => setFormulario((x) => ({ ...x, nombreProveedor: e.target.value }))} /><Entrada etiqueta="NIT" maxLength={20} value={formulario.nit} onChange={(e) => setFormulario((x) => ({ ...x, nit: e.target.value }))} /><SelectorEntidad etiqueta="Estado *" ruta="/api/configuracion/estados" value={formulario.estadoId} etiquetaActual={editando?.estado} onChange={(v) => setFormulario((x) => ({ ...x, estadoId: v ?? "" }))} /></SeccionFormulario><SeccionFormulario titulo="Contacto" icono={<Mail className="h-4 w-4" />}><Entrada etiqueta="Teléfono" maxLength={20} value={formulario.telefono} onChange={(e) => setFormulario((x) => ({ ...x, telefono: e.target.value }))} /><Entrada etiqueta="Correo" type="email" maxLength={100} value={formulario.correo} onChange={(e) => setFormulario((x) => ({ ...x, correo: e.target.value }))} /><div className="sm:col-span-2"><AreaTexto etiqueta="Dirección" maxLength={250} rows={2} value={formulario.direccion} onChange={(e) => setFormulario((x) => ({ ...x, direccion: e.target.value }))} /></div></SeccionFormulario><SeccionFormulario titulo="Condiciones comerciales" icono={<WalletCards className="h-4 w-4" />}><Entrada etiqueta="Días de crédito" type="number" min="0" max="32767" value={formulario.diasCredito} onChange={(e) => setFormulario((x) => ({ ...x, diasCredito: e.target.value }))} /><Entrada etiqueta="Límite de crédito" type="number" min="0" step="0.01" value={formulario.limiteCredito} onChange={(e) => setFormulario((x) => ({ ...x, limiteCredito: e.target.value }))} /><div className="sm:col-span-2"><SelectorEntidad etiqueta="Moneda" opcional opcionesLocales={opcionesMoneda} value={formulario.monedaId} onChange={(v) => setFormulario((x) => ({ ...x, monedaId: v ?? "" }))} /></div></SeccionFormulario>{errorFormulario && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{errorFormulario}</p>}</form></Modal>
    <DialogoConfirmacion abierto={Boolean(cambio)} cerrar={() => setCambio(null)} confirmar={cambiarEstado} cargando={guardando} peligro={cambio?.nombre.toLowerCase() === "inactivo"} titulo={`${cambio?.nombre === "Inactivo" ? "Desactivar" : "Activar"} proveedor`} descripcion={`El proveedor pasará al estado ${cambio?.nombre ?? "seleccionado"}.`} textoConfirmar="Confirmar"><p className="text-sm font-semibold">{cambio?.proveedor.nombreProveedor}</p></DialogoConfirmacion>
  </div>;
}
