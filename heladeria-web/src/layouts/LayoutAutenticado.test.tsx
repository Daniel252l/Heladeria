import { fireEvent, screen, within } from "@testing-library/react";
import { Route, Routes, useLocation } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ContextoAutenticacion } from "../auth/contextoAutenticacion";
import { PERMISOS } from "../auth/permisos";
import { useAutenticacion } from "../auth/useAutenticacion";
import { renderSeguridad } from "../test/renderSeguridad";
import { LayoutAutenticado } from "./LayoutAutenticado";

const grupos = ["Auditoría", "Caja", "Cartera", "Compras", "Configuración", "Inventario", "Producción", "Productos", "Reportes", "Ventas"];
const navegacion = () => within(screen.getByRole("navigation", { name: "Navegación principal" }));
const botonGrupo = (nombre: string) => navegacion().getByRole("button", { name: nombre });
const expandirSidebar = () => fireEvent.click(screen.getByRole("button", { name: "Expandir menú lateral" }));

function RutaActual() {
  return <output data-testid="ruta-layout">{useLocation().pathname}</output>;
}

function SesionConsulta({ children }: { children: React.ReactNode }) {
  const auth = useAutenticacion();
  const usuario = { ...auth.usuario!, nombreUsuario: "consulta", rolId: "CONSULTA", rol: "Consulta" };
  return <ContextoAutenticacion.Provider value={{ ...auth, usuario, sesion: { ...auth.sesion!, usuario } }}>{children}</ContextoAutenticacion.Provider>;
}

function mostrar(ruta = "/dashboard", permisos = Object.values(PERMISOS) as string[], consulta = false) {
  const layout = <Routes><Route element={<LayoutAutenticado />}><Route path="*" element={<RutaActual />} /></Route></Routes>;
  return renderSeguridad(consulta ? <SesionConsulta>{layout}</SesionConsulta> : layout, permisos, ruta);
}

function comprobarCerrados() {
  expect(navegacion().getAllByRole("button").map((b) => b.getAttribute("aria-label") ?? b.textContent)).toEqual(grupos);
  for (const grupo of grupos) expect(botonGrupo(grupo).getAttribute("aria-expanded")).toBe("false");
  expect(navegacion().getAllByRole("link").map((a) => a.getAttribute("href"))).toEqual(["/dashboard"]);
}

