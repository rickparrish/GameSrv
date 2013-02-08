#!/bin/bash

# If you don't want a CPU limit put in place, just remove (or rename) this file

# -p $1 tells cpulimit which pid to throttle ($1 is the pid passed in by GameSrv)
# -l 20 tells cpulimit to not allow the program to exceed 20% CPU.  See man cpulimit for setting this value
# -z tells cpulimit to exit when the process dies (so you don't have tons of cpulimit processes idling)
# 1> and 2> redirect output to null, so you don't see cpulimit messages in the GameSrv window

/usr/bin/cpulimit -p $1 -l 20 -z 1> /dev/null 2>&1