# Fase Visual 2: configuración, productos e inventario

## Alcance

Esta fase conecta el frontend React con los contratos existentes de Configuración,
Productos e Inventario. No incorpora compras, recetas, producción, ventas, POS,
caja, cuentas por cobrar o cuentas por pagar. Tampoco modifica el esquema de SQL
Server ni `Heladeria.sql`.

## Rutas visuales

- Configuración: `/configuracion/estados`, `/clases-estados`, `/tipos`,
  `/clases-tipos`, `/modulos`, `/acciones`, `/niveles-acceso`, `/empresas` y
  `/sucursales`.
- Productos: `/productos`, `/productos/categorias`, `/marcas`, `/tipos`,
  `/unidades-medida`, `/presentaciones` y `/conversiones-unidad`.
- Inventario: `/inventario/existencias` y `/inventario/ajustes`.

Las rutas siguen usando los guardas de sesión y permiso de la Fase Visual 1. Los
botones de escritura se ocultan sin el permiso de administración correspondiente;
no existe bypass por nombre de rol.

## API consumida

### Configuración

Los mantenimientos consumen sus colecciones, detalles y operaciones reales bajo:

- `api/configuracion/clases-estado`
- `api/configuracion/estados`
- `api/configuracion/clases-tipo`
- `api/configuracion/tipos`
- `api/configuracion/modulos`
- `api/configuracion/acciones-sistema`
- `api/configuracion/niveles-acceso`
- `api/configuracion/empresas`
- `api/configuracion/sucursales`

Se agregó `GET api/configuracion/referencias-organizacion` como ajuste mínimo de
backend. Entrega búsquedas paginadas y protegidas de proveedores, clientes,
departamentos y municipios para que el formulario de sucursal use claves reales.
Los proveedores y clientes se limitan a la empresa autorizada; los municipios se
pueden limitar por departamento.

### Productos

- CRUD, detalle y referencias: `api/productos` y `api/productos/referencias`.
- Presentaciones por producto: `api/productos/{productoId}/presentaciones`.
- Catálogos: `categorias`, `marcas`, `tipos`, `unidades-medida` y
  `presentaciones` bajo `api/productos`.
- Conversiones: `api/productos/conversiones-unidad`.

Producto, sus presentaciones y los cinco catálogos envían `VersionFila` al editar
o cambiar estado y ofrecen recarga frente a un conflicto 409. Las conversiones no
tienen versión ni endpoint de detalle en el contrato actual; la interfaz no inventa
ninguno.

### Inventario

- `GET api/inventario/almacenes`
- `GET api/inventario/existencias`
- `POST api/inventario/ajustes`

Un ajuste toma solamente identificadores devueltos por la API. Antes del POST
muestra una confirmación con el almacén, producto, presentación, cantidad y
estimación en unidad base. Esa estimación usa el factor real de la presentación;
el servidor sigue siendo la autoridad. En una salida, la interfaz no envía costo
unitario y deja que el backend aplique el costo promedio vigente.

## Permisos

- `CONFIGURACION.ADMINISTRAR`
- `PRODUCTOS.CONSULTAR` y `PRODUCTOS.ADMINISTRAR`
- `INVENTARIO.CONSULTAR` y `INVENTARIO.ADMINISTRAR`

Para preparar un ajuste se requieren las consultas de Productos e Inventario,
además de `INVENTARIO.ADMINISTRAR` para entrar a la ruta y confirmar el POST.

## Condiciones operativas

- Las listas y selectores consultan el backend con búsqueda, filtros y paginación;
  no contienen identificadores identity codificados.
- El formulario de ajustes necesita una sucursal asignada con bodega y ubicación
  operativas. Si no existen, presenta un estado vacío y no crea datos técnicos.
- La base actual no expone `VersionFila` en los DTO de Configuración ni en
  conversiones de unidad. Por ello el control optimista se aplica solo donde el
  contrato real lo soporta.
- Las imágenes se manejan como las rutas o URL de texto admitidas actualmente por
  el contrato; esta fase no implementa almacenamiento de archivos.

## Validación

La validación de entrega se realiza con:

```text
cd heladeria-web
npm run lint
npm test -- --maxWorkers=1
npm run build

cd ..
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

El límite de un worker en Vitest solo estabiliza el consumo de recursos del entorno
de validación; no altera el código de producción.
