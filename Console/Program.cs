/*
  GameSrv: A BBS Door Game Server
  Copyright (C) 2002-2014  Rick Parrish, R&M Software

  This file is part of GameSrv.

  GameSrv is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  any later version.

  GameSrv is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with GameSrv.  If not, see <http://www.gnu.org/licenses/>.
*/
// TODO Lowercase everything

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using RandM.RMLib;
using System.Diagnostics;

namespace RandM.GameSrv
{
    class Program
    {
        static private Dictionary<ConnectionType, int> _ConnectionCounts = new Dictionary<ConnectionType, int>();
        static private bool _FancyOutput = OSUtils.IsWindows;
        static private GameSrv _GameSrv = new GameSrv();
        static private string _TimeFormatFooter = "hh:mmtt";

        static void Main(string[] args)
        {
            // Check command-line parameters
            foreach (string Arg in args)
            {
                switch (Arg.ToLower())
                {
                    case "24h":
                        _TimeFormatFooter = "HH:mm";
                        break;
                    case "simple":
                        _FancyOutput = false;
                        break;
                }
            }

            // Add connection types to counter
            _ConnectionCounts[ConnectionType.RLogin] = 0;
            _ConnectionCounts[ConnectionType.Telnet] = 0;
            _ConnectionCounts[ConnectionType.WebSocket] = 0;

            // Initialize the screen
            InitConsole();
            Write(Globals.Copyright, false);

            // Check if running as root
            if (Globals.StartedAsRoot)
            {
                WriteLn("", false);
                WriteLn("*** WARNING: Running GameSrv as root is NOT recommended ***", false);
                WriteLn("", false);
                WriteLn("A safer alternative to running GameSrv as root is to run it via 'privbind'", false);
                WriteLn("This will ensure GameSrv is able to bind to ports in the < 1024 range, but", false);
                WriteLn("it will run as a regular unprivileged program in every other way", false);
                WriteLn("", false);
                WriteLn("See start.sh for an example of the recommended method to start GameSrv", false);
                WriteLn("", false);
            }

            // Init GameSrv          
            _GameSrv.AggregatedStatusMessageEvent += new EventHandler<StringEventArgs>(GameSrv_AggregatedStatusMessageEvent);
            _GameSrv.LogOnEvent += new EventHandler<NodeEventArgs>(GameSrv_LogOnEvent);
            _GameSrv.Start();

            // Main program loop
            int LastMinute = -1;
            bool Quit = false;
            while (!Quit)
            {
                while (!Crt.KeyPressed())
                {
                    Crt.Delay(100);
                    if (DateTime.Now.Minute != LastMinute)
                    {
                        UpdateTime();
                        LastMinute = DateTime.Now.Minute;
                    }
                }

                char Ch = Crt.ReadKey();
                switch (Ch.ToString().ToUpper())
                {
                    case "\0":
                        char Ch2 = Crt.ReadKey();
                        if (Ch2 == ';') // F1
                        {
                            WriteLn("", false);
                            WriteLn("GameSrv WFC Screen Help", false);
                            WriteLn("-=-=-=-=-=-=-=-=-=-=-=-", false);
                            WriteLn("F1 = Help  (this screen)", false);
                            WriteLn("C  = Clear (clear the status window)", false);
                            WriteLn("P  = Pause (reject new connections, leave existing connections alone)", false);
                            WriteLn("S  = Setup (launch the config program)", false);
                            WriteLn("Q  = Quit  (shut down and terminate existing connections)", false);
                            WriteLn("", false);
                        }
                        break;
                    case "C":
                        Crt.ClrScr();
                        if (_FancyOutput) Crt.GotoXY(1, 32);
                        break;
                    case "P":
                        _GameSrv.Pause();
                        break;
                    case "S":
                        Process.Start(StringUtils.PathCombine(ProcessUtils.StartupPath, "GameSrvConfig.exe"));
                        break;
                    case "Q":
                        // Check if we're already stopped (or are stopping)
                        if ((_GameSrv.Status != ServerStatus.Stopped) && (_GameSrv.Status != ServerStatus.Stopping))
                        {
                            int ConnectionCount = _GameSrv.ConnectionCount;
                            if (ConnectionCount > 0)
                            {
                                WriteLn("", false);
                                WriteLn("There are " + ConnectionCount.ToString() + " active connections.", false);
                                WriteLn("Are you sure you want to quit [y/N]: ", false);
                                WriteLn("", false);
                                Ch = Crt.ReadKey();
                                if (Ch.ToString().ToUpper() != "Y")
                                {
                                    WriteLn("", false);
                                    WriteLn("Cancelling quit request.", false);
                                    WriteLn("", false);
                                    continue;
                                }
                            }
                        }

                        _GameSrv.Stop();
                        _GameSrv.Dispose();
                        Quit = true;
                        break;
                }
            }

            Environment.Exit(0);
        }

