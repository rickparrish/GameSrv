using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    static class ServerThreadManager {
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

        public static void StartThreads() {
            if ((Config.Instance.RLoginServerPort > 0) || (Config.Instance.TelnetServerPort > 0)) {
                RMLog.Info("Starting Server Threads");

                try {
                    _ServerThreads.Clear();

                    if (Config.Instance.RLoginServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Instance.RLoginServerPort, new RLoginServerThread());
                    }

                    if (Config.Instance.TelnetServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Instance.TelnetServerPort, new TelnetServerThread());
                    }

                    if (Config.Instance.WebSocketServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Instance.WebSocketServerPort, new WebSocketServerThread());
                    }

                    // Now actually start the server threads
                    foreach (var KVP in _ServerThreads) {
                        KVP.Value.Start();
                    }
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in GameSrv::StartServerThreads()");
                }
            } else {
                RMLog.Error("Must specify a port for RLogin and/or Telnet servers");
            }
        }

        public static void StopThreads() {
            RMLog.Info("Stopping Server Threads");

            try {
                foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                    KV.Value.Stop();
                }
                _ServerThreads.Clear();
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in GameSrv::StopServerThread()");
            }
        }
    }
}
