import { soloOperacionPos } from '../auth/inicioOperativo';
import { useMemo, useState } from "react";
import {
  Boxes,
  ChartNoAxesCombined,
  BookOpen,
  Factory,
  Building2,
  ChevronDown,
  ChevronRight,
  CircleGauge,
  FolderTree,
  IceCreamBowl,
  Layers3,
  LayoutDashboard,
  LogOut,
  Menu,
  Monitor,
  ReceiptText,
  Package,
  PanelLeftClose,
  PanelLeftOpen,
  Ruler,
  ShoppingCart,
  Settings2,
  ShieldCheck,
  Tags,
  UserRound,
  Truck,
  Warehouse,
  Wallet,
  X,
  type LucideIcon,
} from "lucide-react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { PERMISOS } from "../auth/permisos";
import { useAutenticacion } from "../auth/useAutenticacion";
import { useToast } from "../components/toast/useToast";
import { clases } from "../utils/clases";
import { reportes } from "../features/reportes/configuracionReportes";

interface ElementoNavegacion {
  etiqueta: string;
  ruta: string;
  icono: LucideIcon;
  permiso?: string;
  exacta?: boolean;
}

interface GrupoNavegacion {
  etiqueta: string;
  icono: LucideIcon;
  permiso?: string;
  elementos: ElementoNavegacion[];
}

const grupos: GrupoNavegacion[] = [
  {
    etiqueta: "Reportes", icono: ChartNoAxesCombined, permiso: PERMISOS.reportesConsultar,
    elementos: [{ etiqueta: "Reporte diario", ruta: "/reportes/diario", icono: ChartNoAxesCombined }, ...reportes.map(({ tipo, nombre }) => ({ etiqueta: `Reporte de ${nombre.toLocaleLowerCase("es")}`, ruta: `/reportes/${tipo}`, icono: ChartNoAxesCombined }))],
  },
  {
    etiqueta: "Auditoría", icono: ShieldCheck, permiso: PERMISOS.auditoriaConsultar,
    elementos: [{ etiqueta: "Auditoría", ruta: "/auditoria", icono: ShieldCheck }],
  },
  {
    etiqueta: "Cartera", icono: Wallet,
    elementos: [
      { etiqueta: "Cuentas por cobrar", ruta: "/cuentas-por-cobrar", icono: ReceiptText, permiso: PERMISOS.cxcConsultar },
      { etiqueta: "Cuentas por pagar", ruta: "/cuentas-por-pagar", icono: ReceiptText, permiso: PERMISOS.cxpConsultar },
    ],
  },
  {
    etiqueta: "Caja", icono: Wallet, permiso: PERMISOS.cajaConsultar,
    elementos: [
      { etiqueta: "Resumen de caja", ruta: "/caja", icono: Wallet, exacta: true },
      { etiqueta: "Cajas", ruta: "/caja/cajas", icono: Wallet },
      { etiqueta: "Sesiones de caja", ruta: "/caja/sesiones", icono: ReceiptText },
    ],
  },
  {
    etiqueta: "Ventas",
    icono: ReceiptText,
    elementos: [
      { etiqueta: "Ventas", ruta: "/ventas", icono: ReceiptText, permiso: PERMISOS.ventasConsultar },
      { etiqueta: "Punto de venta", ruta: "/pos", icono: Monitor, permiso: PERMISOS.posOperar },
    ],
  },
  {
    etiqueta: "Producción",
    icono: Factory,
    elementos: [
      { etiqueta: "Órdenes de producción", ruta: "/produccion", icono: Factory, permiso: PERMISOS.produccionConsultar },
      { etiqueta: "Recetas", ruta: "/recetas", icono: BookOpen, permiso: PERMISOS.recetasConsultar },
    ],
  },
  {
    etiqueta: "Compras",
    icono: ShoppingCart,
    elementos: [
      {
        etiqueta: "Órdenes de compra",
        ruta: "/compras",
        icono: ShoppingCart,
        permiso: PERMISOS.comprasConsultar,
      },
      {
        etiqueta: "Proveedores",
        ruta: "/proveedores",
        icono: Truck,
        permiso: PERMISOS.proveedoresConsultar,
      },
    ],
  },
  {
    etiqueta: "Configuración",
    icono: Settings2,
    permiso: PERMISOS.configuracionAdministrar,
    elementos: [
      {
        etiqueta: "Estados",
        ruta: "/configuracion/estados",
        icono: CircleGauge,
      },
      {
        etiqueta: "Clases de estados",
        ruta: "/configuracion/clases-estados",
        icono: FolderTree,
      },
      { etiqueta: "Tipos", ruta: "/configuracion/tipos", icono: Tags },
      {
        etiqueta: "Clases de tipos",
        ruta: "/configuracion/clases-tipos",
        icono: FolderTree,
      },
      { etiqueta: "Módulos", ruta: "/configuracion/modulos", icono: Layers3 },
      {
        etiqueta: "Acciones",
        ruta: "/configuracion/acciones",
        icono: ShieldCheck,
      },
      {
        etiqueta: "Niveles de acceso",
        ruta: "/configuracion/niveles-acceso",
        icono: ShieldCheck,
      },
      {
        etiqueta: "Empresas",
        ruta: "/configuracion/empresas",
        icono: Building2,
      },
      {
        etiqueta: "Sucursales",
        ruta: "/configuracion/sucursales",
        icono: Warehouse,
      },
    ],
  },
  {
    etiqueta: "Productos",
    icono: Package,
    permiso: PERMISOS.productosConsultar,
    elementos: [
      { etiqueta: "Productos", ruta: "/productos", icono: Package },
      {
        etiqueta: "Categorías",
        ruta: "/productos/categorias",
        icono: FolderTree,
      },
      {
        etiqueta: "Conversiones de unidad",
        ruta: "/productos/conversiones-unidad",
        icono: Ruler,
      },
      { etiqueta: "Marcas", ruta: "/productos/marcas", icono: Tags },
      { etiqueta: "Tipos de producto", ruta: "/productos/tipos", icono: Boxes },
      {
        etiqueta: "Unidades de medida",
        ruta: "/productos/unidades",
        icono: Ruler,
      },
      {
        etiqueta: "Presentaciones",
        ruta: "/productos/presentaciones",
        icono: Layers3,
      },
    ],
  },
  {
    etiqueta: "Inventario",
    icono: Warehouse,
    elementos: [
      {
        etiqueta: "Existencias",
        ruta: "/inventario/existencias",
        icono: Boxes,
        permiso: PERMISOS.inventarioConsultar,
      },
      {
        etiqueta: "Ajustes",
        ruta: "/inventario/ajustes",
        icono: Warehouse,
        permiso: PERMISOS.inventarioAdministrar,
      },
    ],
  },
];

