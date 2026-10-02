import { useState } from "react";
import { ChevronDown, ChevronUp, Filter } from "lucide-react";
import { Tarjeta } from "./Controles";

export function PanelFiltros({
  children,
  acciones,
  titulo = "Filtros de búsqueda",
  descripcion = "Encuentra registros utilizando uno o varios criterios.",
}: {
  children: React.ReactNode;
  acciones?: React.ReactNode;
  titulo?: string;
  descripcion?: string;
}) {
  const [abierto, setAbierto] = useState(true);
  return (
    <Tarjeta className="overflow-visible px-5 sm:px-6">
      <header className="flex items-center justify-between gap-4 py-5">
        <div className="flex min-w-0 items-center gap-3">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
            <Filter className="h-5 w-5" aria-hidden="true" />
          </span>
          <div>
            <h2 className="text-lg font-bold text-slate-950">{titulo}</h2>
            <p className="mt-0.5 text-sm text-slate-500">{descripcion}</p>
          </div>
        </div>
        <button
          type="button"
          className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-600 transition hover:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600"
          aria-label={abierto ? "Contraer filtros" : "Expandir filtros"}
          title={abierto ? "Contraer filtros" : "Expandir filtros"}
          aria-expanded={abierto}
          onClick={() => setAbierto((valor) => !valor)}
        >
          {abierto ? (
            <ChevronUp className="h-4 w-4" />
          ) : (
            <ChevronDown className="h-4 w-4" />
          )}
        </button>
      </header>
      {abierto && (
        <div className="border-t border-slate-200 pb-6 pt-4">
          {children}
          {acciones && (
            <div className="mt-6 flex flex-wrap gap-3">{acciones}</div>
          )}
        </div>
      )}
    </Tarjeta>
  );
}
