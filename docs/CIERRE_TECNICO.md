# Cierre técnico — evidencia y pendientes

Fecha de verificación: 20/10/2026. Backend fases 1–10 y visuales 1–8 aprobados por el propietario. Este documento no aprueba el despliegue ni declara el proyecto 100% completado.

**Actualización del 21/10/2026 (America/Guatemala):** las dos dependencias npm HIGH quedaron corregidas con patches compatibles: Vite 8.2.4 y source-map-js 1.2.2. Auditoría completa: 0 vulnerabilidades. Regresión: 521 pruebas aprobadas, lint/build y Visual 8 correctos. La evidencia del cierre del día 20 se conserva como historial; los resultados finales de dependencias están en la sección de actualización al final.

## Diagnóstico previo a modificaciones

Se inspeccionaron los seis proyectos de la solución, configuración común/Development/Production/Testing, perfiles de inicio, referencias/packages, frontend, maestro/reparaciones SQL, preparadores, seguridad de publicación, documentación histórica, artefactos y conexiones/rutas locales. No se conectó a la BD real ni se exportaron secretos.

| Clasificación | Hallazgo / tratamiento |
|---|---|
| Versionable | Código y pruebas, `.sln`/`.csproj`, global.json, props, tool-manifest, package.json/lock, SQL controlado, configuración común/productiva vacía de secretos, plantillas y documentación |
| Ignorado | bin/obj, `.vs`, archivos VS personales, node_modules/dist, resultados/capturas/coverage, `.env.local`, logs, respaldos, claves/certificados privados, tokens/exports temporales, artifacts y entregas incompletas |
| Solo desarrollo/pruebas | Development y launchSettings con VictusPabloE/localhost, Testing con clave de fixture, scripts visuales y test-data, `.http`, certificado HTTPS localhost, utilidad usuario CONSULTA |
| Externo en producción | Conexión SQL, JWT, identidad runtime, hosts/CORS, TLS, logs/retención, backup, dominios, moneda y configuración real |
| No publicable | Fuentes/tests, SQL de reparación de datos de prueba, configuración local/Testing, node_modules, secretos, certificados, respaldos, `.vs`, capturas, logs; Bootstrap nunca en ruta web pública |

- No hay repositorio `.git` accesible en esta carpeta/ancestros. Se puede establecer qué **debe** versionarse, no certificar historial Git ni retirar secretos de commits inexistentes/inaccesibles. No se inicializó un repositorio sin solicitud.
- Se encontraron bin/obj y `.vs`, node_modules/dist/test-results y una carpeta interna `Heladeria` con bin/obj. Se preservaron; `.gitignore` los excluye. No se borraron archivos útiles.
- Configuración común/Production ya tenían conexión y JWT vacíos; no fue necesario cambiar sus valores. Development conserva la conexión local solicitada. `AllowedHosts=localhost` es un default restrictivo que debe sobrescribirse externamente, no una configuración productiva final.
- No se encontraron claves privadas ni secretos productivos en los archivos fuente/configuración inspeccionados. Las claves de Testing/fixtures no son credenciales reales y no se publican. No se leyeron valores de User Secrets. Esto no sustituye un escaneo del historial Git/gestor de secretos antes de entregar.
- Existen certificados localhost en el almacén personal, uno caducado y otro vigente; solo se inspeccionó metadata pública. No se exportaron, borraron ni sustituyeron; ninguno pertenece al paquete productivo.
- URL API frontend ya era configurable por `VITE_API_URL` con fallback al origen. El riesgo era compilar inadvertidamente `.env.local`; el publicador fija la elección explícita sin editar el archivo local.
- Swagger solo está registrado en Development. Health live/ready/salud ya estaban corregidos; no se modificaron. Seguridad/CORS/refresh/headers/rate limiting se mantienen, con sus límites operativos documentados.
- Maestro final: 128 tablas y cero datos al instalar. Bootstrap/preparadores crean referencias y permisos mediante IDs generados, no datos comerciales. No se requiere modificar SQL, entidades ni reglas financieras.

## Cambios realizados

