@echo off
setlocal
if not exist "%~dp0..\McpWorkspace" (
    mkdir "%~dp0..\McpWorkspace"
    if errorlevel 1 exit /b 1
)
start "" "%~dp0App\UltimateEditor.exe" "--mcp-pipe=dk2-editor" "--mcp-root=%~dp0..\McpWorkspace"
