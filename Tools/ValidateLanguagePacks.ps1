$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$source = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'GestorComunicacionesDNET\SpanishTexts.json') | ConvertFrom-Json -AsHashtable
$files = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'GestorComunicacionesDNET\LanguagePacks') -Filter '*.json'
$errors = @()

foreach ($file in $files) {
    $pack = Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json -AsHashtable
    if ($pack.texts.Count -ne $source.Count) { $errors += "$($file.Name): contiene $($pack.texts.Count) textos; se esperaban $($source.Count)." }
    foreach ($key in $source.Keys) {
        if (-not $pack.texts.ContainsKey($key)) { $errors += "$($file.Name): falta '$key'."; continue }
        $expected = @([regex]::Matches([string]$source[$key], '\{\d+\}') | ForEach-Object Value | Sort-Object)
        $actual = @([regex]::Matches([string]$pack.texts[$key], '\{\d+\}') | ForEach-Object Value | Sort-Object)
        if (($expected -join ',') -ne ($actual -join ',')) { $errors += "$($file.Name): marcadores alterados en '$key'." }
        foreach ($term in @('DNET', 'JSON', 'mailto', 'AppData', 'contactos.json', '*.zip')) {
            if (([string]$source[$key]).Contains($term, [StringComparison]::OrdinalIgnoreCase) -and
                -not ([string]$pack.texts[$key]).Contains($term, [StringComparison]::OrdinalIgnoreCase)) {
                $errors += "$($file.Name): se alteró el término técnico '$term' en '$key'."
            }
        }
    }
}

if ($files.Count -ne 61) { $errors += "Hay $($files.Count) paquetes; se esperaban 61 más el español integrado." }
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host "Correcto: 62 idiomas y $($source.Count) textos completos por idioma."
