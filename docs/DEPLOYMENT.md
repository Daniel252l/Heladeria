# Despliegue y operación inicial

Estado: preparación técnica local; no es autorización de puesta en producción. No se ejecutó contra la BD real. Los dominios y rutas de este documento son ejemplos que debe sustituir el operador.

## 1. Prerrequisitos y decisiones

- Definir responsable, ventana de despliegue, dominio(s), certificados TLS válidos, cuenta de servicio, SQL Server y firewall, backup y rollback. RPO/RTO y retención son decisiones del propietario.
- Build reproducible: SDK de `global.json`, npm con `package-lock.json`, Node compatible con Vite; suite SQL completa requiere Windows + SQL Server LocalDB 2019. No se necesita LocalDB en producción.
- Servidor: ASP.NET Core Runtime 8 parcheado o Hosting Bundle 8 para IIS. Entrega framework-dependent, no lleva runtime. No requiere SDK ni Node para operar.
- Riesgo temporal: .NET 8 termina soporte el 10/11/2026; no planificar operación posterior sin una actualización validada. [Política oficial](https://dotnet.microsoft.com/en-us/platform/support/policy).
- Resolver/validar las vulnerabilidades npm registradas en [CIERRE_TECNICO](CIERRE_TECNICO.md) antes de autorizar despliegue. No servir producción con Vite dev/preview.

## 2. Configuración real

`appsettings.json` contiene defaults comunes sin conexión ni clave; `appsettings.Development.json` es exclusivamente local. `appsettings.Production.json` mantiene conexión vacía y CORS vacío. El publicador solo lleva configuración común/Production. No copia Development, Testing, launchSettings, User Secrets ni `.env.local`.

Las variables externas sobrescriben JSON. API/Bootstrap no leen archivos `.env`. En .NET, `__` representa una sección. Mantener los secretos en el proveedor del hosting/gestor seguro, con ACL, respaldo y rotación. User Secrets es una comodidad de DESARROLLO, no un almacén productivo cifrado.

| Variable | Uso y ejemplo sin secretos |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | API: `Production` |
| `DOTNET_ENVIRONMENT` | Bootstrap: `Production`; si se define también para API, debe coincidir |
| `ConnectionStrings__Heladeria` | Conexión SQL externa, ejemplo abajo |
| `Seguridad__Jwt__ClaveFirma` | Secreto aleatorio, mínimo 32 bytes UTF-8; recomendado generar al menos 64 bytes aleatorios y codificar Base64 |
| `Seguridad__Jwt__Emisor` | Identificador estable del emisor, p. ej. `https://api.heladeria.ejemplo.com` |
| `Seguridad__Jwt__Audiencia` | Audiencia estable, p. ej. `https://heladeria.ejemplo.com` |
| `AllowedHosts` | Solo nombres de host: `api.heladeria.ejemplo.com`, separados por `;` si procede, no URLs ni `*` |
| `Cors__OrigenesPermitidos__0` | Origen frontend exacto: `https://heladeria.ejemplo.com` (sin `/` final) |
| `Seguridad__CookieRefresh__SameSite` | `Strict` por defecto, apropiado para HTTPS en subdominios del mismo sitio; probar navegador real |
| `Seguridad__ZonaHoraria` | `America/Guatemala`; zona de negocio, no alterar timestamps almacenados |
| `Seguridad__Jwt__MinutosAccessToken` / `DiasRefreshToken` | Defaults 10 minutos / 7 días; cambiar solo mediante decisión de seguridad |
| `Seguridad__RateLimiting__IntentosLogin` / `Renovaciones` / `CambiosContrasena` / `VentanaSegundos` | 10 / 20 / 5 solicitudes por ventana de 60 segundos; prefijo completo para cada variable |
| `VITE_API_URL` | Variable PÚBLICA del build web; origen API HTTPS, no secreto ni configuración runtime |

Los nombres exactos de rate limiting y bloqueo se conservan en `Heladeria.Api/appsettings.json`; no se modifica su política durante este cierre. No crear claves desconocidas esperando que la API las valide.

Ejemplo SQL con identidad de servicio Windows administrada (no el usuario local del desarrollador):

```text
Server=tcp:sql.heladeria.ejemplo.com,1433;Database=Heladeria;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;
```

Si se usa autenticación SQL, el proveedor seguro suministra `User ID`/`Password` dentro de la conexión; no escribir valores reales en documentos, argumentos, web.config versionado ni appsettings. Validar cadena TLS y nombre del certificado. No usar VictusPabloE, `TrustServerCertificate=True` ni una conexión local de respaldo en Production.

La identidad de instalación necesita crear esquema. La de Bootstrap necesita las escrituras estructurales y bloqueos de aplicación usados por los preparadores. La API necesita solo DML/lectura sobre sus tablas y bloqueos transaccionales; no `sysadmin`, `db_owner`, CREATE/ALTER/DROP ni permisos de backup. El DBA debe validar las concesiones mínimas mediante staging; no copiar privilegios del desarrollador ni usar una cuenta compartida de instalación.

No imprimir variables ni habilitar transcript/dumps mientras se manipulan secretos. La clave JWT no se genera ni se distribuye en los artefactos. Mantenerla estable entre reinicios; una rotación debe planificar revocación/reautenticación, no cambiarla en cada arranque.

## 3. Validar y publicar

Desde la carpeta de la solución, con Vite detenido:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet list package --vulnerable
dotnet list package --vulnerable --include-transitive
cd heladeria-web
npm ci
npm run lint
npm test
npm run build
cd ..
```

Revisar código de salida de cada comando; no continuar ante fallo. Repetir el análisis de dependencias en cada entrega. No aplicar `npm audit fix --force` ni upgrades mayores sin revisión/regresión.

Publicación preferida, elegir una opción:

```powershell
pwsh -File scripts/Publicar.ps1 -ApiUrl https://api.heladeria.ejemplo.com
# Alternativa para un host que enruta API y SPA en el MISMO origen:
pwsh -File scripts/Publicar.ps1 -MismoOrigen
```

Requiere `artifacts/` inexistente. No borra entregas anteriores. Solo publica/copia; nunca instala SQL, ejecuta Bootstrap ni inicia servicios. Produce:

```text
artifacts/
  api/          # API + dependencias + JSON seguros + web.config para IIS
  web/          # estáticos compilados; nunca node_modules
  database/Heladeria.sql
  bootstrap/    # CLI administrativo separado, NO alojar en servidor web público
  manifest.json # hashes SHA-256; aparece solo si verificación de publicación terminó
```

El script usa `dotnet publish ... --configuration Release --no-restore --self-contained false -p:DebugType=None -p:DebugSymbols=false --output artifacts/api` y equivalente para Bootstrap. Las exclusiones MSBuild funcionan también al publicar manualmente. Eliminar/reubicar entregas incompletas solo después de revisarlas; la ausencia de manifiesto significa que NO están verificadas.

`.env.local` participa en builds Vite incluso en modo production. El publicador sobreescribe `VITE_API_URL` solo para su proceso y luego la restaura. `-MismoOrigen` activa el fallback existente a `window.location.origin`, sin URL local embebida. Los artefactos entregados en este cierre usan esa modalidad: para API en otro dominio, recompilar con su URL definitiva. No basta cambiar una variable en el servidor después del build.

No copiar la solución completa, fuentes, tests, `.vs`, bin/obj, certificados, logs, respaldos ni configuración del usuario. Proteger también los binarios y manifiesto contra escritura no autorizada. Transferir por canal seguro; verificar hashes antes de instalar. Mantener Bootstrap fuera del host público y retirar su acceso después del aprovisionamiento.

## 4. Instalar base y preparar ADMIN

Seguir [BASE_DATOS](BASE_DATOS.md). La API no crea tablas, catálogos ni usuarios al arrancar. Usar una sesión administrativa explícita, separada de la identidad runtime. No ejecutar bootstrap si la instalación ya existe. No incorporar scripts de reparación de pruebas a instalación limpia.

## 5. Hosting A — IIS / Windows Server

1. Instalar IIS y Hosting Bundle de la familia .NET 8 parcheada. Crear app pool dedicado, sin CLR administrado; identidad de servicio con privilegios mínimos.
2. Copiar solo `artifacts/api` a carpeta de versión nueva. Mantener `web.config` generado (ASP.NET Core Module); no guardar secretos en su copia versionada. Configurar entorno y secretos externamente al paquete.
3. Bindings HTTPS con certificado válido, hostname final y permisos de clave privada para el servicio correspondiente. Habilitar acceso anónimo a nivel IIS; autorización JWT/permisos la aplica la API. No forzar Windows Authentication sobre los endpoints públicos de salud/login.
4. App pool con lectura/ejecución de binarios, sin escritura general. Logs, si se configuran, a carpeta protegida separada con ACL/retención. No activar stdout detallado permanentemente.
5. Frontend en sitio estático separado (`artifacts/web`) con HTTPS y fallback SPA a `index.html` para rutas de UI, no para archivos inexistentes ni `/api`, `/health` o `/salud`.
6. Si se eligió `-MismoOrigen`, configurar explícitamente en IIS el enrutamiento del mismo host hacia API para `/api/*`, `/health/*`, `/salud`; requiere reglas/componentes de proxy apropiados, no vienen instalados por el proyecto. Si se usa API en subdominio, recompilar con `-ApiUrl` y configurar CORS.
7. Validar refresh cookie, redirects, host y dirección IP registrada con el proxy real. La integración estándar IIS maneja su escenario; no confiar en headers reenviados de cualquier origen.

[Guía oficial IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-8.0). No se probó un servidor IIS real durante este cierre.

## 6. Hosting B — Kestrel directo

Desde la carpeta publicada, con variables/secretos provisionados al servicio:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:DOTNET_ENVIRONMENT = 'Production'
$env:AllowedHosts = 'api.heladeria.ejemplo.com'
$env:ASPNETCORE_URLS = 'https://0.0.0.0:8443'
dotnet .\Heladeria.Api.dll
```

Configurar certificado de producción mediante configuración estándar `Kestrel:Certificates:Default` (almacén del SO o proveedor protegido), nunca el certificado de desarrollo. Si usa archivo privado, fuera del repositorio/paquete y con ACL mínima; contraseña del certificado externa. El comando anterior no provisiona certificados ni secretos: no es ejecutable sin ese paso.

Instalar supervisión/reinicio/logging mediante el gestor de servicios elegido; `dotnet run` y perfiles launchSettings no son despliegue. Abrir solo el puerto necesario. No ejecutar como administrador/root. No asumir Linux validado: suite completa y esta entrega se verificaron en Windows.

**Proxy genérico:** el código actual no registra `UseForwardedHeaders` para un proxy arbitrario. No desplegar detrás de terminación TLS genérica suponiendo que HTTPS/IP se detectarán solos. Validar IIS integrado o Kestrel TLS directo; cualquier otro proxy requiere configuración específica de proxies/redes de confianza y pruebas, fuera de este cierre. No habilitar confianza global en `X-Forwarded-*`. El rate limiting es local al proceso; múltiples instancias requieren decisión operativa adicional.

## 7. CORS, cookies, logs y comprobación

Ejemplo: SPA `https://heladeria.ejemplo.com`, API `https://api.heladeria.ejemplo.com`. Definir `Cors__OrigenesPermitidos__0=https://heladeria.ejemplo.com`; no añadir el dominio API como sustituto del origen SPA, comodines ni localhost. En mismo origen, el middleware de autenticación acepta el origen propio; se puede mantener también su lista explícita, verificando que esquema y host lleguen correctamente al backend.

Cookie de renovación: HttpOnly, Secure en Production, Path `/api/autenticacion`, SameSite configurable. Mantener Strict para el despliegue same-site propuesto; distinto sitio requiere análisis explícito de SameSite=None/TLS/CSRF y pruebas, no un cambio automático. El access token no se guarda en localStorage. CORS no sustituye permisos.

`GET /health/live` debe dar 200 sin SQL; `/health/ready` y `/salud` dan 200 con SQL y 503 si falla, sin revelar servidor, conexión ni excepción. Confirmar usando HTTPS. Swagger debe dar 404 en Production. No simular caída de SQL de producción: probar 503 en staging o con las pruebas aisladas.

La aplicación usa ILogger y no trae un destino persistente de logs configurado. Definir recolección, acceso, rotación y retención con el host. Nunca habilitar SensitiveDataLogging ni registrar headers Authorization/cookies, bodies de contraseñas o variables completas. Headers existentes: nosniff, DENY, no-referrer, no-store en API; HSTS fuera de Development. El servidor estático debe aplicar sus propias políticas de seguridad/cache.

## 8. Smoke manual de lectura

- [ ] Identidad runtime, configuración externa y dominio correctos; API inicia.
- [ ] Live 200, ready 200, `/salud` compatible, respuesta sanitizada, Swagger no publicado.
- [ ] Login ADMIN; cambio obligatorio a contraseña definitiva en instalación nueva; sesión válida.
- [ ] Empresa/sucursal activas, sucursal seleccionada y permisos esperados.
- [ ] Productos e inventario cargan (vacíos es válido en instalación limpia).
- [ ] Consultar compras, ventas y Caja, sin confirmar/recibir/completar/abrir movimientos.
- [ ] Consultar CxC/CxP, reportes y auditoría; validar fechas/filtros/permisos.
- [ ] Recargar una ruta profunda de SPA, probar renovación y logout.
- [ ] No crear pagos, ventas, compras ni saldos para probar disponibilidad.

Login/logout/cambio de contraseña generan legítimamente sesión/auditoría; “lectura” significa sin nuevas operaciones financieras. La moneda base debe estar configurada antes de las pantallas que la requieran, según BASE_DATOS.

## 9. Backup, rollback y autorización final

Antes de desplegar: backup verificado y restauración ensayada en instancia separada, copia de artefactos previos y configuración externa protegida. [Procedimiento](BASE_DATOS.md#backup-y-restauración).

Desplegar a carpeta de versión nueva, no sobreescribir una versión en servicio. Si falla smoke, detener la nueva versión y restaurar el apuntador/servicio a la anterior, con su configuración compatible. Este cierre no cambia esquema; no ejecutar reparaciones inversas. Si el problema incluye datos, detener escrituras, evaluar impacto y obtener autorización DBA/propietario antes de cualquier restore. Restaurar puede perder operaciones posteriores al backup.

Checklist de autorización: dependencias revisadas, runtime soportado, SQL mínimo privilegio/TLS, CORS/hosts finales, Swagger fuera, cookie segura, ADMIN definitivo, logs/respaldos con ACL, publicación no escribible por la web, rate limits verificados, health sanitizado, backup restaurable, RPO/RTO acordados y smoke firmado. Nunca declarar aprobación solo porque compila.