const etiquetasRuta: Record<string, string> = {
  reportes: "Reportes",
  auditoria: "Auditoría",
  kardex: "Kardex",
  caja: "Caja",
  "productos-vendidos": "Productos vendidos",
  "cuentas-por-cobrar": "Cuentas por cobrar",
  "cuentas-por-pagar": "Cuentas por pagar",
  "registrar-factura": "Registrar factura de proveedor",
  dashboard: "Dashboard",
  ventas: "Ventas",
  pos: "Punto de venta",
  configuracion: "Configuración",
  estados: "Estados",
  "clases-estados": "Clases de estados",
  tipos: "Tipos",
  "clases-tipos": "Clases de tipos",
  modulos: "Módulos",
  acciones: "Acciones",
  "niveles-acceso": "Niveles de acceso",
  empresas: "Empresas",
  sucursales: "Sucursales",
  productos: "Productos",
  categorias: "Categorías",
  marcas: "Marcas",
  unidades: "Unidades de medida",
  presentaciones: "Presentaciones",
  "conversiones-unidad": "Conversiones de unidad",
  inventario: "Inventario",
  existencias: "Existencias",
  ajustes: "Ajustes",
  compras: "Órdenes de compra",
  nueva: "Nueva orden",
  editar: "Editar",
  recepciones: "Recepciones",
  recibir: "Recibir productos",
  proveedores: "Proveedores",
  recetas: "Recetas",
  produccion: "Producción",
  finalizar: "Finalizar producción",
};

