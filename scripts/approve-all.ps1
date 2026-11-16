function Copy-ReceivedToVerified {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    # Resolve script directory safely
    if (-not $PSScriptRoot) {
        $ScriptDir = Get-Location
    } else {
        $ScriptDir = $PSScriptRoot
    }
    
    $AbsolutePath = Join-Path -Path $ScriptDir -ChildPath $RelativePath

    # ONLY log if the root folder doesn't exist
    if (-not (Test-Path -Path $AbsolutePath -PathType Container)) {
        Write-Warning "Directory does not exist: $AbsolutePath"
        return
    }

    # Added -Recurse to include all subfolders
    $ReceivedFiles = Get-ChildItem -Path $AbsolutePath -Filter "*.received.*" -File -Recurse

    # Silent exit if nothing is there to process
    if ($ReceivedFiles.Count -eq 0) {
        return
    }

    foreach ($File in $ReceivedFiles) {
        $NewName = $File.Name -replace '\.received\.', '.verified.'
        # Join against the file's actual parent directory so it stays in its subfolder
        $DestinationPath = Join-Path -Path $File.DirectoryName -ChildPath $NewName

        try {
            Move-Item -Path $File.FullName -Destination $DestinationPath -Force -ErrorAction Stop
            # ONLY log files that were actually moved
            Write-Output "Approved: $($File.Name) -> $NewName"
        }
        catch {
            Write-Error "Failed to move $($File.Name): $_"
        }
    }
}

# --- Call Sites ---
# Standardizing on Write-Output to bypass any Write-Host suppression issues
Write-Output "Starting Verify auto-approval..."

Copy-ReceivedToVerified -RelativePath "..\src\OpenMacroBoard.SDK\src\OpenMacroBoard.Tests\VerifiedSnapshots"
Copy-ReceivedToVerified -RelativePath "..\src\OpenMacroBoard.SDK\src\OpenMacroBoard.Tests-Win\VerifiedSnapshots"
Copy-ReceivedToVerified -RelativePath "..\src\StreamDeckSharp\src\StreamDeckSharp.Tests\VerifiedSnapshots"

Write-Output "Done!"
