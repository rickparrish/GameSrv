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
    }
}