export function LayoutAutenticado() {
  const [colapsado, setColapsado] = useState(true);
  const [menuMovil, setMenuMovil] = useState(false);
  const [perfilAbierto, setPerfilAbierto] = useState(false);
  const [cambiandoSucursal, setCambiandoSucursal] = useState(false);
  const {
    usuario,
    sesion,
    sucursalActiva,
    sucursales,
    tienePermiso,
    cerrarSesion,
    seleccionarSucursal,
  } = useAutenticacion();
  const { mostrar } = useToast();
  const ubicacion = useLocation();
  const partes = ubicacion.pathname.split("/").filter(Boolean);
  const titulo = partes[0] === "ventas" && partes.at(-1) === "nueva" ? "Nueva venta" : etiquetasRuta[partes.at(-1) ?? "dashboard"] ?? "Heladería";
  const operativo = soloOperacionPos(sesion?.permisos ?? []);
  const gruposVisibles = useMemo(
    () =>
      grupos
        .filter((grupo) => !operativo || grupo.etiqueta === "Ventas")
        .filter((grupo) => !grupo.permiso || tienePermiso(grupo.permiso))
        .map((grupo) => ({
          ...grupo,
          elementos: grupo.elementos
            .filter(
              (elemento) => !elemento.permiso || tienePermiso(elemento.permiso),
            )
            .sort((a, b) => a.etiqueta.localeCompare(b.etiqueta, "es")),
        }))
        .filter((grupo) => grupo.elementos.length > 0)
        .sort((a, b) => a.etiqueta.localeCompare(b.etiqueta, "es")),
    [tienePermiso, operativo],
  );
  const sidebarColapsado = colapsado && !menuMovil;

  const cambiarSucursal = async (id: number) => {
    if (id === sucursalActiva?.id) return;
    setCambiandoSucursal(true);
    try {
      await seleccionarSucursal(id);
      mostrar({
        tipo: "exito",
        titulo: "Sucursal actualizada",
        descripcion: "El contexto operativo se cambió correctamente.",
      });
    } catch (excepcion) {
      mostrar({
        tipo: "error",
        titulo: "No fue posible cambiar la sucursal",
        descripcion: excepcion instanceof Error ? excepcion.message : undefined,
      });
    } finally {
      setCambiandoSucursal(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#f4f6f8] text-slate-900">
      {menuMovil && (
        <button
          className="fixed inset-0 z-30 bg-slate-950/50 lg:hidden"
          onClick={() => setMenuMovil(false)}
          aria-label="Cerrar menú"
        />
      )}
      <aside
        data-testid="menu-lateral"
        data-colapsado={sidebarColapsado}
        className={clases(
          "fixed inset-y-0 left-0 z-40 flex flex-col bg-[#17202f] text-slate-300 shadow-xl transition-all duration-200",
          sidebarColapsado ? "w-[5.25rem]" : "w-72",
          menuMovil ? "translate-x-0" : "-translate-x-full lg:translate-x-0",
        )}
      >
        <div
          className={clases(
            "flex h-20 items-center border-b border-white/10",
            sidebarColapsado ? "justify-center px-3" : "justify-between px-5",
          )}
        >
          <NavLink
            to={operativo ? "/pos" : "/dashboard"}
            className="flex items-center gap-3 overflow-hidden text-white"
            onClick={() => setMenuMovil(false)}
          >
            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-teal-600">
              <IceCreamBowl className="h-5 w-5" />
            </span>
            {!sidebarColapsado && (
              <div className="whitespace-nowrap">
                <p className="font-bold">Heladería</p>
                <p className="text-[10px] tracking-[0.15em] text-slate-400">
                  {operativo ? "PUNTO DE VENTA" : "ADMINISTRACIÓN"}
                </p>
              </div>
            )}
          </NavLink>
          <button
            className="rounded-lg p-2 text-slate-400 hover:bg-white/10 hover:text-white lg:hidden"
            onClick={() => setMenuMovil(false)}
            aria-label="Cerrar navegación"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <nav
          className="flex-1 overflow-y-auto px-3 py-5"
          aria-label="Navegación principal"
        >
          {!operativo && <NavLink
            to="/dashboard"
            title="Dashboard"
            aria-label="Dashboard"
            className={({ isActive }) =>
              clases(
                "mb-4 flex min-h-11 items-center gap-3 rounded-lg px-3 text-sm font-medium transition-colors",
                isActive
                  ? "bg-teal-600 text-white"
                  : "hover:bg-white/8 hover:text-white",
                sidebarColapsado && "justify-center",
              )
            }
            onClick={() => setMenuMovil(false)}
          >
            <LayoutDashboard className="h-5 w-5 shrink-0" />
            {!sidebarColapsado && "Dashboard"}
          </NavLink>}
          <div className="space-y-5">
            {gruposVisibles.map((grupo) => (
              <GrupoSidebar
                key={grupo.etiqueta}
                grupo={grupo}
                colapsado={sidebarColapsado}
                cerrarMovil={() => setMenuMovil(false)}
              />
            ))}
          </div>
        </nav>
        <div className="hidden border-t border-white/10 p-3 lg:block">
          <button
            className={clases(
              "flex min-h-10 w-full items-center gap-3 rounded-lg px-3 text-sm text-slate-400 hover:bg-white/8 hover:text-white",
              colapsado && "justify-center",
            )}
            onClick={() => setColapsado((valor) => !valor)}
            aria-expanded={!colapsado}
            aria-label={
              colapsado ? "Expandir menú lateral" : "Colapsar menú lateral"
            }
          >
            {colapsado ? (
              <PanelLeftOpen className="h-5 w-5" />
            ) : (
              <>
                <PanelLeftClose className="h-5 w-5" />{" "}
                <span>Colapsar menú</span>
              </>
            )}
          </button>
        </div>
      </aside>

      <div
        className={clases(
          "transition-[padding] duration-200",
          colapsado ? "lg:pl-[5.25rem]" : "lg:pl-72",
        )}
      >
        <header className="sticky top-0 z-20 flex h-20 items-center justify-between border-b border-slate-200 bg-white/95 px-4 backdrop-blur sm:px-6 lg:px-8">
          <div className="flex min-w-0 items-center gap-3">
            <button
              className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 lg:hidden"
              onClick={() => setMenuMovil(true)}
              aria-label="Abrir navegación"
            >
              <Menu className="h-5 w-5" />
            </button>
            <div className="min-w-0">
              <h2 className="truncate text-lg font-bold text-slate-950">
                {titulo}
              </h2>
              <p className="max-w-40 truncate text-xs font-medium text-slate-500 sm:hidden">
                {sucursalActiva?.nombre}
              </p>
              <div className="mt-0.5 hidden items-center gap-1 text-xs text-slate-400 sm:flex">
                <span>Inicio</span>
                {partes.map((parte) => (
                  <span className="flex items-center gap-1" key={parte}>
                    <ChevronRight className="h-3 w-3" />
                    {partes[0] === "ventas" && parte === "nueva" ? "Nueva venta" : etiquetasRuta[parte] ?? parte}
                  </span>
                ))}
              </div>
            </div>
          </div>
          <div className="flex items-center gap-2 sm:gap-4">
            <div className="hidden sm:block">
              {sucursales.length > 1 ? (
                <label className="sr-only" htmlFor="sucursal-activa">
                  Sucursal activa
                </label>
              ) : null}
              {sucursales.length > 1 ? (
                <select
                  id="sucursal-activa"
                  className="max-w-56 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm font-medium text-slate-700 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
                  value={sucursalActiva?.id ?? ""}
                  onChange={(evento) =>
                    cambiarSucursal(Number(evento.target.value))
                  }
                  disabled={cambiandoSucursal}
                >
                  {sucursales.map((sucursal) => (
                    <option key={sucursal.id} value={sucursal.id}>
                      {sucursal.nombre}
                    </option>
                  ))}
                </select>
              ) : (
                <p className="max-w-48 truncate text-sm font-medium text-slate-600">
                  {sucursalActiva?.nombre}
                </p>
              )}
            </div>
            <div className="relative">
              <button
                className="flex items-center gap-2 rounded-lg p-1.5 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600"
                onClick={() => setPerfilAbierto((valor) => !valor)}
                aria-expanded={perfilAbierto}
                aria-label="Abrir menú de perfil"
              >
                <span className="grid h-9 w-9 place-items-center rounded-full bg-slate-200 text-slate-700">
                  <UserRound className="h-5 w-5" />
                </span>
                <div className="hidden text-left md:block">
                  <p className="max-w-36 truncate text-sm font-semibold text-slate-800">
                    {usuario?.nombreUsuario}
                  </p>
                  <p className="text-xs text-slate-400">{usuario?.rol}</p>
                </div>
                <ChevronDown className="hidden h-4 w-4 text-slate-400 md:block" />
              </button>
              {perfilAbierto && (
                <div className="absolute right-0 mt-2 w-64 rounded-xl border border-slate-200 bg-white p-2 shadow-xl">
                  <div className="border-b border-slate-100 px-3 py-3">
                    <p className="truncate text-sm font-semibold text-slate-900">
                      {usuario?.nombreUsuario}
                    </p>
                    <p className="truncate text-xs text-slate-500">
                      {usuario?.correo}
                    </p>
                  </div>
                  <button
                    className="mt-2 flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-sm font-medium text-rose-600 hover:bg-rose-50"
                    onClick={() => cerrarSesion()}
                  >
                    <LogOut className="h-4 w-4" /> Cerrar sesión
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>
        <main className="p-4 sm:p-6 lg:p-8">
          <div className="mx-auto max-w-[96rem]">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}

function GrupoSidebar({
  grupo,
  colapsado,
  cerrarMovil,
}: {
  grupo: GrupoNavegacion;
  colapsado: boolean;
  cerrarMovil: () => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const Icono = grupo.icono;
  return (
    <section className={colapsado ? "border-t border-white/10 pt-3" : undefined}>
      <button
        className={clases(
          "flex min-h-10 w-full items-center rounded-lg px-3 py-2 text-xs font-bold uppercase tracking-wider text-slate-500 hover:text-slate-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-400",
          colapsado ? "justify-center" : "justify-between",
        )}
        onClick={() => setAbierto((valor) => !valor)}
        aria-expanded={abierto}
        aria-label={grupo.etiqueta}
        title={colapsado ? grupo.etiqueta : undefined}
      >
        <span className="flex items-center gap-2">
          <Icono className="h-4 w-4 shrink-0" />
          {!colapsado && grupo.etiqueta}
        </span>
        {!colapsado && (
          <ChevronDown
            className={clases(
              "h-4 w-4 transition-transform",
              !abierto && "-rotate-90",
            )}
          />
        )}
      </button>
      {abierto && (
        <div className="mt-1 space-y-1">
          {grupo.elementos.map((elemento) => (
            <EnlaceSidebar
              key={elemento.ruta}
              elemento={elemento}
              colapsado={colapsado}
              cerrarMovil={cerrarMovil}
            />
          ))}
        </div>
      )}
    </section>
  );
}

function EnlaceSidebar({
  elemento,
  colapsado,
  cerrarMovil,
}: {
  elemento: ElementoNavegacion;
  colapsado: boolean;
  cerrarMovil: () => void;
}) {
  const Icono = elemento.icono;
  return (
    <NavLink
      to={elemento.ruta}
      end={elemento.exacta}
      aria-label={elemento.etiqueta}
      title={colapsado ? elemento.etiqueta : undefined}
      onClick={cerrarMovil}
      className={({ isActive }) =>
        clases(
          "flex min-h-10 items-center gap-3 rounded-lg px-3 text-sm transition-colors",
          isActive
            ? "bg-white/10 font-semibold text-white"
            : "text-slate-400 hover:bg-white/8 hover:text-white",
          colapsado && "justify-center",
        )
      }
    >
      <Icono className="h-4.5 w-4.5 shrink-0" />
      {!colapsado && elemento.etiqueta}
    </NavLink>
  );
}