1. `.gitignore`: más exclusiones para resultados, cobertura, artefactos, archivos de entorno, material privado y respaldos; excepciones para plantillas `.env.*.example`. Ignorar no elimina archivos ni sanea un historial Git.
2. API csproj: no publicar appsettings de ambientes ajenos a Production, archivos HTTP/perfiles ni material privado conocido. Mantiene configuración común/Production y dependencias runtime.
3. Bootstrap csproj: publicar Production junto a común; Development solo para build local. No modifica Program, servicios ni comportamiento del CLI.
4. `scripts/Publicar.ps1`: salida nueva, opciones HTTPS explícita/mismo origen, build web con precedencia sobre `.env.local`, API/Bootstrap framework-dependent, verificación de contenido y manifiesto SHA-256. Sin ejecución de servicios/SQL/Bootstrap. Rechaza sobrescribir una entrega.
5. Plantilla web pública y documentación operativa. No se cambió ningún componente, pantalla, contrato o cliente API.
6. Cinco pruebas backend de entrega: dos casos de configuración sin secretos, dos de metadata de publicación y una instalación completa SQL/Bootstrap aislada.

Se compararon hashes con una instantánea previa de 535 archivos fuente/configuración/documentación: sin cambios en código funcional, frontend `src`, SQL, package.json/lock, permisos ni health. Los únicos C# nuevos son pruebas de entrega.

## Maestro, bootstrap y entrega

- Maestro conservado, copiado byte por byte a `artifacts/database/Heladeria.sql`; hash coincidente. No es migración idempotente; instalación limpia con aborto ante errores. Ver BASE_DATOS.
- Se conserva ADMIN → fase3 → fase4 → fase5 → fase6 → fase7 → fase8 → fase9 → fase10. ADMIN/3/4 rechazan repetición; 5–10 reutilizan configuración compatible sin duplicar. Transacción por paso, no por secuencia completa. No se inventa un `--preparar-produccion` con garantías distintas.
- Prueba nueva ejecuta el orden completo con DI real, contextos independientes y PasswordHasher registrado. Solo en instancia/BD LocalDB privadas GUID; valida rechazo de repetición/compatibilidad y ausencia de operaciones financieras/demo.
- API y Bootstrap: publish Release framework-dependent sin símbolos de depuración. Configuración externa y Runtime 8 necesarios.
- Web: build con **mismo origen** (`apiUrl: window.location.origin` comprobado en JavaScript publicado). Si API está en subdominio, recompilar con su origen real. No hay API local hardcodeada.
- React Router conserva dos literales internos `http://localhost` sin puerto como base sintáctica de URL. No son configuración ni destino de solicitudes de la API. El verificador se ajustó tras comprobar ese falso positivo; no se alteró la librería.

| Artefacto | Archivos | Bytes |
|---|---:|---:|
| api | 69 | 21.896.163 |
| bootstrap (separado, privado) | 63 | 19.633.566 |
| database | 1 | 177.897 |
| web | 25 | 669.963 |

158 archivos comprobados + `manifest.json`; hashes SHA-256 verificados sin discrepancias. Maestro SHA-256: `BEA99F04F971FA394B04A86C02A6CFAAE2AA3C9D692B5B12ED1A966A0C2ABB0A`. Sin Development/Testing, .env, node_modules, logs, certificados, User Secrets, datos reales ni resultados de pruebas. El manifiesto se refiere al publicador: SQL/Bootstrap no fueron ejecutados por él. Las pruebas sí los ejercitaron exclusivamente en LocalDB aislado.

La primera entrega incompleta se conserva en `artifacts-incompletos-20261020/`, ignorada y **NO desplegable** (sin manifiesto). No se eliminó ningún archivo fuente ni esa copia generada. La entrega válida es únicamente `artifacts/`.

## Validaciones exactas del cierre original — 20/10/2026

Herramientas locales: SDK 8.0.403, Node 24.16.0, npm 11.13.0, SQL Server LocalDB 2019 15.0.4382.1. No se ejecutaron migraciones, reparaciones, backup, restore ni bootstrap sobre la BD real.

