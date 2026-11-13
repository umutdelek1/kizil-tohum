@echo off
setlocal
set "SRC=%~dp0My project\Assets\Resources\Gorseller"
set "DST=%~dp0ESKI_GORSELLER"
tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I "Unity.exe" >NUL
if not errorlevel 1 (
  echo Unity acik. Once Unity'yi kapat, sonra tekrar calistir.
  pause
  exit /b 1
)
if not exist "%SRC%" (
  echo Gorseller klasoru bulunamadi: %SRC%
  pause
  exit /b 1
)
if not exist "%DST%" mkdir "%DST%"
for %%D in (Sahneler Portreler Hucre Mercek Sprite Menu Arayuz) do (
  if exist "%SRC%\%%D" (
    move "%SRC%\%%D" "%DST%\%%D" >NUL
    if exist "%SRC%\%%D.meta" move "%SRC%\%%D.meta" "%DST%\%%D.meta" >NUL
    echo Tasindi: %%D
  )
)
echo.
echo Bitti. Kartlar ve Fontlar yerinde kaldi. Eski gorseller: %DST%
pause
