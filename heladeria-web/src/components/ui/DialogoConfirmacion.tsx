import { CircleAlert, CircleCheck } from "lucide-react";
import { Boton } from "./Controles";
import { Modal } from "./Modal";

export function DialogoConfirmacion({
  abierto,
  titulo,
  descripcion,
  confirmar,
  cerrar,
  cargando = false,
  peligro = false,
  textoConfirmar = "Confirmar",
  children,
}: {
  abierto: boolean;
  titulo: string;
  descripcion?: string;
  confirmar: () => void;
  cerrar: () => void;
  cargando?: boolean;
  peligro?: boolean;
  textoConfirmar?: string;
  children?: React.ReactNode;
}) {
  return (
    <Modal
      abierto={abierto}
      cerrar={() => !cargando && cerrar()}
      titulo={titulo}
      descripcion={descripcion}
      ancho="max-w-lg"
      icono={
        peligro ? (
          <CircleAlert className="h-5 w-5 text-red-600" />
        ) : (
          <CircleCheck className="h-5 w-5 text-emerald-600" />
        )
      }
    >
      <div className="space-y-5 p-6">
        {children}
        <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
          <Boton variante="secundario" onClick={cerrar} disabled={cargando}>
            Cancelar
          </Boton>
          <Boton
            variante={peligro ? "peligro" : "primario"}
            onClick={confirmar}
            cargando={cargando}
          >
            {textoConfirmar}
          </Boton>
        </div>
      </div>
    </Modal>
  );
}
