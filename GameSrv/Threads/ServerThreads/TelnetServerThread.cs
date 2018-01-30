using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    class TelnetServerThread : ServerThread {
        public TelnetServerThread() : base() {
            _ConnectionType = ConnectionType.Telnet;
            _LocalAddress = Config.Instance.TelnetServerIP;
            _LocalPort = Config.Instance.TelnetServerPort;
        }

        protected override void HandleNewConnection(TcpConnection newConnection) {
            if (newConnection == null) {
                throw new ArgumentNullException("newConnection");
            }

            TelnetConnection TypedConnection = new TelnetConnection();
            if (TypedConnection.Open(newConnection.GetSocket())) {
                ClientThread NewClientThread = new ClientThread(TypedConnection, _ConnectionType);
                NewClientThread.Start();
            } else {
                // TODOX Duplicated code.  Maybe add method to base class and call it?
                RMLog.Info("No carrier detected (probably a portscanner)");
                TypedConnection.Close();
            }
        }
    }
}
