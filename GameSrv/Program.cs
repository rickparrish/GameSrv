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
                // Console mode, check for arguments
                if (args.Length > 0) {
                    try {
                        // Check entire parameter string for service install or uninstall request
                        string ParameterString = string.Concat(args).TrimStart('/').TrimStart('-');
                        switch (ParameterString) {
                            case "i":
                            case "install":
                                Console.WriteLine("Installing service...");
                                ManagedInstallerClass.InstallHelper(new string[] { ProcessUtils.ExecutablePath });
                                Console.WriteLine("Service installed successfully!");
                                return;
                            case "u":
                            case "uninstall":
                                Console.WriteLine("Uninstalling service...");
                                ManagedInstallerClass.InstallHelper(new string[] { "/u", ProcessUtils.ExecutablePath });
                                Console.WriteLine("Service uninstalled successfully!");
                                return;
                        }
                    } catch (Exception ex) {
                        Console.WriteLine("Error handling service request: " + ex.Message);
                        return;
                    }
                }

                // If we get here, we're running as console app
                // TODOX Add ability to run as GUI app
                if (args.Contains("console", StringComparer.OrdinalIgnoreCase)) {
                    ConsoleApp.Start(args);
                } else if (args.Contains("gui", StringComparer.OrdinalIgnoreCase)) {
                    GuiApp.Start();
                } else {
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
                    Console.WriteLine("      Install:     GameSrvService.exe /i");
                    Console.WriteLine("      Uninstall:   GameSrvService.exe /u");
                    Console.WriteLine();
                    Console.WriteLine("      Start:       NET START GameSrvService");
                    Console.WriteLine("      Stop:        NET STOP GameSrvService");
                    Console.WriteLine();
                    Console.WriteLine("      Pause:       NET PAUSE GameSrvService");
                    Console.WriteLine("      Resume:      NET CONTINUE GameSrvService");
                    Console.WriteLine();
                    Console.WriteLine("Hit a key to quit");
                    Console.ReadKey();
                }
            } else {
                // Service mode
                ServiceApp.Start();
            }
        }
    }
}
