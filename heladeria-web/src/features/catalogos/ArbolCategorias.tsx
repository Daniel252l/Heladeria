import {
  ChevronDown,
  ChevronRight,
  ChevronsDown,
  ChevronsUp,
} from "lucide-react";
import { BotonAccionTabla, GrupoAccionesTabla } from "../../components/ui/BotonAccionTabla";
import { Boton, Insignia } from "../../components/ui/Controles";
import type { Categoria } from "../../types/productos";
import type { FilaCategoriaArbol } from "./jerarquiaCategorias";

interface PropiedadesArbolCategorias {
  filas: FilaCategoriaArbol[];
  expandidas: ReadonlySet<number>;
  filtroActivo: boolean;
  puedeAdministrar: boolean;
  cargandoDetalleId: number | null;
  alternar: (id: number) => void;
  expandirTodo: () => void;
  contraerTodo: () => void;
  editar: (categoria: Categoria) => void;
  cambiarEstado: (categoria: Categoria) => void;
}

export function ArbolCategorias({
  filas,
  expandidas,
  filtroActivo,
  puedeAdministrar,
  cargandoDetalleId,
  alternar,
  expandirTodo,
  contraerTodo,
  editar,
  cambiarEstado,
}: PropiedadesArbolCategorias) {
  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 bg-slate-50/70 px-4 py-3">
        <p className="text-sm text-slate-600">
          {filas.length} {filas.length === 1 ? "categoría visible" : "categorías visibles"}
        </p>
        <div className="flex flex-wrap gap-2">
          <Boton type="button" variante="secundario" className="min-h-9 px-3 py-1.5" onClick={expandirTodo}>
            <ChevronsDown className="h-4 w-4" aria-hidden="true" /> Expandir todo
          </Boton>
          <Boton type="button" variante="secundario" className="min-h-9 px-3 py-1.5" onClick={contraerTodo}>
            <ChevronsUp className="h-4 w-4" aria-hidden="true" /> Contraer todo
          </Boton>
        </div>
      </div>
      <div className="overflow-x-auto">
        <table
          className="block min-w-full text-left text-sm md:table"
          role="treegrid"
          aria-label="Árbol de categorías"
          data-testid="arbol-categorias"
        >
          <thead className="hidden bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500 md:table-header-group">
            <tr>
              <th className="px-5 py-3">Código</th>
              <th className="px-5 py-3">Nombre</th>
              <th className="px-5 py-3">Nivel</th>
              <th className="px-5 py-3">Estado</th>
              {puedeAdministrar && <th className="px-5 py-3 text-right">Acciones</th>}
            </tr>
          </thead>
          <tbody className="block space-y-3 bg-slate-50 p-3 md:table-row-group md:space-y-0 md:divide-y md:divide-slate-100 md:bg-white md:p-0">
            {filas.map(({ categoria, profundidad, tieneHijos, esUltimoHermano }) => {
              const expandida = filtroActivo || expandidas.has(categoria.id);
              const activa = categoria.estado.trim().toLowerCase() === "activo";
              return (
                <tr
                  key={categoria.id}
                  className="block rounded-xl border border-slate-200 bg-white shadow-sm hover:bg-slate-50/70 md:table-row md:rounded-none md:border-0 md:shadow-none"
                  aria-level={profundidad + 1}
                  data-testid={`fila-categoria-${categoria.id}`}
                >
                  <td className="flex items-center justify-between gap-4 px-4 pt-4 font-mono text-xs font-semibold text-slate-600 md:table-cell md:whitespace-nowrap md:px-5 md:py-4">
                    <span className="font-sans font-medium text-slate-500 md:hidden">Código</span>
                    <span>{categoria.codigo}</span>
                  </td>
                  <td className="px-4 py-3 font-semibold text-slate-900 md:table-cell md:px-5 md:py-4">
                    <div
                      className="flex min-w-0 items-center gap-2"
                      style={{ paddingLeft: `${profundidad * 1.5}rem` }}
                    >
                      {tieneHijos ? (
                        <button
                          type="button"
                          className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 hover:text-teal-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600"
                          onClick={() => alternar(categoria.id)}
                          aria-expanded={expandida}
                          aria-label={`${expandida ? "Contraer" : "Expandir"} ${categoria.nombre}`}
                        >
                          {expandida ? (
                            <ChevronDown className="h-4 w-4" aria-hidden="true" />
                          ) : (
                            <ChevronRight className="h-4 w-4" aria-hidden="true" />
                          )}
                        </button>
                      ) : (
                        <span className="inline-flex h-8 w-8 shrink-0 items-center justify-center text-slate-300" aria-hidden="true">
                          {profundidad > 0 ? (esUltimoHermano ? "└─" : "├─") : "•"}
                        </span>
                      )}
                      <span className="truncate">{categoria.nombre}</span>
                    </div>
                  </td>
                  <td className="flex items-center justify-between gap-4 px-4 py-3 text-slate-600 md:table-cell md:px-5 md:py-4">
                    <span className="font-medium text-slate-500 md:hidden">Nivel</span>
                    <span>{categoria.nivel}</span>
                  </td>
                  <td className="flex items-center justify-between gap-4 px-4 py-3 md:table-cell md:px-5 md:py-4">
                    <span className="font-medium text-slate-500 md:hidden">Estado</span>
                    <Insignia tono={activa ? "positivo" : "neutro"}>{categoria.estado}</Insignia>
                  </td>
                  {puedeAdministrar && (
                    <td className="flex items-center justify-between gap-4 border-t border-slate-100 px-4 py-3 md:table-cell md:whitespace-nowrap md:border-0 md:px-5 md:py-4 md:text-right">
                      <span className="font-medium text-slate-500 md:hidden">Acciones</span>
                      <GrupoAccionesTabla>
                        <BotonAccionTabla
                          tipo="editar"
                          onClick={() => editar(categoria)}
                          etiqueta={`Editar ${categoria.nombre}`}
                          disabled={cargandoDetalleId !== null}
                        />
                        <BotonAccionTabla
                          tipo={activa ? "desactivar" : "activar"}
                          onClick={() => cambiarEstado(categoria)}
                          etiqueta={`${activa ? "Desactivar" : "Activar"} ${categoria.nombre}`}
                          disabled={cargandoDetalleId !== null}
                        />
                      </GrupoAccionesTabla>
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}
