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
using System.Collections.Specialized;

namespace RandM.GameSrv {
    public class ConnectEventArgs : EventArgs {
        public ClientThread ClientThread { get; private set; }
        public int Node { get; set; }

        public ConnectEventArgs(ClientThread clientThread) {
            ClientThread = clientThread;
            Node = -1;
        }
    }

    public class NodeEventArgs : EventArgs {
        public NodeInfo NodeInfo { get; private set; }
        public string Status { get; private set; }
        public NodeEventType EventType { get; private set; }

        public NodeEventArgs(NodeInfo nodeInfo, string status, NodeEventType eventType) {
            NodeInfo = nodeInfo;
            Status = status;
            EventType = eventType;
        }
    }

    public class StatusEventArgs : EventArgs {
        public GameSrvStatus Status { get; private set; }

        public StatusEventArgs(GameSrvStatus status) {
            Status = status;
        }
    }

    public class WhoIsOnlineEventArgs : EventArgs {
        public StringDictionary WhoIsOnline { get; private set; }

        public WhoIsOnlineEventArgs() {
            WhoIsOnline = new StringDictionary();
        }
    }
}
