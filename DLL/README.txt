This is the second alpha release of GameSrv

I've been running it for almost 2 months on my Linux VPS now, and haven't run into any problems.  I also haven't received
any serious bug reports, so things seem to be fairly stable.  I'm still calling it an alpha release though, because there's 
a high degree of likelihood that configuration files/entries will change, which means users may need to re-register in future versions.

That said, please abuse this as much as you can and let me know if you run into any problems.  For bug
reporting purposes, it would be best if you ran GameSrv with the DEBUG command-line parameter, which will
enable full exception reporting.  Then email a screenshot of the screen containing the error to rick@gamesrv.ca
along with a description of what you were doing to cause it, if possible

Should be pretty straightforward to get things up and running on Windows (just edit the various .ini files in the various subdirectories),
but for Linux it's a little more involved.  So here's how I got it running on my Ubuntu Server 11.04 VPS (which was virtualized with
KVM -- I didn't have any luck with the OpenVZ virtualization offerings)

Install pre-requisites:
sudo apt-get install build-essential dosemu libglib2.0-dev mono-gmcs mono-runtime pkg-config privbind unzip

Download and extract GameSrv
sudo mkdir /gamesrv
cd /gamesrv
sudo wget whatever_the_latest_release_archive_is.zip
unzip GameSrv_*

Extract the dosutils.tgz archive, which contains useful stuff for running doors in dosemu
cd dosutils
sudo tar zxvf dosutils.tgz
cd ..

Compile pty-sharp, which is required to launch dosemu in a new pty from mono applications
sudo tar zxvf pty-sharp-1.0.tgz
cd pty-sharp-1.0
./configure --prefix=/usr
make
sudo make install

Create a new user/group to run gamesrv as, since we don't want it always running as root (optional, but highly recommended)
sudo groupadd gamesrv
sudo useradd -g gamesrv -s /usr/sbin/nologin gamesrv
sudo chown -R gamesrv:gamesrv /gamesrv

Try running dosemu, and if you get an error indicating to do so, run:
sudo sysctl -w vm.mmap_min_addr=0

Launch GameSrv
sudo chmod a+x cpulimit.sh
sudo chmod a+x start.sh
sudo ./start.sh