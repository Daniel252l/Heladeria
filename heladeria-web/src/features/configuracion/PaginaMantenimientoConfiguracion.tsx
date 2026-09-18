import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { Plus, Search } from "lucide-react";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import {
  Boton,
  Entrada,
  Insignia,
  Seleccion,
  Tarjeta,
  AreaTexto,
} from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import {
  BotonAccionTabla,
  GrupoAccionesTabla,
} from "../../components/ui/BotonAccionTabla";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../../components/ui/Estados";
import { Modal } from "../../components/ui/Modal";
import { Paginacion } from "../../components/ui/Paginacion";
import { SeccionFormulario } from "../../components/ui/SeccionFormulario";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { useToast } from "../../components/toast/useToast";
import type { Paginado, Estado } from "../../types/api";
import type {
  RegistroConfiguracion,
  TipoMantenimientoConfiguracion,
} from "../../types/configuracion";

type ValorFormulario = string | number | boolean | null;
type Formulario = Record<string, ValorFormulario>;
type TipoCampo =
  "texto" | "email" | "url" | "numero" | "area" | "booleano" | "entidad";

interface CampoDef {
  clave: string;
  etiqueta: string;
  tipo?: TipoCampo;
  requerido?: boolean;
  seccion?: string;
  maxLength?: number;
  opcional?: boolean;
  soloCrear?: boolean;
  ruta?: (formulario: Formulario) => string;
  claveEtiquetaActual?: string;
}

interface ColumnaDef {
  clave: string;
  etiqueta: string;
  tipo?: "texto" | "estado" | "booleano";
}

interface Definicion {
  titulo: string;
  singular: string;
  descripcion: string;
  ruta: string;
  campos: CampoDef[];
  columnas: ColumnaDef[];
  estado?: "activo" | "estado";
  construir?: (
    formulario: Formulario,
    actual: RegistroConfiguracion | null,
  ) => Record<string, unknown>;
}

