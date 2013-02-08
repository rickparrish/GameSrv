@ECHO OFF
REM TODO UPDATE SO IT ACTUALLY WORKS
REM (WHEN YOU RUN AS ADMINISTRATOR IT STARTS YOU IN C:\WINDOWS\SYSTEM32, SO NEED TO FIND A WAY TO KNOW WHAT DIRECTORY INSTALL.CMD WAS RUN FROM)

REM Modified from http://stackoverflow.com/questions/4051883/batch-script-how-to-check-for-admin-rights

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

del cpulimit.sh
del dosutils.zip
del install.sh
del pty-sharp-1.0.tgz
del start.sh

copy sbbsexec.dll %windir%\system32\sbbsexec.dll

start GameSrvGUI.exe

@echo ON
