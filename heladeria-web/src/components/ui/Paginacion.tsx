import { ChevronLeft, ChevronRight } from "lucide-react";
import { useId } from "react";
import { Boton } from "./Controles";

export function Paginacion({
  pagina,
  totalPaginas,
  totalRegistros,
  cambiar,
  tamanioPagina = 10,
  cambiarTamanio,
  entidad = "registro",
}: {
  pagina: number;
  totalPaginas: number;
  totalRegistros: number;
  cambiar: (pagina: number) => void;
  tamanioPagina?: number;
  cambiarTamanio?: (tamanio: number) => void;
  entidad?: string;
}) {
  const tamanioId = useId();
  const inicio = totalRegistros === 0 ? 0 : (pagina - 1) * tamanioPagina + 1;
  const fin = Math.min(pagina * tamanioPagina, totalRegistros);
  const plural = totalRegistros === 1 ? entidad : `${entidad}s`;
  return (
    <footer className="flex flex-col gap-3 border-t border-slate-200 px-4 py-4 text-sm text-slate-600 sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <span>
        Mostrando {inicio === fin ? fin : `${inicio}-${fin}`} de{" "}
        {totalRegistros} {plural}
      </span>
      <div className="flex flex-wrap items-center gap-2">
        {cambiarTamanio && (
          <label className="sr-only" htmlFor={tamanioId}>
            Registros por página
          </label>
        )}
        {cambiarTamanio && (
          <select
            id={tamanioId}
            className="min-h-10 rounded-lg border border-slate-300 bg-white px-3 text-sm text-slate-700 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
            value={tamanioPagina}
            onChange={(evento) => cambiarTamanio(Number(evento.target.value))}
          >
            {[10, 20, 50].map((cantidad) => (
              <option key={cantidad} value={cantidad}>
                {cantidad} por página
              </option>
            ))}
          </select>
        )}
        <Boton
          variante="secundario"
          className="h-10 min-h-10 w-10 px-0"
          disabled={pagina <= 1}
          onClick={() => cambiar(pagina - 1)}
          aria-label="Página anterior"
          title="Página anterior"
        >
          <ChevronLeft className="h-4 w-4" />
        </Boton>
        <span aria-current="page" aria-label={`Página ${pagina}`} className="inline-flex h-10 min-w-10 items-center justify-center rounded-lg bg-teal-700 px-3 font-semibold text-white">
          {pagina}
        </span>
        <Boton
          variante="secundario"
          className="h-10 min-h-10 w-10 px-0"
          disabled={totalPaginas === 0 || pagina >= totalPaginas}
          onClick={() => cambiar(pagina + 1)}
          aria-label="Página siguiente"
          title="Página siguiente"
        >
          <ChevronRight className="h-4 w-4" />
        </Boton>
      </div>
    </footer>
  );
}
