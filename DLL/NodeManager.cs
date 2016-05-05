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
using System.Collections;
using RandM.RMLib;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RandM.GameSrv
{
    class NodeManager
    {
        private Dictionary<int, ClientThread> _ClientThreads = new Dictionary<int, ClientThread>();
        private object _ListLock = new Object();
        private int _NodeFirst = 0;
        private int _NodeLast = 0;

        public event EventHandler<IntEventArgs> ConnectionCountChangeEvent = null;
        public event EventHandler<StringEventArgs> ErrorMessageEvent = null;
        public event EventHandler<ExceptionEventArgs> ExceptionEvent = null;
        public event EventHandler<NodeEventArgs> LogOffEvent = null;
        public event EventHandler<NodeEventArgs> LogOnEvent = null;
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<StringEventArgs> WarningMessageEvent = null;

        public NodeManager(int nodeFirst, int nodeLast)
        {
            _NodeFirst = nodeFirst;
            _NodeLast = nodeLast;
        }

        void ClientThread_ErrorMessageEvent(object sender, StringEventArgs e)
        {
            RaiseErrorMessageEvent(sender, e.Text);
        }

        void ClientThread_ExceptionEvent(object sender, ExceptionEventArgs e)
        {
            RaiseExceptionEvent(sender, e);
        }

        void ClientThread_LogOffEvent(object sender, NodeEventArgs e)
        {
            if (!e.Stopped) SetFreeNode(e.NodeInfo.Node);
            RaiseLogOffEvent(sender, e);
        }

        void ClientThread_LogOnEvent(object sender, NodeEventArgs e)
        {
            // Kill other session if the user is a logged in user (ie not a RUNBBS.INI connection) and the user isn't allowed on multiple nodes
            if ((e.NodeInfo.User.UserId > 0) && (!e.NodeInfo.User.AllowMultipleConnections))
            {
                KillOtherSession(e.NodeInfo.User.Alias, e.NodeInfo.Node);
            }
            RaiseLogOnEvent(sender, e);
        }

        void ClientThread_NodeEvent(object sender, NodeEventArgs e)
        {
            RaiseNodeEvent(sender, e);
            UpdateWhoIsOnlineFile();
        }

        void ClientThread_WarningMessageEvent(object sender, StringEventArgs e)
        {
            RaiseWarningMessageEvent(sender, e.Text);
        }

        void ClientThread_WhosOnlineEvent(object sender, WhoIsOnlineEventArgs e)
        {
            lock (_ListLock)
            {
                for (int NodeLoop = _NodeFirst; NodeLoop <= _NodeLast; NodeLoop++)
                {
                    // Make sure this node has a client
                    if (_ClientThreads[NodeLoop] == null)
                    {
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_ALIAS", "");
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_IPADDRESS", "");
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_STATUS", "Waiting for caller");
                    }
                    else
                    {
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_ALIAS", _ClientThreads[NodeLoop].Alias);
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_IPADDRESS", _ClientThreads[NodeLoop].IPAddress);
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_STATUS", _ClientThreads[NodeLoop].Status);
                    }
                }
            }
        }

        public int ConnectionCount
        {
            get
            {
                int Result = 0;

                lock (_ListLock)
                {
                    // Check for a free node
                    for (int i = _NodeFirst; i <= _NodeLast; i++)
                    {
                        if (_ClientThreads[i] != null) Result += 1;
                    }
                }

                return Result;
            }
        }

        public void DisconnectNode(int node)
        {
            bool Raise = false;

            if (IsValidNode(node))
            {
                lock (_ListLock)
                {
                    if (_ClientThreads[node] != null)
                    {
                        _ClientThreads[node].Stop();
                        _ClientThreads[node] = null;
                        Raise = true;
                    }
                }
            }

            if (Raise)
            {
                RaiseConnectionCountChangeEvent();
                UpdateWhoIsOnlineFile();
            }
        }

        private void DisplayAnsi(string ansi, int node)
        {
            if (IsValidNode(node))
            {
                lock (_ListLock)
                {
                    if (_ClientThreads[node] != null)
                    {
                        _ClientThreads[node].DisplayAnsi(ansi);
                    }
                }
            }
        }

        public int GetFreeNode(ClientThread clientThread)
        {
            int Result = 0;
            bool Raise = false;

            lock (_ListLock)
            {
                // Check for a free node
                for (int i = _NodeFirst; i <= _NodeLast; i++)
                {
                    if (_ClientThreads[i] == null)
                    {
                        clientThread.ErrorMessageEvent += new EventHandler<StringEventArgs>(ClientThread_ErrorMessageEvent);
                        clientThread.ExceptionEvent += new EventHandler<ExceptionEventArgs>(ClientThread_ExceptionEvent);
                        clientThread.LogOffEvent += new EventHandler<NodeEventArgs>(ClientThread_LogOffEvent);
                        clientThread.LogOnEvent += new EventHandler<NodeEventArgs>(ClientThread_LogOnEvent);
                        clientThread.NodeEvent += new EventHandler<NodeEventArgs>(ClientThread_NodeEvent);
                        clientThread.WarningMessageEvent += new EventHandler<StringEventArgs>(ClientThread_WarningMessageEvent);
                        clientThread.WhoIsOnlineEvent += new EventHandler<WhoIsOnlineEventArgs>(ClientThread_WhosOnlineEvent);
                        _ClientThreads[i] = clientThread;

                        Result = i;
                        Raise = true;

                        break;
                    }
                }
            }

            if (Raise) RaiseConnectionCountChangeEvent();
            return Result;
        }

        private bool IsValidNode(int node)
        {
            return ((node >= _NodeFirst) && (node <= _NodeLast));
        }

        public void KillOtherSession(string alias, int node)
        {
            int NodeToKill = 0;

            lock (_ListLock)
            {
                for (int NodeLoop = _NodeFirst; NodeLoop <= _NodeLast; NodeLoop++)
                {
                    // Make sure we don't kill our own node!
                    if (NodeLoop != node)
                    {
                        // Make sure this node has a client
                        if (_ClientThreads[NodeLoop] != null)
                        {
                            // Make sure this node matches the alias
                            if (_ClientThreads[NodeLoop].Alias.ToUpper() == alias.ToUpper())
                            {
                                NodeToKill = NodeLoop;
                            }
                        }
                    }
                }
            }

            if (NodeToKill > 0)
            {
                // Show "you're on too many nodes" message before disconnecting
                DisplayAnsi("LOGON_TWO_NODES", NodeToKill);
                DisconnectNode(NodeToKill);
            }
        }

        private void RaiseConnectionCountChangeEvent()
        {
            EventHandler<IntEventArgs> Handler = ConnectionCountChangeEvent;
            if (Handler != null) Handler(this, new IntEventArgs(ConnectionCount));
        }

        private void RaiseErrorMessageEvent(object sender, string message)
        {
            EventHandler<StringEventArgs> Handler = ErrorMessageEvent;
            if (Handler != null) Handler(sender, new StringEventArgs(message));
        }

        private void RaiseExceptionEvent(object sender, ExceptionEventArgs e)
        {
            EventHandler<ExceptionEventArgs> Handler = ExceptionEvent;
            if (Handler != null) Handler(sender, e);
        }

        private void RaiseLogOffEvent(object sender, NodeEventArgs logOffEvent)
        {
            EventHandler<NodeEventArgs> Handler = LogOffEvent;
            if (Handler != null) Handler(sender, logOffEvent);
        }

        private void RaiseLogOnEvent(object sender, NodeEventArgs logOnEvent)
        {
            EventHandler<NodeEventArgs> Handler = LogOnEvent;
            if (Handler != null) Handler(sender, logOnEvent);
        }

        private void RaiseNodeEvent(object sender, NodeEventArgs nodeEvent)
        {
            EventHandler<NodeEventArgs> Handler = NodeEvent;
            if (Handler != null) Handler(sender, nodeEvent);
        }

        private void RaiseWarningMessageEvent(object sender, string message)
        {
            EventHandler<StringEventArgs> Handler = WarningMessageEvent;
            if (Handler != null) Handler(sender, new StringEventArgs(message));
        }

        public void SetFreeNode(int node)
        {
            if (IsValidNode(node))
            {
                lock (_ListLock)
                {
                    _ClientThreads[node] = null;
                }
            }

            RaiseConnectionCountChangeEvent();
            UpdateWhoIsOnlineFile();
        }

        public void Start()
        {
            lock (_ListLock)
            {
                _ClientThreads.Clear();
                for (int Node = _NodeFirst; Node <= _NodeLast; Node++)
                {
                    _ClientThreads[Node] = null;
                }
            }

            RaiseConnectionCountChangeEvent();
            UpdateWhoIsOnlineFile();
        }

        public void Stop()
        {
            lock (_ListLock)
            {
                // Shutdown any client threads that are still active
                for (int Node = _NodeFirst; Node <= _NodeLast; Node++)
                {
                    if (_ClientThreads[Node] != null)
                    {
                        _ClientThreads[Node].Stop();
                        _ClientThreads[Node] = null;
                    }
                }
            }

            RaiseConnectionCountChangeEvent();
            UpdateWhoIsOnlineFile();
        }

        private void UpdateWhoIsOnlineFile()
        {
            try
            {
                var SB = new StringBuilder();
                SB.AppendLine("Node,RemoteIP,User,Status");
                lock (_ListLock)
                {
                    // Get status from each node
                    for (int Node = _NodeFirst; Node <= _NodeLast; Node++)
                    {
                        if (_ClientThreads[Node] == null)
                        {
                            SB.AppendLine($"{Node}\t\t\tWaiting for caller");
                        }
                        else
                        {
                            SB.AppendLine($"{Node}\t{_ClientThreads[Node].IPAddress}\t{_ClientThreads[Node].Alias}\t{_ClientThreads[Node].Status}");
                        }
                    }
                }

                string WhoIsOnlineFilename = StringUtils.PathCombine(ProcessUtils.StartupPath, "whoisonline.txt");
                FileUtils.FileWriteAllText(WhoIsOnlineFilename, SB.ToString());
            }
            catch (Exception ex)
            {
                RaiseExceptionEvent(this, new ExceptionEventArgs("Unable to update whoisonline.txt", ex));
            }
        }
    }
}
