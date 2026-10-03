import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { renderSeguridad } from "../../test/renderSeguridad";
import type { Estado, Paginado } from "../../types/api";
import type { Categoria } from "../../types/productos";
import { PaginaCatalogo } from "./PaginaCatalogo";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/clienteHttp")>()),
  solicitar: vi.fn(),
}));

const api = vi.mocked(solicitar);

const activa = 1;
const inactiva = 3;
const categorias: Categoria[] = [
  categoria(7, "SABORIZANTES", "Saborizantes", 4, 2),
  categoria(2, "HELADOS", "Helados Artesanales", null, 1),
  categoria(8, "POSTRES", "Postres", null, 1),
  categoria(6, "LACTEOS", "Lácteos", 4, 2, inactiva, "Inactivo"),
  categoria(1, "BEBIDAS", "Bebidas", null, 1),
  categoria(3, "HELADOS-CREMA", "Helados de Crema", 2, 2),
  categoria(4, "MATERIAS-PRIMAS", "Materias Primas", null, 1),
  categoria(5, "ENDULZANTES", "Endulzantes", 4, 2),
];

const estados: Estado[] = [
  { id: activa, nombre: "Activo", activo: true, claseEstadoId: 1, claseEstado: "General", moduloId: null },
  { id: inactiva, nombre: "Inactivo", activo: true, claseEstadoId: 1, claseEstado: "General", moduloId: null },
];

function categoria(
  id: number,
  codigo: string,
  nombre: string,
  categoriaPadreId: number | null,
  nivel: number,
  estadoId = activa,
  estado = "Activo",
): Categoria {
  return {
    id,
    empresaId: 4,
    codigo,
    nombre,
    categoriaPadreId,
    nivel,
    descripcion: null,
    estadoId,
    estado,
    versionFila: `version-${id}`,
  };
}

function pagina<T>(elementos: T[]): Paginado<T> {
  return {
    elementos,
    pagina: 1,
    tamanioPagina: 100,
    totalRegistros: elementos.length,
    totalPaginas: 1,
  };
}

function idsFilas() {
  return screen
    .queryAllByTestId(/^fila-categoria-/)
    .map((fila) => Number(fila.getAttribute("data-testid")?.split("-").at(-1)));
}

async function renderizar(permisos: string[] = [PERMISOS.productosConsultar]) {
  renderSeguridad(<PaginaCatalogo tipo="categorias" />, permisos);
  await screen.findByRole("treegrid", { name: "Árbol de categorías" });
}

