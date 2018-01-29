using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RandM.GameSrv {
    static class ServiceApp {
        public static void Start() {
            ServiceBase.Run(new MainService());
        }

        public static void Install() {
            try {
                Console.WriteLine();
                Console.WriteLine("*********************");
                Console.WriteLine("Installing service...");
                Console.WriteLine("*********************");
                Console.WriteLine();

                ManagedInstallerClass.InstallHelper(new string[] { ProcessUtils.ExecutablePath });

                Console.WriteLine();
                Console.WriteLine("*******************************");
                Console.WriteLine("Service installed successfully!");
                Console.WriteLine("*******************************");
                Console.WriteLine();
            } catch (Exception ex) {
                Console.WriteLine();
                Console.WriteLine("*************************");
                Console.WriteLine("Error installing service!");
                Console.WriteLine("*************************");
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
            }
        }

        public static void Uninstall() {
            try {
                Console.WriteLine();
                Console.WriteLine("***********************");
                Console.WriteLine("Uninstalling service...");
                Console.WriteLine("***********************");
                Console.WriteLine();

                ManagedInstallerClass.InstallHelper(new string[] { "/u", ProcessUtils.ExecutablePath });

                Console.WriteLine();
                Console.WriteLine("*********************************");
                Console.WriteLine("Service uninstalled successfully!");
                Console.WriteLine("*********************************");
                Console.WriteLine();
            } catch (Exception ex) {
                Console.WriteLine();
                Console.WriteLine("***************************");
                Console.WriteLine("Error uninstalling service!");
                Console.WriteLine("***************************");
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
            }
        }
    }
}
