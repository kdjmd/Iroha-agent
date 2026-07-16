param(
    [Parameter(Mandatory = $true)][string]$DocxPath,
    [Parameter(Mandatory = $true)][string]$PdfPath,
    [string]$LogPath = "$env:TEMP\iroha-engineering-book-word-render.log"
)

$ErrorActionPreference = "Stop"
$word = $null
$document = $null

function Write-RenderLog([string]$Message) {
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -LiteralPath $LogPath -Value "$stamp $Message" -Encoding UTF8
}

try {
    Set-Content -LiteralPath $LogPath -Value "" -Encoding UTF8
    Write-RenderLog "Starting Microsoft Word renderer"
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.ScreenUpdating = $false
    $word.DisplayAlerts = 0
    $word.AutomationSecurity = 3
    $word.Options.UpdateLinksAtOpen = $false
    $word.Options.ConfirmConversions = $false
    Write-RenderLog "Opening DOCX"

    $document = $word.Documents.Open(
        [System.IO.Path]::GetFullPath($DocxPath),
        $false,
        $true,
        $false
    )
    Write-RenderLog "DOCX opened"

    $pages = $document.ComputeStatistics(2)
    Write-RenderLog "Computed page count: $pages"

    $document.ExportAsFixedFormat(
        [System.IO.Path]::GetFullPath($PdfPath),
        17,
        $false,
        1,
        0,
        1,
        $pages,
        0,
        $true,
        $true,
        0,
        $true,
        $true,
        $false
    )
    Write-RenderLog "PDF export complete"
}
catch {
    Write-RenderLog ("ERROR: " + $_.Exception.Message)
    throw
}
finally {
    if ($null -ne $document) {
        $document.Close($false)
        Write-RenderLog "DOCX closed"
    }
    if ($null -ne $word) {
        $word.Quit()
        Write-RenderLog "Word closed"
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
