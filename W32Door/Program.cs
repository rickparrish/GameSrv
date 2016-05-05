using RandM.GameSrv;
using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace W32Door
{
    class Program
    {
        static string _LogPath = StringUtils.PathCombine(ProcessUtils.StartupPath, "logs", "w32door.log");

        static void Main(string[] args)
        {
            try
            {
                // Ensure we have enough command-line parameters
                if (args.Length < 3)
                {
                    Console.WriteLine("ERROR: Not enough command-line parameters supplied");
                    Console.WriteLine();
                    Console.WriteLine("USAGE: W32DOOR <path_to_door.sys> <command_to_run> <parameters_for_command>");
                    Console.WriteLine();
                    Console.WriteLine(@"EXAMPLE: W32DOOR C:\BBS\NODE1\DOOR.SYS C:\DOOR\START.BAT -DC:\BBS\NODE1\DOOR32.SYS NORIP");
                    Console.WriteLine();
                    Thread.Sleep(2500);
                    return;
                }

                // Store the command-line parameters
                string DoorSysPath = args[0];
                string DoorCommand = args[1];
                string DoorParameters = string.Join(" ", args, 2, args.Length - 2);

                // Create the DOOR32.SYS
                int Node = GetNodeFromDoorSys(DoorSysPath);

                // Create the W32DOOR.RUN
                string W32DoorRunPath = CreateW32DoorRun(Node, DoorSysPath, DoorCommand, DoorParameters);

                // Wait for W32DOOR.RUN to be deleted
                while (File.Exists(W32DoorRunPath))
                {
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.Message}.  See logs\\w32door.log for more information");
                Log($"EXCEPTION: {ex.ToString()}");
            }
        }

        static string CreateW32DoorRun(int node, string doorSysPath, string command, string parameters)
        {
            string W32DoorRunPath = StringUtils.PathCombine(ProcessUtils.StartupPath, $"node{node}", "w32door.run");
            Log($"Creating: {W32DoorRunPath}{Environment.NewLine} - Command: {command}{Environment.NewLine} - Parameters: {parameters}");

            FileUtils.FileWriteAllLines(W32DoorRunPath, new string[] {
                doorSysPath,
                command,
                parameters
            });

            return W32DoorRunPath;
        }

        static int GetNodeFromDoorSys(string doorSysPath)
        {
            string[] DoorSysLines = FileUtils.FileReadAllLines(doorSysPath);
            return Convert.ToInt32(DoorSysLines[3]); // Node number
        }

        static void Log(string message)
        {
            FileUtils.FileAppendAllText(_LogPath, $"{message}{Environment.NewLine}");
        }
    }
}
