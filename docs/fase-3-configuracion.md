# Fase 3 — Configuración y mantenimientos

## Alcance implementado

La fase expone mantenimientos transversales sobre el modelo Database First existente, sin modificar `BaseDatos/Heladeria.sql` ni generar migraciones:

| Mantenimiento | Ruta base | Estado lógico |
|---|---|---|
| Clases de estados | `/api/configuracion/clases-estado` | `Activo` |
| Estados | `/api/configuracion/estados` | `Activo` |
| Clases de tipos | `/api/configuracion/clases-tipo` | `EstadoId` |
| Tipos | `/api/configuracion/tipos` | `EstadoId` |
| Módulos | `/api/configuracion/modulos` | `Activo` |
| Acciones del sistema | `/api/configuracion/acciones-sistema` | `EstadoId` |
| Niveles de acceso | `/api/configuracion/niveles-acceso` | El esquema no contiene estado lógico |
| Empresas | `/api/configuracion/empresas` | `EstadoId` |
| Sucursales | `/api/configuracion/sucursales` | `EstadoId` |

No se agregaron CRUD para `Permisos`, `Roles`, `RolesPermisos`, `SesionesUsuarios`, `HistorialAccesos`, `Bitacoras` ni tablas operativas. No existen endpoints `DELETE`: las entidades que lo soportan cambian de estado mediante `PUT /{id}/estado`.

## Contrato HTTP común

Cada controlador ofrece:

- `GET /`: listado paginado.
- `GET /{id}`: consulta individual; devuelve 404 si no existe dentro del alcance autorizado.
- `POST /`: creación; devuelve 201 y `Location`.
- `PUT /{id}`: actualización; devuelve 200.
- `PUT /{id}/estado`: cambio lógico; devuelve 200. `NivelesDeAccesos` no lo ofrece porque la tabla no tiene `Activo` ni `EstadoId`.

Los listados aceptan `pagina` (predeterminado 1), `tamanioPagina` (predeterminado 20, máximo 100) y `busqueda`. Según la entidad también aceptan `activo`, `estadoId`, `claseEstadoId`, `claseTipoId` o `moduloId`. El filtrado, conteo, orden estable y paginación se ejecutan en SQL con `AsNoTracking`.

Las respuestas paginadas contienen `elementos`, `pagina`, `tamanioPagina`, `totalRegistros` y `totalPaginas`.

## Autorización y alcance

- Todos los endpoints requieren un JWT y una sesión vigentes.
- Las consultas globales de catálogos requieren autenticación, pero no un rol específico.
- Todas las escrituras requieren el permiso dinámico `CONFIGURACION.ADMINISTRAR`.
- El permiso se resuelve en la base mediante `RolesPermisos`, igual que en Fase 2; no se compara el nombre del rol en los controladores.
- Quien tiene `CONFIGURACION.ADMINISTRAR` posee alcance global de empresas/sucursales para poder administrarlas.
- Sin ese permiso, empresas queda restringido a `empresa_id` del token y sucursales a asignaciones activas del usuario dentro de esa empresa. Un acceso fuera del alcance responde 404 para no confirmar la existencia del recurso.

Los registros estructurales de bootstrap no pueden desactivar el módulo `SEGURIDAD`, el módulo `CONFIGURACION`, el estado `Activo` usado por identidades/permisos, las clases de seguridad, los tipos usados por roles/autenticación, la acción `ADMINISTRAR`, el nivel del rol `ADMIN`, ni la empresa/sucursal principal activa del administrador.

## Preparación controlada del permiso

La API no siembra datos durante el arranque. Desde la raíz de la solución, el operador debe ejecutar una sola vez:

```powershell
dotnet run --project .\Heladeria.Bootstrap\Heladeria.Bootstrap.csproj --configuration Release --launch-profile Bootstrap -- --preparar-fase3
```

La herramienta exige una terminal interactiva y la confirmación `PREPARAR FASE 3`. No solicita ni maneja contraseñas. Dentro de una transacción `Serializable` con bloqueo exclusivo crea exactamente:

1. `Modulos`: `CONFIGURACION`, activo.
2. `Permisos`: `CONFIGURACION.ADMINISTRAR`, reutilizando la acción `ADMINISTRAR` y el estado activo existentes.
3. `RolesPermisos`: asignación permitida al rol `ADMIN` existente.
4. `Bitacoras`: marcador `FASE3_CONFIGURADA`.

Se rechaza si falta el administrador activo, el permiso base de seguridad o si existe una carga completa/parcial previa. No se hardcodean IDs `IDENTITY` y cualquier fallo revierte los cuatro registros. La implementación no ejecutó este comando contra la base real.

Verificación sin exponer secretos:

```sql
SELECT m.Id, m.Nombre, m.Activo,
       p.Id AS PermisoId, p.Codigo,
       rp.RoleId, rp.Permitido
FROM Modulos AS m
JOIN Permisos AS p ON p.ModuloId = m.Id
JOIN RolesPermisos AS rp ON rp.PermisoId = p.Id
WHERE m.Id = 'CONFIGURACION'
  AND p.Codigo = 'CONFIGURACION.ADMINISTRAR'
  AND rp.RoleId = 'ADMIN';

SELECT Id, Entidad, Accion, Fecha, UsuarioId
FROM Bitacoras
WHERE Entidad = 'Instalacion'
  AND Accion = 'FASE3_CONFIGURADA';
```

