import { useCallback, useEffect, useId, useState, type FormEvent } from "react";
import {
  CheckCircle2,
  Building2,
  CircleGauge,
  Database,
  List,
  Package,
  Plus,
  RotateCcw,
  Save,
  Search,
  Tag,
} from "lucide-react";
import { solicitar } from "../api/clienteHttp";
import { PERMISOS } from "../auth/permisos";
import { useAutenticacion } from "../auth/useAutenticacion";
import { useToast } from "../components/toast/useToast";
import {
  AreaTexto,
  Boton,
  Entrada,
  Insignia,
  Seleccion,
  Tarjeta,
} from "../components/ui/Controles";
import { DialogoConfirmacion } from "../components/ui/DialogoConfirmacion";
import {
  BotonAccionTabla,
  GrupoAccionesTabla,
} from "../components/ui/BotonAccionTabla";
import { EncabezadoPagina } from "../components/ui/EncabezadoPagina";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../components/ui/Estados";
import { Modal } from "../components/ui/Modal";
import { Paginacion } from "../components/ui/Paginacion";
import { PanelFiltros } from "../components/ui/PanelFiltros";
import { SeccionFormulario } from "../components/ui/SeccionFormulario";
import { SelectorEntidad } from "../components/ui/SelectorEntidad";
import { SwitchCampo } from "../components/ui/SwitchCampo";
import { PresentacionesProducto } from "../features/productos/PresentacionesProducto";
import { ComposicionVenta } from "../features/productos/ComposicionVenta";
import { filasComposicion, validarComposicion, type FilaComponente } from "../features/productos/utilidadesComposicion";
import type { Paginado } from "../types/api";
import type { Producto, ReferenciasProducto } from "../types/productos";
import { formatoCosto, formatoNumero } from "../utils/formato";

type Formulario = Record<string, string | number | boolean | null>;
const clasificacion = [
  ["categoriaId", "Categoría *", "categorias", "categoria"],
  ["marcaId", "Marca", "marcas", "marca"],
  ["tipoProductoId", "Tipo de producto *", "tipos", "tipoProducto"],
  ["unidadBaseId", "Unidad base *", "unidades-medida", "unidadBase"],
] as const;
const inicial: Formulario = {
  sku: "",
  nombre: "",
  descripcion: "",
  modelo: "",
  imagen: "",
  peso: "",
  categoriaId: "",
  marcaId: "",
  tipoProductoId: "",
  unidadBaseId: "",
  tipoSeguimientoId: "",
  metodoValuacionId: "",
  controlaVencimiento: false,
  esComprable: true,
  esVendible: true,
  permiteDescuento: true,
  esCombo: false,
  precioVentaReferencial: "",
  estadoId: "",
};

