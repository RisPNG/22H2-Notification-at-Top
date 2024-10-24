@echo off

REM Define the base paths
set CSC_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set LIB_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
set REFERENCE="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Microsoft.VisualBasic.dll"
set SOURCE_FILE="topnotify.cs"

REM Define all possible output file names
set EXE_NAMES=topleft topright bottomleft bottomright middleleft middleright topmiddle bottommiddle

REM Specify the icon file (it should be the same for all or use different icons for different positions)
set ICON_FILE="1.ico"

REM Loop through each name and generate the corresponding executable with the icon
for %%i in (%EXE_NAMES%) do (
    %CSC_PATH% -lib:%LIB_PATH% -r:%REFERENCE% -target:winexe -win32icon:%ICON_FILE% -out:"%%i.exe" %SOURCE_FILE%
)

PAUSE
