@echo off

REM  WCAMB.BAT - Batch file for running Ambroshia on Wildcat WinServer
REM  You will need to fix directories as explained below.

REM  Please note:
REM  - Only use this batch file if you are using Wildcat Winserver.
REM  - Be sure that the door is able to find Winserver's DOOR32.DLL; please
REM    see README.TXT for more info.

REM  The example here assumes c:\ambroshia is your ambroshia directory, and
REM  also assumes c:\doors\node# is where your BBS puts its dropfiles.  You'll
REM  need to change the line below to reflect your own system's directories.

REM  *** Mofiy the directories in the line below as noted above ***

cd c:\ambv4
c:\ambv4\amblaunch.exe -n %wcnodeid% -d c:\doors\node%wcnodeid%

REM  End of batch file.
