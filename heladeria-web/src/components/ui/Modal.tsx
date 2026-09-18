import { useEffect, useId } from "react";
import { X } from "lucide-react";

export function Modal({
  abierto,
  titulo,
  descripcion,
  cerrar,
  children,
  ancho = "max-w-xl",
  icono,
  footer,
}: {
  abierto: boolean;
  titulo: string;
  descripcion?: string;
  cerrar: () => void;
  children: React.ReactNode;
  ancho?: string;
  icono?: React.ReactNode;
  footer?: React.ReactNode;
}) {
  const tituloId = useId();
  useEffect(() => {
    if (!abierto) return;
    const manejarTecla = (evento: KeyboardEvent) => {
      if (evento.key === "Escape") cerrar();
    };
    document.addEventListener("keydown", manejarTecla);
    return () => document.removeEventListener("keydown", manejarTecla);
  }, [abierto, cerrar]);

  if (!abierto) return null;
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/55 p-4"
      role="presentation"
      onMouseDown={cerrar}
    >
      <section
        className={`flex max-h-[90vh] w-full ${ancho} flex-col overflow-hidden rounded-2xl bg-white shadow-2xl`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={tituloId}
        onMouseDown={(evento) => evento.stopPropagation()}
      >
        <header className="flex shrink-0 items-start justify-between border-b border-slate-200 px-5 py-4 sm:px-6">
          <div className="flex items-center gap-3">
            {icono && (
              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-teal-50 text-teal-700">
                {icono}
              </span>
            )}
            <div>
              <h2
                id={tituloId}
                className="text-lg font-bold text-slate-900"
              >
                {titulo}
              </h2>
              {descripcion && (
                <p className="mt-0.5 text-sm text-slate-500">{descripcion}</p>
              )}
            </div>
          </div>
          <button
            type="button"
            className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600"
            onClick={cerrar}
            aria-label="Cerrar ventana"
            title="Cerrar ventana"
          >
            <X className="h-5 w-5" />
          </button>
        </header>
        <div className="min-h-0 flex-1 overflow-y-auto">{children}</div>
        {footer && (
          <footer className="flex shrink-0 justify-end gap-3 border-t border-slate-200 bg-white px-5 py-3.5 sm:px-6">
            {footer}
          </footer>
        )}
      </section>
    </div>
  );
}
