# Heladería

Sistema de gestión de productos, inventario, compras, producción/recetas, ventas/POS, Caja, CxC/CxP, reportes y auditoría. Backend fases 1–10 y visuales 1–8 aprobados por el propietario. Cierre técnico preparado; despliegue y aceptación final todavía pendientes.

## Arquitectura y requisitos

ASP.NET Core Web API / C# / .NET 8, EF Core **Database First**, SQL Server; React 19 + TypeScript 6 + Vite 8 + Tailwind CSS. Autenticación JWT de acceso en memoria del navegador y renovación mediante cookie HttpOnly.

- Build: SDK indicado por `global.json` (8.0.403, latestPatch), Node compatible con Vite (20.19+ o 22.12+; validado con 24.16.0), npm y SQL Server LocalDB 2019 para la suite completa Windows.
- Servidor: ASP.NET Core Runtime 8 parcheado; Hosting Bundle si se usa IIS; SQL Server con TLS. Node **no** es necesario para servir el frontend compilado.
- .NET 8 termina soporte el **10/11/2026**: debe planificarse actualización validada antes de operar fuera de soporte. Este cierre no migra dependencias. [Política oficial](https://dotnet.microsoft.com/en-us/platform/support/policy).

| Directorio | Responsabilidad |
|---|---|
| Heladeria.Api | HTTP, validación, permisos, errores, health |
| Heladeria.Aplicacion / Heladeria.Dominio | Contratos y reglas |
| Heladeria.Infraestructura | EF generado, servicios, transacciones, seguridad |
| Heladeria.Bootstrap | Administración inicial y preparadores explícitos |
| Heladeria.Pruebas | Regresión, HTTP y SQL Server aislado |
| heladeria-web | Aplicación React |
| BaseDatos | Maestro y reparaciones históricas controladas |
| docs / scripts | Operación, evidencia y publicación |
| artifacts | Entrega local generada, ignorada por Git |

## Desarrollo local

Desde la carpeta que contiene `Heladeria.sln`:

```powershell
dotnet restore
dotnet tool restore
dotnet dev-certs https --trust  # Solo desarrollo; revisar la solicitud del sistema.
dotnet run --project Heladeria.Api --launch-profile https
```

Antes de iniciar: instalar la BD y preparar ADMIN siguiendo [BASE_DATOS](docs/BASE_DATOS.md). La conexión a VictusPabloE se conserva **solo en Development**. Configurar `Seguridad:Jwt:ClaveFirma` mediante User Secrets del proyecto API (por ejemplo, “Administrar secretos de usuario” en Visual Studio); nunca escribirla en Git. API y Bootstrap comparten el identificador de secretos de desarrollo.

Frontend, en otra consola:

```powershell
cd heladeria-web
npm ci
# Crear .env.local a partir de .env.example (sin secretos).
npm run dev
```

API local HTTPS: `https://localhost:7216`; HTTP de perfil: `http://localhost:5154`; frontend: `http://localhost:5173`. Swagger está habilitado exclusivamente en Development. Detener Vite antes de reinstalar dependencias en Windows.

## Producción e instalación

No publicar carpetas del proyecto completas ni reutilizar la BD de validación como instalación limpia.

1. Instalar el maestro en una BD NUEVA con una identidad de instalación.
2. Configurar conexiones, JWT, hosts, CORS y TLS externamente.
3. Ejecutar Bootstrap interactivo y preparadores en el orden documentado; no arrancan con la API.
4. Cambiar contraseña temporal de ADMIN; revisar empresa/sucursal y configuración real de moneda/catálogos.
5. Publicar artefactos, iniciar API y comprobar health antes del frontend.
6. Hacer smoke de lectura y validar backup/restauración.

[DEPLOYMENT](docs/DEPLOYMENT.md) contiene variables exactas, IIS/Kestrel, publicación y checklist. [BASE_DATOS](docs/BASE_DATOS.md) contiene instalación, bootstrap, Database First y backup/restore.

Variables esenciales: `ConnectionStrings__Heladeria`, `Seguridad__Jwt__ClaveFirma`, `Seguridad__Jwt__Emisor`, `Seguridad__Jwt__Audiencia`, `AllowedHosts`, `Cors__OrigenesPermitidos__0`; `ASPNETCORE_ENVIRONMENT=Production` para API y `DOTNET_ENVIRONMENT=Production` para Bootstrap. `VITE_API_URL` es **pública y de compilación**, no un secreto.

## Pruebas y entrega

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
# Elegir UNA modalidad; este script no ejecuta SQL ni Bootstrap:
pwsh -File scripts/Publicar.ps1 -MismoOrigen
# O: pwsh -File scripts/Publicar.ps1 -ApiUrl https://api.heladeria.ejemplo.com
```

El script exige que `artifacts/` no exista: conservar/reubicar la entrega previa manualmente. Verifica archivos publicados y genera un manifiesto SHA-256. La suite SQL crea instancias/BD LocalDB privadas, nunca usa la conexión Development. Las pruebas web usan servicios simulados.

## Salud y seguridad

- `GET /health/live`: anónimo, 200 si responde el proceso; no consulta SQL.
- `GET /health/ready`: anónimo, consulta SQL; 200/503 con respuesta sanitizada.
- `GET /salud`: compatibilidad; comprueba SQL.
- JWT externo, cookie Secure en producción, CORS de orígenes explícitos, permisos por recurso y sucursal, rate limiting, headers y logs sanitizados. La API no activa SensitiveDataLogging.
- Los endpoints health no garantizan que catálogos, permisos, moneda o saldo estén listos para operar.

## Troubleshooting y límites

- API no inicia: revisar configuración externa y entorno; no imprimir secretos.
- 401/403: sesión, cambio de contraseña, permisos y sucursal operativa; no conceder ADMIN como solución genérica.
- Ready 503: TLS/conectividad/credenciales SQL; revisar logs protegidos, no exponer excepciones al cliente.
- CORS/refresh: origen exacto, HTTPS, cookie y hosts; no usar wildcard con credenciales.
- Preparador rechaza: conservar mensaje y revisar compatibilidad; no borrar marcadores ni reiniciar correlativos.
- Lecturas sin moneda: configurar moneda base real mediante procedimiento controlado; no cargar fixtures.
- El cierre no decide hosting, dominio, certificado, RPO/RTO, moneda, datos fiscales ni retención. No promete disponibilidad, escalado o alta disponibilidad.

Ver [CIERRE_TECNICO](docs/CIERRE_TECNICO.md) para diagnóstico, resultados y riesgos; [DATOS_DE_PRUEBA](docs/DATOS_DE_PRUEBA.md) para separación de datos. Los documentos de fases anteriores son evidencia histórica: sus conteos/alcance no sustituyen esta guía final.
