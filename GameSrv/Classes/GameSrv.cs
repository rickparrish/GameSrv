//TODOX Handle gamesrv.stop requests here via file system watcher?
//TODOX Also handle changes to gamesrv.ini via file system watcher?  not all changes can be dynamically updated, but maybe some can
/*
  GameSrv: A BBS Door Game Server
  Copyright (C) Rick Parrish, R&M Software

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
        private LogHandler _LogHandler = null;
        private GameSrvStatus _Status = GameSrvStatus.Offline;

        public event EventHandler<IntEventArgs> ConnectionCountChangeEvent = null;
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<StatusEventArgs> StatusChangeEvent = null;

        public GameSrv() {
            _LogHandler = new LogHandler(Config.Instance.TimeFormatLog);
        }

        private bool LoadGlobalSettings() {
            // Settings are actually loaded already, just checking that the node numbers are sane here
            RMLog.Info("Loading Global Settings");
            if (Config.Instance.FirstNode > Config.Instance.LastNode) {
                RMLog.Error("FirstNode cannot be greater than LastNode!");
                return false;
            }

            return Config.Instance.Loaded;
        }

        void NodeManager_ConnectionCountChangeEvent(object sender, IntEventArgs e) {
            ConnectionCountChangeEvent?.Invoke(sender, e);
        }

        public void Pause() {
            if (_Status == GameSrvStatus.Paused) {
                UpdateStatus(GameSrvStatus.Resuming);
                ServerThreadManager.ResumeThreads();
                UpdateStatus(GameSrvStatus.Started);
            } else if (_Status == GameSrvStatus.Started) {
                UpdateStatus(GameSrvStatus.Pausing);
                ServerThreadManager.PauseThreads();
                UpdateStatus(GameSrvStatus.Paused);
            }
        }

        public bool Start() {
            if (_Status == GameSrvStatus.Paused) {
                // If we're paused, call Pause() again to un-pause
                Pause();
                return true;
            } else if (_Status == GameSrvStatus.Stopped) {
                UpdateStatus(GameSrvStatus.Starting);
                _Status = GameSrvStatus.Started;
                return true;
            } else if (_Status == GameSrvStatus.Offline) { 
                // Clean up the files not needed by this platform
                Helpers.CleanUpFiles();

                // Load the Global settings
                if (!LoadGlobalSettings()) {
                    RMLog.Info("Unable To Load Global Settings...Will Use Defaults");
                    Config.Instance.Save();
                }

                // Start the node manager
                if (!StartNodeManager()) {
                    RMLog.Error("Unable To Start Node Manager");
                    // Undo previous actions
                    goto ERROR;
                }

                // Start the server threads
                if (!ServerThreadManager.StartThreads()) {
                    RMLog.Error("Unable To Start Server Threads");
                    // Undo previous actions
                    ServerThreadManager.StopThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // Start the ignored ips thread
                if (!IgnoredIPsThread.StartThread()) {
                    RMLog.Error("Unable To Start Ignored IPs Thread");
                    // Undo previous actions
                    ServerThreadManager.StopThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // Drop root, if necessary
                try {
                    Helpers.DropRoot(Config.Instance.UnixUser);
                } catch (ArgumentOutOfRangeException aoorex) {
                    RMLog.Exception(aoorex, "Unable to drop from root to '" + Config.Instance.UnixUser + "'");
                    // Undo previous actions
                    IgnoredIPsThread.StopThread();
                    ServerThreadManager.StopThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // If we get here, we're online
                UpdateStatus(GameSrvStatus.Started);
                return true;

                ERROR:

                // TODOX could all the rolling back be handled here via NULL checks?

                // If we get here, we failed to go online
                UpdateStatus(GameSrvStatus.Offline);
                return false;
            }

            return false;
        }

        private bool StartNodeManager() {
            RMLog.Info("Starting Node Manager");

            try {
                NodeManager.ConnectionCountChangeEvent += NodeManager_ConnectionCountChangeEvent;
                NodeManager.Start(Config.Instance.FirstNode, Config.Instance.LastNode);
                return true;
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in GameSrv::StartNodeManager()");
                return false;
            }
        }

        public GameSrvStatus Status {
            get { return _Status; }
        }

        public void Stop(bool shutdown) {
            if ((_Status == GameSrvStatus.Paused) || (_Status == GameSrvStatus.Started)) {
                if (shutdown) {
                    UpdateStatus(GameSrvStatus.Stopping);

                    IgnoredIPsThread.StopThread();
                    ServerThreadManager.StopThreads();
                    StopNodeManager();

                    UpdateStatus(GameSrvStatus.Offline);
                } else {
                    // TODOX Need to let the server/client threads know so they can reject new connections?
                    UpdateStatus(GameSrvStatus.Stopped);
                }
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
                    break;
                case GameSrvStatus.Stopping:
                    RMLog.Info("Server(s) are stopping...");
                    break;
            }
        }

        public static string Version {
            get { return ProcessUtils.ProductVersionOfCallingAssembly; }
        }

        #region IDisposable Support
        private bool _Disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // dispose managed state (managed objects).
                    IgnoredIPsThread.StopThread();
                    ServerThreadManager.StopThreads();
                    if (_LogHandler != null) {
                        _LogHandler.Dispose();
                        _LogHandler = null;
                    }
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                _Disposed = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GameSrv() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