Se espera una fila habilitada en cada resultado.

## Validaciones e integridad

- Los DTO de entrada son clases con propiedades `init` y DataAnnotations aplicadas a propiedades.
- Las longitudes reflejan los `VARCHAR` de SQL Server; valores obligatorios compuestos solo por espacios se rechazan.
- Nombres y correos se recortan; correos se normalizan a minúsculas; códigos e IDs textuales se normalizan a mayúsculas.
- Se validan duplicados antes de persistir y un conflicto tardío de SQL se normaliza como 409.
- Los IDs manuales de `ClaseEstados`, `Estados`, `ClaseTipos`, `Tipos` y `NivelesDeAccesos` se calculan bajo transacción serializable y `sp_getapplock`; los `IDENTITY` son generados por SQL Server.
- Solo se admiten estados, clases y módulos disponibles.
- `Sucursales.ProveedorId` es obligatorio: el proveedor debe existir, estar activo y pertenecer a la misma empresa.
- Cliente, municipio, departamento, sucursal central y sucursal de transporte se validan antes de guardar. Municipio/departamento y todas las referencias empresariales deben ser coherentes.
- `LugarId` no se acepta en la API porque el esquema no define una FK que permita validarlo.
- Las acciones `CREAR`, `ACTUALIZAR` y `CAMBIAR_ESTADO` se registran en `Bitacoras` sin almacenar cuerpos completos ni información sensible.

## Respuestas de error

- 400: DataAnnotations o relación/estado inválido.
- 401: token ausente/inválido o sesión no vigente.
- 403: falta `CONFIGURACION.ADMINISTRAR` en una escritura.
- 404: recurso inexistente o fuera del alcance.
- 409: duplicado, protección estructural o conflicto de persistencia.
- 500: error no controlado, con mensaje genérico y `traceId`.

## Guía manual en Swagger

1. Inicie la API con `dotnet run --project .\Heladeria.Api\Heladeria.Api.csproj --launch-profile https`.
2. Abra `/swagger`, ejecute `POST /api/autenticacion/iniciar-sesion` y copie únicamente el `accessToken`.
3. Pulse **Authorize** e ingrese el token sin prefijo adicional.
4. Pruebe `GET /api/configuracion/modulos?pagina=1&tamanioPagina=20&busqueda=SEG`.
5. Pruebe un `GET /api/configuracion/.../{id}` válido y otro inexistente para confirmar 200/404.
6. Con `admin`, después de preparar Fase 3, cree un registro no estructural, actualícelo y cambie su estado. Verifique 201/200 y la fila de `Bitacoras`.
7. Intente crear un duplicado para confirmar 409 y una FK inexistente para confirmar 400.
8. Inicie sesión como `consulta`: los GET de organización deben limitarse a su empresa/sucursal y cualquier POST/PUT debe devolver 403.
9. Repita un GET sin encabezado Bearer para confirmar 401.

Swagger se sirve desde el mismo origen de la API y no necesita una excepción CORS. Desarrollo conserva el origen React HTTP configurado; producción no fue ampliada ni debilitada.

## Observaciones del esquema (sin cambios aplicados)

- `Sucursales.ProveedorId` obligatorio mezcla la identidad de una sucursal con una relación comercial. Además, su FK simple no garantiza que sucursal y proveedor pertenezcan a la misma empresa; la API compensa esa carencia. Conceptualmente convendría hacerlo nullable o mover la relación a una configuración explícita, previa revisión funcional.
- Como esta fase prohíbe implementar proveedores funcionales, no se crea un proveedor implícito ni se expone su CRUD. La sucursal inicial puede reutilizar `PROV-INICIAL`; una empresa nueva necesitará que exista primero un proveedor válido de esa empresa mediante un mecanismo futuro autorizado. Esto deja incompleto el alta integral empresa → primera sucursal mientras la FK siga siendo obligatoria.
- `Empresas.ClienteId` referencia `Tipos`, mientras `Sucursales.ClienteId` referencia `Clientes`; el mismo nombre representa conceptos distintos.
- `Sucursales.LugarId` no tiene FK.
- Municipio y departamento se almacenan simultáneamente sin restricción compuesta que garantice coherencia.
- No hay restricciones únicas para nombres de `ClaseEstados`, `Estados`, `Modulos`, `NivelesDeAccesos`, ni para correo/código/centralidad de sucursales; esas reglas quedan solo en la aplicación y son vulnerables a escrituras externas.
- Cinco tablas usan enteros manuales en lugar de `IDENTITY` o secuencias.
- Las nueve tablas no poseen `rowversion`, de modo que no hay control optimista de edición concurrente.
- `Empresas.RoleId` es obligatorio pero su semántica no expresa claramente si es un rol predeterminado, propietario o de configuración.

Estas observaciones no autorizaron cambios de esquema; cualquier corrección requiere modificar primero el SQL fuente y regenerar el modelo Database First.
