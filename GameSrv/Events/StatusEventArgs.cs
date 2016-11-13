using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    public class StatusEventArgs : EventArgs {
        public GameSrvStatus Status { get; private set; }

        public StatusEventArgs(GameSrvStatus status) {
            Status = status;
        }
    }
}