        static void GameSrv_AggregatedStatusMessageEvent(object sender, StringEventArgs e)
        {
            WriteLn(e.Text);
        }

        static void GameSrv_LogOnEvent(object sender, NodeEventArgs e)
        {
            if (_FancyOutput)
            {
                _ConnectionCounts[e.NodeInfo.ConnectionType] += 1;

                Crt.FastWrite(StringUtils.PadRight(e.NodeInfo.User.Alias + " (" + e.NodeInfo.Connection.GetRemoteIP() + ":" + e.NodeInfo.Connection.GetRemotePort() + ")", ' ', 65), 8, 1, (Crt.Blue << 4) + Crt.White);
                Crt.FastWrite(StringUtils.PadRight(DateTime.Now.ToString("dddd MMMM dd, yyyy  " + _TimeFormatFooter), ' ', 65), 8, 2, (Crt.Blue << 4) + Crt.White);
                Crt.FastWrite(StringUtils.PadRight(e.NodeInfo.ConnectionType.ToString(), ' ', 65), 8, 3, (Crt.Blue << 4) + Crt.White);
                Crt.FastWrite(_ConnectionCounts[ConnectionType.RLogin].ToString(), 87, 1, (Crt.Blue << 4) + Crt.White);
                Crt.FastWrite(_ConnectionCounts[ConnectionType.Telnet].ToString(), 87, 2, (Crt.Blue << 4) + Crt.White);
                Crt.FastWrite(_ConnectionCounts[ConnectionType.WebSocket].ToString(), 87, 3, (Crt.Blue << 4) + Crt.White);
                UpdateTime();
            }
        }

        private static void InitConsole()
        {
            if (_FancyOutput)
            {
                //TODO Can do this without System.Windows.Forms reference? Crt.SetIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("GameSrv.GameSrv16+32.ico")).Handle);
                Crt.SetTitle("GameSrv WFC Screen v" + GameSrv.Version);
                Crt.SetWindowSize(90, 40);
                Crt.HideCursor();
                Crt.ClrScr();

                // WFC Screen
                Ansi.Write("[0;1;1;44;36m Last: [37mNo callers yet...                                                 [0;44;30m³    [1;36mRLogin: [37m0[36m      On: [37mNo callers yet...                                                 [0;44;30m³    [1;36mTelnet: [37m0[36m    Type: [37mNo callers yet...                                                 [0;44;30m³ [1;36mWebSocket: [37m0[36m   [0;34mÚðð[1mStatus[0;34mððÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ¿³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³³[37m[88C[34m³ÃÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÂÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÙ³[37m [32mTime: [37m[8C[32mDate: [37m[29C[34m³ °±²Û[1;44;37mGameSrv WFC Screen v" + GameSrv.Version + " [0;34m²±° ÀÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÙ[37m  Press [1;30m[[33mF1[30m][0m For Help or [1;30m[[33mQ[30m][0m To Quit");
                Crt.FastWrite(DateTime.Now.ToString(_TimeFormatFooter).ToLower(), 9, 38, Crt.LightGreen);
                Crt.FastWrite(DateTime.Now.ToString("dddd MMMM dd, yyyy"), 23, 38, Crt.LightGreen);

                // Setup scrolling region with a window
                Crt.Window(3, 5, 88, 36);
                Crt.GotoXY(1, 32); 
            }
            else
            {
                Crt.ClrScr();
            }
        }

        private static void UpdateTime()
        {
            if (_FancyOutput)
            {
                // Update time
                Crt.FastWrite(StringUtils.PadRight(DateTime.Now.ToString(_TimeFormatFooter).ToLower(), ' ', 7), 9, 38, Crt.LightGreen);
                Crt.FastWrite(StringUtils.PadRight(DateTime.Now.ToString("dddd MMMM dd, yyyy"), ' ', 28), 23, 38, Crt.LightGreen);
            }
        }

        static private void Write(string text)
        {
            Write(text, true);
        }

        static private void Write(string text, bool prefixWithTime)
        {
            if (prefixWithTime && (!string.IsNullOrEmpty(text))) Crt.Write(DateTime.Now.ToString(_GameSrv.TimeFormatUI) + "  ");

            if (text.Contains("ERROR:") || text.Contains("EXCEPTION:"))
            {
                Crt.TextColor(Crt.LightRed);
            }
            else if (text.Contains("WARNING:"))
            {
                Crt.TextColor(Crt.Yellow);
            }
            else if (text.Contains("DEBUG:"))
            {
                Crt.TextColor(Crt.LightCyan);
            }
            else
            {
                Crt.TextColor(Crt.LightGray);
            }

            Crt.Write(text);
        }

        static private void WriteLn(string text)
        {
            Write(text + "\r\n");
        }

        static private void WriteLn(string text, bool prefixWithTime)
        {
            Write(text + "\r\n", prefixWithTime);
        }
    }
}