export function PaginaProductos() {
  const formularioId = useId();
  const [datos, setDatos] = useState<Paginado<Producto> | null>(null);
  const [filtros, setFiltros] = useState<Record<string, string>>({});
  const [borradorFiltros, setBorradorFiltros] = useState<
    Record<string, string>
  >({});
  const [pagina, setPagina] = useState(1);
  const [tamanioPagina, setTamanioPagina] = useState(10);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editado, setEditado] = useState<Producto | null>(null);
  const [detalle, setDetalle] = useState<Producto | null>(null);
  const [formulario, setFormulario] = useState<Formulario>(inicial);
  const [modal, setModal] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [componentes, setComponentes] = useState<FilaComponente[]>([]);
  const cambiarComponente = useCallback((clave: string, cambios: Partial<FilaComponente>) =>
    setComponentes((filas) => filas.map((f) => f.clave === clave ? { ...f, ...cambios } : f)), []);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [referencias, setReferencias] = useState<ReferenciasProducto>({
    tiposSeguimiento: [],
    metodosValuacion: [],
  });
  const [cambio, setCambio] = useState<Producto | null>(null);
  const [estadoDestino, setEstadoDestino] = useState("");
  const { tienePermiso } = useAutenticacion();
  const puedeAdministrar = tienePermiso(PERMISOS.productosAdministrar);
  const { mostrar } = useToast();

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    const params = new URLSearchParams({
      pagina: String(pagina),
      tamanioPagina: String(tamanioPagina),
    });
    for (const [k, v] of Object.entries(filtros)) if (v) params.set(k, v);
    try {
      setDatos(await solicitar<Paginado<Producto>>(`/api/productos?${params}`));
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "No fue posible cargar productos.",
      );
    } finally {
      setCargando(false);
    }
  }, [filtros, pagina, tamanioPagina]);
  useEffect(() => {
    // La consulta remota sincroniza los filtros y la página solicitada.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);
  const aplicarFiltros = () => {
    setPagina(1);
    setFiltros({ ...borradorFiltros });
  };
  const limpiarFiltros = () => {
    setBorradorFiltros({});
    setFiltros({});
    setPagina(1);
  };
  const cargarDetalle = async (p: Producto) => {
    try {
      return await solicitar<Producto>(`/api/productos/${p.id}`);
    } catch (e) {
      mostrar({
        tipo: "error",
        titulo: "No fue posible cargar el producto",
        descripcion: e instanceof Error ? e.message : undefined,
      });
      return null;
    }
  };
  const abrirFormulario = async (p?: Producto) => {
    try {
      const [ref, actual] = await Promise.all([
        solicitar<ReferenciasProducto>("/api/productos/referencias"),
        p ? cargarDetalle(p) : Promise.resolve(null),
      ]);
      if (p && !actual) return;
      setReferencias(ref);
      setEditado(actual);
      const f = { ...inicial };
      if (actual)
        for (const k of Object.keys(inicial))
          f[k] = (actual as unknown as Formulario)[k] ?? "";
      setFormulario(f);
      setComponentes(filasComposicion(actual?.componentes));
      setErrorFormulario(null);
      setModal(true);
    } catch (e) {
      mostrar({
        tipo: "error",
        titulo: "Referencias no disponibles",
        descripcion: e instanceof Error ? e.message : undefined,
      });
    }
  };
  const guardar = async (e: FormEvent) => {
    e.preventDefault();
    setErrorFormulario(null);
    const obligatorios = [
      "sku",
      "nombre",
      "categoriaId",
      "tipoProductoId",
      "unidadBaseId",
      "tipoSeguimientoId",
      "metodoValuacionId",
      "estadoId",
    ];
    if (obligatorios.some((k) => !formulario[k])) {
      setErrorFormulario("Complete todos los campos obligatorios.");
      return;
    }
    const errorComposicion = validarComposicion(componentes, editado?.id);
    if (errorComposicion || componentes.length > 0 && !formulario.esVendible) {
      setErrorFormulario(errorComposicion || "Un producto con composición de venta debe ser vendible.");
      return;
    }
    setGuardando(true);
    try {
      const cuerpo: Record<string, unknown> = { ...formulario };
      cuerpo.componentes = componentes.map((c) => ({ productoId: c.productoId,
        productoPresentacionId: c.productoPresentacionId, cantidad: Number(c.cantidad) }));
      cuerpo.esCombo = componentes.length > 0;
      for (const k of ["marcaId", "peso", "precioVentaReferencial"])
        cuerpo[k] = formulario[k] === "" ? null : Number(formulario[k]);
      for (const k of ["sku", "nombre", "descripcion", "modelo", "imagen"])
        cuerpo[k] = String(formulario[k]).trim() || null;
      if (editado) cuerpo.versionFila = editado.versionFila;
      await solicitar(
        editado ? `/api/productos/${editado.id}` : "/api/productos",
        { method: editado ? "PUT" : "POST", body: JSON.stringify(cuerpo) },
      );
      setModal(false);
      mostrar({
        tipo: "exito",
        titulo: editado ? "Producto actualizado" : "Producto creado",
      });
      await cargar();
    } catch (excepcion) {
      setErrorFormulario(
        excepcion instanceof Error
          ? excepcion.message
          : "No fue posible guardar el producto.",
      );
    } finally {
      setGuardando(false);
    }
  };
  const confirmarEstado = async () => {
    if (!cambio || !estadoDestino) return;
    setGuardando(true);
    try {
      await solicitar(`/api/productos/${cambio.id}/estado`, {
        method: "PUT",
        body: JSON.stringify({ estadoId: Number(estadoDestino) }),
      });
      setCambio(null);
      mostrar({ tipo: "exito", titulo: "Estado actualizado" });
      await cargar();
    } catch (e) {
      mostrar({
        tipo: "error",
        titulo: "Cambio rechazado",
        descripcion: e instanceof Error ? e.message : undefined,
      });
    } finally {
      setGuardando(false);
    }
  };
  const setCampo = (k: string, v: string | number | boolean | null) =>
    setFormulario((f) => ({ ...f, [k]: v }));

  return (
    <div className="space-y-6">
      <EncabezadoPagina
        titulo="Productos"
        descripcion="Catálogo comercial e inventariable, con presentaciones y equivalencias reales."
        icono={<Package className="h-7 w-7" />}
        acciones={
          puedeAdministrar ? (
            <Boton
              className="min-h-12 px-5"
              onClick={() => void abrirFormulario()}
            >
              <Plus className="h-4 w-4" /> Nuevo producto
            </Boton>
          ) : undefined
        }
      />
      <form
        onSubmit={(evento) => {
          evento.preventDefault();
          aplicarFiltros();
        }}
      >
        <PanelFiltros
          acciones={
            <>
              <Boton type="submit" className="min-h-11 px-5">
                <Search className="h-4 w-4" /> Buscar productos
              </Boton>
              <Boton
                type="button"
                variante="secundario"
                className="min-h-11 px-5"
                onClick={limpiarFiltros}
              >
                <RotateCcw className="h-4 w-4" /> Limpiar filtros
              </Boton>
            </>
          }
        >
          <div className="grid gap-x-5 gap-y-4 md:grid-cols-2 lg:grid-cols-3">
            <label className="block text-sm font-medium text-slate-700">
              SKU o nombre
              <span className="relative mt-1.5 block">
                <span
                  aria-hidden="true"
                  className="pointer-events-none absolute inset-y-px left-px flex w-11 items-center justify-center rounded-l-lg border-r border-slate-200 bg-slate-50/60 text-slate-500"
                >
                  <Search className="h-4.5 w-4.5" />
                </span>
                <input
                  className="min-h-11 w-full rounded-lg border border-slate-300 bg-white py-2.5 pl-14 pr-3 text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
                  value={borradorFiltros.busqueda ?? ""}
                  onChange={(evento) =>
                    setBorradorFiltros((actual) => ({
                      ...actual,
                      busqueda: evento.target.value,
                    }))
                  }
                  placeholder="Buscar por SKU o nombre…"
                />
              </span>
            </label>
            {clasificacion
              .filter(([k]) => k !== "unidadBaseId")
              .map(([k, label, ruta]) => (
                <SelectorEntidad
                  key={k}
                  etiqueta={label.replace(" *", "")}
                  ruta={`/api/productos/${ruta}`}
                  icono={
                    k === "categoriaId" ? (
                      <Tag className="h-4.5 w-4.5" />
                    ) : k === "marcaId" ? (
                      <Building2 className="h-4.5 w-4.5" />
                    ) : (
                      <Package className="h-4.5 w-4.5" />
                    )
                  }
                  opcional
                  value={borradorFiltros[k] ?? ""}
                  onChange={(valor) =>
                    setBorradorFiltros((actual) => ({
                      ...actual,
                      [k]: valor ?? "",
                    }))
                  }
                />
              ))}
            <SelectorEntidad
              etiqueta="Estado"
              ruta="/api/configuracion/estados"
              icono={<CircleGauge className="h-4.5 w-4.5" />}
              opcional
              value={borradorFiltros.estadoId ?? ""}
              onChange={(valor) =>
                setBorradorFiltros((actual) => ({
                  ...actual,
                  estadoId: valor ?? "",
                }))
              }
            />
          </div>
        </PanelFiltros>
      </form>
      <Tarjeta className="overflow-hidden">
        <header className="flex items-center gap-3 border-b border-slate-200 px-5 py-4 sm:px-6">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
            <List className="h-5 w-5" aria-hidden="true" />
          </span>
          <div>
            <h2 className="text-lg font-bold text-slate-950">Lista de productos</h2>
            <p className="mt-0.5 text-sm text-slate-500">
              {cargando || !datos
                ? "Consultando productos…"
                : `${datos.totalRegistros} ${datos.totalRegistros === 1 ? "producto encontrado" : "productos encontrados"}`}
            </p>
          </div>
        </header>
        {cargando ? (
          <EstadoCargando />
        ) : error ? (
          <EstadoError mensaje={error} reintentar={cargar} />
        ) : !datos?.elementos.length ? (
          <EstadoVacio titulo="No se encontraron productos" />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[72rem] divide-y divide-slate-200 text-left text-sm">
                <thead className="bg-slate-50/90 text-xs uppercase tracking-wide text-slate-500">
                  <tr>
                    {[
                      "SKU / Producto",
                      "Categoría",
                      "Marca",
                      "Tipo",
                      "Unidad base",
                      "Precio referencial",
                      "Existencia",
                      "Comprable",
                      "Vendible",
                      "Estado",
                      "Acciones",
                    ].map((c) => (
                      <th key={c} className="px-4 py-3.5 font-semibold">
                        {c}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {datos.elementos.map((p) => (
                    <tr
                      key={p.id}
                      className="transition-colors hover:bg-slate-50/70"
                    >
                      <td className="min-w-52 px-4 py-4.5">
                        <div className="flex items-center gap-3">
                          <MiniaturaProducto imagen={p.imagen} />
                          <div>
                            <p className="font-semibold text-slate-900">
                              {p.nombre}
                            </p>
                            <p className="mt-0.5 text-xs text-slate-500">
                              {p.sku}
                            </p>
                          </div>
                        </div>
                      </td>
                      {[
                        p.categoria,
                        p.marca ?? "—",
                        p.tipoProducto,
                        p.unidadBase,
                        p.precioVentaReferencial == null
                          ? "—"
                          : formatoCosto.format(p.precioVentaReferencial),
                      ].map((v, i) => (
                        <td key={i} className="max-w-40 px-4 py-4 text-slate-600">
                          {v}
                        </td>
                      ))}
                      {[p.manejaExistencia, p.esComprable, p.esVendible].map(
                        (v, i) => (
                          <td key={i} className="px-4 py-4">
                            <Insignia tono={v ? "positivo" : "neutro"}>
                              {v && (
                                <CheckCircle2 className="mr-1 h-3.5 w-3.5" />
                              )}
                              {v ? "Sí" : "No"}
                            </Insignia>
                          </td>
                        ),
                      )}
                      <td className="px-4 py-4">
                        <Insignia
                          tono={
                            p.estado.toLowerCase() === "activo"
                              ? "positivo"
                              : "negativo"
                          }
                        >
                          {p.estado.toLowerCase() === "activo" && (
                            <CheckCircle2
                              className="mr-1 h-3.5 w-3.5"
                              aria-hidden="true"
                            />
                          )}
                          {p.estado}
                        </Insignia>
                      </td>
                      <td className="whitespace-nowrap px-4 py-4 text-right">
                        <GrupoAccionesTabla>
                          <BotonAccionTabla
                            tipo="ver"
                            etiqueta={`Ver detalle de ${p.nombre}`}
                            onClick={() =>
                              void cargarDetalle(p).then(
                                (d) => d && setDetalle(d),
                              )
                            }
                          />
                          {puedeAdministrar && (
                            <>
                              <BotonAccionTabla
                                tipo="editar"
                                etiqueta={`Editar ${p.nombre}`}
                                onClick={() => void abrirFormulario(p)}
                              />
                              <BotonAccionTabla
                                tipo={
                                  p.estado.toLowerCase() === "activo"
                                    ? "desactivar"
                                    : "activar"
                                }
                                etiqueta={`${p.estado.toLowerCase() === "activo" ? "Desactivar" : "Activar"} ${p.nombre}`}
                                onClick={() => {
                                  setCambio(p);
                                  setEstadoDestino("");
                                }}
                              />
                            </>
                          )}
                        </GrupoAccionesTabla>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Paginacion
              pagina={datos.pagina}
              totalPaginas={datos.totalPaginas}
              totalRegistros={datos.totalRegistros}
              cambiar={setPagina}
              tamanioPagina={tamanioPagina}
              cambiarTamanio={(nuevoTamanio) => {
                setTamanioPagina(nuevoTamanio);
                setPagina(1);
              }}
              entidad="producto"
            />
          </>
        )}
      </Tarjeta>
      <Modal
        abierto={modal}
        cerrar={() => !guardando && setModal(false)}
        titulo={editado ? "Editar producto" : "Nuevo producto"}
        descripcion={
          editado
            ? "Actualiza la información comercial e inventariable del producto."
            : "Completa la información para registrar un nuevo producto en el sistema."
        }
        icono={<Package className="h-5 w-5" />}
        ancho="max-w-4xl"
        footer={
          <>
            <Boton
              type="button"
              variante="secundario"
              disabled={guardando}
              onClick={() => setModal(false)}
            >
              Cancelar
            </Boton>
            <Boton type="submit" form={formularioId} cargando={guardando}>
              <Save className="h-4 w-4" aria-hidden="true" /> Guardar
            </Boton>
          </>
        }
      >
        <form
          id={formularioId}
          className="space-y-4 p-5 pt-4 sm:p-6 sm:pt-4 [&_input:not([role=switch])]:min-h-10 [&_input:not([role=switch])]:py-2 [&_select]:min-h-10 [&_select]:py-2"
          onSubmit={guardar}
        >
          <SeccionFormulario
            compacto
            titulo="Información general"
            descripcion="Datos básicos y clasificación del producto."
            icono={<Tag className="h-4 w-4" />}
          >
            <Entrada
              etiqueta="Nombre del producto *"
              maxLength={200}
              placeholder="Ej. Helado de Chocolate"
              value={String(formulario.nombre)}
              onChange={(e) => setCampo("nombre", e.target.value)}
            />
            <Entrada
              etiqueta="SKU / Código *"
              maxLength={50}
              placeholder="Ej. HEL-CHO-001"
              value={String(formulario.sku)}
              onChange={(e) => setCampo("sku", e.target.value)}
            />
            {clasificacion.map(([k, label, ruta, actualKey]) => (
              <SelectorEntidad
                key={k}
                compacto
                etiqueta={label}
                ruta={`/api/productos/${ruta}`}
                value={String(formulario[k])}
                opcional={k === "marcaId"}
                placeholder={`Buscar o seleccionar ${label.replace(" *", "").toLowerCase()}…`}
                etiquetaActual={
                  editado ? String(editado[actualKey] ?? "") : undefined
                }
                onChange={(v) => setCampo(k, v ? Number(v) : "")}
              />
            ))}
            <div className="sm:col-span-2">
              <AreaTexto
                etiqueta="Descripción"
                rows={2}
                maxLength={1000}
                value={String(formulario.descripcion)}
                onChange={(e) => setCampo("descripcion", e.target.value)}
              />
            </div>
            <Entrada
              etiqueta="Modelo"
              maxLength={100}
              value={String(formulario.modelo)}
              onChange={(e) => setCampo("modelo", e.target.value)}
            />
            <Entrada
              etiqueta="Peso"
              type="number"
              min="0"
              step="0.000001"
              value={String(formulario.peso)}
              onChange={(e) => setCampo("peso", e.target.value)}
            />
            <div className="sm:col-span-2">
              <Entrada
                etiqueta="Imagen (URL o ruta)"
                maxLength={300}
                value={String(formulario.imagen)}
                onChange={(e) => setCampo("imagen", e.target.value)}
              />
            </div>
          </SeccionFormulario>
          <SeccionFormulario
            compacto
            titulo="Control de inventario"
            descripcion="Define cómo se gestiona el inventario de este producto."
            icono={<Database className="h-4 w-4" />}
          >
            {(
              [
                [
                  "tipoSeguimientoId",
                  "Tipo de seguimiento *",
                  referencias.tiposSeguimiento,
                ],
                [
                  "metodoValuacionId",
                  "Método de valuación *",
                  referencias.metodosValuacion,
                ],
              ] as const
            ).map(([k, label, opciones]) => (
              <Seleccion
                key={k}
                etiqueta={label}
                value={String(formulario[k])}
                onChange={(e) => setCampo(k, Number(e.target.value))}
              >
                <option value="">Selecciona una opción…</option>
                {opciones.map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.codigo} · {o.nombre}
                  </option>
                ))}
              </Seleccion>
            ))}
            <SwitchCampo
              sinTarjeta
              className="sm:col-span-2"
              etiqueta="Controla vencimiento"
              descripcion="Activa esta opción si el producto tiene fecha de vencimiento."
              activo={Boolean(formulario.controlaVencimiento)}
              cambiar={(v) => setCampo("controlaVencimiento", v)}
            />
          </SeccionFormulario>
          <SeccionFormulario
            compacto
            titulo="Configuración comercial"
            descripcion="Define la disponibilidad y precios del producto."
            icono={<Tag className="h-4 w-4" />}
          >
            <div className="grid gap-2.5 sm:col-span-2 sm:grid-cols-2 md:grid-cols-4">
              {(
                [
                  ["esComprable", "Comprable", "Puede ser comprado."],
                  ["esVendible", "Vendible", "Puede ser vendido."],
                  [
                    "permiteDescuento",
                    "Permite descuento",
                    "Aplica descuentos.",
                  ],
                ] as const
              ).map(([k, label, descripcion]) => (
                <SwitchCampo
                  key={k}
                  compacto
                  etiqueta={label}
                  descripcion={descripcion}
                  activo={Boolean(formulario[k])}
                  cambiar={(v) => setCampo(k, v)}
                />
              ))}
            </div>
            <Entrada
              etiqueta="Precio de venta referencial"
              type="number"
              min="0"
              step="0.01"
              value={String(formulario.precioVentaReferencial)}
              onChange={(e) =>
                setCampo("precioVentaReferencial", e.target.value)
              }
            />
            <SelectorEntidad
              compacto
              sinIcono
              placeholder="Selecciona un estado…"
              etiqueta="Estado *"
              ruta="/api/configuracion/estados"
              value={String(formulario.estadoId)}
              etiquetaActual={editado?.estado}
              onChange={(v) => setCampo("estadoId", v ? Number(v) : "")}
            />
          </SeccionFormulario>
          <ComposicionVenta filas={componentes} cambiar={cambiarComponente} deshabilitado={guardando}
            agregar={() => setComponentes((filas) => [...filas, { clave: crypto.randomUUID(), productoId: 0,
              productoPresentacionId: null, cantidad: "1", nombre: "", presentacion: null }])}
            quitar={(clave) => setComponentes((filas) => filas.filter((f) => f.clave !== clave))} />
          {errorFormulario && (
            <div
              role="alert"
              className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700"
            >
              {errorFormulario}
              {editado && (
                <Boton
                  type="button"
                  variante="sutil"
                  onClick={() => void abrirFormulario(editado)}
                >
                  Recargar detalle
                </Boton>
              )}
            </div>
          )}
        </form>
      </Modal>
      <Modal
        abierto={Boolean(detalle)}
        cerrar={() => setDetalle(null)}
        titulo={detalle?.nombre ?? "Detalle"}
        descripcion={detalle?.sku}
        icono={<Package className="h-5 w-5" />}
        ancho="max-w-6xl"
      >
        {detalle && (
          <div className="space-y-6 p-6">
            <div className="grid gap-4 md:grid-cols-3">
              <Panel
                titulo="Información general"
                filas={[
                  ["Descripción", detalle.descripcion ?? "—"],
                  ["Modelo", detalle.modelo ?? "—"],
                  [
                    "Peso",
                    detalle.peso == null
                      ? "—"
                      : formatoNumero.format(detalle.peso),
                  ],
                ]}
              />
              <Panel
                titulo="Clasificación"
                filas={[
                  ["Categoría", detalle.categoria],
                  ["Marca", detalle.marca ?? "—"],
                  ["Tipo", detalle.tipoProducto],
                ]}
              />
              <Panel
                titulo="Inventario, precio y estado"
                filas={[
                  [
                    "Unidad base",
                    `${detalle.unidadBase} (${detalle.abreviaturaUnidadBase})`,
                  ],
                  ["Seguimiento", detalle.tipoSeguimiento],
                  ["Valuación", detalle.metodoValuacion],
                  [
                    "Precio referencial",
                    detalle.precioVentaReferencial == null
                      ? "—"
                      : formatoCosto.format(detalle.precioVentaReferencial),
                  ],
                  ["Estado", detalle.estado],
                ]}
              />
            </div>
            <PresentacionesProducto producto={detalle} />
            {!!detalle.componentes?.length && <Panel titulo="Composición de venta"
              filas={detalle.componentes.map((c) => [`${c.producto} · ${c.presentacion || "Unidad base"}`,
                `${formatoNumero.format(c.cantidad)} ${c.abreviaturaUnidad} = ${formatoNumero.format(c.cantidadBase)} ${c.abreviaturaUnidadBase}`])} />}
          </div>
        )}
      </Modal>
      <DialogoConfirmacion
        abierto={Boolean(cambio)}
        titulo={
          cambio?.estado.toLowerCase() === "activo"
            ? "Desactivar producto"
            : "Activar producto"
        }
        cerrar={() => setCambio(null)}
        confirmar={confirmarEstado}
        cargando={guardando}
        peligro={cambio?.estado.toLowerCase() === "activo"}
        textoConfirmar={
          cambio?.estado.toLowerCase() === "activo" ? "Desactivar" : "Activar"
        }
      >
        <p className="text-sm text-slate-600">
          {cambio?.estado.toLowerCase() === "activo"
            ? "¿Está seguro de que desea desactivar este producto?"
            : "¿Está seguro de que desea activar este producto?"}
        </p>
        <SelectorEntidad
          etiqueta="Estado destino"
          ruta="/api/configuracion/estados"
          value={estadoDestino}
          onChange={(v) => setEstadoDestino(v ?? "")}
        />
      </DialogoConfirmacion>
    </div>
  );
}

function MiniaturaProducto({ imagen }: { imagen: string | null }) {
  const [imagenFallida, setImagenFallida] = useState<string | null>(null);
  return (
    <span
      className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-teal-50 text-teal-700"
      aria-hidden="true"
    >
      {imagen && imagen !== imagenFallida ? (
        <img
          src={imagen}
          alt=""
          className="h-full w-full object-cover"
          onError={() => setImagenFallida(imagen)}
        />
      ) : (
        <Package className="h-6 w-6" />
      )}
    </span>
  );
}

function Panel({
  titulo,
  filas,
}: {
  titulo: string;
  filas: [string, string][];
}) {
  return (
    <section className="rounded-xl border border-slate-200 p-4">
      <h3 className="font-bold text-slate-900">{titulo}</h3>
      <dl className="mt-3 space-y-3">
        {filas.map(([k, v]) => (
          <div key={k} className="text-sm">
            <dt className="text-xs text-slate-500">{k}</dt>
            <dd className="mt-1 font-medium text-slate-800">{v}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
