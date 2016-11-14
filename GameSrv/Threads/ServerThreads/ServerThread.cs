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
using System.IO;
using RandM.RMLib;
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;

// TODOX Add check for flash socket policy request by doing a peek with a 1 second timeout or something
//       If peeked character is < then peek another character to see if it's the flash request string
namespace RandM.GameSrv {
    public abstract class ServerThread : RMThread {
        protected Config _Config;
        protected ConnectionType _ConnectionType;
        protected string _LocalAddress;
        protected int _LocalPort;

        public event EventHandler BoundEvent = null;

        public ServerThread(Config config) {
            _Config = config;
            _Paused = false;
        }

        protected override void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects)
                // set large fields to null.

                // Call the base dispose
                base.Dispose(disposing);
            }
        }

        private void DisplayAnsi(string fileName, TcpConnection connection, TerminalType terminalType) {
            try {
                List<string> FileNames = new List<string>();
                if (terminalType == TerminalType.RIP) {
                    FileNames.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".rip"));
                }
                if ((terminalType == TerminalType.RIP) || (terminalType == TerminalType.ANSI)) {
                    FileNames.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".ans"));
                }
                FileNames.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".asc"));

                foreach (string FullFileName in FileNames) {
                    if (File.Exists(FullFileName)) {
                        connection.Write(FileUtils.FileReadAllText(FullFileName));
                        break;
                    }
                }
            } catch (IOException ioex) {
                RMLog.Exception(ioex, "Unable to display '" + fileName + "'");
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to display '" + fileName + "'");
            }
        }

        protected override void Execute() {
            while (!_Stop) {
                using (TcpConnection Connection = new TcpConnection()) {
                    if (Connection.Listen(_LocalAddress, _LocalPort)) {
                        BoundEvent?.Invoke(this, EventArgs.Empty);

                        while (!_Stop) {
                            // Accept an incoming connection
                            if (Connection.CanAccept(1000)) // 1 second
                            {
                                try {
                                    TcpConnection NewConnection = Connection.AcceptTCP();
                                    if (NewConnection != null) {
                                        HandleNewConnection(NewConnection);
                                    }
                                } catch (Exception ex) {
                                    RMLog.Exception(ex, "Error in ServerThread::Execute()");
                                }
                            }
                        }
                    } else {
                        RMLog.Error($"{_ConnectionType} Server Thread unable to listen on {_LocalAddress}:{_LocalPort}.  Retry in 15 seconds.");
                        for (int i = 1; i <= 15; i++) {
                            Thread.Sleep(1000);
                            if (_Stop) break;
                        }
                    }
                }
            }
        }

        protected abstract void HandleNewConnection(TcpConnection newConnection);
    }
}
