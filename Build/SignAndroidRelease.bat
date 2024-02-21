if %1.==. goto MissingArgs
if %2.==. goto MissingArgs
if %3.==. goto MissingArgs

set packagefile=%1
set keystore=%2
set keystorepassword=%3

set jarsigner="C:\Program Files (x86)\Android\android-sdk\build-tools\34.0.0\apksigner.bat"
set jarsignerparams=sign --min-sdk-version 21 --ks %keystore% --ks-pass pass:%keystorepassword% %packagefile%

call %jarsigner% %jarsignerparams%

if %errorlevel%==0 (
  echo.
  echo ==============================
  echo Signing completed successfully.
  echo ==============================
  echo.
)

if NOT %errorlevel%==0 (
  echo.
  echo ==============================
  echo Signing failed.
  echo ==============================
  echo.
)

goto End

:MissingArgs
  echo.
  echo Missing arguments. This script requires two arguments.
  echo.
  echo   Usage:   BuildAndroid.bat /path/to/package /path/to/keystore keystorepassword
  echo.
 
:End
