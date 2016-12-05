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
using System;
using System.Collections.Generic;
using RandM.RMLib;
using System.Security.Principal;
using System.IO;
using System.Timers;
using System.Globalization;
using System.Net;

namespace RandM.GameSrv {
    public class GameSrv : IDisposable {
        private int _BindCount = 0;
        private bool _BindFailed = false;
        private int _BoundCount = 0;
        private object _BoundEventLock = new object();
        private Config _Config = null;
        private bool _Disposed = false;
        private IgnoredIPsThread _IgnoredIPsThread = null;
        private List<string> _Log = new List<string>();
        private object _LogLock = new object();
        private Timer _LogTimer = new Timer();
        private Dictionary<int, ServerThread> _ServerThreads = new Dictionary<int, ServerThread>();
        private GameSrvStatus _Status = GameSrvStatus.Stopped;

        public event EventHandler<IntEventArgs> ConnectionCountChangeEvent = null;
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<StatusEventArgs> StatusChangeEvent = null;

        public GameSrv() {
            _Config = new Config();

            // Ensure the log directory exists
            Directory.CreateDirectory(StringUtils.PathCombine(ProcessUtils.StartupPath, "logs"));

            _LogTimer.Interval = 60000; // 1 minute
            _LogTimer.Elapsed += LogTimer_Elapsed;
            _LogTimer.Start();

            RMLog.Handler += RMLog_Handler;
        }

        ~GameSrv() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            // Check to see if Dispose has already been called.
            if (!_Disposed) {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing) {
                    // Dispose managed resources.
                    if (_IgnoredIPsThread != null) {
                        _IgnoredIPsThread.Stop();
                        _IgnoredIPsThread.Dispose();
                    }
                    if (_LogTimer != null) {
                        _LogTimer.Stop();
                        _LogTimer.Dispose();
                    }
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _Disposed = true;
            }
        }

        private void AddToLog(string logMessage) {
            lock (_LogLock) {
                _Log.Add(DateTime.Now.ToString(_Config.TimeFormatLog) + "  " + logMessage);
            }
        }

        private void CleanUpFiles() {
            if (OSUtils.IsWindows) {
                FileUtils.FileDelete("cpulimit.sh");
                FileUtils.FileDelete("dosutils.zip");
                FileUtils.FileDelete("install.sh");
                FileUtils.FileDelete("pty-sharp-1.0.zip");
                FileUtils.FileDelete("start.sh");
                if (OSUtils.IsWinNT) {
                    if (ProcessUtils.Is64BitOperatingSystem) {
                        FileUtils.FileDelete("dosxtrn.exe");
                        FileUtils.FileDelete("dosxtrn.pif");
                        FileUtils.FileDelete("sbbsexec.dll");
                        if (!Globals.IsDOSBoxInstalled()) {
                            RMLog.Error("PLEASE INSTALL DOSBOX 0.73 IF YOU PLAN ON RUNNING DOS DOORS USING DOSBOX");
                        }
                    } else {
                        FileUtils.FileDelete("dosbox.conf");
                        if (!File.Exists(StringUtils.PathCombine(Environment.SystemDirectory, "sbbsexec.dll"))) {
                            RMLog.Error("PLEASE COPY SBBSEXEC.DLL TO " + StringUtils.PathCombine(Environment.SystemDirectory, "sbbsexec.dll").ToUpper() + " IF YOU PLAN ON RUNNING DOS DOORS USING THE EMBEDDED SYNCHRONET FOSSIL");
                        }
                    }
                }
            } else if (OSUtils.IsUnix) {
                FileUtils.FileDelete("dosbox.conf");
                FileUtils.FileDelete("dosxtrn.exe");
                FileUtils.FileDelete("dosxtrn.pif");
                FileUtils.FileDelete("install.cmd");
                FileUtils.FileDelete("sbbsexec.dll");
            }
        }

        public int ConnectionCount {
            get {
                return NodeManager.ConnectionCount;
            }
        }

        public void DisconnectNode(int node) {
            NodeManager.DisconnectNode(node);
        }

        public int FirstNode {
            get { return _Config.FirstNode; }
        }

