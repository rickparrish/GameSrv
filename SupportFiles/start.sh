#!/bin/bash

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

# for DOSEMU -- may be able to comment out in newer releases
sysctl -w vm.mmap_min_addr="0"

cd /gamesrv
privbind -u gamesrv -g gamesrv mono GameSrvConsole.exe