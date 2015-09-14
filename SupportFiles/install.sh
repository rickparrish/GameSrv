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
   echo "You could also try running 'sudo install.sh' too."
   echo "##################################################"
   echo ""
   exit 1
fi

while true; do
    echo "This has only been tested on Ubuntu 14.04 LTS (32bit)"
    read -p "So do you want to try running it now? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

chmod a+x cpulimit.sh
rm dosxtrn.exe
rm dosxtrn.pif
rm install.cmd
rm Mono.Posix.dll
rm pty-sharp.dll
rm sbbsexec.dll
rm sbbsexec.vxd
chmod a+x start.sh

while true; do
    read -p "OK, so should I continue with installing the pre-requisites? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

apt-get install build-essential cpulimit dosemu libglib2.0-dev libmono2.0-cil mono-gmcs mono-runtime pkg-config privbind unzip

while true; do
    read -p "OK, so should I continue with extracting the dosutils.zip archive? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

unzip -a dosutils.zip

while true; do
    read -p "OK, so should I continue with compiling pty-sharp? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

tar zxvf pty-sharp-1.0.tgz
cd pty-sharp-1.0
./configure --prefix=/usr LIBS=-lglib-2.0
make
make install
cd ..
rm pty-sharp-1.0.tgz

while true; do
    read -p "OK, so should I continue with adding the gamesrv user and group? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

groupadd gamesrv
useradd -g gamesrv -s /usr/sbin/nologin gamesrv
chown -R gamesrv:gamesrv /gamesrv

while true; do
    read -p "OK, so should I continue with testing DOSEMU? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

dosemu

while true; do
    read -p "OK, so should I continue with starting GameSrv? " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit;;
        * ) echo "Please answer yes or no.";;
    esac
done

./start.sh