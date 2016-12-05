using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class TelnetServerThread : ServerThread {
        public TelnetServerThread(Config config) : base(config) {
            _ConnectionType = ConnectionType.Telnet;
            _LocalAddress = config.TelnetServerIP;
            _LocalPort = config.TelnetServerPort;
        }

        protected override void HandleNewConnection(TcpConnection newConnection) {
            TelnetConnection TypedConnection = new TelnetConnection();
            if (TypedConnection.Open(newConnection.GetSocket())) {
                ClientThread NewClientThread = new ClientThread(TypedConnection, _ConnectionType, _Config.TerminalType);
                NewClientThread.Start();
            } else {
                // TODOX Duplicated code.  Maybe add method to base class and call it?
                RMLog.Info("No carrier detected (probably a portscanner)");
                TypedConnection.Close();
            }
        }
    }
}
