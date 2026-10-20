[CmdletBinding(DefaultParameterSetName = 'ApiSeparada')]
param(
    [Parameter(Mandatory, ParameterSetName = 'ApiSeparada')][uri]$ApiUrl,
    [Parameter(Mandatory, ParameterSetName = 'MismoOrigen')][switch]$MismoOrigen
)

# Solo compila/copia archivos locales. Nunca inicia API, Bootstrap ni SQL.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$raizEntrega = Split-Path -Parent $PSScriptRoot
$destinoEntrega = Join-Path $raizEntrega 'artifacts'
if (Test-Path -LiteralPath $destinoEntrega) {
    throw 'artifacts ya existe. Conserve/reubique la entrega anterior manualmente; este script no borra ni mezcla entregas.'
}
$urlPublica = ''
if (!$MismoOrigen) {
    if (!$ApiUrl.IsAbsoluteUri -or $ApiUrl.Scheme -ne 'https' -or $ApiUrl.IsLoopback -or
        $ApiUrl.UserInfo -or $ApiUrl.Query -or $ApiUrl.Fragment -or $ApiUrl.AbsolutePath -ne '/') {
        throw 'ApiUrl debe ser un origen HTTPS público, sin credenciales, ruta, query ni fragmento.'
    }
    $urlPublica = $ApiUrl.GetLeftPart([System.UriPartial]::Authority)
}
foreach ($nombre in @('appsettings.json', 'appsettings.Production.json')) {
    $config = Get-Content -LiteralPath (Join-Path $raizEntrega "Heladeria.Api/$nombre") -Raw | ConvertFrom-Json
    if (![string]::IsNullOrWhiteSpace($config.ConnectionStrings.Heladeria)) {
        throw "$nombre contiene una conexión: no se publica. Suminístrela externamente."
    }
    if ($nombre -eq 'appsettings.json' -and ![string]::IsNullOrWhiteSpace($config.Seguridad.Jwt.ClaveFirma)) {
        throw 'appsettings.json contiene una clave JWT: no se publica.'
    }
}

New-Item -ItemType Directory -Path $destinoEntrega | Out-Null
Push-Location $raizEntrega
try {
    foreach ($componente in @(@('Heladeria.Api', 'api'), @('Heladeria.Bootstrap', 'bootstrap'))) {
        & dotnet publish $componente[0] --configuration Release --no-restore --self-contained false `
            -p:DebugType=None -p:DebugSymbols=false --output (Join-Path $destinoEntrega $componente[1])
        if ($LASTEXITCODE -ne 0) { throw "Falló publish de $($componente[0]). Entrega incompleta, no desplegar." }
    }
    # Variable de proceso explícita tiene prioridad sobre .env.local (incluso vacía = mismo origen).
    $apiUrlAnterior = [Environment]::GetEnvironmentVariable('VITE_API_URL', 'Process')
    Push-Location (Join-Path $raizEntrega 'heladeria-web')
    try {
        [Environment]::SetEnvironmentVariable('VITE_API_URL', $urlPublica, 'Process')
        # En Windows una variable vacía puede eliminarse: un espacio garantiza override y trim() => fallback.
        if ($MismoOrigen) { $env:VITE_API_URL = ' ' }
        & npm run build
        if ($LASTEXITCODE -ne 0) { throw 'Falló build web. Entrega incompleta, no desplegar.' }
        Copy-Item -LiteralPath 'dist' -Destination (Join-Path $destinoEntrega 'web') -Recurse
    } finally {
        [Environment]::SetEnvironmentVariable('VITE_API_URL', $apiUrlAnterior, 'Process')
        Pop-Location
    }
    New-Item -ItemType Directory -Path (Join-Path $destinoEntrega 'database') | Out-Null
    Copy-Item -LiteralPath 'BaseDatos/Heladeria.sql' -Destination (Join-Path $destinoEntrega 'database/Heladeria.sql')

    # Verificación sobre los archivos realmente publicados, no solo sobre reglas MSBuild.
    $archivosEntrega = @(Get-ChildItem -LiteralPath $destinoEntrega -File -Recurse)
    foreach ($archivo in $archivosEntrega) {
        $relativa = $archivo.FullName.Substring($destinoEntrega.Length + 1)
        if ($relativa -match '(?i)(^|[\\/])(node_modules|TestResults|test-results|logs|Properties)([\\/]|$)' -or
            $archivo.Name -match '(?i)(^\.env|^secrets\.json$|^launchSettings\.json$|^appsettings\.(?!Production\.json$).+\.json$|\.(pfx|p12|pem|key|log|user|suo|http|bak|mdf|ldf|pdb)$)') {
            throw "Archivo no publicable: $relativa. Entrega incompleta, no desplegar."
        }
        if ($archivo.Extension -in @('.json', '.js', '.html', '.css', '.config')) {
            $contenido = Get-Content -LiteralPath $archivo.FullName -Raw
            # AllowedHosts=localhost en configuración común es un default restrictivo; se cambia externamente.
            # React Router contiene http://localhost sin puerto como base sintáctica interna de URL.
            # No es el destino API. Rechazar URLs locales con puerto/ruta y material privado.
            if ($contenido -match '(?i)https?://(localhost|127\.0\.0\.1|\[::1\])(:[0-9]+|/)|https://localhost|VictusPabloE|BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY') {
                throw "Referencia local o material privado en $relativa. Entrega incompleta, no desplegar."
            }
        }
    }
    $manifest = [ordered]@{
        creadoUtc = [DateTime]::UtcNow.ToString('o')
        modalidadWeb = $(if ($MismoOrigen) { 'mismo-origen: requiere /api y /health en ese origen' } else { $urlPublica })
        sqlEjecutado = $false
        bootstrapEjecutado = $false
        archivos = @($archivosEntrega | Sort-Object FullName | ForEach-Object {
            [ordered]@{ ruta = $_.FullName.Substring($destinoEntrega.Length + 1); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
        })
    }
    # Artefacto generado: el manifiesto aparece solo después de verificar toda la entrega.
    $manifest | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $destinoEntrega 'manifest.json') -Encoding utf8
    Write-Host 'Entrega local verificada en artifacts/. No se desplegó ni se ejecutó SQL/Bootstrap.'
} finally { Pop-Location }
