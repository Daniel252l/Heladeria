export function calcularCantidadBase(
  cantidad: number,
  factor: number,
): number | null {
  const resultado = cantidad * factor;
  return Number.isFinite(cantidad) &&
    Number.isFinite(factor) &&
    cantidad > 0 &&
    factor > 0 &&
    Number.isFinite(resultado)
    ? resultado
    : null;
}
