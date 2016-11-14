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

        protected override void HandleNewConnection(TcpConnection newConnection) {
            WebSocketConnection TypedConnection = new WebSocketConnection();
            if (TypedConnection.Open(newConnection.GetSocket())) {
                // TODOX Start a proxy thread instead of a clientthread
                ClientThread NewClientThread = new ClientThread(TypedConnection, _ConnectionType, _Config.TerminalType);
                NewClientThread.Start();
            } else {
                RMLog.Info("No carrier detected (probably a portscanner)");
                TypedConnection.Close();
            }
        }
    }
}