        private void FlushLog() {
            lock (_LogLock) {
                // Flush log to disk
                if (_Log.Count > 0) {
                    try {
                        FileUtils.FileAppendAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "logs", "gamesrv.log"), string.Join(Environment.NewLine, _Log.ToArray()) + Environment.NewLine);
                        _Log.Clear();
                    } catch (Exception ex) {
                        RMLog.Exception(ex, "Unable to update gamesrv.log");
                    }
                }
            }
        }

        public int LastNode {
            get { return _Config.LastNode; }
        }

        private bool LoadGlobalSettings() {
            RMLog.Info("Loading Global Settings");
            if (_Config.FirstNode > _Config.LastNode) {
                RMLog.Error("FirstNode cannot be greater than LastNode!");
                return false;
            }

            return _Config.Loaded;
        }

        void LogTimer_Elapsed(object sender, ElapsedEventArgs e) {
            _LogTimer.Stop();
            FlushLog();
            _LogTimer.Start();
        }

        void NodeManager_ConnectionCountChangeEvent(object sender, IntEventArgs e) {
            ConnectionCountChangeEvent?.Invoke(sender, e);
        }

        void NodeManager_NodeEvent(object sender, NodeEventArgs e) {
            NodeEvent?.Invoke(sender, e);
        }

        public void Pause() {
            if (_Status == GameSrvStatus.Paused) {
                UpdateStatus(GameSrvStatus.Resuming);
                foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                    KV.Value.Pause();
                }
                UpdateStatus(GameSrvStatus.Started);

                // We really want to be in the Started state, so override the above (but dont raise an event)
                _Status = GameSrvStatus.Started;
            } else if (_Status == GameSrvStatus.Started) {
                UpdateStatus(GameSrvStatus.Pausing);
                foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads) {
                    KV.Value.Pause();
                }
                UpdateStatus(GameSrvStatus.Paused);
            }
        }

        private void RMLog_Handler(object sender, RMLogEventArgs e) {
            AddToLog($"[{e.Level.ToString()}] {e.Message}");
        }

        private void ServerThread_BoundEvent(object sender, EventArgs e) {
            // Check if all server threads are now bound
            lock (_BoundEventLock) {
                if (++_BoundCount == _BindCount) {
                    try {
                        Globals.DropRoot(_Config.UnixUser);
                    } catch (ArgumentOutOfRangeException aoorex) {
                        RMLog.Exception(aoorex, "Unable to drop from root to '" + _Config.UnixUser + "'");

                        // Abort the server
                        if (_Status == GameSrvStatus.Started) {
                            Stop();
                        } else {
                            _BindFailed = true;
                        }
                    }
                }
            }
        }

        public bool Start() {
            if (_Status == GameSrvStatus.Paused) {
                // If we're paused, call Pause() again to un-pause
                Pause();
                return true;
            } else if (_Status == GameSrvStatus.Stopped) {
                UpdateStatus(GameSrvStatus.Starting);

                // Clean up the files not needed by this platform
                CleanUpFiles();

                // Load the Global settings
                if (!LoadGlobalSettings()) {
                    RMLog.Info("Unable To Load Global Settings...Will Use Defaults");
                    _Config.Save();
                }

                // Start the node manager
                if (!StartNodeManager()) {
                    RMLog.Info("Unable To Start Node Manager");
                    // Undo previous actions
                    goto ERROR;
                }

                // Reset bind variables
                _BindCount = GetBindCount();
                _BindFailed = false;
                _BoundCount = 0;

                // Start the server threads
                if (!StartServerThreads()) {
                    RMLog.Info("Unable To Start Server Threads");
                    // Undo previous actions
                    StopServerThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // Start the ignored ips thread
                if (!StartIgnoredIPsThread()) {
                    RMLog.Error("Unable To Start Ignored IPs Thread");
                    // Undo previous actions
                    StopServerThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // Check if we had a bind failure before finishing
                if (_BindFailed) {
                    RMLog.Error("One Or More Servers Failed To Bind To Their Assigned Ports");
                    // Undo previous actions
                    StopIgnoredIPsThread();
                    StopServerThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // If we get here, we're online
                UpdateStatus(GameSrvStatus.Started);
                return true;

                ERROR:

                // TODOX could all the rolling back be handled here via NULL checks?

                // If we get here, we failed to go online
                UpdateStatus(GameSrvStatus.Stopped);
                return false;
            }

            return false;
        }

        private int GetBindCount() {
            int Result = 0;
            if (_Config.RLoginServerPort > 0) Result += 1;
            if (_Config.TelnetServerPort > 0) Result += 1;
            if (_Config.WebSocketServerPort > 0) Result += 1;
            return Result;
        }

        private bool StartIgnoredIPsThread() {
            RMLog.Info("Starting Ignored IPs Thread");

            try {
                // Create Ignored IPs Thread and Thread objects
                _IgnoredIPsThread = new IgnoredIPsThread();
                _IgnoredIPsThread.Start();
                return true;
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in GameSrv::StartIgnoredIPsThread()");
                return false;
            }

        }

        private bool StartNodeManager() {
            RMLog.Info("Starting Node Manager");

            try {
                NodeManager.ConnectionCountChangeEvent += NodeManager_ConnectionCountChangeEvent;
                NodeManager.Start(_Config.FirstNode, _Config.LastNode);
                return true;
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in GameSrv::StartNodeManager()");
                return false;
            }
        }

        private bool StartServerThreads() {
            if ((_Config.RLoginServerPort > 0) || (_Config.TelnetServerPort > 0) || (_Config.WebSocketServerPort > 0)) {
                RMLog.Info("Starting Server Threads");

                try {
                    _ServerThreads.Clear();

                    if (_Config.RLoginServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(_Config.RLoginServerPort, new RLoginServerThread(_Config));
                        _ServerThreads[_Config.RLoginServerPort].BoundEvent += ServerThread_BoundEvent;
                    }

                    if (_Config.TelnetServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(_Config.TelnetServerPort, new TelnetServerThread(_Config));
                        _ServerThreads[_Config.TelnetServerPort].BoundEvent += ServerThread_BoundEvent;
                    }

                    if (_Config.WebSocketServerPort > 0) {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(_Config.WebSocketServerPort, new WebSocketServerThread(_Config));
                        _ServerThreads[_Config.WebSocketServerPort].BoundEvent += ServerThread_BoundEvent;
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

        public GameSrvStatus Status {
            get { return _Status; }
        }

        public void Stop() {
            if ((_Status == GameSrvStatus.Paused) || (_Status == GameSrvStatus.Started)) {
                UpdateStatus(GameSrvStatus.Stopping);

                StopIgnoredIPsThread();
                StopServerThreads();
                StopNodeManager();

                UpdateStatus(GameSrvStatus.Stopped);
            }
        }

        private bool StopNodeManager() {
                RMLog.Info("Stopping Node Manager");

                try {
                    NodeManager.Stop();

                    return true;
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in GameSrv::StopNodeManger()");
                    return false;
                }
        }

        private bool StopServerThreads() {
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

        private bool StopIgnoredIPsThread() {
            if (_IgnoredIPsThread != null) {
                RMLog.Info("Stopping Ignored IPs Thread");

                try {
                    _IgnoredIPsThread.Stop();
                    _IgnoredIPsThread.Dispose();
                    _IgnoredIPsThread = null;

                    return true;
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in GameSrv::StopIgnoredIPsThread()");
                    return false;
                }
            } else {
                return false;
            }
        }

        public string TimeFormatLog {
            get { return _Config.TimeFormatLog; }
        }

        public string TimeFormatUI {
            get { return _Config.TimeFormatUI; }
        }

        private void UpdateStatus(GameSrvStatus newStatus) {
            // Record the new status
            _Status = newStatus;

            StatusChangeEvent?.Invoke(this, new StatusEventArgs(newStatus));

            switch (newStatus) {
                case GameSrvStatus.Paused:
                    RMLog.Info("Server(s) are paused");
                    break;
                case GameSrvStatus.Pausing:
                    RMLog.Info("Server(s) are pausing...");
                    break;
                case GameSrvStatus.Resuming:
                    RMLog.Info("Server(s) are resuming...");
                    break;
                case GameSrvStatus.Started:
                    RMLog.Info("Server(s) have started");
                    break;
                case GameSrvStatus.Starting:
                    RMLog.Info("Server(s) are starting...");
                    break;
                case GameSrvStatus.Stopped:
                    RMLog.Info("Server(s) have stopped");
                    FlushLog();
                    break;
                case GameSrvStatus.Stopping:
                    RMLog.Info("Server(s) are stopping...");
                    break;
            }
        }

        public static string Version {
            get { return ProcessUtils.ProductVersionOfCallingAssembly; }
        }
    }
}
