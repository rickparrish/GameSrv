@echo off

REM  WCMUD.BAT - Batch file for running DoorMUD under WildCat Winserver on
REM              Windows NT and Windows 2000.

REM  Please note:
REM  - Only use this batch file if you are using Wildcat Winserver.
REM  - Do not use this batch file for Windows 95/98/ME.  Door32 games cannot
REM    be run from batch files under Win 98/98/ME.
REM  - Do not add any other lines to this batch file!  DoorMUD will switch to
REM    its own directory automatically.  Adding additional statements to this
REM    batch file may prevent DoorMUD from running properly.

REM  The example here assumes c:\doors\doormud is your DoorMUD directory, and
REM  also assumes c:\doors\node# is where your BBS puts its dropfiles.  You'll
REM  need to change the line below to reflect your own system's directories.

REM  *** Mofiy the directories in the line below as noted above ***
c:\doors\doormud\dmud32d.exe -n %wcnodeid% -d c:\doors\node%wcnodeid%

REM  End of batch file.