| Comando | Resultado final |
|---|---|
| `dotnet restore` | exit 0; los seis proyectos al día |
| `dotnet build --configuration Release` | exit 0; 0 errores, 0 advertencias (4,02 s en ejecución final explícita) |
| `dotnet test --configuration Release` | exit 0; **622 aprobadas, 0 fallidas, 0 omitidas**; 2 min 18 s |
| Prueba dirigida `FullyQualifiedName~CierreTecnicoPruebas` | 5 aprobadas, 0 fallidas, 0 omitidas; 24 s |
| `dotnet list package --vulnerable` | exit 0; ningún paquete vulnerable reportado para los seis proyectos en los feeds consultados |
| `dotnet list package --vulnerable --include-transitive` | exit 0; ningún paquete vulnerable reportado, incluidas transitivas |
| `npm ci` | exit 0; 121 paquetes instalados, 122 auditados; reportó 2 dependencias con severidad alta |
| `npm run lint` | exit 0; sin diagnósticos |
| `npm test` | exit 0; **521 aprobadas, 0 fallidas, 0 omitidas**, 27 archivos; 91,80 s |
| `npm run build` | exit 0; TypeScript/Vite completados, 1.942 módulos; build explícito 1,15 s |
| `npm audit --json` (adicional) | exit 1; 2 high, 0 critical; detalle abajo |
| `Publicar.ps1 -MismoOrigen` | exit 0; ambas publicaciones y build final web correctos; manifiesto verificado |
| Protección de entrega existente | Segunda ejecución rechazada (exit 1 esperado); manifiesto sin cambios |
| Enlaces Markdown locales y sintaxis PowerShell | Correctos; sin errores |

Incidencias previas, resueltas sin debilitar pruebas: primer `npm ci` falló EPERM por Vite abierto; el usuario lo detuvo y se repitió correctamente. La nueva prueba tuvo un error de inicialización de propiedad init; corregido en la prueba, no en el modelo. El primer chequeo de publicación detectó el literal interno de React Router; se comprobó y ajustó la clasificación, conservando el paquete incompleto aparte.

La suite completa incluye regresión de autenticación/permisos, configuración/empresa/sucursal, productos/inventario/Kardex/costo, compras, producción/recetas, ventas/POS, Caja, cartera, reportes, auditoría, concurrencia, health y HTTP. No se omitieron pruebas para cerrar el informe.

## Inventario de riesgos residuales — no ocultarlos

