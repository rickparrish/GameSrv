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
        private GameSrvStatus _Status = GameSrvStatus.Stopped;

        public event EventHandler<StatusEventArgs> StatusChangeEvent = null;

        public GameSrv() {
            _LogHandler = new LogHandler(Config.Instance.TimeFormatLog);
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

        public void Start() {
            if (_Status == GameSrvStatus.Paused) {
                // If we're paused, call Pause() again to un-pause
                Pause();
            } else if (_Status == GameSrvStatus.Stopped) {
                // Clean up the files not needed by this platform
                Helpers.CleanUpFiles();

                // Check for 3rd party software
                Helpers.CheckFor3rdPartySoftware();

                // Load the Global settings
                Config.Instance.Init();

                // Start the node manager
                NodeManager.Start();

                // Start the server threads
                ServerThreadManager.StartThreads();

                // Start the ignored ips thread
                IgnoredIPsThread.StartThread();

                // Drop root, if necessary
                try {
                    Helpers.DropRoot(Config.Instance.UnixUser);
                } catch (ArgumentOutOfRangeException aoorex) {
                    RMLog.Exception(aoorex, "Unable to drop from root to '" + Config.Instance.UnixUser + "'");
                    
                    // Undo previous actions on error
                    IgnoredIPsThread.StopThread();
                    ServerThreadManager.StopThreads();
                    NodeManager.Stop();

                    // If we get here, we failed to go online
                    UpdateStatus(GameSrvStatus.Stopped);
                    return;
                }

                // If we get here, we're online
                UpdateStatus(GameSrvStatus.Started);
            }
        }

        public GameSrvStatus Status {
            get { return _Status; }
        }

        // TODOX I don't really like this shutdown parameter, or the Offline vs Stopped states.  Need to make that more clear
        public void Stop() {
            if ((_Status == GameSrvStatus.Paused) || (_Status == GameSrvStatus.Started)) {
                UpdateStatus(GameSrvStatus.Stopping);

                IgnoredIPsThread.StopThread();
                ServerThreadManager.StopThreads();
                NodeManager.Stop();

                UpdateStatus(GameSrvStatus.Stopped);
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
            // or, if code analysis gives a CA1816
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
