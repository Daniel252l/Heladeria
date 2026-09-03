# Fase 2 — Seguridad, autenticación y autorización

## Alcance y arquitectura

La fase implementa seguridad sobre el modelo Database First existente. No se modificó `BaseDatos/Heladeria.sql`, no se agregaron migraciones y no se creó un modelo paralelo de usuarios o permisos.

La separación aplicada es:

- `Heladeria.Aplicacion`: contratos, solicitudes/respuestas, opciones y excepciones de seguridad.
- `Heladeria.Infraestructura`: hash de contraseñas, emisión de tokens, sesiones, bloqueo, validación de permisos y auditoría mediante EF Core.
- `Heladeria.Api`: autenticación Bearer, políticas dinámicas, cookie, rate limiting, middleware de contraseña obligatoria y controladores HTTP.
- `Heladeria.Pruebas`: pruebas del servicio con EF Core InMemory y pruebas HTTP con `WebApplicationFactory`.
- `Heladeria.Bootstrap`: inicialización administrativa transaccional, explícita y de una sola ejecución.

Cada solicitud con JWT valida además que la sesión continúe abierta, no haya excedido la vida absoluta del refresh token y que el usuario conserve acceso activo a la sucursal y empresa. La autorización de permisos consulta el estado actual de la base; no confía solo en claims del cliente.

Las operaciones de autenticación y bootstrap usan transacciones explícitas. Los reintentos automáticos de SQL Server están desactivados para esos procesos para evitar repetir parcialmente una transacción iniciada por la aplicación; cualquier fallo revierte y la solicitud o comando debe repetirse.

## Tablas utilizadas

| Tabla | Uso |
|---|---|
| `Usuarios` | Identidad, hash, estado, horario, contador de fallos, contraseña temporal y fecha del último cambio. |
| `Roles` | Rol actual y meses configurados para vencimiento de contraseña. |
| `RolesPermisos` | Asociación del rol con permisos y bandera `Permitido`. |
| `Permisos` | Código de permiso, módulo, acción y estado. |
| `Modulos` | Habilitación del módulo asociado al permiso. |
| `AccionesSistema` | Acción y estado asociado al permiso. |
| `UsuariosSucursales` | Sucursales permitidas, principal y estado de la asignación. |
| `Sucursales` | Sucursal activa y su estado. |
| `Empresas` | Empresa de la sucursal y su estado. |
| `Estados` | Activación efectiva de usuarios, permisos, acciones, empresas, sucursales y asignaciones. |
| `SesionesUsuarios` | Sesión, inicio/cierre, IP, dispositivo y hash del refresh token vigente. |
| `HistorialAccesos` | Login exitoso/fallido y ancla temporal del bloqueo. |
| `Bitacoras` | Logout, renovación, cambio de contraseña, cambio/denegación de sucursal y permiso denegado. |
| `Tipos`, `ClaseTipos`, `NivelesDeAccesos` | Catálogos ya relacionados con usuarios y roles; no se duplican en la capa de seguridad. |

El campo heredado `Usuarios.PasswordTemporal` no se utiliza para almacenar secretos. Al cambiar contraseña se deja en `NULL`; la única contraseña persistida por la aplicación es `PasswordHash`.

## Endpoints

| Método y ruta | Acceso | Resultado principal |
|---|---|---|
| `POST /api/autenticacion/iniciar-sesion` | Público, limitado por IP | Access token, usuario, sucursales y cookie refresh. |
| `POST /api/autenticacion/renovar` | Cookie refresh, limitado por IP | Rota refresh y entrega un access token nuevo. |
| `POST /api/autenticacion/seleccionar-sucursal` | JWT + cookie refresh | Verifica asignación, rota tokens y cambia la sucursal activa. |
| `POST /api/autenticacion/cerrar-sesion` | JWT; permitido con contraseña temporal | Cierra la sesión y elimina la cookie. |
| `POST /api/autenticacion/cambiar-contrasena` | JWT; permitido con contraseña temporal; limitado | Actualiza el hash y revoca todas las sesiones del usuario. |
| `GET /api/autenticacion/sesion` | JWT | Devuelve usuario, sucursal activa y estado de cambio obligatorio. |
| `GET /api/seguridad/permisos` | JWT + `SEGURIDAD.ADMINISTRAR` | Ejemplo funcional de política dinámica y permisos actuales. |

Los errores conocidos se normalizan como JSON y usan 400, 401, 403 o 429 según corresponda. Los errores no controlados usan 500 sin exponer detalles internos.

## Claims del access token

- `sub`: identificador del usuario.
- `nombre_usuario`.
- `rol_id`.
- `empresa_id`.
- `sucursal_id`.
- `sid`: identificador de `SesionesUsuarios`.
- `requiere_cambio_contrasena`.
- `jti`: identificador único del JWT.

No se incluyen correo, permisos, contraseña, tokens de renovación ni otros datos sensibles.

## Estrategia de tokens y sesiones

