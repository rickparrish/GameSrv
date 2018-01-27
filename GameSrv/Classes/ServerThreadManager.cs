using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class ServerThreadManager {
        private static int _BindCount = 0;
        private static bool _BindFailed = false;
        private static int _BoundCount = 0;
        private static object _BoundEventLock = new object();
        private static Dictionary<int, ServerThread> _ServerThreads = new Dictionary<int, ServerThread>();

        public static bool BindFailed {
            get { return _BindFailed; }
        }

        private static int GetBindCount() {
            int Result = 0;
            if (Config.Default.RLoginServerPort > 0)
                Result += 1;
            if (Config.Default.TelnetServerPort > 0)
                Result += 1;
            if (Config.Default.WebSocketServerPort > 0)
                Result += 1;
            return Result;
        }

        public static void Pause() {
            foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                KV.Value.Pause();
            }
        }

        public static void Resume() {
            foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                KV.Value.Resume();
            }
        }

        private static void ServerThread_BoundEvent(object sender, EventArgs e) {
            // Check if all server threads are now bound
            lock (_BoundEventLock) {
                if (++_BoundCount == _BindCount) {
                    try {
                        Helpers.DropRoot(Config.Default.UnixUser);
                    } catch (ArgumentOutOfRangeException aoorex) {
                        RMLog.Exception(aoorex, "Unable to drop from root to '" + Config.Default.UnixUser + "'");

                        // TODOX Abort the server
                        //if (_Status == GameSrvStatus.Started) {
                        //    Stop(true);
                        //} else {
                        //    _BindFailed = true;
                        //}
                    }
                }
            }
        }

        public static bool Start() {
            // Reset bind variables
            _BindCount = GetBindCount();
            _BindFailed = false;
            _BoundCount = 0;

            if ((Config.Default.RLoginServerPort > 0) || (Config.Default.TelnetServerPort > 0) || (Config.Default.WebSocketServerPort > 0)) {
                RMLog.Info("Starting Server Threads");

                try {
                    _ServerThreads.Clear();

                    if (Config.Default.RLoginServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Default.RLoginServerPort, new RLoginServerThread());
                        _ServerThreads[Config.Default.RLoginServerPort].BoundEvent += ServerThread_BoundEvent;
                    }

                    if (Config.Default.TelnetServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Default.TelnetServerPort, new TelnetServerThread());
                        _ServerThreads[Config.Default.TelnetServerPort].BoundEvent += ServerThread_BoundEvent;
                    }

                    if (Config.Default.WebSocketServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(Config.Default.WebSocketServerPort, new WebSocketServerThread());
                        _ServerThreads[Config.Default.WebSocketServerPort].BoundEvent += ServerThread_BoundEvent;
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

        public static bool Stop() {
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
        }
    }
}
