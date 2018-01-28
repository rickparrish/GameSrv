using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class SimpleConsoleApp {
        private static Dictionary<ConnectionType, int> _ConnectionCounts = new Dictionary<ConnectionType, int>();
        private static GameSrv _GameSrv = null;
        private static object _StatusTextLock = new object();

        public static void Start() {
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
            _GameSrv.Start();

            // Main program loop
            int LastSecond = -1;
            bool Quit = false;
            while (!Quit) {
                while (!Crt.KeyPressed()) {
                    Crt.Delay(100);

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

        private static void InitConsole() {
            Crt.ClrScr();
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
    }
}
