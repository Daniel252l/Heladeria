# Alta y consulta mínima de clientes — validación Fase 9

## Diagnóstico y alcance

Se reutilizan `dbo.Clientes`, su entidad Database First y `Bitacoras`. No hay dependencia obligatoria de Persona/Tercero. `ContactosClientes` y `DireccionesClientes` son hijos opcionales: el alta mínima no los crea. No cambia SQL, EF generado, Ventas, CxC/CxP, frontend, bootstrap ni permisos asignados.

El esquema exige:

| Campo | Regla real / tratamiento |
| --- | --- |
| Id | Identity SQL Server; nunca se recibe ni se calcula en la API. |
| EmpresaId | FK obligatoria a Empresas; procede del contexto autenticado. |
| CodigoCliente | varchar(20), obligatorio; trim y mayúsculas; único **por empresa** (`UQ_ClientesCodigo`). |
| NombreCliente | varchar(150), obligatorio; trim. |
| Nit / Telefono / Correo | Opcionales, varchar(20)/varchar(20)/varchar(100). Correo válido si se proporciona. No existe unicidad de NIT/correo. |
| LimiteCredito | decimal(12,2), obligatorio, >= 0; predeterminado 0; se rechaza precisión excedida, sin redondear. |
| DiasCredito | smallint, obligatorio, >= 0; predeterminado 0. |
| EstadoId | FK obligatoria a Estados. Selección automática de estado operativo existente. |
| ListaPrecioId / EstadoCreditoId | FKs opcionales a ListasPrecios/Estados; quedan NULL. |

Cliente es un catálogo de empresa, no de sucursal. Se exige asignación usuario/sucursal válida y que usuario, asignación, sucursal y empresa sean operativos. Listado/detalle nunca exponen otra empresa.

Se elige un estado con `Activo = true` **y** nombre incluido en `Seguridad:EstadosActivos`, igual que Ventas. Se prefieren estados generales (`ModuloId = NULL`), después los del módulo VENTAS; el desempate es por Id ascendente. No se utilizan estados de otros módulos ni se confunde un estado semántico Inactivo con un estado operativo. Si no hay candidato, se rechaza con 409 `ESTADO_CLIENTE_NO_DISPONIBLE`, sin insertar.

El POST solamente admite los datos de cliente indicados abajo. Empresa, Id y estado son controlados por servidor. Los campos desconocidos no se mapean a la entidad. No se agregan reglas de crédito: los límites/días se almacenan tal como permite el catálogo existente; CxC mantiene su comportamiento previo.

## Endpoints y permisos existentes

| Endpoint | Permiso | Respuesta |
| --- | --- | --- |
| `GET /api/clientes` | `VENTAS.CONSULTAR` | 200, `elementos`, `pagina`, `tamanioPagina`, `totalRegistros`, `totalPaginas`. |
| `GET /api/clientes/{id}` | `VENTAS.CONSULTAR` | 200 o 404 `CLIENTE_NO_ENCONTRADO`, también para Id de otra empresa. |
| `POST /api/clientes` | `VENTAS.ADMINISTRAR` | 201, cliente creado y cabecera Location al detalle. |

Listado admite los filtros ya existentes: `busqueda` (código/nombre/NIT), `estadoId`, `pagina` (1 por defecto), `tamanioPagina` (20 por defecto, máximo 100). Orden por nombre e Id. El listado incluye estados no operativos cuando no se filtra; las referencias de Ventas siguen incluyendo solamente los operativos.

No se crean permisos ni se amplía CONSULTA. ADMIN ya tiene los permisos de Ventas mediante la preparación existente de Fase 7. Las referencias de Ventas mantienen `POS.OPERAR`.

## Validación manual

Con sesión de ADMIN, usar en Swagger `POST /api/clientes`:

```json
{
  "codigoCliente": "CLI-VALIDACION",
  "nombreCliente": "Cliente de validación CxC",
  "nit": "123456-7",
  "telefono": "55550000",
  "correo": "cliente@example.test",
  "limiteCredito": 1000.00,
  "diasCredito": 30
}
```

Solamente código y nombre son obligatorios en el JSON. Omitir los opcionales que no se conozcan. El ejemplo es para captura manual: no hay clientes preinsertados en la BD real.

El resultado contiene `id`, `empresaId`, `codigoCliente`, `nombreCliente`, datos opcionales, límite/días y `estadoId`/`estado`. Usar el **id devuelto**, no uno supuesto:

1. Consultar `GET /api/clientes/{id}` y `GET /api/clientes?busqueda=CLI-VALIDACION`.
2. Consultar `GET /api/ventas/referencias`: el mismo cliente aparece inmediatamente, sin caché ni cambio del contrato de Ventas.
3. Utilizar ese `clienteId` en el flujo existente de venta a crédito y su fecha de vencimiento.

Repetir el código en la misma empresa devuelve 409 `CLIENTE_DUPLICADO`; NIT/correo iguales con códigos distintos están permitidos por el esquema. Datos inválidos devuelven 400 (validación MVC o `VALIDACION_CLIENTES`). La ausencia de permiso devuelve 403; sin autenticación, 401.

Alta y bitácora `CLIENTE_CREADO` se confirman en una única transacción. La auditoría registra Id, usuario, empresa, sucursal y hora UTC, sin copiar datos personales al comentario. Si falla la bitácora se revierte el cliente. La restricción única real resuelve altas concurrentes; su error se traduce a 409.

## Pruebas y archivos

- `Heladeria.Aplicacion/Clientes/ContratosClientes.cs`: solicitud sellada, validaciones, respuesta, interfaz y error controlado.
- `Heladeria.Infraestructura/Clientes/ServicioClientes.cs`: alcance, consultas, alta transaccional y auditoría.
- `Heladeria.Infraestructura/InyeccionDependencias.cs`: registro del servicio.
- `Heladeria.Api/Controllers/ClientesController.cs`: tres endpoints y permisos.
- `Heladeria.Api/ManejoErrores/ManejadorGlobalExcepciones.cs`: códigos HTTP de clientes.
- `Heladeria.Pruebas/Fase9/ClientesHttpPruebas.cs`: HTTP, validación, permisos, detalle/listado/referencias y CxC positiva con cliente registrado por API.
- `Heladeria.Pruebas/Fase9/ClientesServicioPruebas.cs`: clave natural, aislamiento, estado, datos mínimos y rollback/reintento.
- `Heladeria.Pruebas/Fase9/ClientesSqlPruebas.cs`: concurrencia con clave única real sobre LocalDB privada desechable.
- Este documento.

Ejecutar desde el directorio de la solución, secuencialmente:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Los fixtures SQL usan una instancia y base privadas con nombres generados, nunca conexiones de appsettings ni la BD real. No hay scripts SQL nuevos ni cambios de esquema.

Resultado verificado el 2026-10-12: build Release con 0 errores y 0 advertencias; suite completa con **521 aprobadas, 0 fallidas y 0 omitidas** (33 nuevas de clientes, incluyendo concurrencia SQL Server y validación positiva de CxC). No fue necesario cambiar pruebas anteriores. No se ejecutó bootstrap ni ninguna operación contra la BD real; las preparaciones de fixtures se ejecutaron exclusivamente en bases aisladas de pruebas.
