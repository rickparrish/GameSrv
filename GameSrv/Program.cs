using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RandM.GameSrv {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args) {
            // Check for service mode or console mode
            if (Environment.UserInteractive || OSUtils.IsUnix) {
                // Interactive mode (in other words, not service mode)
                if (args.Contains("service", StringComparer.OrdinalIgnoreCase) && args.Contains("install", StringComparer.OrdinalIgnoreCase)) {
                    ServiceApp.Install();
                } else if (args.Contains("service", StringComparer.OrdinalIgnoreCase) && args.Contains("uninstall", StringComparer.OrdinalIgnoreCase)) {
                    ServiceApp.Uninstall();
                } else if (args.Contains("console", StringComparer.OrdinalIgnoreCase)) {
                    ConsoleApp.Start(args);
                } else if (args.Contains("gui", StringComparer.OrdinalIgnoreCase)) {
                    GuiApp.Start();
                } else {
                    DisplayUsage();
                }
            } else {
                // Non-interactive mode (in other words, service mode)
                ServiceApp.Start();
            }
        }

        private static void DisplayUsage() {
            Console.WriteLine();
            Console.WriteLine("GameSrv.exe Usage:");
            Console.WriteLine();
            Console.WriteLine("  CONSOLE MODE");
            Console.WriteLine("  ============");
            Console.WriteLine("      Fancy:       GameSrv.exe console");
            Console.WriteLine("      Simple:      GameSrv.exe console simple");
            Console.WriteLine();
            Console.WriteLine("  GUI MODE");
            Console.WriteLine("  ========");
            Console.WriteLine("      Normal:      GameSrv.exe gui");
            Console.WriteLine();
            Console.WriteLine("  SERVICE MODE");
            Console.WriteLine("  ============");
            Console.WriteLine("      Install:     GameSrv.exe service install");
            Console.WriteLine("      Uninstall:   GameSrv.exe service uninstall");
            Console.WriteLine();
            Console.WriteLine("      Start:       NET START GameSrv");
            Console.WriteLine("      Stop:        NET STOP GameSrv");
            Console.WriteLine();
            Console.WriteLine("      Pause:       NET PAUSE GameSrv");
            Console.WriteLine("      Resume:      NET CONTINUE GameSrv");
            Console.WriteLine();
            Console.WriteLine("Hit a key to quit");
            Console.ReadKey();
        }
    }
}
