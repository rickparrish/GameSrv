using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RandM.GameSrv {
    class ServiceApp {
        public static void Start() {
            ServiceBase.Run(new MainService());
        }
    }
}
