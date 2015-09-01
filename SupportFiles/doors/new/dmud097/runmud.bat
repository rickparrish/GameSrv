@echo off

REM  RUNMUD.BAT - BATCH FILE FOR RUNNING DOORMUD
REM   This batch file changes to DoorMUD's directory and runs the door.  You
REM   MUST run the game from a batch file like this one.
REM
REM   Only modify the lines indicated -- don't change any other lines!

REM *** Uncomment & change the directory below to DoorMUD's directory ***
REM cd\bbs\door\doormud

dmud %1 %2 %3 %4 %5 %6 %7 %8
if errorlevel 5 goto entrance
goto done

:entrance
dmud -nointro %1 %2 %3 %4 %5 %6 %7 %8
if errorlevel 5 goto entrance

:done
REM *** Uncomment & change the directory below to your BBS's directory ***
REM cd\bbs



