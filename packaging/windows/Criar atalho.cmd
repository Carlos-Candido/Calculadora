@echo off
setlocal DisableDelayedExpansion
set "CALCULADORA_DIR=%~dp0"
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -Command "$ErrorActionPreference = 'Stop'; try { $exePath = Join-Path -Path $env:CALCULADORA_DIR -ChildPath 'Calculadora.Desktop.exe'; if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) { throw 'Calculadora.Desktop.exe nao foi encontrado na pasta deste arquivo.' }; $desktopPath = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory); if ([string]::IsNullOrWhiteSpace($desktopPath) -or -not (Test-Path -LiteralPath $desktopPath -PathType Container)) { throw 'Nao foi possivel localizar a Area de Trabalho.' }; $shortcutPath = Join-Path -Path $desktopPath -ChildPath 'Calculadora.lnk'; $shell = New-Object -ComObject WScript.Shell; $shortcut = $shell.CreateShortcut($shortcutPath); $shortcut.TargetPath = $exePath; $shortcut.WorkingDirectory = $env:CALCULADORA_DIR; $shortcut.Description = 'Calculadora'; $shortcut.IconLocation = $exePath + ',0'; $shortcut.Save(); Write-Host ('Atalho criado: ' + $shortcutPath); exit 0 } catch { Write-Host ('Erro: ' + $_.Exception.Message); exit 1 }"
set "CALCULADORA_EXIT=%errorlevel%"
pause
exit /b %CALCULADORA_EXIT%
