using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class RLoginServerThread : ServerThread {
        public RLoginServerThread(Config config) : base(config) {
            _ConnectionType = ConnectionType.RLogin;
            _LocalAddress = config.RLoginServerIP;
            _LocalPort = config.RLoginServerPort;
        }

        protected override void HandleNewConnection(TcpConnection newConnection) {
            RLoginConnection TypedConnection = new RLoginConnection();
            if (TypedConnection.Open(newConnection.GetSocket())) {
                ClientThread NewClientThread = new ClientThread(TypedConnection, _ConnectionType, _Config.TerminalType);
                NewClientThread.Start();
            } else {
                RMLog.Info("Timeout waiting for RLogin header");
                TypedConnection.Close();
            }
        }
    }
}