describe("Ajuste A: grupos del layout con apertura explícita", () => {
  it.each(["/dashboard", "/ventas", "/caja", "/cuentas-por-cobrar", "/reportes/ventas", "/auditoria", "/productos", "/inventario/existencias", "/compras", "/produccion", "/configuracion/empresas"])("ADMIN inicia todos los grupos cerrados incluso en %s", (ruta) => {
    mostrar(ruta);
    comprobarCerrados();
    expect(screen.getByTestId("ruta-layout").textContent).toBe(ruta);
    expandirSidebar();
    comprobarCerrados();
  });

  it.each([false, true])("clic abre y segundo clic cierra solo el grupo elegido (sidebar ancho=%s)", (ancho) => {
    mostrar();
    if (ancho) expandirSidebar();
    fireEvent.click(botonGrupo("Reportes"));
    expect(botonGrupo("Reportes").getAttribute("aria-expanded")).toBe("true");
    expect(navegacion().getByRole("link", { name: "Reporte de ventas" }).getAttribute("href")).toBe("/reportes/ventas");
    for (const otro of grupos.filter((g) => g !== "Reportes")) expect(botonGrupo(otro).getAttribute("aria-expanded")).toBe("false");
    fireEvent.click(botonGrupo("Reportes"));
    comprobarCerrados();
  });

  it("mantiene la elección manual al cambiar el ancho del sidebar, sin abrir otros grupos", () => {
    mostrar();
    fireEvent.click(botonGrupo("Caja"));
    expandirSidebar();
    expect(botonGrupo("Caja").getAttribute("aria-expanded")).toBe("true");
    expect(botonGrupo("Ventas").getAttribute("aria-expanded")).toBe("false");
    fireEvent.click(screen.getByRole("button", { name: "Colapsar menú lateral" }));
    expect(botonGrupo("Caja").getAttribute("aria-expanded")).toBe("true");
    fireEvent.click(botonGrupo("Caja"));
    comprobarCerrados();
  });

  it("navega al destino existente sin abrir otros grupos y conserva Dashboard como enlace directo", () => {
    mostrar();
    fireEvent.click(botonGrupo("Reportes"));
    fireEvent.click(navegacion().getByRole("link", { name: "Reporte de ventas" }));
    expect(screen.getByTestId("ruta-layout").textContent).toBe("/reportes/ventas");
    expect(navegacion().getByRole("link", { name: "Reporte de ventas" }).getAttribute("aria-current")).toBe("page");
    expect(botonGrupo("Ventas").getAttribute("aria-expanded")).toBe("false");
    fireEvent.click(botonGrupo("Reportes"));
    comprobarCerrados();
    fireEvent.click(navegacion().getByRole("link", { name: "Dashboard" }));
    expect(screen.getByTestId("ruta-layout").textContent).toBe("/dashboard");
  });

  it("un nuevo montaje descarta la apertura anterior, incluso al entrar a una ruta del grupo", () => {
    const anterior = mostrar();
    fireEvent.click(botonGrupo("Reportes"));
    anterior.unmount();
    mostrar("/reportes/ventas");
    comprobarCerrados();
  });

  it("drawer móvil inicia cerrado, permite abrir grupos y se cierra al navegar", () => {
    mostrar();
    expect(screen.queryByRole("button", { name: "Cerrar menú" })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Abrir navegación" }));
    expect(screen.getByTestId("menu-lateral").getAttribute("data-colapsado")).toBe("false");
    comprobarCerrados();
    fireEvent.click(botonGrupo("Cartera"));
    fireEvent.click(navegacion().getByRole("link", { name: "Cuentas por cobrar" }));
    expect(screen.getByTestId("ruta-layout").textContent).toBe("/cuentas-por-cobrar");
    expect(screen.queryByRole("button", { name: "Cerrar menú" })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Abrir navegación" }));
    expect(botonGrupo("Cartera").getAttribute("aria-expanded")).toBe("true");
    fireEvent.click(botonGrupo("Cartera"));
    comprobarCerrados();
    fireEvent.click(screen.getByRole("button", { name: "Cerrar navegación" }));
    expect(screen.queryByRole("button", { name: "Cerrar menú" })).toBeNull();
  });

  it("CONSULTA sin permisos conserva únicamente Dashboard, también al expandir el menú", () => {
    mostrar("/dashboard", [], true);
    expect(navegacion().queryAllByRole("button")).toHaveLength(0);
    expandirSidebar();
    expect(navegacion().queryAllByRole("button")).toHaveLength(0);
    expect(navegacion().getAllByRole("link").map((a) => a.getAttribute("href"))).toEqual(["/dashboard"]);
  });

  it("los permisos de cada elemento se respetan tras abrir su grupo", () => {
    mostrar("/dashboard", [PERMISOS.inventarioConsultar, PERMISOS.cxcConsultar], true);
    expect(navegacion().getAllByRole("button").map((b) => b.getAttribute("aria-label"))).toEqual(["Cartera", "Inventario"]);
    fireEvent.click(botonGrupo("Inventario"));
    fireEvent.click(botonGrupo("Cartera"));
    expect(navegacion().getAllByRole("link").map((a) => a.getAttribute("href"))).toEqual(["/dashboard", "/cuentas-por-cobrar", "/inventario/existencias"]);
    expect(navegacion().queryByRole("link", { name: "Ajustes" })).toBeNull();
    expect(navegacion().queryByRole("link", { name: "Cuentas por pagar" })).toBeNull();
    expect(navegacion().queryByRole("button", { name: "Reportes" })).toBeNull();
  });
});
