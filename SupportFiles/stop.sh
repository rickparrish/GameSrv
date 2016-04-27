#!/bin/bash
# Stop script for gamesrv start.sh script
file="/var/run/gamesrv/start_sh.pid"
# Check the effective user id to see if it's root (EUID works with sudo, UID does not)
if (( EUID != 0 )); then
   echo "######## ########  ########   #######  ########"
   echo "##       ##     ## ##     ## ##     ## ##     ##"
   echo "##       ##     ## ##     ## ##     ## ##     ##"
   echo "######   ########  ########  ##     ## ########"
   echo "##       ##   ##   ##   ##   ##     ## ##   ##"
   echo "##       ##    ##  ##    ##  ##     ## ##    ##"
   echo "######## ##     ## ##     ##  #######  ##     ##"
   echo ""
   echo ""
   echo "####### ERROR: ROOT PRIVILEGES REQUIRED #########"
   echo "This script must be run as root to work properly!"
   echo "You could also try running 'sudo start.sh' too."
   echo "##################################################"
   echo ""
   exit 1
fi

# Find the PID we created on startup and placed in /var/run/gamesrv/start_sh.pid 
if [ -e $file ];
then
   # Writes the file GameSrvConsole.stop to the /gamesrv Directory
   # GameSrvConsole.exe checks every 2 sec for the file and if it exist
   # Gracefully shuts down the process
   echo $$ >> /gamesrv/GameSrvConsole.stop
   # start_sh.pid is then removed cleaning things up for next time
   rm $file
# If the start_sh.pid does not exist then process is not running
else echo "Process is NOT running."
fi
