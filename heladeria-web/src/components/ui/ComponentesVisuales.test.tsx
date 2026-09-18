import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { BotonAccionTabla } from "./BotonAccionTabla";
import { SwitchCampo } from "./SwitchCampo";
import { Modal } from "./Modal";

describe("componentes visuales compartidos", () => {
  it.each([
    ["ver", "Ver detalle", "bg-blue-50"],
    ["editar", "Editar", "bg-amber-50"],
    ["activar", "Activar", "bg-emerald-50"],
    ["desactivar", "Desactivar", "bg-red-50"],
    ["eliminar", "Eliminar", "bg-red-50"],
  ] as const)("la acción %s conserva color, icono y nombre accesible", (tipo, nombre, color) => {
    render(<BotonAccionTabla tipo={tipo} />);
    const boton = screen.getByRole("button", { name: nombre });
    expect(boton.classList.contains(color)).toBe(true);
    expect(boton.getAttribute("title")).toBe(nombre);
    expect(boton.textContent).toBe("");
  });
  it("los switches conservan la etiqueta y comunican el valor", () => {
    const cambiar = vi.fn();
    render(<SwitchCampo etiqueta="Comprable" activo={false} cambiar={cambiar} />);
    fireEvent.click(screen.getByRole("switch", { name: "Comprable" }));
    expect(cambiar).toHaveBeenCalledWith(true);
  });
  it("dos modales no reutilizan el identificador de su encabezado", () => {
    render(<><Modal abierto titulo="Primero" cerrar={vi.fn()}>Contenido uno</Modal><Modal abierto titulo="Segundo" cerrar={vi.fn()}>Contenido dos</Modal></>);
    expect(screen.getByRole("dialog", { name: "Primero" }).getAttribute("aria-labelledby"))
      .not.toBe(screen.getByRole("dialog", { name: "Segundo" }).getAttribute("aria-labelledby"));
  });
});
