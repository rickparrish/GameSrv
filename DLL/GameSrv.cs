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

namespace RandM.GameSrv
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Srv")]
    public class GameSrv : IDisposable
    {
        private int _BindCount = 0;
        private bool _BindFailed = false;
        private int _BoundCount = 0;
        private object _BoundEventLock = new object();
        private Config _Config = new Config();
        private bool _Disposed = false;
        private List<string> _Log = new List<string>();
        private object _LogLock = new object();
        private Timer _LogTimer = new Timer();
        private NodeManager _NodeManager = null;
        private Dictionary<int, ServerThread> _ServerThreads = new Dictionary<int, ServerThread>();
        private FlashSocketPolicyServerThread _FlashSocketPolicyServerThread = null;
        private ServerStatus _Status = ServerStatus.Stopped;

        public event EventHandler<StringEventArgs> AggregatedStatusMessageEvent = null;
        public event EventHandler<IntEventArgs> ConnectionCountChangeEvent = null;
        public event EventHandler<StringEventArgs> ErrorMessageEvent = null;
        public event EventHandler<ExceptionEventArgs> ExceptionEvent = null;
        public event EventHandler<NodeEventArgs> LogOffEvent = null;
        public event EventHandler<NodeEventArgs> LogOnEvent = null;
        public event EventHandler<StringEventArgs> MessageEvent = null;
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<StatusEventArgs> StatusEvent = null;
        public event EventHandler<StringEventArgs> StatusMessageEvent = null;
        public event EventHandler<StringEventArgs> WarningMessageEvent = null;

        public GameSrv()
        {
            _LogTimer.Interval = 60000; // 1 minute
            _LogTimer.Elapsed += LogTimer_Elapsed;
            _LogTimer.Start();
        }

        ~GameSrv()
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
                    if (_FlashSocketPolicyServerThread != null) _FlashSocketPolicyServerThread.Dispose();
                    if (_LogTimer != null) _LogTimer.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _Disposed = true;
            }
        }

        private void AddToLog(string logMessage)
        {
            lock (_LogLock)
            {
                _Log.Add(DateTime.Now.ToString(_Config.TimeFormatLog) + "  " + logMessage);
            }
        }

        private void CleanUpFiles()
        {
            if (OSUtils.IsWindows)
            {
                FileUtils.FileDelete("cpulimit.sh");
                FileUtils.FileDelete("dosutils.zip");
                FileUtils.FileDelete("install.sh");
                FileUtils.FileDelete("pty-sharp-1.0.tgz");
                FileUtils.FileDelete("start.sh");
                if (OSUtils.IsWin9x)
                {
                    FileUtils.FileDelete("dosbox.conf");
                    FileUtils.FileDelete("sbbsexec.dll");
                }
                else if (OSUtils.IsWinNT)
                {
                    FileUtils.FileDelete("sbbsexec.vxd");
                    if (ProcessUtils.Is64BitOperatingSystem)
                    {
                        FileUtils.FileDelete("sbbsexec.dll");
                        string ProgramFilesX86 = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        string DOSBoxExe = StringUtils.PathCombine(ProgramFilesX86, @"DOSBox-0.73\dosbox.exe");
                        if (!File.Exists(DOSBoxExe))
                        {
                            RaiseErrorMessageEvent("PLEASE INSTALL DOSBOX 0.73 TO \"" + Path.GetDirectoryName(DOSBoxExe).ToUpper() + "\" IF YOU PLAN ON RUNNING DOS DOORS USING DOSBOX");
                        }
                    }
                    else
                    {
                        FileUtils.FileDelete("dosbox.conf");
                        if (!File.Exists(StringUtils.PathCombine(Environment.SystemDirectory, "sbbsexec.dll")))
                        {
                            RaiseErrorMessageEvent("PLEASE COPY SBBSEXEC.DLL TO " + StringUtils.PathCombine(Environment.SystemDirectory, "sbbsexec.dll").ToUpper() + " IF YOU PLAN ON RUNNING DOS DOORS USING THE EMBEDDED SYNCHRONET FOSSIL");
                        }
                    }
                }
            }
            else if (OSUtils.IsUnix)
            {
                FileUtils.FileDelete("dosbox.conf");
                FileUtils.FileDelete("dosxtrn.exe");
                FileUtils.FileDelete("dosxtrn.pif");
                FileUtils.FileDelete("install.cmd");
                FileUtils.FileDelete("sbbsexec.dll");
                FileUtils.FileDelete("sbbsexec.vxd");
            }
        }

        public int ConnectionCount
        {
            get
            {
                if (_NodeManager == null) return 0;
                return _NodeManager.ConnectionCount;
            }
        }

        public void DisconnectNode(int node)
        {
            _NodeManager.DisconnectNode(node);
        }

        public int FirstNode
        {
            get { return _Config.FirstNode; }
        }

        private void FlashSocketPolicyServerThread_ErrorMessageEvent(object sender, StringEventArgs e)
        {
            RaiseErrorMessageEvent(sender, e.Text);
        }

        private void FlashSocketPolicyServerThread_MessageEvent(object sender, StringEventArgs e)
        {
            RaiseStatusMessageEvent(e.Text);
        }

        private void FlashSocketPolicyServerThread_WarningMessageEvent(object sender, StringEventArgs e)
        {
            RaiseWarningMessageEvent(sender, e.Text);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void FlushLog()
        {
            lock (_LogLock)
            {
                // Flush log to disk
                if (_Log.Count > 0)
                {
                    try
                    {
                        FileUtils.FileAppendAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "gamesrv.log"), string.Join(Environment.NewLine, _Log.ToArray()) + Environment.NewLine);
                        _Log.Clear();
                    }
                    catch (Exception ex)
                    {
                        RaiseExceptionEvent("Unable to update gamesrv.log", ex);
                    }
                }
            }
        }

        public int LastNode
        {
            get { return _Config.LastNode; }
        }

        private bool LoadGlobalSettings()
        {
            RaiseStatusMessageEvent("Loading Global Settings");
            if (_Config.FirstNode > _Config.LastNode)
            {
                RaiseErrorMessageEvent("FirstNode cannot be greater than LastNode!");
                return false;
            }

            return _Config.Loaded;
        }

        void LogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _LogTimer.Stop();
            FlushLog();
            _LogTimer.Start();
        }

        void NodeManager_ConnectionCountChangeEvent(object sender, IntEventArgs e)
        {
            RaiseConnectionCountChangeEvent(sender, e.Value);
        }

        void NodeManager_ErrorMessageEvent(object sender, StringEventArgs e)
        {
            RaiseErrorMessageEvent(sender, e.Text);
        }

        void NodeManager_ExceptionEvent(object sender, ExceptionEventArgs e)
        {
            RaiseExceptionEvent(sender, e);
        }

        void NodeManager_LogOffEvent(object sender, NodeEventArgs e)
        {
            RaiseLogOffEvent(sender, e);
        }

        void NodeManager_LogOnEvent(object sender, NodeEventArgs e)
        {
            RaiseLogOnEvent(sender, e);
        }

        void NodeManager_NodeEvent(object sender, NodeEventArgs e)
        {
            RaiseNodeEvent(sender, e);
        }

        void NodeManager_WarningMessageEvent(object sender, StringEventArgs e)
        {
            RaiseWarningMessageEvent(sender, e.Text);
        }

        public void Pause()
        {
            if (_Status == ServerStatus.Paused)
            {
                RaiseStatusEvent(ServerStatus.Resuming);
                foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads)
                {
                    KV.Value.Pause();
                }
                RaiseStatusEvent(ServerStatus.Resumed);

                // We really want to be in the Started state, so override the above (but dont raise an event)
                _Status = ServerStatus.Started;
            }
            else if (_Status == ServerStatus.Started)
            {
                RaiseStatusEvent(ServerStatus.Pausing);
                foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads)
                {
                    KV.Value.Pause();
                }
                RaiseStatusEvent(ServerStatus.Paused);
            }
        }

        private void RaiseAggregatedStatusMessageEvent(object sender, string message)
        {
            EventHandler<StringEventArgs> Handler = AggregatedStatusMessageEvent;
            if (Handler != null) Handler(sender, new StringEventArgs(message));
            AddToLog(message);
        }

        private void RaiseConnectionCountChangeEvent(object sender, int value)
        {
            EventHandler<IntEventArgs> Handler = ConnectionCountChangeEvent;
            if (Handler != null) Handler(sender, new IntEventArgs(value));
        }

        private void RaiseErrorMessageEvent(string message)
        {
            RaiseErrorMessageEvent(this, message);
        }

        private void RaiseErrorMessageEvent(object sender, string message)
        {
            EventHandler<StringEventArgs> Handler = ErrorMessageEvent;
            if (Handler != null) Handler(sender, new StringEventArgs(message));
            RaiseAggregatedStatusMessageEvent(sender, "ERROR: " + message);
        }

        private void RaiseExceptionEvent(string message, Exception ex)
        {
            RaiseExceptionEvent(this, new ExceptionEventArgs(message, ex));
        }

        private void RaiseExceptionEvent(object sender, ExceptionEventArgs e)
        {
            EventHandler<ExceptionEventArgs> Handler = ExceptionEvent;
            if (Handler != null) Handler(sender, e);
            if (Globals.Debug)
            {
                RaiseAggregatedStatusMessageEvent(sender, "EXCEPTION: " + e.Message + " (" + e.Exception.ToString() + ")");
            }
            else
            {
                RaiseAggregatedStatusMessageEvent(sender, "EXCEPTION: " + e.Message + " (" + e.Exception.Message + ")");
            }
        }

        private void RaiseLogOffEvent(object sender, NodeEventArgs logOffEvent)
        {
            EventHandler<NodeEventArgs> Handler = LogOffEvent;
            if (Handler != null) Handler(sender, logOffEvent);
            if (logOffEvent.NodeInfo.UserLoggedOn)
            {
                RaiseAggregatedStatusMessageEvent(sender, "Node " + logOffEvent.NodeInfo.Node.ToString() + " (LOGOFF): " + logOffEvent.NodeInfo.User.Alias + ": " + logOffEvent.Status);
            }
            else
            {
                RaiseAggregatedStatusMessageEvent(sender, "Node " + logOffEvent.NodeInfo.Node.ToString() + " (LOGOFF): " + logOffEvent.Status);
            }
        }

        private void RaiseLogOnEvent(object sender, NodeEventArgs logOnEvent)
        {
            EventHandler<NodeEventArgs> Handler = LogOnEvent;
            if (Handler != null) Handler(sender, logOnEvent);
            RaiseAggregatedStatusMessageEvent(sender, "Node " + logOnEvent.NodeInfo.Node.ToString() + " (LOGON): " + logOnEvent.NodeInfo.User.Alias + " " + logOnEvent.Status);
        }

        private void RaiseMessageEvent(object sender, string message)
        {
            EventHandler<StringEventArgs> Handler = MessageEvent;
            if (Handler != null) Handler(sender, new StringEventArgs(message));
            RaiseAggregatedStatusMessageEvent(sender, message);
        }

        private void RaiseNodeEvent(object sender, NodeEventArgs nodeEvent)
        {
            EventHandler<NodeEventArgs> Handler = NodeEvent;
            if (Handler != null) Handler(sender, nodeEvent);
            if (nodeEvent.NodeInfo.UserLoggedOn)
            {
                RaiseAggregatedStatusMessageEvent(sender, "Node " + nodeEvent.NodeInfo.Node.ToString() + ": " + nodeEvent.NodeInfo.User.Alias + ": " + nodeEvent.Status);
            }
            else
            {
                RaiseAggregatedStatusMessageEvent(sender, "Node " + nodeEvent.NodeInfo.Node.ToString() + ": " + nodeEvent.Status);
            }
        }

        private void RaiseStatusEvent(ServerStatus status)
        {
            // Record the new status
            _Status = status;

            EventHandler<StatusEventArgs> Handler = StatusEvent;
            if (Handler != null) Handler(this, new StatusEventArgs(status));

            switch (status)
            {
                case ServerStatus.Pausing:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are pausing...");
                    break;
                case ServerStatus.Resuming:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are resuming...");
                    break;
                case ServerStatus.Starting:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are going online...");
                    break;
                case ServerStatus.Stopping:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are going offline...");
                    break;
                case ServerStatus.Paused:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are paused");
                    break;
                case ServerStatus.Resumed:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) have resumed");
                    break;
                case ServerStatus.Started:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are online");
                    break;
                case ServerStatus.Stopped:
                    RaiseAggregatedStatusMessageEvent(null, "Server(s) are offline");
                    FlushLog();
                    break;
            }
        }

        private void RaiseStatusMessageEvent(string message)
        {
            EventHandler<StringEventArgs> Handler = StatusMessageEvent;
            if (Handler != null) Handler(this, new StringEventArgs(message));
            RaiseAggregatedStatusMessageEvent(null, message);
        }

        private void RaiseWarningMessageEvent(object sender, string message)
        {
            EventHandler<StringEventArgs> Handler = WarningMessageEvent;
            if (Handler != null) Handler(sender, new StringEventArgs(message));
            RaiseAggregatedStatusMessageEvent(sender, "WARNING: " + message);
        }

        private void ServerThread_BindFailedEvent(object sender, EventArgs e)
        {
            // Bind failed on one or more server threads, so abort the server
            if (_Status == ServerStatus.Started)
            {
                Stop();
            }
            else
            {
                _BindFailed = true;
            }
        }

        private void ServerThread_BoundEvent(object sender, EventArgs e)
        {
            // Check if all server threads are now bound
            lock (_BoundEventLock)
            {
                if (++_BoundCount == _BindCount)
                {
                    try
                    {
                        Globals.DropRoot(_Config.UnixUser);
                    }
                    catch (ArgumentOutOfRangeException aoorex)
                    {
                        RaiseExceptionEvent("Unable to drop from root to '" + _Config.UnixUser + "'", aoorex);

                        // Abort the server
                        if (_Status == ServerStatus.Started)
                        {
                            Stop();
                        }
                        else
                        {
                            _BindFailed = true;
                        }
                    }
                }
            }
        }

        private void ServerThread_ConnectEvent(object sender, ConnectEventArgs e)
        {
            e.Node = _NodeManager.GetFreeNode(e.ClientThread);
        }

        private void ServerThread_ErrorMessageEvent(object sender, StringEventArgs e)
        {
            RaiseErrorMessageEvent(sender, e.Text);
        }

        void ServerThread_ExceptionEvent(object sender, ExceptionEventArgs e)
        {
            RaiseExceptionEvent(sender, e);
        }

        private void ServerThread_MessageEvent(object sender, StringEventArgs e)
        {
            RaiseMessageEvent(sender, e.Text);
        }

        private void ServerThread_WarningMessageEvent(object sender, StringEventArgs e)
        {
            RaiseWarningMessageEvent(sender, e.Text);
        }

        public bool Start()
        {
            if (_Status == ServerStatus.Paused)
            {
                // If we're paused, call Pause() again to un-pause
                Pause();
                return true;
            }
            else if (_Status == ServerStatus.Stopped)
            {
                RaiseStatusEvent(ServerStatus.Starting);

                // Clean up the files not needed by this platform
                CleanUpFiles();

                // Load the Global settings
                if (!LoadGlobalSettings())
                {
                    RaiseStatusMessageEvent("Unable To Load Global Settings...Will Use Defaults");
                    _Config.Save();
                }

                // Start the node manager
                if (!StartNodeManager())
                {
                    RaiseStatusMessageEvent("Unable To Start Node Manager");
                    // Undo previous actions
                    goto ERROR;
                }

                // Reset bind variables
                _BindCount = GetBindCount();
                _BindFailed = false;
                _BoundCount = 0;

                // Start the server threads
                if (!StartServerThreads())
                {
                    RaiseStatusMessageEvent("Unable To Start Server Threads");
                    // Undo previous actions
                    StopServerThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // Start the flash socket policy server thread
                if (!StartFlashSocketPolicyServerThread())
                {
                    RaiseErrorMessageEvent("Unable To Start Flash Socket Policy Server Thread");
                    // Undo previous actions
                    StopServerThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // Check if we had a bind failure before finishing
                if (_BindFailed)
                {
                    RaiseErrorMessageEvent("One Or More Servers Failed To Bind To Their Assigned Ports");
                    // Undo previous actions
                    StopFlashSocketPolicyServerThread();
                    StopServerThreads();
                    StopNodeManager();
                    goto ERROR;
                }

                // If we get here, we're online
                RaiseStatusEvent(ServerStatus.Started);
                return true;

            ERROR:

                // If we get here, we failed to go online
                RaiseStatusEvent(ServerStatus.Stopped);
                return false;
            }

            return false;
        }

        private int GetBindCount()
        {
            int Result = 0;
            if (_Config.FlashSocketPolicyServerPort > 0) Result += 1;
            if (_Config.RLoginServerPort > 0) Result += 1;
            if (_Config.TelnetServerPort > 0) Result += 1;
            if (_Config.WebSocketServerPort > 0) Result += 1;
            return Result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool StartFlashSocketPolicyServerThread()
        {
            if (_Config.FlashSocketPolicyServerPort > 0)
            {
                RaiseStatusMessageEvent("Starting Flash Socket Policy Server Thread");

                try
                {
                    // Create Flash Socket Policy Server Thread and Thread objects
                    _FlashSocketPolicyServerThread = new FlashSocketPolicyServerThread(_Config.FlashSocketPolicyServerIP, _Config.FlashSocketPolicyServerPort, _Config.ServerPorts);
                    _FlashSocketPolicyServerThread.BindFailedEvent += new EventHandler(ServerThread_BindFailedEvent);
                    _FlashSocketPolicyServerThread.BoundEvent += new EventHandler(ServerThread_BoundEvent);
                    _FlashSocketPolicyServerThread.ErrorMessageEvent += new EventHandler<StringEventArgs>(FlashSocketPolicyServerThread_ErrorMessageEvent);
                    _FlashSocketPolicyServerThread.MessageEvent += new EventHandler<StringEventArgs>(FlashSocketPolicyServerThread_MessageEvent);
                    _FlashSocketPolicyServerThread.WarningMessageEvent += new EventHandler<StringEventArgs>(FlashSocketPolicyServerThread_WarningMessageEvent);
                    _FlashSocketPolicyServerThread.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Error in GameSrv::StartFlashSocketPolicyServerThread()", ex);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool StartNodeManager()
        {
            RaiseStatusMessageEvent("Starting Node Manager");

            try
            {
                _NodeManager = new NodeManager(_Config.FirstNode, _Config.LastNode);
                _NodeManager.ConnectionCountChangeEvent += new EventHandler<IntEventArgs>(NodeManager_ConnectionCountChangeEvent);
                _NodeManager.ErrorMessageEvent += new EventHandler<StringEventArgs>(NodeManager_ErrorMessageEvent);
                _NodeManager.ExceptionEvent += new EventHandler<ExceptionEventArgs>(NodeManager_ExceptionEvent);
                _NodeManager.LogOffEvent += new EventHandler<NodeEventArgs>(NodeManager_LogOffEvent);
                _NodeManager.LogOnEvent += new EventHandler<NodeEventArgs>(NodeManager_LogOnEvent);
                _NodeManager.NodeEvent += new EventHandler<NodeEventArgs>(NodeManager_NodeEvent);
                _NodeManager.WarningMessageEvent += new EventHandler<StringEventArgs>(NodeManager_WarningMessageEvent);
                _NodeManager.Start();
                return true;
            }
            catch (Exception ex)
            {
                RaiseExceptionEvent("Error in GameSrv::StartNodeManager()", ex);
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool StartServerThreads()
        {
            if ((_Config.RLoginServerPort > 0) || (_Config.TelnetServerPort > 0) || (_Config.WebSocketServerPort > 0))
            {
                RaiseStatusMessageEvent("Starting Server Threads");

                try
                {
                    _ServerThreads.Clear();

                    if (_Config.RLoginServerPort > 0)
                    {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(_Config.RLoginServerPort, new ServerThread(_Config.RLoginServerIP, _Config.RLoginServerPort, ConnectionType.RLogin));
                        _ServerThreads[_Config.RLoginServerPort].BindFailedEvent += new EventHandler(ServerThread_BindFailedEvent);
                        _ServerThreads[_Config.RLoginServerPort].BoundEvent += new EventHandler(ServerThread_BoundEvent);
                        _ServerThreads[_Config.RLoginServerPort].ConnectEvent += new EventHandler<ConnectEventArgs>(ServerThread_ConnectEvent);
                        _ServerThreads[_Config.RLoginServerPort].ErrorMessageEvent += new EventHandler<StringEventArgs>(ServerThread_ErrorMessageEvent);
                        _ServerThreads[_Config.RLoginServerPort].ExceptionEvent += new EventHandler<ExceptionEventArgs>(ServerThread_ExceptionEvent);
                        _ServerThreads[_Config.RLoginServerPort].MessageEvent += new EventHandler<StringEventArgs>(ServerThread_MessageEvent);
                        _ServerThreads[_Config.RLoginServerPort].WarningMessageEvent += new EventHandler<StringEventArgs>(ServerThread_WarningMessageEvent);
                    }

                    if (_Config.TelnetServerPort > 0)
                    {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(_Config.TelnetServerPort, new ServerThread(_Config.TelnetServerIP, _Config.TelnetServerPort, ConnectionType.Telnet));
                        _ServerThreads[_Config.TelnetServerPort].BindFailedEvent += new EventHandler(ServerThread_BindFailedEvent);
                        _ServerThreads[_Config.TelnetServerPort].BoundEvent += new EventHandler(ServerThread_BoundEvent);
                        _ServerThreads[_Config.TelnetServerPort].ConnectEvent += new EventHandler<ConnectEventArgs>(ServerThread_ConnectEvent);
                        _ServerThreads[_Config.TelnetServerPort].ErrorMessageEvent += new EventHandler<StringEventArgs>(ServerThread_ErrorMessageEvent);
                        _ServerThreads[_Config.TelnetServerPort].ExceptionEvent += new EventHandler<ExceptionEventArgs>(ServerThread_ExceptionEvent);
                        _ServerThreads[_Config.TelnetServerPort].MessageEvent += new EventHandler<StringEventArgs>(ServerThread_MessageEvent);
                        _ServerThreads[_Config.TelnetServerPort].WarningMessageEvent += new EventHandler<StringEventArgs>(ServerThread_WarningMessageEvent);
                    }

                    if (_Config.WebSocketServerPort > 0)
                    {
                        // Create Server Thread and add to collection
                        _ServerThreads.Add(_Config.WebSocketServerPort, new ServerThread(_Config.WebSocketServerIP, _Config.WebSocketServerPort, ConnectionType.WebSocket));
                        _ServerThreads[_Config.WebSocketServerPort].BindFailedEvent += new EventHandler(ServerThread_BindFailedEvent);
                        _ServerThreads[_Config.WebSocketServerPort].BoundEvent += new EventHandler(ServerThread_BoundEvent);
                        _ServerThreads[_Config.WebSocketServerPort].ConnectEvent += new EventHandler<ConnectEventArgs>(ServerThread_ConnectEvent);
                        _ServerThreads[_Config.WebSocketServerPort].ErrorMessageEvent += new EventHandler<StringEventArgs>(ServerThread_ErrorMessageEvent);
                        _ServerThreads[_Config.WebSocketServerPort].ExceptionEvent += new EventHandler<ExceptionEventArgs>(ServerThread_ExceptionEvent);
                        _ServerThreads[_Config.WebSocketServerPort].MessageEvent += new EventHandler<StringEventArgs>(ServerThread_MessageEvent);
                        _ServerThreads[_Config.WebSocketServerPort].WarningMessageEvent += new EventHandler<StringEventArgs>(ServerThread_WarningMessageEvent);
                    }

                    // Now actually start the server threads
                    foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads)
                    {
                        KV.Value.Start();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Error in GameSrv::StartServerThreads()", ex);
                    return false;
                }
            }
            else
            {
                RaiseErrorMessageEvent("No server ports found");
                return false;
            }
        }

        public ServerStatus Status
        {
            get { return _Status; }
        }

        public void Stop()
        {
            if ((_Status == ServerStatus.Paused) || (_Status == ServerStatus.Started))
            {
                RaiseStatusEvent(ServerStatus.Stopping);

                StopFlashSocketPolicyServerThread();
                StopServerThreads();
                StopNodeManager();

                RaiseStatusEvent(ServerStatus.Stopped);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool StopNodeManager()
        {
            if (_NodeManager != null)
            {
                RaiseStatusMessageEvent("Stopping Node Manager");

                try
                {
                    _NodeManager.Stop();
                    _NodeManager = null;

                    return true;
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Error in GameSrv::StopNodeManger()", ex);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool StopServerThreads()
        {
            RaiseStatusMessageEvent("Stopping Server Threads");

            try
            {
                foreach (KeyValuePair<int, ServerThread> KV in _ServerThreads)
                {
                    KV.Value.Stop();
                }
                _ServerThreads.Clear();
                return true;
            }
            catch (Exception ex)
            {
                RaiseExceptionEvent("Error in GameSrv::StopServerThread()", ex);
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool StopFlashSocketPolicyServerThread()
        {
            if (_FlashSocketPolicyServerThread != null)
            {
                RaiseStatusMessageEvent("Stopping Flash Socket Policy Server Thread");

                try
                {
                    _FlashSocketPolicyServerThread.Stop();
                    _FlashSocketPolicyServerThread.Dispose();
                    _FlashSocketPolicyServerThread = null;

                    return true;
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Error in GameSrv::StopFlashSocketPolicyServerThread()", ex);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public string TimeFormatLog
        {
            get { return _Config.TimeFormatLog; }
        }

        public string TimeFormatUI
        {
            get { return _Config.TimeFormatUI; }
        }

        static public string Version
        {
            get { return ProcessUtils.ProductVersionOfCallingAssembly; }
        }
    }
}
