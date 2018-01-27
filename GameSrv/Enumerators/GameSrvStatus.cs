using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    public enum GameSrvStatus {
        Offline,
        Paused,
        Pausing,
        Resuming,
        Started,
        Starting,
        Stopped,
        Stopping,
    }
}
