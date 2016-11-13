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
    }
}
