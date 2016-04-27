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
                int Node = CreateDoor32Sys(DoorSysPath);

                // Create the W32DOOR.RUN
                string W32DoorRunPath = CreateW32DoorRun(Node, DoorCommand, DoorParameters);

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

        static int CreateDoor32Sys(string doorSysPath)
        {
            string[] DoorSysLines = FileUtils.FileReadAllLines(doorSysPath);
            List<string> Door32SysLines = new List<string>()
            {
                "2", // Telnet
                Environment.GetEnvironmentVariable("GS_SOCKET"), // Socket handle
                DoorSysLines[1], // Baud rate
                ProcessUtils.ProductName + " v" + GameSrv.Version, // BBSID
                (Convert.ToInt32(DoorSysLines[25]) + 1).ToString(), // User's record position (convert 0-based DOOR.SYS to 1-based DOOR32.SYS)
                DoorSysLines[9], // Real name
                DoorSysLines[35], // Alias
                DoorSysLines[14], // Access level
                DoorSysLines[18], // Time left (in minutes)
                "1", // Emulation (1=ANSI, a sane default I think)
                DoorSysLines[3] // Node number
            };

            string Door32SysPath = StringUtils.PathCombine(Path.GetDirectoryName(doorSysPath), "door32.sys");
            Log($"Creating: {Door32SysPath}");
            FileUtils.FileWriteAllLines(Door32SysPath, Door32SysLines.ToArray());

            return Convert.ToInt32(DoorSysLines[3]); // Node number
        }

        static string CreateW32DoorRun(int node, string command, string parameters)
        {
            string W32DoorRunPath = StringUtils.PathCombine(ProcessUtils.StartupPath, $"node{node}", "w32door.run");
            Log($"Creating: {W32DoorRunPath}{Environment.NewLine} - Command: {command}{Environment.NewLine} - Parameters: {parameters}");

            FileUtils.FileWriteAllLines(W32DoorRunPath, new string[] {
                command,
                parameters
            });

            return W32DoorRunPath;
        }

        static void Log(string message)
        {
            FileUtils.FileAppendAllText(_LogPath, $"{message}{Environment.NewLine}");
        }
    }
}