const estado = (clave: string, etiqueta = "Estado"): CampoDef => ({
  clave,
  etiqueta,
  tipo: "entidad",
  requerido: true,
  ruta: () => "/api/configuracion/estados",
});
const definiciones: Record<TipoMantenimientoConfiguracion, Definicion> = {
  "clases-estados": {
    titulo: "Clases de estados",
    singular: "clase de estado",
    descripcion: "Agrupe los estados por su propósito funcional.",
    ruta: "/api/configuracion/clases-estado",
    estado: "activo",
    campos: [
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 200 },
      { clave: "activo", etiqueta: "Activo", tipo: "booleano" },
    ],
    columnas: [
      { clave: "id", etiqueta: "Id" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "activo", etiqueta: "Activo", tipo: "booleano" },
    ],
  },
  estados: {
    titulo: "Estados",
    singular: "estado",
    descripcion: "Configure estados operativos y su alcance por módulo.",
    ruta: "/api/configuracion/estados",
    estado: "activo",
    campos: [
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 100 },
      {
        clave: "claseEstadoId",
        etiqueta: "Clase de estado",
        tipo: "entidad",
        requerido: true,
        ruta: () => "/api/configuracion/clases-estado?activo=true",
        claveEtiquetaActual: "claseEstado",
      },
      {
        clave: "moduloId",
        etiqueta: "Módulo",
        tipo: "entidad",
        opcional: true,
        ruta: () => "/api/configuracion/modulos?activo=true",
      },
      { clave: "activo", etiqueta: "Activo", tipo: "booleano" },
    ],
    columnas: [
      { clave: "id", etiqueta: "Id" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "claseEstado", etiqueta: "Clase de estado" },
      { clave: "moduloId", etiqueta: "Módulo" },
      { clave: "activo", etiqueta: "Activo", tipo: "booleano" },
    ],
  },
  "clases-tipos": {
    titulo: "Clases de tipos",
    singular: "clase de tipo",
    descripcion: "Organice los tipos utilizados por la configuración.",
    ruta: "/api/configuracion/clases-tipo",
    estado: "estado",
    campos: [
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 100 },
      estado("estadoId"),
    ],
    columnas: [
      { clave: "id", etiqueta: "Id" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "estado", etiqueta: "Estado", tipo: "estado" },
    ],
  },
  tipos: {
    titulo: "Tipos",
    singular: "tipo",
    descripcion: "Mantenga los clasificadores vinculados con clases y módulos.",
    ruta: "/api/configuracion/tipos",
    estado: "estado",
    campos: [
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 100 },
      {
        clave: "claseTipoId",
        etiqueta: "Clase de tipo",
        tipo: "entidad",
        requerido: true,
        ruta: () => "/api/configuracion/clases-tipo",
        claveEtiquetaActual: "claseTipo",
      },
      {
        clave: "moduloId",
        etiqueta: "Módulo",
        tipo: "entidad",
        opcional: true,
        ruta: () => "/api/configuracion/modulos?activo=true",
      },
      estado("estadoId"),
    ],
    columnas: [
      { clave: "id", etiqueta: "Id" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "claseTipo", etiqueta: "Clase de tipo" },
      { clave: "moduloId", etiqueta: "Módulo" },
      { clave: "estado", etiqueta: "Estado", tipo: "estado" },
    ],
  },
  modulos: {
    titulo: "Módulos",
    singular: "módulo",
    descripcion: "Administre los identificadores funcionales del sistema.",
    ruta: "/api/configuracion/modulos",
    estado: "activo",
    campos: [
      {
        clave: "id",
        etiqueta: "Código",
        requerido: true,
        maxLength: 20,
        soloCrear: true,
      },
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 50 },
      { clave: "icono", etiqueta: "Icono", requerido: true, maxLength: 50 },
      {
        clave: "activo",
        etiqueta: "Activo",
        tipo: "booleano",
        soloCrear: true,
      },
    ],
    columnas: [
      { clave: "id", etiqueta: "Código" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "icono", etiqueta: "Icono" },
      { clave: "activo", etiqueta: "Activo", tipo: "booleano" },
    ],
    construir: (f, actual) =>
      actual ? { nombre: f.nombre, icono: f.icono } : f,
  },
  acciones: {
    titulo: "Acciones",
    singular: "acción",
    descripcion: "Defina las operaciones reutilizadas por los permisos.",
    ruta: "/api/configuracion/acciones-sistema",
    estado: "estado",
    campos: [
      { clave: "codigo", etiqueta: "Código", requerido: true, maxLength: 50 },
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 100 },
      estado("estadoId"),
    ],
    columnas: [
      { clave: "codigo", etiqueta: "Código" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "estado", etiqueta: "Estado", tipo: "estado" },
    ],
  },
  "niveles-acceso": {
    titulo: "Niveles de acceso",
    singular: "nivel de acceso",
    descripcion: "Configure la jerarquía nominal de acceso.",
    ruta: "/api/configuracion/niveles-acceso",
    campos: [
      { clave: "nombre", etiqueta: "Nombre", requerido: true, maxLength: 100 },
    ],
    columnas: [
      { clave: "id", etiqueta: "Id" },
      { clave: "nombre", etiqueta: "Nombre" },
    ],
  },
  empresas: {
    titulo: "Empresas",
    singular: "empresa",
    descripcion:
      "Administre identidad legal, contacto y estado de cada empresa.",
    ruta: "/api/configuracion/empresas",
    estado: "estado",
    campos: [
      {
        clave: "razonSocial",
        etiqueta: "Razón social",
        requerido: true,
        seccion: "Identidad",
        maxLength: 150,
      },
      {
        clave: "nombreComercial",
        etiqueta: "Nombre comercial",
        seccion: "Identidad",
        maxLength: 150,
      },
      {
        clave: "eslogan",
        etiqueta: "Eslogan",
        requerido: true,
        seccion: "Identidad",
        maxLength: 150,
      },
      {
        clave: "nit",
        etiqueta: "NIT",
        requerido: true,
        seccion: "Identidad",
        maxLength: 25,
      },
      { clave: "dpi", etiqueta: "DPI", seccion: "Identidad", maxLength: 25 },
      {
        clave: "direccion",
        etiqueta: "Dirección",
        tipo: "area",
        requerido: true,
        seccion: "Contacto",
        maxLength: 250,
      },
      {
        clave: "telefono",
        etiqueta: "Teléfono",
        requerido: true,
        seccion: "Contacto",
        maxLength: 100,
      },
      {
        clave: "email",
        etiqueta: "Correo",
        tipo: "email",
        requerido: true,
        seccion: "Contacto",
        maxLength: 100,
      },
      {
        clave: "url",
        etiqueta: "Sitio web",
        tipo: "url",
        seccion: "Contacto",
        maxLength: 150,
      },
      {
        clave: "clienteId",
        etiqueta: "Tipo de cliente",
        tipo: "entidad",
        opcional: true,
        seccion: "Configuración",
        ruta: () => "/api/configuracion/tipos",
      },
      { ...estado("estadoId"), seccion: "Configuración" },
    ],
    columnas: [
      { clave: "razonSocial", etiqueta: "Razón social" },
      { clave: "nombreComercial", etiqueta: "Nombre comercial" },
      { clave: "nit", etiqueta: "NIT" },
      { clave: "telefono", etiqueta: "Teléfono" },
      { clave: "email", etiqueta: "Correo" },
      { clave: "estado", etiqueta: "Estado", tipo: "estado" },
    ],
    construir: (f, actual) => ({
      ...f,
      imagen: actual?.imagen ?? "sin-imagen",
      icono: actual?.icono ?? "sin-icono",
      imagenReportes: actual?.imagenReportes ?? "sin-imagen",
    }),
  },
  sucursales: {
    titulo: "Sucursales",
    singular: "sucursal",
    descripcion:
      "Configure sedes, relaciones organizativas y capacidades operativas.",
    ruta: "/api/configuracion/sucursales",
    estado: "estado",
    campos: [
      {
        clave: "codigo",
        etiqueta: "Código",
        seccion: "Identidad",
        maxLength: 25,
      },
      {
        clave: "nombre",
        etiqueta: "Nombre",
        requerido: true,
        seccion: "Identidad",
        maxLength: 150,
      },
      {
        clave: "empresaId",
        etiqueta: "Empresa",
        tipo: "entidad",
        requerido: true,
        seccion: "Identidad",
        ruta: () => "/api/configuracion/empresas",
        claveEtiquetaActual: "empresa",
      },
      { ...estado("estadoId"), seccion: "Identidad" },
      {
        clave: "direccion",
        etiqueta: "Dirección",
        tipo: "area",
        requerido: true,
        seccion: "Ubicación",
        maxLength: 250,
      },
      {
        clave: "direccionCorta",
        etiqueta: "Dirección corta",
        requerido: true,
        seccion: "Ubicación",
        maxLength: 50,
      },
      {
        clave: "departamentoId",
        etiqueta: "Departamento",
        tipo: "entidad",
        opcional: true,
        seccion: "Ubicación",
        ruta: () =>
          "/api/configuracion/referencias-organizacion?tipo=DEPARTAMENTOS",
      },
      {
        clave: "municipioId",
        etiqueta: "Municipio",
        tipo: "entidad",
        opcional: true,
        seccion: "Ubicación",
        ruta: (f) =>
          `/api/configuracion/referencias-organizacion?tipo=MUNICIPIOS${f.departamentoId ? `&departamentoId=${f.departamentoId}` : ""}`,
      },
      {
        clave: "telefono",
        etiqueta: "Teléfono",
        requerido: true,
        seccion: "Contacto",
        maxLength: 100,
      },
      {
        clave: "email",
        etiqueta: "Correo",
        tipo: "email",
        requerido: true,
        seccion: "Contacto",
        maxLength: 100,
      },
      {
        clave: "representanteLegal",
        etiqueta: "Representante legal",
        seccion: "Contacto",
        maxLength: 100,
      },
      {
        clave: "proveedorId",
        etiqueta: "Proveedor",
        tipo: "entidad",
        requerido: true,
        seccion: "Relaciones",
        ruta: (f) =>
          f.empresaId
            ? `/api/configuracion/referencias-organizacion?tipo=PROVEEDORES&empresaId=${f.empresaId}`
            : "",
        claveEtiquetaActual: "proveedor",
      },
      {
        clave: "clienteId",
        etiqueta: "Cliente",
        tipo: "entidad",
        opcional: true,
        seccion: "Relaciones",
        ruta: (f) =>
          f.empresaId
            ? `/api/configuracion/referencias-organizacion?tipo=CLIENTES&empresaId=${f.empresaId}`
            : "",
      },
      {
        clave: "sucursalCentralId",
        etiqueta: "Sucursal central relacionada",
        tipo: "entidad",
        opcional: true,
        seccion: "Relaciones",
        ruta: (f) =>
          f.empresaId
            ? `/api/configuracion/sucursales?empresaId=${f.empresaId}`
            : "",
      },
      {
        clave: "sucursalTransporteId",
        etiqueta: "Sucursal de transporte",
        tipo: "entidad",
        opcional: true,
        seccion: "Relaciones",
        ruta: (f) =>
          f.empresaId
            ? `/api/configuracion/sucursales?empresaId=${f.empresaId}`
            : "",
      },
      {
        clave: "esCentral",
        etiqueta: "Es sucursal central",
        tipo: "booleano",
        seccion: "Capacidades",
      },
      {
        clave: "esBodega",
        etiqueta: "Opera como bodega",
        tipo: "booleano",
        seccion: "Capacidades",
      },
      {
        clave: "transportes",
        etiqueta: "Gestiona transportes",
        tipo: "booleano",
        seccion: "Capacidades",
      },
    ],
    columnas: [
      { clave: "codigo", etiqueta: "Código" },
      { clave: "nombre", etiqueta: "Nombre" },
      { clave: "empresa", etiqueta: "Empresa" },
      { clave: "direccionCorta", etiqueta: "Dirección corta" },
      { clave: "telefono", etiqueta: "Teléfono" },
      { clave: "email", etiqueta: "Correo" },
      { clave: "esCentral", etiqueta: "Central", tipo: "booleano" },
      { clave: "esBodega", etiqueta: "Bodega", tipo: "booleano" },
      { clave: "estado", etiqueta: "Estado", tipo: "estado" },
    ],
    construir: (f, actual) => ({
      ...f,
      imagen: actual?.imagen ?? null,
      imagenReportes: actual?.imagenReportes ?? null,
    }),
  },
};

