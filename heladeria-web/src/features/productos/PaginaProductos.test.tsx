import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { PaginaProductos } from "../../pages/PaginaProductos";
import { paginaPrueba, productoPrueba } from "../../test/datosFaseVisual2";
import { renderSeguridad } from "../../test/renderSeguridad";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/clienteHttp")>()),
  solicitar: vi.fn(),
}));
const solicitarSimulado = vi.mocked(solicitar);
describe("productos conectados", () => {
  beforeEach(() => {
    solicitarSimulado.mockReset();
    solicitarSimulado.mockImplementation(async (ruta, opciones) => {
      if (opciones?.method === "PUT") return productoPrueba as never;
      if (ruta === "/api/productos/referencias")
        return {
          tiposSeguimiento: [{ id: 10, nombre: "Ninguno", codigo: "NINGUNO" }],
          metodosValuacion: [{ id: 11, nombre: "Promedio", codigo: "PROM" }],
        } as never;
      if (ruta === "/api/productos/17") return productoPrueba as never;
      if (ruta.startsWith("/api/productos?"))
        return paginaPrueba([productoPrueba]) as never;
      return paginaPrueba([
        { id: 6, nombre: "Activo" },
        { id: 7, nombre: "Helados" },
        { id: 8, nombre: "Inventariable" },
        { id: 9, nombre: "Mililitro" },
      ]) as never;
    });
  });
  it("un usuario de consulta no ve acciones de escritura", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar]);
    await screen.findByText("Helado de vainilla");
    expect(screen.queryByRole("button", { name: "Nuevo producto" })).toBeNull();
    expect(
      screen.queryByRole("button", { name: "Editar Helado de vainilla" }),
    ).toBeNull();
    expect(
      screen.getByRole("button", { name: "Ver detalle de Helado de vainilla" }),
    ).toBeDefined();
  });
  it("carga el detalle y envía su VersionFila al actualizar el Id solicitado", async () => {
    renderSeguridad(<PaginaProductos />, [
      PERMISOS.productosConsultar,
      PERMISOS.productosAdministrar,
    ]);
    fireEvent.click(
      await screen.findByRole("button", { name: "Editar Helado de vainilla" }),
    );
    const nombre = await screen.findByLabelText("Nombre del producto *");
    fireEvent.change(nombre, { target: { value: "Helado actualizado" } });
    fireEvent.click(screen.getByRole("button", { name: "Guardar" }));
    await waitFor(() =>
      expect(
        solicitarSimulado.mock.calls.some(
          ([ruta, opciones]) =>
            ruta === "/api/productos/17" && opciones?.method === "PUT",
        ),
      ).toBe(true),
    );
    const llamada = solicitarSimulado.mock.calls.find(
      ([ruta, opciones]) =>
        ruta === "/api/productos/17" && opciones?.method === "PUT",
    )!;
    expect(JSON.parse(String(llamada[1]?.body))).toMatchObject({
      versionFila: "AQIDBA==",
      nombre: "Helado actualizado",
      categoriaId: 7,
      marcaId: null,
    });
  });
  it("aplica los filtros juntos y limpiar restaura la consulta", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar]);
    await screen.findByText("Helado de vainilla");
    solicitarSimulado.mockClear();
    fireEvent.change(screen.getByLabelText("SKU o nombre"), { target: { value: "vainilla" } });
    expect(solicitarSimulado).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Buscar productos" }));
    await waitFor(() => expect(solicitarSimulado).toHaveBeenCalledWith("/api/productos?pagina=1&tamanioPagina=10&busqueda=vainilla"));
    fireEvent.click(screen.getByRole("button", { name: "Limpiar filtros" }));
    await waitFor(() => expect(solicitarSimulado).toHaveBeenCalledWith("/api/productos?pagina=1&tamanioPagina=10"));
    expect((screen.getByLabelText("SKU o nombre") as HTMLInputElement).value).toBe("");
  });
  it("permite colapsar filtros sin perder el texto ingresado", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar]);
    await screen.findByText("Helado de vainilla");
    fireEvent.change(screen.getByLabelText("SKU o nombre"), { target: { value: "helado" } });
    fireEvent.click(screen.getByRole("button", { name: "Contraer filtros" }));
    expect(screen.queryByLabelText("SKU o nombre")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Expandir filtros" }));
    expect((screen.getByLabelText("SKU o nombre") as HTMLInputElement).value).toBe("helado");
  });
  it("cambia el tamaño de página con paginación remota", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar]);
    await screen.findByText("Helado de vainilla");
    fireEvent.change(screen.getByLabelText("Registros por página"), { target: { value: "20" } });
    await waitFor(() => expect(solicitarSimulado).toHaveBeenCalledWith("/api/productos?pagina=1&tamanioPagina=20"));
  });
  it("el footer fijo mantiene Guardar asociado al formulario y todos los campos", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar, PERMISOS.productosAdministrar]);
    fireEvent.click(await screen.findByRole("button", { name: "Editar Helado de vainilla" }));
    const dialogo = await screen.findByRole("dialog", { name: "Editar producto" });
    const guardar = within(dialogo).getByRole("button", { name: "Guardar" }) as HTMLButtonElement;
    expect(guardar.closest("footer")).not.toBeNull();
    expect(guardar.form).not.toBeNull();
    expect(guardar.form!.contains(guardar)).toBe(false);
    for (const etiqueta of ["Descripción", "Modelo", "Peso", "Imagen (URL o ruta)", "Precio de venta referencial"])
      expect(within(dialogo).getByLabelText(etiqueta)).toBeDefined();
    // Compuesto se determina por sus componentes; ya no existe un switch Combo independiente.
    expect(within(dialogo).getAllByRole("switch")).toHaveLength(4);
    expect(within(dialogo).getByText("Composición de venta")).toBeDefined();
    expect(within(dialogo).getByRole("switch", { name: /Comprable/ }).getAttribute("type")).toBe("checkbox");
  });
  it("Escape en un selector solo cierra el popover, no el modal de producto", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar, PERMISOS.productosAdministrar]);
    fireEvent.click(await screen.findByRole("button", { name: "Editar Helado de vainilla" }));
    const dialogo = await screen.findByRole("dialog", { name: "Editar producto" });
    fireEvent.click(within(dialogo).getByRole("button", { name: /Categoría \*/ }));
    const busqueda = within(dialogo).getByRole("textbox", { name: "Buscar categoría" });
    fireEvent.keyDown(busqueda, { key: "Escape" });
    expect(within(dialogo).queryByRole("listbox")).toBeNull();
    expect(screen.getByRole("dialog", { name: "Editar producto" })).toBeDefined();
  });
  it("un 409 conserva los valores y ofrece recargar el detalle", async () => {
    solicitarSimulado.mockImplementation(async (ruta, opciones) => {
      if (opciones?.method === "PUT") throw new ErrorApi("El registro fue modificado por otro usuario.", 409, { codigo: "CONFLICTO_FASE4" });
      if (ruta === "/api/productos/referencias") return { tiposSeguimiento: [], metodosValuacion: [] } as never;
      if (ruta === "/api/productos/17") return productoPrueba as never;
      if (ruta.startsWith("/api/productos?")) return paginaPrueba([productoPrueba]) as never;
      return paginaPrueba([]) as never;
    });
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar, PERMISOS.productosAdministrar]);
    fireEvent.click(await screen.findByRole("button", { name: "Editar Helado de vainilla" }));
    const dialogo = within(await screen.findByRole("dialog", { name: "Editar producto" }));
    fireEvent.change(dialogo.getByLabelText("Nombre del producto *"), { target: { value: "Cambio sin guardar" } });
    fireEvent.click(dialogo.getByRole("button", { name: "Guardar" }));
    expect(await dialogo.findByRole("alert")).toBeDefined();
    expect((dialogo.getByLabelText("Nombre del producto *") as HTMLInputElement).value).toBe("Cambio sin guardar");
    expect(dialogo.getByRole("button", { name: "Recargar detalle" })).toBeDefined();
  });
});
