using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    // TODOX Only used to get a new node number and register the client thread -- refactor it out
    public class ConnectEventArgs : EventArgs {
        public ClientThread ClientThread { get; private set; }
        public int Node { get; set; }

        public ConnectEventArgs(ClientThread clientThread) {
            ClientThread = clientThread;
            Node = -1;
        }
    }
}
