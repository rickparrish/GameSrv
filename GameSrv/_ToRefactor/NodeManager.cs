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

namespace RandM.GameSrv {
    class NodeManager {
        private Dictionary<int, ClientThread> _ClientThreads = new Dictionary<int, ClientThread>();
        private object _ListLock = new Object();
        private int _NodeFirst = 0;
        private int _NodeLast = 0;

        public event EventHandler<IntEventArgs> ConnectionCountChangeEvent = null;
        public event EventHandler<NodeEventArgs> NodeEvent = null;

        public NodeManager(int nodeFirst, int nodeLast) {
            _NodeFirst = nodeFirst;
            _NodeLast = nodeLast;
        }

        private void ClientThread_FinishEvent(object sender, EventArgs e) {
            var FinishedClientThread = sender as ClientThread;

            // Free up the node that the finished thread was using
            bool FoundClientThread = false;
            lock (_ListLock) {
                for (int NodeLoop = _NodeFirst; NodeLoop <= _NodeLast; NodeLoop++) {
                    if (_ClientThreads[NodeLoop] == FinishedClientThread) {
                        _ClientThreads[NodeLoop].Dispose();
                        _ClientThreads[NodeLoop] = null;
                        FoundClientThread = true;
                        break;
                    }
                }
            }

            if (FoundClientThread) {
                // Raise event and update data files
                UpdateConnectionCount();
                UpdateWhoIsOnlineFile();
            }
        }

        void ClientThread_NodeEvent(object sender, NodeEventArgs e) {
            if (e.EventType == NodeEventType.LogOn) {
                // Kill other session if the user is a logged in user (ie not a RUNBBS.INI connection) and the user isn't allowed on multiple nodes
                if ((e.NodeInfo.User.UserId > 0) && (!e.NodeInfo.User.AllowMultipleConnections)) {
                    KillOtherSession(e.NodeInfo.User.Alias, e.NodeInfo.Node);
                }

            }

            NodeEvent?.Invoke(sender, e);
            UpdateWhoIsOnlineFile();
        }

        void ClientThread_WhosOnlineEvent(object sender, WhoIsOnlineEventArgs e) {
            lock (_ListLock) {
                for (int NodeLoop = _NodeFirst; NodeLoop <= _NodeLast; NodeLoop++) {
                    // Make sure this node has a client
                    if (_ClientThreads[NodeLoop] == null) {
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_ALIAS", "");
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_IPADDRESS", "");
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_STATUS", "Waiting for caller");
                    } else {
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_ALIAS", _ClientThreads[NodeLoop].Alias);
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_IPADDRESS", _ClientThreads[NodeLoop].IPAddress);
                        e.WhoIsOnline.Add("WHOSONLINE_" + NodeLoop.ToString() + "_STATUS", _ClientThreads[NodeLoop].Status);
                    }
                }
            }
        }

        public int ConnectionCount {
            get {
                int Result = 0;

                lock (_ListLock) {
                    // Check for a free node
                    for (int i = _NodeFirst; i <= _NodeLast; i++) {
                        if (_ClientThreads[i] != null) Result += 1;
                    }
                }

                return Result;
            }
        }

        public void DisconnectNode(int node) {
            bool Raise = false;

            if (IsValidNode(node)) {
                lock (_ListLock) {
                    if (_ClientThreads[node] != null) {
                        _ClientThreads[node].Stop();
                        _ClientThreads[node] = null;
                        Raise = true;
                    }
                }
            }

            if (Raise) {
                UpdateConnectionCount();
                UpdateWhoIsOnlineFile();
            }
        }

        private void DisplayAnsi(string ansi, int node) {
            if (IsValidNode(node)) {
                lock (_ListLock) {
                    if (_ClientThreads[node] != null) {
                        _ClientThreads[node].DisplayAnsi(ansi);
                    }
                }
            }
        }

        public int GetFreeNode(ClientThread clientThread) {
            int Result = 0;
            bool Raise = false;

            lock (_ListLock) {
                // Check for a free node
                for (int i = _NodeFirst; i <= _NodeLast; i++) {
                    if (_ClientThreads[i] == null) {
                        clientThread.FinishEvent += ClientThread_FinishEvent; // TODOX This one should be responsible for freeing up the node
                        clientThread.NodeEvent += ClientThread_NodeEvent;
                        clientThread.WhoIsOnlineEvent += ClientThread_WhosOnlineEvent;
                        _ClientThreads[i] = clientThread;

                        Result = i;
                        Raise = true;

                        break;
                    }
                }
            }

            if (Raise) UpdateConnectionCount();
            return Result;
        }

        private bool IsValidNode(int node) {
            return ((node >= _NodeFirst) && (node <= _NodeLast));
        }

        public void KillOtherSession(string alias, int node) {
            int NodeToKill = 0;

            lock (_ListLock) {
                for (int NodeLoop = _NodeFirst; NodeLoop <= _NodeLast; NodeLoop++) {
                    // Make sure we don't kill our own node!
                    if (NodeLoop != node) {
                        // Make sure this node has a client
                        if (_ClientThreads[NodeLoop] != null) {
                            // Make sure this node matches the alias
                            if (_ClientThreads[NodeLoop].Alias.ToUpper() == alias.ToUpper()) {
                                NodeToKill = NodeLoop;
                            }
                        }
                    }
                }
            }

            if (NodeToKill > 0) {
                // Show "you're on too many nodes" message before disconnecting
                DisplayAnsi("LOGON_TWO_NODES", NodeToKill);
                DisconnectNode(NodeToKill);
            }
        }

        public void Start() {
            lock (_ListLock) {
                _ClientThreads.Clear();
                for (int Node = _NodeFirst; Node <= _NodeLast; Node++) {
                    _ClientThreads[Node] = null;
                }
            }

            UpdateConnectionCount();
            UpdateWhoIsOnlineFile();
        }

        public void Stop() {
            lock (_ListLock) {
                // Shutdown any client threads that are still active
                for (int Node = _NodeFirst; Node <= _NodeLast; Node++) {
                    if (_ClientThreads[Node] != null) {
                        _ClientThreads[Node].Stop();
                        _ClientThreads[Node] = null;
                    }
                }
            }

            UpdateConnectionCount();
            UpdateWhoIsOnlineFile();
        }

        private void UpdateConnectionCount() {
            ConnectionCountChangeEvent?.Invoke(this, new IntEventArgs(ConnectionCount));
        }

        private void UpdateWhoIsOnlineFile() {
            try {
                var SB = new StringBuilder();
                SB.AppendLine("Node,RemoteIP,User,Status");
                lock (_ListLock) {
                    // Get status from each node
                    for (int Node = _NodeFirst; Node <= _NodeLast; Node++) {
                        if (_ClientThreads[Node] == null) {
                            SB.AppendLine($"{Node}\t\t\tWaiting for caller");
                        } else {
                            SB.AppendLine($"{Node}\t{_ClientThreads[Node].IPAddress}\t{_ClientThreads[Node].Alias}\t{_ClientThreads[Node].Status}");
                        }
                    }
                }

                string WhoIsOnlineFilename = StringUtils.PathCombine(ProcessUtils.StartupPath, "whoisonline.txt");
                FileUtils.FileWriteAllText(WhoIsOnlineFilename, SB.ToString());
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to update whoisonline.txt");
            }
        }
    }
}
