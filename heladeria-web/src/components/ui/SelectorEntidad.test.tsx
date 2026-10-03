import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar } from "../../api/clienteHttp";
import { paginaPrueba } from "../../test/datosFaseVisual2";
import { SelectorEntidad } from "./SelectorEntidad";

vi.mock("../../api/clienteHttp", () => ({ solicitar: vi.fn() }));
const remoto = vi.mocked(solicitar);

describe("selector remoto como un único control", () => {
  beforeEach(() => {
    remoto.mockReset();
    remoto.mockResolvedValue(paginaPrueba([{ id: 17, nombre: "Helados" }]) as never);
  });
  it("oculta la búsqueda hasta abrir el desplegable y mantiene el contrato remoto", async () => {
    render(<SelectorEntidad etiqueta="Categoría" ruta="/api/productos/categorias" value="" onChange={vi.fn()} />);
    await waitFor(() => expect(remoto).toHaveBeenCalledWith("/api/productos/categorias?pagina=1&tamanioPagina=30"));
    expect(screen.queryByRole("textbox")).toBeNull();
    expect(screen.queryByRole("listbox")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: /Categoría/ }));
    expect(screen.getByRole("textbox", { name: "Buscar categoría" })).toBeDefined();
    expect(screen.getByRole("listbox", { name: "Categoría" })).toBeDefined();
  });
  it("busca con Enter sin enviar el formulario contenedor", async () => {
    const enviar = vi.fn((evento) => evento.preventDefault());
    render(<form onSubmit={enviar}><SelectorEntidad etiqueta="Marca" ruta="/api/productos/marcas" value="" onChange={vi.fn()} /></form>);
    fireEvent.click(screen.getByRole("button", { name: /Marca/ }));
    const busqueda = screen.getByRole("textbox", { name: "Buscar marca" });
    fireEvent.change(busqueda, { target: { value: "artesanal" } });
    fireEvent.keyDown(busqueda, { key: "Enter" });
    await waitFor(() => expect(remoto).toHaveBeenCalledWith("/api/productos/marcas?pagina=1&tamanioPagina=30&busqueda=artesanal"));
    expect(enviar).not.toHaveBeenCalled();
  });
  it("carga páginas adicionales sin perder las opciones anteriores", async () => {
    remoto.mockResolvedValueOnce({ ...paginaPrueba([{ id: 17, nombre: "Helados" }]), totalPaginas: 2 } as never);
    remoto.mockResolvedValueOnce({ ...paginaPrueba([{ id: 28, nombre: "Conos" }]), pagina: 2, totalPaginas: 2 } as never);
    render(<SelectorEntidad etiqueta="Categoría" ruta="/api/productos/categorias" value="" onChange={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: /Categoría/ }));
    fireEvent.click(await screen.findByRole("button", { name: "Cargar más opciones" }));
    expect(await screen.findByRole("option", { name: "Conos" })).toBeDefined();
    expect(screen.getByRole("option", { name: "Helados" })).toBeDefined();
    expect(remoto).toHaveBeenCalledWith("/api/productos/categorias?pagina=2&tamanioPagina=30");
  });
  it("muestra la selección, admite Sin asignar y no asume IDs", async () => {
    const cambiar = vi.fn();
    render(<SelectorEntidad etiqueta="Marca" ruta="/api/productos/marcas" value={17} onChange={cambiar} opcional etiquetaActual="Helados" />);
    fireEvent.click(screen.getByRole("button", { name: /Marca Helados/ }));
    const seleccionada = await screen.findByRole("option", { name: "Helados" });
    expect(seleccionada.getAttribute("aria-selected")).toBe("true");
    fireEvent.click(screen.getByRole("option", { name: "Sin asignar" }));
    expect(cambiar).toHaveBeenCalledWith(null);
    expect(screen.queryByRole("listbox")).toBeNull();
  });
  it("permite placeholder de formulario sin eliminar la opción de limpiar", async () => {
    render(<SelectorEntidad etiqueta="Marca" ruta="/api/productos/marcas" value="" onChange={vi.fn()} opcional placeholder="Buscar o seleccionar marca…" />);
    fireEvent.click(screen.getByRole("button", { name: "Marca Buscar o seleccionar marca…" }));
    expect(screen.getByRole("option", { name: "Sin asignar" })).toBeDefined();
  });
  it("renderiza directamente la versión vigente de las opciones locales", () => {
    const propiedades = { etiqueta: "Presentación", value: "base", onChange: vi.fn() };
    const { rerender } = render(<SelectorEntidad {...propiedades} opcionesLocales={[{ id: "base", nombre: "Unidad base · ml · × 1" }]} />);
    rerender(<SelectorEntidad {...propiedades} opcionesLocales={[{ id: "base", nombre: "Unidad base · ml · × 1" }, { id: 1, nombre: "Litro · L · × 1,000" }]} />);
    fireEvent.click(screen.getByRole("button", { name: /Presentación/ }));
    expect(screen.getByRole("option", { name: "Unidad base · ml · × 1" })).toBeDefined();
    expect(screen.getByRole("option", { name: "Litro · L · × 1,000" })).toBeDefined();
  });
});
