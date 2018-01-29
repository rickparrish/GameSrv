using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    static class SimpleConsoleApp {
        private static GameSrv _GameSrv = null;

        public static void Start() {
            // Initialize the screen
            Crt.ClrScr();
            StatusText(Helpers.Copyright, false);

            // Check if running as root
            if (Helpers.StartedAsRoot) {
                StatusText("", false);
                StatusText("*** WARNING: Running GameSrv as root is NOT recommended ***", false);
                StatusText("", false);
                StatusText("A safer alternative to running GameSrv as root is to run it via 'privbind'", false);
                StatusText("This will ensure GameSrv is able to bind to ports in the < 1024 range, but", false);
                StatusText("it will run as a regular unprivileged program in every other way", false);
                StatusText("", false);
                StatusText("See start.sh for an example of the recommended method to start GameSrv", false);
                StatusText("", false);
            }

            // Setup log handler
            RMLog.Handler += RMLog_Handler;

            // Init GameSrv 
            _GameSrv = new GameSrv();
            _GameSrv.Start();

            // Main program loop
            bool Quit = false;
            while (!Quit) {
                char Ch = Crt.ReadKey();
                switch (Ch.ToString().ToUpper()) {
                    case "\0":
                        char Ch2 = Crt.ReadKey();
                        if (Ch2 == ';') // F1
                        {
                            StatusText("", false);
                            StatusText("GameSrv WFC Screen Help", false);
                            StatusText("-=-=-=-=-=-=-=-=-=-=-=-", false);
                            StatusText("F1 = Help  (this screen)", false);
                            StatusText("C  = Clear (clear the status window)", false);
                            StatusText("P  = Pause (reject new connections, leave existing connections alone)", false);
                            StatusText("S  = Setup (launch the config program)", false);
                            StatusText("Q  = Quit  (shut down and terminate existing connections)", false);
                            StatusText("", false);
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
                            int ConnectionCount = NodeManager.ConnectionCount;
                            if (ConnectionCount > 0) {
                                StatusText("", false);
                                StatusText("There are " + ConnectionCount.ToString() + " active connections.", false);
                                StatusText("Are you sure you want to quit [y/N]: ", false);
                                Ch = Crt.ReadKey();
                                if (Ch.ToString().ToUpper() != "Y") {
                                    StatusText("", false);
                                    StatusText("Cancelling quit request.", false);
                                    StatusText("", false);
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

            Environment.Exit(0);
        }

        // TODOX Have entries in the INI file that define which colour to use for each type of message
        private static void RMLog_Handler(object sender, RMLogEventArgs e) {
            StatusText($"{e.Level.ToString().ToUpper()}: {e.Message}");
        }

        private static void StatusText(string text, bool prefixWithTime = true) {
            if (prefixWithTime && (!string.IsNullOrEmpty(text))) {
                text = $"{DateTime.Now.ToString(Config.Instance.TimeFormatUI)}  {text}";
            }
            Crt.Write($"{text}\r\n");
        }
    }
}
