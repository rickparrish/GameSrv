/*
  GameSrv: A BBS Door Game Server
  Copyright (C) 2002-2013  Rick Parrish, R&M Software

  This file is part of GameSrv.

  GameSrv is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  any later version.

  GameSrv is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with GameSrv.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using System.Text;

namespace RandM.GameSrv
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                try
                {
                    string parameter = string.Concat(args);
                    switch (parameter)
                    {
                        case "/i":
                        case "/install":
                        case "--install":
                            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                            Console.WriteLine("Service installed successfully");
                            break;
                        case "/u":
                        case "/uninstall":
                        case "--uninstall":
                            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                            Console.WriteLine("Service uninstalled successfully");
                            break;
                        default:
                            Console.WriteLine();
                            Console.WriteLine("Usage:");
                            Console.WriteLine();
                            Console.WriteLine(" Install:   GameSrvService.exe /i");
                            Console.WriteLine(" Uninstall: GameSrvService.exe /u");
                            Console.WriteLine();
                            Console.WriteLine(" Start:     NET START GameSrvService");
                            Console.WriteLine(" Stop:      NET STOP GameSrvService");
                            Console.WriteLine();
                            Console.WriteLine(" Pause:     NET PAUSE GameSrvService");
                            Console.WriteLine(" Resume:    NET CONTINUE GameSrvService");
                            Console.WriteLine();
                            Console.WriteLine("Hit a key to quit");
                            Console.ReadKey();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new svcMain() 
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
