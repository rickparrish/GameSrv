using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class WebSocketServerThread : ServerThread {
        public WebSocketServerThread(Config config) : base(config) {
            _ConnectionType = ConnectionType.WebSocket;
            _LocalAddress = config.WebSocketServerIP;
            _LocalPort = config.WebSocketServerPort;
        }
    }
}