1. **Dependencias npm, corregidas el 21/10/2026:** las versiones vulnerables `vite@8.2.2` y `source-map-js@1.2.1` se sustituyeron por 8.2.4 y 1.2.2. Auditoría final completa: 0 vulnerabilidades de cualquier severidad. Se conserva abajo el detalle de los cuatro advisories originales; no se ocultaron dependencias dev/optional. Mantener auditorías periódicas: un resultado limpio hoy no garantiza ausencia de vulnerabilidades futuras. No exponer Vite dev/preview como hosting productivo.
2. **Soporte .NET:** fin de soporte 8 el 10/11/2026. Dependencias EF/ASP.NET están fijadas a 8.0.30; un resultado NuGet sin avisos no garantiza vigencia futura. Planificar actualización soportada/parches con regresión, no migrada en este cierre. [Política](https://dotnet.microsoft.com/en-us/platform/support/policy).
3. **Destino aún indefinido:** hosting, certificados, dominio, secretos, AllowedHosts/CORS, cuenta de servicio y privilegios SQL deben aprovisionarse/validarse por operador. IIS/Kestrel están documentados, no desplegados.
4. **Configuración comercial:** moneda base real y datos de empresa/sucursal/cajas/catálogos. No existe mantenimiento HTTP de Monedas; alta controlada por DBA con autorización del propietario. No se inventan datos para hacer pasar un smoke.
5. **Operación:** backups/restauración, RPO/RTO, retención, monitoreo, logs y rollback necesitan ensayo y firma humana. No se ejecutó smoke contra producción.
6. **Escalado/proxy:** no se declara soporte validado para balanceo multiinstancia. Rate limits son locales al proceso; proxy genérico requiere tratamiento explícito de headers confiables. No desactivar controles para acomodar infraestructura.
7. **Repositorio:** falta historial Git auditable. Antes de versionar/subir, revisar staged files y realizar secret scanning; `.gitignore` por sí solo no prueba ausencia histórica de secretos.
8. **Datos de validación existentes:** no inspeccionados ni limpiados. Preferir BD limpia. No convertir IDs/documentos históricos de prueba en semillas productivas.

## Archivos del cierre

Modificados (5):

- `.gitignore`
- `README.md`
- `Heladeria.Api/Heladeria.Api.csproj`
- `Heladeria.Bootstrap/Heladeria.Bootstrap.csproj`
- `heladeria-web/README.md`

Creados (7):

- `scripts/Publicar.ps1`
- `heladeria-web/.env.production.example`
- `Heladeria.Pruebas/Entrega/CierreTecnicoPruebas.cs`
- `docs/DEPLOYMENT.md`
- `docs/BASE_DATOS.md`
- `docs/DATOS_DE_PRUEBA.md`
- `docs/CIERRE_TECNICO.md`

Generados locales/ignorados: `artifacts/`, copia incompleta separada, bin/obj, node_modules/dist. Sin archivos fuente eliminados. Sin cambios funcionales, visuales, contratos, esquema, datos reales o permisos.

## Checklist manual final

- [x] Resolver vulnerabilidades npm con actualización revisada y volver a ejecutar validación completa (21/10/2026; evidencia abajo).
- [ ] Aprobar plan de runtime soportado, hosting, DNS y certificados.
- [ ] Instalar BD nueva con maestro verificado; o plan independiente para BD existente.
- [ ] Provisionar secretos/identidades/privilegios mínimos/TLS/AllowedHosts/CORS finales.
- [ ] Ejecutar explícitamente bootstrap y preparadores solo donde correspondan; verificar marcadores.
- [ ] Contraseña definitiva del administrador, empresa/sucursal operativas y moneda/configuración real.
- [ ] Publicar artefactos correctos, hashes y URL/API/modalidad web verificados; no publicar Bootstrap públicamente.
- [ ] Health 200, 503 ensayado en staging, Swagger ausente en Production, cookies/headers/permisos probados.
- [ ] Smoke: login, sesión, sucursal, productos, inventario, consultas de compras/ventas/Caja/CxC/CxP/reportes/auditoría y logout, sin transacciones financieras de prueba.
- [ ] Backup previo, restore aislado ensayado, RPO/RTO/retención y rollback acordados.
- [ ] Revisión de secretos/versionado y aceptación final firmada por propietario.

## Actualización controlada de dependencias frontend — 21/10/2026

### Diagnóstico antes de modificar los manifiestos

Se ejecutaron `npm audit`, `npm audit --json` y `npm outdated` antes de editar package.json/lock. Audit devolvió exit 1, 2 paquetes HIGH y 0 CRITICAL. Outdated devolvió exit 1 porque existen actualizaciones disponibles (incluida Vite 8.3.4); no se aplicó una actualización general. La corrección mínima no requería minor ni major.

| Paquete | Tipo | Antes | Final | Causa y solución |
|---|---|---|---|---|
| vite | devDependency directa; compartida por plugins/Vitest | 8.2.2 (`^8.2.2`) | **8.2.4**, versión exacta en package.json y lock | launch-editor incluido por Vite podía manejar argumentos/rutas de forma insegura; en Windows, evasión de comprobación de archivo y escape insuficiente podían conducir a ejecución de comandos. Patch 8.2.4 corregido |
| source-map-js | Transitiva, no añadida como directa | 1.2.1 | **1.2.2**, fijada por lock | Offsets no validados de mapas indexados maliciosos podían bloquear el event loop. Patch 1.2.2 valida esos valores |

Avisos originales, conservados para trazabilidad:

- [GHSA-698v-4r3x-3vcv](https://github.com/vitejs/launch-editor/security/advisories/GHSA-698v-4r3x-3vcv): escape de `%` en Windows (moderate en audit).
- [GHSA-r825-w8mm-5pxw](https://github.com/vitejs/launch-editor/security/advisories/GHSA-r825-w8mm-5pxw): argumentos interpretados como flags del editor (moderate).
- [GHSA-587h-7wmj-h5pm](https://github.com/vitejs/launch-editor/security/advisories/GHSA-587h-7wmj-h5pm): evasión de `existsSync` en Windows (high).
- [GHSA-68fv-2mgg-jv7q](https://github.com/advisories/GHSA-68fv-2mgg-jv7q): denegación de servicio con mapas indexados (high); [corrección oficial source-map-js 1.2.2](https://github.com/7rulnik/source-map-js/releases/tag/v1.2.2).

Las dos alertas de severidad moderada estaban agrupadas bajo el paquete Vite HIGH; no eran dos paquetes adicionales. Los advisories siguen publicados como historial, pero ninguna versión del árbol final aparece afectada en la auditoría consultada.

Árbol exacto relevante después de la corrección (mismos padres/versiones que antes):

```text
heladeria-web
├─ vite@8.2.4
│  └─ postcss@8.5.26 ─ source-map-js@1.2.2
├─ @tailwindcss/vite@4.3.3
│  ├─ vite@8.2.4 (deduplicado)
│  └─ @tailwindcss/node@4.3.3 ─ source-map-js@1.2.2
├─ jsdom@30.0.1
│  └─ css-tree@3.2.1 ─ source-map-js@1.2.2
├─ @vitejs/plugin-react@6.1.1 ─ vite@8.2.4 (deduplicado)
└─ vitest@5.0.0
   ├─ vite@8.2.4 (deduplicado)
   └─ @vitest/mocker@5.0.0 ─ vite@8.2.4 (deduplicado)
```

Todos los padres de source-map-js declaran `^1.2.1`, que admite 1.2.2. No hizo falta actualizar padres, agregar dependencia directa, usar overrides/resolutions ni ejecutar audit fix (tampoco `--force`). Se fijó Vite a 8.2.4 para evitar un minor incidental; el resto de rangos existentes se conservó.

Compatibilidad comprobada con manifests publicados y locales:

- Node actual **24.16.0**, npm **11.13.0**. Vite/plugin-react admiten `^20.19.0 || >=22.12.0`; source-map-js admite Node >=0.10.0; Vitest 5 admite Node 24. Node actual cumple todos ellos.
- plugin-react 6.1.1 admite Vite `^8.0.0`; Tailwind Vite 4.3.3 admite `^5.2.0 || ^6 || ^7 || ^8`; Vitest 5.0.0 admite `^6.4.0 || ^7.0.0 || ^8.0.0`.
- React/ReactDOM **19.2.8**, TypeScript **6.0.3**, Tailwind **4.3.3**, plugin-react **6.1.1**, Vitest **5.0.0** y PostCSS **8.5.26** permanecen intactos. No se actualizó TypeScript 7, React 19.3 ni los demás paquetes listados como outdated.
- `npm ls --all --json`: exit 0, sin dependencias inválidas ni conflictos de peers.

### Cambios mínimos y lockfile

Únicos archivos fuente/documentación modificados en esta actualización:

1. `heladeria-web/package.json`: `vite: "^8.2.2"` → `"8.2.4"`.
2. `heladeria-web/package-lock.json`: resolución/integridad nuevas de Vite y source-map-js, generadas mediante `npm update vite source-map-js --package-lock-only --ignore-scripts`.
3. Este documento: diagnóstico, evidencia y pendientes actualizados.

Npm también completó seis nodos opcionales anidados que ya declaraba `@tailwindcss/oxide-wasm32-wasi@4.3.3`: `@emnapi/core@1.11.1`, `@emnapi/runtime@1.11.1`, `@emnapi/wasi-threads@1.2.2`, `@napi-rs/wasm-runtime@1.1.4`, `@tybys/wasm-util@0.10.2` y `tslib@2.8.1`. Son resolución nativa del lock para la variante **wasm32**, no dependencias directas ni una actualización de Tailwind. En Windows continúan instalándose 121 paquetes. El árbol multiplataforma de audit pasa de 185 a 191 dependencias por estos seis nodos opcionales. No se suprimieron manualmente para maquillar el diff o la auditoría.

No cambió la versión de ningún otro nodo preexistente. Sin cambios a backend, SQL, contratos, permisos, código de vistas/UX, CSS, lógica de negocio, pruebas existentes ni scripts visuales/publicador. Sin tests eliminados, expectativas debilitadas o nuevos overrides.

### Regresión final

| Ejecución | Resultado |
|---|---|
| `npm ci` | **exit 0**; 121 paquetes instalados, 122 auditados en 18 s; 0 vulnerabilidades |
| `npm audit` | **exit 0; found 0 vulnerabilities** |
| `npm audit --json` | **exit 0**; `vulnerabilities: {}`; info/low/moderate/high/critical/total = **0** |
| `npm run lint` | **exit 0; 0 errores**, sin diagnósticos |
| `npm test` | **exit 0; 521 aprobadas, 0 fallidas, 0 omitidas**, 27 archivos, 92,37 s |
| `npm run build` | **exit 0**, TypeScript y Vite 8.2.4 correctos; 1.942 módulos; Vite 809 ms |
| Script Visual 8 | **exit 0**, antes y después; 9 vistas × 4 anchos = **36 escenarios/capturas**; 0 errores |
| `Publicar.ps1 -MismoOrigen` | **exit 0**; Release API/Bootstrap + web; build web publicado 543 ms |

Visual: `node scripts/verificar-visual-reportes.mjs http://127.0.0.1:5190 <node_modules-bundled-con-playwright> msedge`, con Edge headless, anchos 320/390/768/1366, zona del navegador Tokyo y sucursal Guatemala. Todas las peticiones API/autenticación fueron interceptadas; cero peticiones inesperadas, errores de consola, overflow de página, fallos de filtros/paginación/limpieza, permisos o navegación. No se usaron usuarios, sesiones ni datos reales.

La primera invocación antes de iniciar el preview devolvió conexión rechazada; se inició el servidor local y se repitió correctamente. No fue un fallo de aplicación ni se cambió una prueba para pasarlo.

Comparación visual sin alterar capturas ni código: 10/36 PNG idénticos por hash (incluidos los 9 desktop); 26 diferencias limitadas al contenido de las tablas desplazables después de ArrowRight/restauración del scroll. Dimensiones iguales y regiones externas intactas. La revisión de capturas, límites de diferencias y **artefactos web byte por byte idénticos** confirma que no es un cambio de diseño; es la captura del scroll aún en movimiento. El script sigue siendo smoke funcional/responsive, no una prueba determinista de píxeles con scroll estabilizado. Se informa esta limitación, sin debilitar ni modificar el script.

### Entrega regenerada y preservación

El publicador aprobado no ofrece modo web-only; se ejecutó sin modificarlo. Antes se conservó la entrega completa anterior en `heladeria-web/test-results/entrega-anterior-dependencias-20261021/` (ignorada, recuperable). La referencia visual quedó en `heladeria-web/test-results/dependencias-antes-20261021/`; la posterior en `heladeria-web/test-results/visual-reportes/`.

Se regeneraron `artifacts/web` y `manifest.json`, y necesariamente `api/database/bootstrap` como parte del publicador completo. **158/158 archivos coinciden por SHA-256 con la entrega anterior**, incluidos todos los binarios API/Bootstrap, el maestro SQL y los 25 archivos web. Solo cambia la fecha de generación del manifiesto, cuyos hashes fueron recalculados y verificados. Es esperable: los parches corrigen herramientas dev/build, no cambian los bytes de esta aplicación compilada.

Se mantiene modo mismo origen, sin API localhost embebida, Development/Testing, secretos, node_modules ni datos de prueba. No se ejecutaron API, SQL, bootstrap, migraciones o cambios contra BD real. Los previews de pruebas se cerraron al terminar. No se desplegó al hosting.

Backend conserva su resultado previo de **622/622** del cierre del día 20; no se volvió a ejecutar esa suite en este ajuste exclusivamente npm. La publicación Release sí se completó y los binarios permanecieron idénticos.

### Riesgos residuales después de esta corrección

- Ningún advisory afecta al árbol npm final según audit consultado el 21/10/2026; repetirlo antes de futuras entregas. Existen versiones más recientes no necesarias para resolver estos avisos; no se actualizaron indiscriminadamente.
- No usar Vite dev/preview como servidor de producción ni exponerlo públicamente; el hosting sirve estáticos publicados.
- La comparación screenshot a screenshot está limitada por el scroll animado ya existente, descrito arriba; no se promete igualdad de píxeles en todas las capturas del smoke.
- Siguen pendientes los puntos operativos del inventario: soporte .NET, hosting/TLS/secretos/CORS, configuración real, backup/restore y aceptación humana. Resolver npm no aprueba automáticamente la instalación productiva.

DEPENDENCIAS FRONTEND REVISADAS — PENDIENTE DE VALIDACIÓN FINAL DEL USUARIO