export function PaginaMantenimientoConfiguracion({
  tipo,
}: {
  tipo: TipoMantenimientoConfiguracion;
}) {
  const def = definiciones[tipo];
  const [datos, setDatos] = useState<Paginado<RegistroConfiguracion> | null>(
    null,
  );
  const [estados, setEstados] = useState<Estado[]>([]);
  const [pagina, setPagina] = useState(1);
  const [texto, setTexto] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [filtros, setFiltros] = useState<Record<string, string>>({});
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actual, setActual] = useState<RegistroConfiguracion | null>(null);
  const [formulario, setFormulario] = useState<Formulario>({});
  const [modal, setModal] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [cambioEstado, setCambioEstado] =
    useState<RegistroConfiguracion | null>(null);
  const { mostrar } = useToast();

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    const params = new URLSearchParams({
      pagina: String(pagina),
      tamanioPagina: "10",
    });
    if (busqueda) params.set("busqueda", busqueda);
    for (const [k, v] of Object.entries(filtros)) if (v) params.set(k, v);
    try {
      const [respuesta, estadosRespuesta] = await Promise.all([
        solicitar<Paginado<RegistroConfiguracion>>(`${def.ruta}?${params}`),
        solicitar<Paginado<Estado>>(
          "/api/configuracion/estados?pagina=1&tamanioPagina=100",
        ),
      ]);
      setDatos(respuesta);
      setEstados(estadosRespuesta.elementos);
    } catch (excepcion) {
      setError(
        excepcion instanceof Error
          ? excepcion.message
          : "No fue posible cargar los registros.",
      );
    } finally {
      setCargando(false);
    }
  }, [busqueda, def.ruta, filtros, pagina]);
  useEffect(() => {
    // La consulta remota sincroniza búsqueda, filtros y paginación.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);

  const secciones = useMemo(
    () => [
      ...new Set(def.campos.map((campo) => campo.seccion ?? "Datos generales")),
    ],
    [def],
  );
  const abrirNuevo = () => {
    const inicial: Formulario = {};
    for (const campo of def.campos)
      inicial[campo.clave] =
        campo.tipo === "booleano" ? campo.clave === "activo" : "";
    const activo = estados.find(
      (item) => item.nombre.toLowerCase() === "activo",
    );
    if ("estadoId" in inicial)
      inicial.estadoId = activo?.id ?? estados[0]?.id ?? "";
    setActual(null);
    setFormulario(inicial);
    setErrorFormulario(null);
    setModal(true);
  };
  const abrirEditar = async (registro: RegistroConfiguracion) => {
    try {
      const detalle = await solicitar<RegistroConfiguracion>(
        `${def.ruta}/${encodeURIComponent(String(registro.id))}`,
      );
      const valores: Formulario = {};
      for (const campo of def.campos)
        valores[campo.clave] =
          (detalle[campo.clave] as ValorFormulario | undefined) ??
          (campo.tipo === "booleano" ? false : "");
      setActual(detalle);
      setFormulario(valores);
      setErrorFormulario(null);
      setModal(true);
    } catch (excepcion) {
      mostrar({
        tipo: "error",
        titulo: "No fue posible cargar el detalle",
        descripcion: excepcion instanceof Error ? excepcion.message : undefined,
      });
    }
  };
  const guardar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErrorFormulario(null);
    const faltante =
      def.campos.find(
        (campo) =>
          campo.requerido && !campo.soloCrear && !formulario[campo.clave],
      ) ??
      def.campos.find(
        (campo) => campo.requerido && !actual && !formulario[campo.clave],
      );
    if (faltante) {
      setErrorFormulario(`${faltante.etiqueta} es obligatorio.`);
      return;
    }
    setGuardando(true);
    try {
      let cuerpo: Record<string, unknown> = {};
      for (const campo of def.campos) {
        if (actual && campo.soloCrear) continue;
        const valor = formulario[campo.clave];
        cuerpo[campo.clave] =
          campo.tipo === "numero" || campo.tipo === "entidad"
            ? valor === "" || valor == null
              ? null
              : campo.clave === "moduloId"
                ? valor
                : Number(valor)
            : typeof valor === "string"
              ? valor.trim() || null
              : valor;
      }
      cuerpo = def.construir?.(cuerpo as Formulario, actual) ?? cuerpo;
      await solicitar(
        actual
          ? `${def.ruta}/${encodeURIComponent(String(actual.id))}`
          : def.ruta,
        { method: actual ? "PUT" : "POST", body: JSON.stringify(cuerpo) },
      );
      mostrar({
        tipo: "exito",
        titulo: actual ? "Registro actualizado" : "Registro creado",
        descripcion: `La ${def.singular} se guardó correctamente.`,
      });
      setModal(false);
      await cargar();
    } catch (excepcion) {
      setErrorFormulario(
        excepcion instanceof ErrorApi
          ? excepcion.message
          : "No fue posible guardar el registro.",
      );
    } finally {
      setGuardando(false);
    }
  };

  const cambiarCampo = (clave: string, valor: ValorFormulario) =>
    setFormulario((f) => ({
      ...f,
      ...(clave === "empresaId"
        ? {
            proveedorId: "",
            clienteId: "",
            sucursalCentralId: "",
            sucursalTransporteId: "",
          }
        : {}),
      ...(clave === "departamentoId" ? { municipioId: "" } : {}),
      [clave]: valor,
    }));
  const camposFiltro = def.campos.filter((campo) =>
    [
      "estadoId",
      "claseEstadoId",
      "claseTipoId",
      "moduloId",
      "empresaId",
    ].includes(campo.clave),
  );
  const confirmarEstado = async () => {
    if (!cambioEstado || !def.estado) return;
    setGuardando(true);
    try {
      const esActivo =
        def.estado === "activo"
          ? Boolean(cambioEstado.activo)
          : String(cambioEstado.estado).toLowerCase() === "activo";
      const destino = estados.find(
        (item) =>
          item.nombre.toLowerCase() === (esActivo ? "inactivo" : "activo"),
      );
      const cuerpo =
        def.estado === "activo"
          ? { activo: !esActivo }
          : { estadoId: destino?.id };
      if (def.estado === "estado" && !destino)
        throw new Error("No existe un estado alterno configurado.");
      await solicitar(
        `${def.ruta}/${encodeURIComponent(String(cambioEstado.id))}/estado`,
        { method: "PUT", body: JSON.stringify(cuerpo) },
      );
      mostrar({ tipo: "exito", titulo: "Estado actualizado" });
      setCambioEstado(null);
      await cargar();
    } catch (excepcion) {
      mostrar({
        tipo: "error",
        titulo: "No fue posible cambiar el estado",
        descripcion: excepcion instanceof Error ? excepcion.message : undefined,
      });
    } finally {
      setGuardando(false);
    }
  };

  return (
    <div className="space-y-6">
      <EncabezadoPagina
        titulo={def.titulo}
        descripcion={def.descripcion}
        acciones={
          <Boton onClick={abrirNuevo}>
            <Plus className="h-4 w-4" /> Nuevo
          </Boton>
        }
      />
      <Tarjeta className="overflow-hidden">
        <form
          className="flex gap-3 border-b border-slate-200 p-4"
          onSubmit={(e) => {
            e.preventDefault();
            setPagina(1);
            setBusqueda(texto.trim());
          }}
        >
          <div className="relative max-w-md flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              aria-label="Buscar registros"
              className="min-h-10 w-full rounded-lg border border-slate-300 pl-9 pr-3 text-sm"
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
              placeholder="Buscar…"
            />
          </div>
          <Boton variante="secundario">Buscar</Boton>
        </form>
        {(def.estado || camposFiltro.length > 0) && (
          <div className="grid gap-4 border-b p-4 md:grid-cols-3">
            {def.estado === "activo" && (
              <Seleccion
                etiqueta="Activo"
                value={filtros.activo ?? ""}
                onChange={(e) => {
                  setPagina(1);
                  setFiltros((f) => ({ ...f, activo: e.target.value }));
                }}
              >
                <option value="">Todos</option>
                <option value="true">Sí</option>
                <option value="false">No</option>
              </Seleccion>
            )}
            {camposFiltro.map((campo) => (
              <SelectorEntidad
                key={campo.clave}
                etiqueta={campo.etiqueta}
                ruta={campo.ruta?.({}) ?? ""}
                opcional
                value={filtros[campo.clave] ?? ""}
                onChange={(v) => {
                  setPagina(1);
                  setFiltros((f) => ({ ...f, [campo.clave]: v ?? "" }));
                }}
              />
            ))}
          </div>
        )}
        {cargando ? (
          <EstadoCargando />
        ) : error ? (
          <EstadoError mensaje={error} reintentar={cargar} />
        ) : !datos?.elementos.length ? (
          <EstadoVacio />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200 text-left text-sm">
                <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                  <tr>
                    {def.columnas.map((c) => (
                      <th key={c.clave} className="px-5 py-3">
                        {c.etiqueta}
                      </th>
                    ))}
                    <th className="px-5 py-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {datos.elementos.map((registro) => (
                    <tr key={String(registro.id)}>
                      {def.columnas.map((columna) => (
                        <td
                          key={columna.clave}
                          className="whitespace-nowrap px-5 py-4 text-slate-600"
                        >
                          {columna.tipo === "booleano" ? (
                            <Insignia
                              tono={
                                registro[columna.clave] ? "positivo" : "neutro"
                              }
                            >
                              {registro[columna.clave] ? "Sí" : "No"}
                            </Insignia>
                          ) : columna.tipo === "estado" ? (
                            <Insignia
                              tono={
                                String(
                                  registro[columna.clave],
                                ).toLowerCase() === "activo"
                                  ? "positivo"
                                  : "neutro"
                              }
                            >
                              {String(registro[columna.clave] ?? "—")}
                            </Insignia>
                          ) : (
                            String(registro[columna.clave] ?? "—")
                          )}
                        </td>
                      ))}
                      <td className="whitespace-nowrap px-5 py-4 text-right">
                        <GrupoAccionesTabla>
                          <BotonAccionTabla
                            tipo="editar"
                            etiqueta={`Editar ${registro.nombre ?? registro.id}`}
                            onClick={() => void abrirEditar(registro)}
                          />
                          {def.estado && (
                            <BotonAccionTabla
                              tipo={
                                (
                                  def.estado === "activo"
                                    ? Boolean(registro.activo)
                                    : String(registro.estado).toLowerCase() ===
                                      "activo"
                                )
                                  ? "desactivar"
                                  : "activar"
                              }
                              etiqueta={`${(def.estado === "activo" ? Boolean(registro.activo) : String(registro.estado).toLowerCase() === "activo") ? "Desactivar" : "Activar"} ${registro.nombre ?? registro.id}`}
                              onClick={() => setCambioEstado(registro)}
                            />
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
            />
          </>
        )}
      </Tarjeta>
      <Modal
        abierto={modal}
        cerrar={() => !guardando && setModal(false)}
        titulo={actual ? `Editar ${def.singular}` : `Nueva ${def.singular}`}
        ancho="max-w-4xl"
      >
        <form className="space-y-5 p-6" onSubmit={guardar}>
          {secciones.map((seccion) => (
            <SeccionFormulario key={seccion} titulo={seccion}>
              {def.campos
                .filter(
                  (c) =>
                    (c.seccion ?? "Datos generales") === seccion &&
                    (!actual || !c.soloCrear),
                )
                .map((campo) => {
                  const etiqueta = `${campo.etiqueta}${campo.requerido ? " *" : ""}`;
                  if (campo.tipo === "booleano")
                    return (
                      <label
                        key={campo.clave}
                        className="flex items-center gap-3 rounded-lg border border-slate-200 p-4 text-sm font-medium"
                      >
                        <input
                          type="checkbox"
                          checked={Boolean(formulario[campo.clave])}
                          onChange={(e) =>
                            cambiarCampo(campo.clave, e.target.checked)
                          }
                        />
                        {campo.etiqueta}
                      </label>
                    );
                  if (campo.tipo === "entidad" && campo.ruta)
                    return (
                      <SelectorEntidad
                        key={campo.clave}
                        etiqueta={etiqueta}
                        ruta={campo.ruta(formulario)}
                        value={String(formulario[campo.clave] ?? "")}
                        opcional={campo.opcional}
                        deshabilitado={
                          !campo.ruta(formulario) ||
                          Boolean(
                            actual &&
                            tipo === "sucursales" &&
                            campo.clave === "empresaId",
                          )
                        }
                        etiquetaActual={
                          campo.claveEtiquetaActual
                            ? String(actual?.[campo.claveEtiquetaActual] ?? "")
                            : undefined
                        }
                        onChange={(valor) =>
                          cambiarCampo(campo.clave, valor ?? "")
                        }
                      />
                    );
                  if (campo.tipo === "area")
                    return (
                      <AreaTexto
                        key={campo.clave}
                        etiqueta={etiqueta}
                        value={String(formulario[campo.clave] ?? "")}
                        maxLength={campo.maxLength}
                        onChange={(e) =>
                          cambiarCampo(campo.clave, e.target.value)
                        }
                      />
                    );
                  return (
                    <Entrada
                      key={campo.clave}
                      etiqueta={etiqueta}
                      type={
                        campo.tipo === "numero"
                          ? "number"
                          : campo.tipo === "email" || campo.tipo === "url"
                            ? campo.tipo
                            : "text"
                      }
                      value={String(formulario[campo.clave] ?? "")}
                      maxLength={campo.maxLength}
                      onChange={(e) =>
                        cambiarCampo(campo.clave, e.target.value)
                      }
                    />
                  );
                })}
            </SeccionFormulario>
          ))}
          {errorFormulario && (
            <div
              role="alert"
              className="rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700"
            >
              {errorFormulario}
            </div>
          )}
          <div className="flex justify-end gap-3">
            <Boton
              type="button"
              variante="secundario"
              disabled={guardando}
              onClick={() => setModal(false)}
            >
              Cancelar
            </Boton>
            <Boton cargando={guardando}>Guardar</Boton>
          </div>
        </form>
      </Modal>
      <DialogoConfirmacion
        abierto={Boolean(cambioEstado)}
        cerrar={() => setCambioEstado(null)}
        confirmar={confirmarEstado}
        cargando={guardando}
        peligro
        titulo="Confirmar cambio de estado"
      >
        <p className="text-sm text-slate-600">
          El backend comprobará las dependencias estructurales antes de cambiar{" "}
          <strong>{cambioEstado?.nombre ?? cambioEstado?.id}</strong>.
        </p>
      </DialogoConfirmacion>
    </div>
  );
}
