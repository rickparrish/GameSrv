Run TelnetDoor.exe for some usage information.  Additional parameters not listed there:

-S		Specify the server (hostname or ip address) to connect to
-P		Override the default port number (which is 23)

-R		Use rlogin instead of telnet
-X		Override the default client username (which is the user's alias)
-Y		Override the default server username (which is the user's alias)
-Z		Override the default terminal type (which is ansi-bbs)

-E		Turn on local echo (default is for server to echo)


If you don't specify a server with the -S parameter then the servers listed in TelnetDoor.ini
will be displayed to the user.

If a TelnetDoor-Header.ans exists, this header will be displayed before the list of servers.

If a TelnetDoor.ans exists, this screen will be displayed INSTEAD OF the list of servers.  This
means TelnetDoor.ans will have to include the list of servers.