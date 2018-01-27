using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class ConsoleApp {
        private static Dictionary<ConnectionType, int> _ConnectionCounts = new Dictionary<ConnectionType, int>();
        private static bool _FancyOutput = OSUtils.IsWindows;
        private static GameSrv _GameSrv = null;
        private static object _StatusTextLock = new object();
        private static string _TimeFormatFooter = "hh:mmtt";

        public static void Start(string[] args) {
            // Check command-line parameters
            foreach (string Arg in args) {
                switch (Arg.ToLower()) {
                    case "24h":
                        _TimeFormatFooter = "HH:mm";
                        break;
                    case "simple":
                        _FancyOutput = false;
                        break;
                }
            }

            // Remove old "stop requested" file
            if (File.Exists(StringUtils.PathCombine(ProcessUtils.StartupPath, "gamesrvconsole.stop"))) {
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "gamesrvconsole.stop"));
            }

            // Add connection types to counter
            _ConnectionCounts[ConnectionType.RLogin] = 0;
            _ConnectionCounts[ConnectionType.Telnet] = 0;
            _ConnectionCounts[ConnectionType.WebSocket] = 0;

            // Initialize the screen
            InitConsole();
            StatusText(Helpers.Copyright, Crt.White, false);

            // Check if running as root
            if (Helpers.StartedAsRoot) {
                StatusText("", Crt.LightMagenta, false);
                StatusText("*** WARNING: Running GameSrv as root is NOT recommended ***", Crt.LightMagenta, false);
                StatusText("", Crt.LightMagenta, false);
                StatusText("A safer alternative to running GameSrv as root is to run it via 'privbind'", Crt.LightMagenta, false);
                StatusText("This will ensure GameSrv is able to bind to ports in the < 1024 range, but", Crt.LightMagenta, false);
                StatusText("it will run as a regular unprivileged program in every other way", Crt.LightMagenta, false);
                StatusText("", Crt.LightMagenta, false);
                StatusText("See start.sh for an example of the recommended method to start GameSrv", Crt.LightMagenta, false);
                StatusText("", Crt.LightMagenta, false);
            }

            // Setup log handler
            RMLog.Handler += RMLog_Handler;

            // Init GameSrv 
            _GameSrv = new GameSrv();
            _GameSrv.NodeEvent += GameSrv_NodeEvent;
            _GameSrv.Start();

            // Main program loop
            int LastMinute = -1;
            int LastSecond = -1;
            bool Quit = false;
            while (!Quit) {
                while (!Crt.KeyPressed()) {
                    Crt.Delay(100);
                    if (DateTime.Now.Minute != LastMinute) {
                        UpdateTime();
                        LastMinute = DateTime.Now.Minute;
                    }

                    if ((DateTime.Now.Second % 2 == 0) && (DateTime.Now.Second != LastSecond)) {
                        LastSecond = DateTime.Now.Second;
                        if (File.Exists(StringUtils.PathCombine(ProcessUtils.StartupPath, "gamesrvconsole.stop"))) {
                            FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "gamesrvconsole.stop"));

                            _GameSrv.Stop(true);
                            _GameSrv.Dispose();
                            Quit = true;
                            break;
                        }
                    }
                }

                if (Crt.KeyPressed()) {
                    char Ch = Crt.ReadKey();
                    switch (Ch.ToString().ToUpper()) {
                        case "\0":
                            char Ch2 = Crt.ReadKey();
                            if (Ch2 == ';') // F1
                            {
                                StatusText("", Crt.White, false);
                                StatusText("GameSrv WFC Screen Help", Crt.White, false);
                                StatusText("-=-=-=-=-=-=-=-=-=-=-=-", Crt.White, false);
                                StatusText("F1 = Help  (this screen)", Crt.White, false);
                                StatusText("C  = Clear (clear the status window)", Crt.White, false);
                                StatusText("P  = Pause (reject new connections, leave existing connections alone)", Crt.White, false);
                                StatusText("S  = Setup (launch the config program)", Crt.White, false);
                                StatusText("Q  = Quit  (shut down and terminate existing connections)", Crt.White, false);
                                StatusText("", Crt.White, false);
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
                            if ((_GameSrv.Status != GameSrvStatus.Stopped) && (_GameSrv.Status != GameSrvStatus.Stopping)) {
                                int ConnectionCount = _GameSrv.ConnectionCount;
                                if (ConnectionCount > 0) {
                                    StatusText("", Crt.White, false);
                                    StatusText("There are " + ConnectionCount.ToString() + " active connections.", Crt.White, false);
                                    StatusText("Are you sure you want to quit [y/N]: ", Crt.White, false);
                                    StatusText("", Crt.White, false);
                                    Ch = Crt.ReadKey();
                                    if (Ch.ToString().ToUpper() != "Y") {
                                        StatusText("", Crt.White, false);
                                        StatusText("Cancelling quit request.", Crt.White, false);
                                        StatusText("", Crt.White, false);
                                        continue;
                                    }
                                }
                            }

                            _GameSrv.Stop(true);
                            _GameSrv.Dispose();
                            Quit = true;
                            break;
                    }
                }
            }

            Environment.Exit(0);
        }

        private static void GameSrv_NodeEvent(object sender, NodeEventArgs e) {
            if (e.EventType == NodeEventType.LogOn) {
                if (_FancyOutput) {
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
        }

        private static void InitConsole() {
            if (_FancyOutput) {
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
            } else {
                Crt.ClrScr();
            }
        }

        // TODOX Have entries in the INI file that define which colour to use for each type of message
        private static void RMLog_Handler(object sender, RMLogEventArgs e) {
            switch (e.Level) {
                case LogLevel.Debug:
                    StatusText("DEBUG: " + e.Message, Crt.LightCyan);
                    break;
                case LogLevel.Error:
                    StatusText("ERROR: " + e.Message, Crt.LightRed);
                    break;
                case LogLevel.Info:
                    StatusText(e.Message, Crt.LightGray);
                    break;
                case LogLevel.Trace:
                    StatusText("TRACE: " + e.Message, Crt.DarkGray);
                    break;
                case LogLevel.Warning:
                    StatusText("WARNING: " + e.Message, Crt.Yellow);
                    break;
                default:
                    StatusText("UNKNOWN: " + e.Message, Crt.White);
                    break;
            }
        }

        private static void StatusText(string text, int foreColour, bool prefixWithTime = true) {
            lock (_StatusTextLock) {
                if (prefixWithTime && (!string.IsNullOrEmpty(text))) {
                    Crt.TextColor(Crt.LightGray);
                    Crt.Write(DateTime.Now.ToString(_GameSrv.TimeFormatUI) + "  ");
                }
                Crt.TextColor(foreColour);
                Crt.Write(text + "\r\n");
            }
        }

        private static void UpdateTime() {
            if (_FancyOutput) {
                // Update time
                Crt.FastWrite(StringUtils.PadRight(DateTime.Now.ToString(_TimeFormatFooter).ToLower(), ' ', 7), 9, 38, Crt.LightGreen);
                Crt.FastWrite(StringUtils.PadRight(DateTime.Now.ToString("dddd MMMM dd, yyyy"), ' ', 28), 23, 38, Crt.LightGreen);
            }
        }
    }
}
