using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
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
}