El access token JWT usa HMAC-SHA256, emisor y audiencia validados, 10 minutos de duración por defecto y 15 segundos de tolerancia de reloj. La clave debe tener al menos 32 bytes y se mantiene fuera del repositorio: user-secrets en desarrollo y un proveedor de secretos o `Seguridad__Jwt__ClaveFirma` en producción.

El refresh token es opaco y criptográficamente aleatorio. Contiene únicamente los identificadores de sesión/sucursal más un secreto de 64 bytes. Solo su SHA-256 se guarda en `SesionesUsuarios.TokenIdentificador`. Tiene una vida absoluta de siete días por defecto, rota en cada renovación o cambio de sucursal e invalida inmediatamente el valor anterior. Logout, expiración, desactivación y cambio de contraseña lo revocan.

El refresh token se envía en una cookie `HttpOnly`, `Secure` en producción, `SameSite=Strict`, esencial y limitada a `/api/autenticacion`. Nunca aparece en el JSON ni se destina a `localStorage`.

## Permisos y sucursales

`[RequierePermiso("CODIGO")]` crea una policy dinámica. El manejador comprueba usuario, rol y permiso vigentes en `Usuarios`, `RolesPermisos`, `Permisos`, `Modulos`, `AccionesSistema` y `Estados`. Un rol agrupa permisos, pero el servidor siempre decide.

La sucursal activa procede de una asignación vigente en `UsuariosSucursales`. También se verifican los estados de sucursal y empresa. Para cambiarla deben coincidir el usuario/sesión/sucursal del JWT y el refresh token; después se emite un par rotado. En módulos posteriores, los casos de uso deberán tomar la sucursal de este contexto autenticado, nunca de un `SucursalId` libre enviado por React.

## Contraseñas y bloqueo

La configuración predeterminada exige 10 caracteres, mayúscula, minúscula, número y carácter especial. La contraseña nueva debe coincidir con su confirmación y diferir de la actual. Se usa `PasswordHasher<Usuarios>`, se renueva `SecurityStamp`, se actualiza `UltimoCambioDeContrasenia`, se desactiva `ContraseniaTemporal` y se revocan todas las sesiones.

El vencimiento se calcula con `Roles.CambioDeContrasenia` en meses. Una contraseña temporal, sin fecha de cambio o vencida emite un JWT limitado: solo permite cambiar contraseña o cerrar sesión.

Tras cinco fallos por defecto, `AccessFailedCount` y un evento `BLOQUEO_APLICADO` en `HistorialAccesos` bloquean al usuario durante 15 minutos. Un login correcto reinicia el contador. Usuario inexistente, contraseña inválida, estado/horario inválido y sucursal inicial no autorizada comparten la respuesta `Credenciales inválidas.`.

## Rate limiting, Swagger y auditoría

Los límites configurables por IP y ventana se aplican a login (10/minuto), renovación (20/minuto) y cambio de contraseña (5/minuto). Una denegación devuelve 429.

Swagger, disponible solo en desarrollo, define el esquema HTTP Bearer y marca las operaciones protegidas con respuestas 401/403. En **Authorize** debe introducirse únicamente el access token.

`HistorialAccesos` conserva intentos de login y bloqueos. `Bitacoras` conserva eventos posteriores autenticados. No se escriben contraseñas, JWT, refresh tokens ni sus hashes en logs o auditoría.

## Configuración

Las opciones viven bajo `Seguridad` en `appsettings.json` y se validan al arrancar. Los valores sensibles no tienen un valor predeterminado operativo. Ejemplo para producción:

```powershell
$env:Seguridad__Jwt__ClaveFirma = '<secreto-de-al-menos-32-bytes>'
```

Los valores `appsettings.Testing.json` son sintéticos, exclusivos del entorno automatizado y no deben reutilizarse fuera de pruebas.

## Pruebas implementadas

La batería cubre los 13 escenarios pedidos y un control adicional de middleware:

1. Login correcto.
2. Contraseña incorrecta.
3. Usuario inexistente.
4. Usuario inactivo.
5. Bloqueo tras intentos fallidos.
6. Refresh válido y rotación.
7. Refresh revocado.
8. Endpoint sin token: 401.
9. Usuario sin permiso: 403 y auditoría.
10. Usuario con permiso: 200.
11. Cambio de contraseña exitoso y revocación.
12. Contraseña temporal marca cambio obligatorio.
13. Sucursal no autorizada rechazada.
14. Contraseña temporal bloquea un endpoint normal.
15. Bootstrap sobre base vacía crea el entorno completo y permite login limitado.
16. Una segunda ejecución del bootstrap se rechaza sin duplicar registros.
17. Un rol `ADMIN` existente bloquea el bootstrap.
18. Un marcador de instalación existente bloquea el bootstrap.
19. Un usuario administrador existente bloquea el bootstrap.
20. Una contraseña inicial débil se rechaza antes de crear registros.
21. Catálogos preexistentes demuestran que no se asumen IDs fijos ni `IDENTITY`.

## Archivos

### Creados

