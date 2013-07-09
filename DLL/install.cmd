@ECHO OFF

REM Modified from http://stackoverflow.com/questions/4051883/batch-script-how-to-check-for-admin-rights
REM Get current directory from http://stackoverflow.com/questions/130112/command-line-cmd-bat-script-how-to-get-directory-of-running-script

REM Run NET SESSION and check the errorlevel to see if we're running with admin priviledge
NET SESSION >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
   echo ######## ########  ########   #######  ########  
   echo ##       ##     ## ##     ## ##     ## ##     ## 
   echo ##       ##     ## ##     ## ##     ## ##     ## 
   echo ######   ########  ########  ##     ## ########  
   echo ##       ##   ##   ##   ##   ##     ## ##   ##   
   echo ##       ##    ##  ##    ##  ##     ## ##    ##  
   echo ######## ##     ## ##     ##  #######  ##     ## 
   echo.
   echo.
   echo ####### ERROR: ADMINISTRATOR PRIVILEGES REQUIRED #########
   echo This script must be run as administrator to work properly!  
   echo Right click install.cmd and select "Run As Administrator".
   echo ##########################################################
   echo.
   PAUSE
   EXIT /B 1
)

REM copy sbbsexec.dll to the windows\system32 directory (needed to run dos doors under Vista and newer, doesn't hurt on XP)
copy %~dp0sbbsexec.dll %windir%\system32\sbbsexec.dll

REM start the GUI version of GameSrv
start %~dp0GameSrvGUI.exe

@echo ON
