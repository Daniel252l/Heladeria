import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import {
  ContextoAutenticacion,
  type ValorAutenticacion,
} from "../auth/contextoAutenticacion";
import { PERMISOS } from "../auth/permisos";
import { ProveedorToast } from "../components/toast/ProveedorToast";
import { sesionActual } from "../test/datosAutenticacion";
import { EnrutadorAplicacion } from "./EnrutadorAplicacion";

describe("clasificación de rutas", () => {
  it.each([[PERMISOS.cajaConsultar], [PERMISOS.cajaAbrir]])("abrir caja exige consulta y apertura, no basta %s", async (permisos) => {
    renderAplicacion("/caja/abrir", [permisos]);
    await screen.findByText("Acceso no autorizado");
  });
  it.each([
    "/configuracion",
    "/configuracion/acciones",
    "/configuracion/clases-estados",
    "/configuracion/clases-tipos",
    "/configuracion/empresas",
    "/configuracion/estados",
    "/configuracion/modulos",
    "/configuracion/niveles-acceso",
    "/configuracion/sucursales",
    "/configuracion/tipos",
    "/inventario",
    "/inventario/ajustes",
    "/inventario/existencias",
    "/productos",
    "/productos/categorias",
    "/productos/conversiones-unidad",
    "/productos/marcas",
    "/productos/presentaciones",
    "/productos/tipos",
    "/productos/unidades",
    "/compras",
    "/compras/nueva",
    "/compras/123",
    "/compras/123/editar",
    "/compras/123/recepciones",
    "/compras/123/recibir",
    "/proveedores",
    "/proveedores/123",
    "/recetas",
    "/recetas/nueva",
    "/recetas/123",
    "/recetas/123/editar",
    "/produccion",
    "/produccion/nueva",
    "/produccion/123",
    "/produccion/123/editar",
    "/produccion/123/finalizar",
    "/ventas",
    "/ventas/nueva",
    "/ventas/123",
    "/ventas/123/editar",
    "/pos",
    "/caja",
    "/caja/cajas",
    "/caja/sesiones",
    "/caja/sesiones/123",
    "/caja/abrir",
    "/reportes",
    "/reportes/ventas",
    "/reportes/productos-vendidos",
    "/reportes/inventario",
    "/reportes/kardex",
    "/reportes/compras",
    "/reportes/caja",
    "/reportes/cuentas-por-cobrar",
    "/reportes/cuentas-por-pagar",
    "/auditoria",
  ])(
    "envía la ruta privada conocida %s a 403 cuando falta el permiso",
    async (ruta) => {
      renderAplicacion(ruta, []);

      await screen.findByText("Acceso no autorizado");
      expect(screen.getByText("Error 403")).toBeDefined();
      await waitFor(() =>
        expect(screen.getByTestId("ruta-actual").textContent).toBe("/403"),
      );
    },
  );

  it("mantiene una ruta desconocida en la página 404", () => {
    renderAplicacion("/ruta-que-no-existe", []);

    expect(screen.getByText("Página no encontrada")).toBeDefined();
    expect(screen.getByTestId("ruta-actual").textContent).toBe(
      "/ruta-que-no-existe",
    );
  });

  it("inicia con el menú lateral colapsado y permite expandirlo", () => {
    renderAplicacion("/dashboard", Object.values(PERMISOS));

    expect(
      screen.getByTestId("menu-lateral").getAttribute("data-colapsado"),
    ).toBe("true");
    fireEvent.click(
      screen.getByRole("button", { name: "Expandir menú lateral" }),
    );

    expect(
      screen.getByTestId("menu-lateral").getAttribute("data-colapsado"),
    ).toBe("false");
    expect(
      screen.getByRole("button", { name: "Colapsar menú lateral" }),
    ).toBeDefined();
  });

  it("ordena Dashboard primero y los grupos y opciones alfabéticamente en español", () => {
    renderAplicacion("/dashboard", Object.values(PERMISOS));
    fireEvent.click(
      screen.getByRole("button", { name: "Expandir menú lateral" }),
    );

    const navegacion = screen.getByRole("navigation", {
      name: "Navegación principal",
    });
    // La apertura ahora es explícita; conservar todas las comprobaciones de orden y accesos.
    for (const grupo of within(navegacion).getAllByRole("button")) {
      expect(grupo.getAttribute("aria-expanded")).toBe("false");
      fireEvent.click(grupo);
    }
    const etiquetas = within(navegacion)
      .getAllByRole("link")
      .map((enlace) => enlace.getAttribute("aria-label"));

    expect(etiquetas).toEqual([
      "Dashboard",
      "Auditoría",
      "Cajas",
      "Resumen de caja",
      "Sesiones de caja",
      "Cuentas por cobrar",
      "Cuentas por pagar",
      "Órdenes de compra",
      "Proveedores",
      "Acciones",
      "Clases de estados",
      "Clases de tipos",
      "Empresas",
      "Estados",
      "Módulos",
      "Niveles de acceso",
      "Sucursales",
      "Tipos",
      "Ajustes",
      "Existencias",
      "Órdenes de producción",
      "Recetas",
      "Categorías",
      "Conversiones de unidad",
      "Marcas",
      "Presentaciones",
      "Productos",
      "Tipos de producto",
      "Unidades de medida",
      "Reporte de caja",
      "Reporte de compras",
      "Reporte de cuentas por cobrar",
      "Reporte de cuentas por pagar",
      "Reporte de inventario",
      "Reporte de kardex",
      "Reporte de productos vendidos",
      "Reporte de ventas",
      "Reporte diario",
      "Punto de venta",
      "Ventas",
    ]);
    const textoNavegacion = navegacion.textContent ?? "";
    expect(textoNavegacion.indexOf("Caja")).toBeLessThan(textoNavegacion.indexOf("Cartera"));
    expect(textoNavegacion.indexOf("Cartera")).toBeLessThan(textoNavegacion.indexOf("Compras"));
    expect(textoNavegacion.indexOf("Compras")).toBeLessThan(
      textoNavegacion.indexOf("Configuración"),
    );
    expect(textoNavegacion.indexOf("Configuración")).toBeLessThan(
      textoNavegacion.indexOf("Inventario"),
    );
    expect(textoNavegacion.indexOf("Inventario")).toBeLessThan(
      textoNavegacion.indexOf("Productos"),
    );
  });
});

function renderAplicacion(ruta: string, permisos: string[]) {
  const valor: ValorAutenticacion = {
    sesion: { ...sesionActual, permisos },
    usuario: sesionActual.usuario,
    sucursalActiva: sesionActual.usuario.sucursalActiva,
    sucursales: sesionActual.usuario.sucursales,
    autenticado: true,
    cargando: false,
    iniciarSesion: vi.fn(),
    cerrarSesion: vi.fn(),
    renovarSesion: vi.fn(),
    cambiarContrasena: vi.fn(),
    seleccionarSucursal: vi.fn(),
    tienePermiso: (permiso) => permisos.includes(permiso),
  };

  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <ContextoAutenticacion.Provider value={valor}>
        <ProveedorToast>
          <ObservadorRuta />
          <EnrutadorAplicacion />
        </ProveedorToast>
      </ContextoAutenticacion.Provider>
    </MemoryRouter>,
  );
}

function ObservadorRuta() {
  const ubicacion = useLocation();
  return (
    <output className="sr-only" data-testid="ruta-actual">
      {ubicacion.pathname}
    </output>
  );
}