- `Heladeria.Aplicacion/Seguridad/Autenticacion/IServicioAutenticacion.cs`
- `Heladeria.Aplicacion/Seguridad/Autenticacion/ModelosAutenticacion.cs`
- `Heladeria.Aplicacion/Seguridad/Autenticacion/ValidadorPoliticaContrasena.cs`
- `Heladeria.Aplicacion/Seguridad/Autorizacion/ClaimsSeguridad.cs`
- `Heladeria.Aplicacion/Seguridad/Autorizacion/ContratosAutorizacion.cs`
- `Heladeria.Aplicacion/Seguridad/Bootstrap/IBootstrapAdministrador.cs`
- `Heladeria.Aplicacion/Seguridad/Configuracion/OpcionesSeguridad.cs`
- `Heladeria.Aplicacion/Seguridad/Excepciones/ExcepcionesSeguridad.cs`
- `Heladeria.Infraestructura/Seguridad/Autenticacion/EvaluadorSeguridadUsuario.cs`
- `Heladeria.Infraestructura/Seguridad/Autenticacion/GeneradorTokensJwt.cs`
- `Heladeria.Infraestructura/Seguridad/Autenticacion/ServicioAutenticacion.cs`
- `Heladeria.Infraestructura/Seguridad/Autorizacion/ServicioAuditoriaSeguridad.cs`
- `Heladeria.Infraestructura/Seguridad/Autorizacion/ServicioPermisos.cs`
- `Heladeria.Infraestructura/Seguridad/Autorizacion/ServicioValidacionSesion.cs`
- `Heladeria.Infraestructura/Seguridad/Bootstrap/BootstrapAdministrador.cs`
- `Heladeria.Api/Configuracion/ConfiguracionSeguridad.cs`
- `Heladeria.Api/Configuracion/ConfiguracionRateLimiting.cs`
- `Heladeria.Api/Configuracion/FiltroSeguridadSwagger.cs`
- `Heladeria.Api/Controllers/AutenticacionController.cs`
- `Heladeria.Api/Controllers/SeguridadController.cs`
- `Heladeria.Api/Seguridad/Autorizacion/ExtensionesClaimsPrincipal.cs`
- `Heladeria.Api/Seguridad/Autorizacion/ManejadorPermiso.cs`
- `Heladeria.Api/Seguridad/Autorizacion/ProveedorPoliticasPermisos.cs`
- `Heladeria.Api/Seguridad/Autorizacion/RequierePermisoAttribute.cs`
- `Heladeria.Api/Seguridad/Contrasenas/CambioContrasenaObligatorioMiddleware.cs`
- `Heladeria.Api/Seguridad/Contrasenas/PermitirContrasenaTemporalAttribute.cs`
- `Heladeria.Pruebas/Seguridad/AutorizacionHttpPruebas.cs`
- `Heladeria.Pruebas/Seguridad/EscenarioSeguridad.cs`
- `Heladeria.Pruebas/Seguridad/ServicioAutenticacionPruebas.cs`
- `Heladeria.Pruebas/Seguridad/BootstrapAdministradorPruebas.cs`
- `Heladeria.Bootstrap/Heladeria.Bootstrap.csproj`
- `Heladeria.Bootstrap/Program.cs`
- `docs/seguridad-fase-2.md`
- `docs/bootstrap-administrador.md`

### Modificados

- `Heladeria.Infraestructura/InyeccionDependencias.cs`
- `Heladeria.Api/Program.cs`
- `Heladeria.Api/ManejoErrores/ManejadorGlobalExcepciones.cs`
- `Heladeria.Api/appsettings.json`
- `Heladeria.Api/appsettings.Testing.json`
- `Heladeria.Api/Heladeria.Api.csproj`
- `Heladeria.Infraestructura/Heladeria.Infraestructura.csproj`
- `Heladeria.Pruebas/Heladeria.Pruebas.csproj`
- `Heladeria.sln`
- `README.md`

## Riesgos y pendientes autorizables

- La base local revisada no contiene usuarios, roles, estados, sucursales ni permisos. El bootstrap explícito ya está disponible, pero debe ejecutarlo el operador para introducir la contraseña sin exponerla al proceso de desarrollo automatizado.
- Para alto volumen conviene aprobar índices en `SesionesUsuarios(UsuarioId, Cerrada)`, `HistorialAccesos(UsuarioId, Fecha)` y `Bitacoras(UsuarioId, Fecha)`. No se aplicaron porque cambiarían el esquema fuente.
- El rate limiting es por instancia. Un despliegue con varias réplicas debe aplicar un límite distribuido en gateway/proxy.
- Si el frontend se hospeda en otro sitio y obliga a cambiar `SameSite`, debe agregarse defensa CSRF antes de permitir cookies cross-site.
- En un proxy inverso deben configurarse hosts/redes confiables para encabezados reenviados antes de usar la IP de cliente en límites y auditoría.

## Recomendación para Fase 3

Antes de iniciar módulos funcionales, ejecutar y verificar el bootstrap documentado; después, hacer que todos los casos de uso de Fase 3 reciban `usuarioId`, `empresaId` y `sucursalId` exclusivamente del contexto autenticado.
