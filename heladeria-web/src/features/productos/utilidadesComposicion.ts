import type { ComponenteVenta } from "../../types/productos";

export interface FilaComponente {
  clave: string;
  productoId: number;
  productoPresentacionId: number | null;
  cantidad: string;
  nombre: string;
  presentacion: string | null;
}

export function filasComposicion(componentes: ComponenteVenta[] = []): FilaComponente[] {
  return componentes.map((c, i) => ({ clave: `existente-${i}`, productoId: c.productoId,
    productoPresentacionId: c.productoPresentacionId, cantidad: String(c.cantidad), nombre: c.producto, presentacion: c.presentacion }));
}

export function validarComposicion(filas: FilaComponente[], padre?: number): string | null {
  if (filas.length > 100) return "La composición admite hasta 100 componentes.";
  const claves = new Set<string>();
  for (const f of filas) {
    if (!f.productoId) return "Seleccione el producto de cada componente.";
    if (f.productoId === padre) return "Un producto no puede ser componente de sí mismo.";
    if (!/^\d+(\.\d{1,6})?$/.test(f.cantidad) || Number(f.cantidad) <= 0 || Number(f.cantidad) > Number("9999999999999.999999"))
      return "La cantidad del componente debe ser mayor que cero, con hasta seis decimales.";
    const clave = `${f.productoId}/${f.productoPresentacionId ?? "base"}`;
    if (claves.has(clave)) return "No se puede repetir el mismo componente y presentación.";
    claves.add(clave);
  }
  return null;
}
