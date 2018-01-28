using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class ServerThreadManager {
        private static Dictionary<int, ServerThread> _ServerThreads = new Dictionary<int, ServerThread>();

        public static void PauseThreads() {
            foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                KV.Value.Pause();
            }
        }

        public static void ResumeThreads() {
            foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                KV.Value.Resume();
            }
        }

        public static bool StartThreads() {
            if ((Config.Default.RLoginServerPort > 0) || (Config.Default.TelnetServerPort > 0) || (Config.Default.WebSocketServerPort > 0)) {
                RMLog.Info("Starting Server Threads");

                try {
                    _ServerThreads.Clear();

                    if (Config.Default.RLoginServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Default.RLoginServerPort, new RLoginServerThread());
                    }

                    if (Config.Default.TelnetServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Default.TelnetServerPort, new TelnetServerThread());
                    }

                    if (Config.Default.WebSocketServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Default.WebSocketServerPort, new WebSocketServerThread());
                    }

                    // Now actually start the server threads
                    foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                        KV.Value.Start();
                    }

                    return true;
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in GameSrv::StartServerThreads()");
                    return false;
                }
            } else {
                RMLog.Error("No server ports found");
                return false;
            }
        }

        public static bool StopThreads() {
            if (_ServerThreads.Any()) {
                RMLog.Info("Stopping Server Threads");

                try {
                    foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                        KV.Value.Stop();
                    }
                    _ServerThreads.Clear();
                    return true;
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in GameSrv::StopServerThread()");
                    return false;
                }
            } else {
                return true;
            }
        }
    }
}
