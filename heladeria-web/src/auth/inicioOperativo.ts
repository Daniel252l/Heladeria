import { PERMISOS } from './permisos'

export const permisosOperativosPos: readonly string[] = [PERMISOS.posOperar, PERMISOS.ventasConsultar,
  PERMISOS.ventasAdministrar, PERMISOS.ventasVender, PERMISOS.cajaConsultar, PERMISOS.cajaOperar, PERMISOS.cajaCobrar]

// La experiencia depende de capacidades, nunca del nombre del usuario o del rol.
export function soloOperacionPos(permisos: readonly string[]) {
  return permisos.includes(PERMISOS.posOperar) && permisos.every((p) => permisosOperativosPos.includes(p))
}
