/*
  GameSrv: A BBS Door Game Server
  Copyright (C) 2002-2013  Rick Parrish, R&M Software

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

namespace RandM.GameSrv
{
    class ServerThread : RMThread, IDisposable
    {
        private ConnectionType _ConnectionType;
        private bool _Disposed = false;
        private string _LocalAddress;
        private int _LocalPort;

        public event EventHandler BindFailedEvent = null;
        public event EventHandler BoundEvent = null;
        public event EventHandler<ConnectEventArgs> ConnectEvent = null;
        public event EventHandler<StringEventArgs> ErrorMessageEvent = null;
        public event EventHandler<ExceptionEventArgs> ExceptionEvent = null;
        public event EventHandler<StringEventArgs> WarningMessageEvent = null;

        public ServerThread(string ALocalAddress, int ALocalPort, ConnectionType AConnectionType)
        {
            _LocalAddress = ALocalAddress;
            _LocalPort = ALocalPort;
            _ConnectionType = AConnectionType;
            _Paused = false;
        }

        ~ServerThread()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_Disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _Disposed = true;
            }
        }

        private void DisplayAnsi(string fileName, TcpConnection ATCP)
        {
            try
            {
                ATCP.Write(FileUtils.FileReadAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName + ".ans")));
            }
            catch (IOException ioex)
            {
                RaiseExceptionEvent("I/O exception displaying " + fileName + ": " + ioex.Message, ioex);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override void Execute()
        {
            using (TcpConnection Connection = new TcpConnection())
            {
                if (Connection.Listen(_LocalAddress, _LocalPort))
                {
                    RaiseBoundEvent();

                    while (!_Stop)
                    {
                        // Accept an incoming connection
                        if (Connection.CanAccept(1000)) // 1 second
                        {
                            try
                            {
                                TcpConnection NewConnection = Connection.AcceptTCP();
                                if (NewConnection != null)
                                {
                                    if (IsBannedIP(NewConnection.GetRemoteIP()))
                                    {
                                        DisplayAnsi("IP_BANNED", NewConnection);
                                        RaiseWarningMessageEvent("IP " + NewConnection.GetRemoteIP() + " matches banned IP filter");
                                        NewConnection.Close();
                                    }
                                    else if (_Paused)
                                    {
                                        DisplayAnsi("SERVER_PAUSED", NewConnection);
                                        NewConnection.Close();
                                    }
                                    else
                                    {
                                        ClientThread NewClientThread = new ClientThread();
                                        int NewNode = RaiseConnectEvent(ref NewClientThread);
                                        if (NewNode == 0)
                                        {
                                            NewClientThread.Dispose();
                                            DisplayAnsi("SERVER_BUSY", NewConnection);
                                            NewConnection.Close();
                                        }
                                        else
                                        {
                                            NewClientThread.Start(NewNode, NewConnection, _ConnectionType);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                RaiseExceptionEvent("Unhandled exception in ServerThread::Execute(): " + ex.Message, ex);
                            }
                        }
                    }
                }
                else
                {
                    RaiseErrorMessageEvent("Server Thread unable to listen on " + _LocalAddress + ":" + _LocalPort);
                    RaiseBindFailedEvent();
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool IsBannedIP(string ip)
        {
            try
            {
                string BannedIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-ips.txt");
                if (File.Exists(BannedIPsFileName))
                {
                    string[] ConnectionOctets = ip.Split('.');
                    if (ConnectionOctets.Length == 4)
                    {
                        string[] BannedIPs = FileUtils.FileReadAllLines(BannedIPsFileName);
                        foreach (string BannedIP in BannedIPs)
                        {
                            if (BannedIP.StartsWith(";")) continue;

                            string[] BannedOctets = BannedIP.Split('.');
                            if (BannedOctets.Length == 4)
                            {
                                bool Match = true;
                                for (int i = 0; i < 4; i++)
                                {
                                    if ((BannedOctets[i] == "*") || (BannedOctets[i] == ConnectionOctets[i]))
                                    {
                                        // We still have a match
                                        continue;
                                    }
                                    else
                                    {
                                        // No longer have a match
                                        Match = false;
                                        break;
                                    }
                                }

                                // If we still have a match after the loop, it's a banned IP
                                if (Match) return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionEvent("Unable to validate client IP against banned-ips.txt", ex);
            }

            // If we get here, it's an OK IP
            return false;
        }

        private void RaiseBindFailedEvent()
        {
            EventHandler Handler = BindFailedEvent;
            if (Handler != null) Handler(this, EventArgs.Empty);
        }

        private void RaiseBoundEvent()
        {
            EventHandler Handler = BoundEvent;
            if (Handler != null) Handler(this, EventArgs.Empty);
        }

        private int RaiseConnectEvent(ref ClientThread AClientThread)
        {
            EventHandler<ConnectEventArgs> Handler = ConnectEvent;
            if (Handler != null)
            {
                ConnectEventArgs e = new ConnectEventArgs(AClientThread);
                Handler(this, e);
                return e.Node;
            }

            return 0;
        }

        private void RaiseErrorMessageEvent(string AMessage)
        {
            EventHandler<StringEventArgs> Handler = ErrorMessageEvent;
            if (Handler != null) Handler(this, new StringEventArgs(AMessage));
        }

        private void RaiseExceptionEvent(string AMessage, Exception AException)
        {
            EventHandler<ExceptionEventArgs> Handler = ExceptionEvent;
            if (Handler != null) Handler(this, new ExceptionEventArgs(AMessage, AException));
        }

        private void RaiseWarningMessageEvent(string AMessage)
        {
            EventHandler<StringEventArgs> Handler = WarningMessageEvent;
            if (Handler != null) Handler(this, new StringEventArgs(AMessage));
        }
    }
}