describe("árbol jerárquico de categorías", () => {
  beforeEach(() => {
    api.mockReset();
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta.startsWith("/api/configuracion/estados?")) return pagina(estados) as never;
      if (ruta === "/api/productos/categorias?pagina=1&tamanioPagina=100")
        return pagina(categorias) as never;
      const detalle = ruta.match(/^\/api\/productos\/categorias\/(\d+)$/);
      if (detalle && !opciones?.method)
        return categorias.find((item) => item.id === Number(detalle[1])) as never;
      if (opciones?.method === "PUT")
        return categorias.find((item) => ruta.includes(`/${item.id}`)) as never;
      throw new Error(`Ruta inesperada en prueba: ${ruta}`);
    });
  });

  it("muestra cada raíz con sus hijos inmediatamente debajo", async () => {
    await renderizar();
    expect(idsFilas()).toEqual([1, 2, 3, 4, 5, 6, 7, 8]);
    expect(screen.getByTestId("fila-categoria-2").getAttribute("aria-level")).toBe("1");
    expect(screen.getByTestId("fila-categoria-3").getAttribute("aria-level")).toBe("2");
  });

  it("muestra varias raíces antes de sus ramas sin convertir hijos en raíces", async () => {
    await renderizar();
    for (const id of [1, 2, 4, 8])
      expect(screen.getByTestId(`fila-categoria-${id}`).getAttribute("aria-level")).toBe("1");
    for (const id of [3, 5, 6, 7])
      expect(screen.getByTestId(`fila-categoria-${id}`).getAttribute("aria-level")).toBe("2");
  });

  it("ordena alfabéticamente raíces e hijos aunque la API llegue desordenada", async () => {
    await renderizar();
    const nombres = idsFilas().map(
      (id) => categorias.find((item) => item.id === id)?.nombre,
    );
    expect(nombres).toEqual([
      "Bebidas",
      "Helados Artesanales",
      "Helados de Crema",
      "Materias Primas",
      "Endulzantes",
      "Lácteos",
      "Saborizantes",
      "Postres",
    ]);
  });

  it("contrae y vuelve a expandir una categoría padre", async () => {
    await renderizar();
    const contraer = screen.getByRole("button", { name: "Contraer Materias Primas" });
    expect(contraer.getAttribute("aria-expanded")).toBe("true");
    fireEvent.click(contraer);
    expect(idsFilas()).toEqual([1, 2, 3, 4, 8]);
    const expandir = screen.getByRole("button", { name: "Expandir Materias Primas" });
    expect(expandir.getAttribute("aria-expanded")).toBe("false");
    fireEvent.click(expandir);
    expect(idsFilas()).toEqual([1, 2, 3, 4, 5, 6, 7, 8]);
  });

  it("expande todas las ramas sin solicitar nuevamente los datos", async () => {
    await renderizar();
    fireEvent.click(screen.getByRole("button", { name: "Contraer todo" }));
    const llamadasAntes = api.mock.calls.length;
    fireEvent.click(screen.getByRole("button", { name: "Expandir todo" }));
    expect(idsFilas()).toEqual([1, 2, 3, 4, 5, 6, 7, 8]);
    expect(api.mock.calls).toHaveLength(llamadasAntes);
  });

  it("contrae todas las ramas sin solicitar nuevamente los datos", async () => {
    await renderizar();
    const llamadasAntes = api.mock.calls.length;
    fireEvent.click(screen.getByRole("button", { name: "Contraer todo" }));
    expect(idsFilas()).toEqual([1, 2, 4, 8]);
    expect(api.mock.calls).toHaveLength(llamadasAntes);
  });

  it("al buscar un hijo conserva visible su padre como contexto", async () => {
    await renderizar();
    fireEvent.change(screen.getByLabelText("Buscar registros"), {
      target: { value: "Lacteos" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Buscar" }));
    await waitFor(() => expect(idsFilas()).toEqual([4, 6]));
    expect(screen.getByRole("button", { name: "Contraer Materias Primas" }).getAttribute("aria-expanded")).toBe("true");
  });

  it("si coincide un padre muestra toda su rama de forma consistente", async () => {
    await renderizar();
    fireEvent.change(screen.getByLabelText("Buscar registros"), {
      target: { value: "materias" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Buscar" }));
    await waitFor(() => expect(idsFilas()).toEqual([4, 5, 6, 7]));
  });

  it("filtra por estado y conserva el padre activo de un hijo inactivo", async () => {
    await renderizar();
    fireEvent.change(screen.getByLabelText("Estado"), { target: { value: String(inactiva) } });
    await waitFor(() => expect(idsFilas()).toEqual([4, 6]));
    expect(within(screen.getByTestId("fila-categoria-6")).getByText("Inactivo")).toBeDefined();
  });

  it("una categoría sin hijos no presenta un control de expansión", async () => {
    await renderizar();
    const fila = within(screen.getByTestId("fila-categoria-8"));
    expect(fila.queryByRole("button", { name: /Postres/ })).toBeNull();
    expect(fila.getByText("Postres")).toBeDefined();
  });

  it("mantiene una única estructura responsive: tarjetas móviles y tabla desde tablet", async () => {
    await renderizar();
    const arbol = screen.getByTestId("arbol-categorias");
    expect(arbol.className).toContain("block");
    expect(arbol.className).toContain("md:table");
    const fila = screen.getByTestId("fila-categoria-1");
    expect(fila.className).toContain("rounded-xl");
    expect(fila.className).toContain("md:table-row");
  });

  it("mantiene editar y activar/desactivar con sus endpoints y permisos", async () => {
    await renderizar([
      PERMISOS.productosConsultar,
      PERMISOS.productosAdministrar,
    ]);
    fireEvent.click(screen.getByRole("button", { name: "Editar Lácteos" }));
    expect(await screen.findByRole("dialog", { name: "Editar categoría" })).toBeDefined();
    expect(api).toHaveBeenCalledWith("/api/productos/categorias/6");
    fireEvent.click(screen.getByRole("button", { name: "Cerrar ventana" }));

    fireEvent.click(screen.getByRole("button", { name: "Desactivar Bebidas" }));
    fireEvent.click(within(screen.getByRole("dialog", { name: "Confirmar cambio de estado" })).getByRole("button", { name: "Confirmar" }));
    await waitFor(() =>
      expect(
        api.mock.calls.some(
          ([ruta, opciones]) =>
            ruta === "/api/productos/categorias/1/estado" &&
            opciones?.method === "PUT" &&
            JSON.parse(String(opciones.body)).estadoId === inactiva,
        ),
      ).toBe(true),
    );
  });
});
