using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unix;
using System.Threading;
using System.Collections;

namespace RandM.GameSrv {
    public class RunDoor {
        private Config _Config = new Config();
        private ClientThread _CT;

        public RunDoor(ClientThread clientThread) {
            _CT = clientThread;
        }

        private void ConvertDoorSysToDoor32Sys(string doorSysPath) {
            string[] DoorSysLines = FileUtils.FileReadAllLines(doorSysPath);
            List<string> Door32SysLines = new List<string>()
            {
                "2", // Telnet
                _CT.NodeInfo.Connection.Handle.ToString(), // Socket
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
            FileUtils.FileWriteAllLines(Door32SysPath, Door32SysLines.ToArray());
        }

        private void CreateNodeDirectory() {
            Directory.CreateDirectory(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString()));

            // Create string list
            List<string> Sl = new List<string>();

            // Create DOOR.SYS
            Sl.Clear();
            Sl.Add("COM1:");                                                    // 1 - Comm Port
            Sl.Add("57600");                                                    // 2 - Connection Baud Rate
            Sl.Add("8");                                                        // 3 - Parity
            Sl.Add(_CT.NodeInfo.Node.ToString());                                  // 4 - Current Node Number
            Sl.Add("57600");                                                    // 5 - Locked Baud Rate
            Sl.Add("Y");                                                        // 6 - Screen Display
            Sl.Add("Y");                                                        // 7 - Printer Toggle
            Sl.Add("Y");                                                        // 8 - Page Bell
            Sl.Add("Y");                                                        // 9 - Caller Alarm
            Sl.Add(_CT.NodeInfo.User.Alias);                                       // 10 - User's Real Name
            Sl.Add("City, State");                                              // 11 - User's Location
            Sl.Add("555-555-5555");                                             // 12 - User's Home Phone #
            Sl.Add("555-555-5555");                                             // 13 - User's Work Phone #
            Sl.Add("PASSWORD");                                                 // 14 - User's Password
            Sl.Add(_CT.NodeInfo.User.AccessLevel.ToString());                      // 15 - User's Access Level
            Sl.Add("1");                                                        // 16 - User's Total Calls
            Sl.Add("00/00/00");                                                 // 17 - User's Last Call Date
            Sl.Add(_CT.NodeInfo.SecondsLeft.ToString());                           // 18 - Users's Seconds Left This Call
            Sl.Add(_CT.NodeInfo.MinutesLeft.ToString());                           // 19 - User's Minutes Left This Call (I love redundancy!)
            Sl.Add("GR");                                                       // 20 - Graphics Mode GR=Graphics, NG=No Graphics, 7E=7-bit
            Sl.Add("24");                                                       // 21 - Screen Length
            Sl.Add("N");                                                        // 22 - Expert Mode
            Sl.Add("");                                                         // 23 - Conferences Registered In
            Sl.Add("");                                                         // 24 - Conference Exited To Door From
            Sl.Add("00/00/00");                                                 // 25 - User's Expiration Date
            Sl.Add((_CT.NodeInfo.User.UserId - 1).ToString());                     // 26 - User's Record Position (0 based)
            Sl.Add("Z");                                                        // 27 - User's Default XFer Protocol
            Sl.Add("0");                                                        // 28 - Total Uploads
            Sl.Add("0");                                                        // 29 - Total Downloads
            Sl.Add("0");                                                        // 30 - Total Downloaded Today (kB)
            Sl.Add("0");                                                        // 31 - Daily Download Limit (kB)
            Sl.Add("00/00/00");                                                 // 32 - User's Birthday
            Sl.Add(StringUtils.ExtractShortPathName(ProcessUtils.StartupPath)); // 33 - Path To User File
            Sl.Add(StringUtils.ExtractShortPathName(ProcessUtils.StartupPath)); // 34 - Path To GEN Directory
            Sl.Add(_Config.SysopFirstName + " " + _Config.SysopLastName);       // 35 - SysOp's Name
            Sl.Add(_CT.NodeInfo.User.Alias);                                       // 36 - User's Alias
            Sl.Add("00:00");                                                    // 37 - Next Event Time
            Sl.Add("Y");                                                        // 38 - Error Correcting Connection
            Sl.Add(_CT.NodeInfo.TerminalType == TerminalType.ASCII ? "N" : "Y");   // 39 - ANSI Supported
            Sl.Add("Y");                                                        // 40 - Use Record Locking
            Sl.Add("7");                                                        // 41 - Default BBS Colour
            Sl.Add("0");                                                        // 42 - Time Credits (In Minutes)
            Sl.Add("00/00/00");                                                 // 43 - Last New File Scan
            Sl.Add("00:00");                                                    // 44 - Time Of This Call
            Sl.Add("00:00");                                                    // 45 - Time Of Last Call
            Sl.Add("0");                                                        // 46 - Daily File Limit
            Sl.Add("0");                                                        // 47 - Files Downloaded Today
            Sl.Add("0");                                                        // 48 - Total Uploaded (kB)
            Sl.Add("0");                                                        // 49 - Total Downloaded (kB)
            Sl.Add("No Comment");                                               // 50 - User's Comment
            Sl.Add("0");                                                        // 51 - Total Doors Opened
            Sl.Add("0");                                                        // 52 - Total Messages Left
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "door.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOOR32.SYS
            Sl.Clear();
            Sl.Add("2");                                                            // 1 - Comm Type (0=Local, 1=Serial, 2=Telnet)
            Sl.Add(_CT.NodeInfo.Connection.Handle.ToString());                         // 2 - Comm Or Socket Handle
            Sl.Add("57600");                                                        // 3 - Baud Rate
            Sl.Add(ProcessUtils.ProductName + " v" + GameSrv.Version);              // 4 - BBSID (Software Name & Version
            Sl.Add(_CT.NodeInfo.User.UserId.ToString());                               // 5 - User's Record Position (1 based)
            Sl.Add(_CT.NodeInfo.User.Alias);                                           // 6 - User's Real Name
            Sl.Add(_CT.NodeInfo.User.Alias);                                           // 7 - User's Handle/Alias
            Sl.Add(_CT.NodeInfo.User.AccessLevel.ToString());                          // 8 - User's Access Level
            Sl.Add(_CT.NodeInfo.MinutesLeft.ToString());                               // 9 - User's Time Left (In Minutes)
            switch (_CT.NodeInfo.TerminalType)                                         // 10 - Emulation (0=Ascii, 1=Ansi, 2=Avatar, 3=RIP, 4=MaxGfx)
            {
                case TerminalType.ANSI: Sl.Add("1"); break;
                case TerminalType.ASCII: Sl.Add("0"); break;
                case TerminalType.RIP: Sl.Add("3"); break;
            }
            Sl.Add(_CT.NodeInfo.Node.ToString());                                      // 11 - Current Node Number
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "door32.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOORFILE.SR
            Sl.Clear();
            Sl.Add(_CT.NodeInfo.User.Alias);                                       // Complete name or handle of user
            Sl.Add(_CT.NodeInfo.TerminalType == TerminalType.ASCII ? "0" : "1");   // ANSI status:  1 = yes, 0 = no, -1 = don't know
            Sl.Add("1");                                                        // IBM Graphic characters:  1 = yes, 0 = no, -1 = unknown
            Sl.Add("24");                                                       // Page length of screen, in lines.  Assume 25 if unknown
            Sl.Add("57600");                                                    // Baud Rate:  300, 1200, 2400, 9600, 19200, etc.
            Sl.Add("1");                                                        // Com Port:  1, 2, 3, or 4; 0 if local.
            Sl.Add(_CT.NodeInfo.MinutesLeft.ToString());                           // Time Limit:  (in minutes); -1 if unknown.
            Sl.Add(_CT.NodeInfo.User.Alias);                                       // Real name (the same as line 1 if not known)
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "doorfile.sr"), String.Join("\r\n", Sl.ToArray()));

            // Create DORINFO.DEF
            Sl.Clear();
            Sl.Add(_Config.BBSName);                                           // 1 - BBS Name
            Sl.Add(_Config.SysopFirstName);                                    // 2 - Sysop's First Name
            Sl.Add(_Config.SysopLastName);                                     // 3 - Sysop's Last Name
            Sl.Add("COM1");                                                    // 4 - Comm Number in COMxxx Form
            Sl.Add("57600 BAUD,N,8,1");                                        // 5 - Baud Rate in 57600 BAUD,N,8,1 Form
            Sl.Add("0");                                                       // 6 - Networked?
            Sl.Add(_CT.NodeInfo.User.Alias);                                      // 7 - User's First Name / Alias
            Sl.Add("");                                                        // 8 - User's Last Name
            Sl.Add("City, State");                                             // 9 - User's Location (City, State, etc.)
            Sl.Add(_CT.NodeInfo.TerminalType == TerminalType.ASCII ? "0" : "1");  // 10 - User's Emulation (0=Ascii, 1=Ansi)
            Sl.Add(_CT.NodeInfo.User.AccessLevel.ToString());                     // 11 - User's Access Level
            Sl.Add(_CT.NodeInfo.MinutesLeft.ToString());                          // 12 - User's Time Left (In Minutes)
            Sl.Add("1");                                                       // 13 - Fossil?
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo1.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo" + _CT.NodeInfo.Node.ToString() + ".def"), String.Join("\r\n", Sl.ToArray()));
        }

        private void DeleteNodeDirectory() {
            if (!Globals.Debug) {
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "door.sys"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "door32.sys"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "doorfile.sr"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo.def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo1.def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo" + _CT.NodeInfo.Node.ToString() + ".def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dosemu.log"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "external.bat"));
                FileUtils.DirectoryDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString()));
            }
        }

        public void Run(string door) {
            _CT.NodeInfo.Door = new DoorInfo(door);
            if (_CT.NodeInfo.Door.Loaded) {
                Run();
            } else {
                RMLog.Error("Unable to find door: '" + door + "'");
            }
        }

        public void Run() {
            try {
                // Clear the buffers and reset the screen
                _CT.NodeInfo.Connection.ReadString();
                _CT.ClrScr();

                // Create the node directory and drop files
                CreateNodeDirectory();

                // Determine how to run the door
                if (((_CT.NodeInfo.Door.Platform == OSUtils.Platform.Linux) && OSUtils.IsUnix) || ((_CT.NodeInfo.Door.Platform == OSUtils.Platform.Windows) && OSUtils.IsWindows)) {
                    RunDoorNative(TranslateCLS(_CT.NodeInfo.Door.Command), TranslateCLS(_CT.NodeInfo.Door.Parameters));
                } else if ((_CT.NodeInfo.Door.Platform == OSUtils.Platform.DOS) && OSUtils.IsWindows) {
                    if (ProcessUtils.Is64BitOperatingSystem) {
                        if (Globals.IsDOSBoxInstalled()) {
                            RunDoorDOSBox(TranslateCLS(_CT.NodeInfo.Door.Command), TranslateCLS(_CT.NodeInfo.Door.Parameters));
                        } else {
                            RMLog.Error("DOS doors are not supported on 64bit Windows (unless you install DOSBox 0.73)");
                        }
                    } else {
                        RunDoorSBBSEXECNT(TranslateCLS(_CT.NodeInfo.Door.Command), TranslateCLS(_CT.NodeInfo.Door.Parameters), _CT.NodeInfo.Door.ForceQuitDelay);
                    }
                } else if ((_CT.NodeInfo.Door.Platform == OSUtils.Platform.DOS) && OSUtils.IsUnix) {
                    if (Globals.IsDOSEMUInstalled()) {
                        // TODOZ Doesn't this allow a door to hang if the user hangs up?  We need some method to force-quit it!
                        RunDoorDOSEMU(TranslateCLS(_CT.NodeInfo.Door.Command), TranslateCLS(_CT.NodeInfo.Door.Parameters));
                    } else {
                        RMLog.Error("DOS doors are not supported on Linux (unless you install DOSEMU)");
                    }
                } else {
                    RMLog.Error("Unsure how to run door on current platform");
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error while running door '" + _CT.NodeInfo.Door.Name + "'");
            } finally {
                // Clean up
                try {
                    _CT.ClrScr();
                    _CT.NodeInfo.Connection.SetBlocking(true); // In case native door disabled blocking sockets
                    DeleteNodeDirectory();
                } catch { /* Ignore */ }
            }
        }

        private void RunDoorDOSBox(string command, string parameters) {
            if (Globals.Debug) _CT.UpdateStatus("DEBUG: DOSBox launching " + command + " " + parameters);

            string DOSBoxConf = StringUtils.PathCombine("node" + _CT.NodeInfo.Node.ToString(), "dosbox.conf");
            string ProgramFilesX86 = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string DOSBoxExe = StringUtils.PathCombine(ProgramFilesX86, @"DOSBox-0.73\dosbox.exe"); // TODOZ add configuration variable so this path is not hardcoded

            // Copy base dosbox.conf 
            FileUtils.FileDelete(DOSBoxConf);
            FileUtils.FileCopy("dosbox.conf", DOSBoxConf);

            // If we're running a batch file, add a CALL to it
            if (command.ToUpper().Contains(".BAT")) command = "call " + command;

            string[] ExternalBat = new string[] { "mount c " + StringUtils.ExtractShortPathName(ProcessUtils.StartupPath), "C:", command + " " + parameters, "exit" };
            FileUtils.FileAppendAllText(DOSBoxConf, string.Join("\r\n", ExternalBat));

            // TODOZ Todd/maskreet does it this way -- maybe safer with commands passed this way, or at the very least with -securemode?
            /* dosbox.exe -c "mount d c:\games\!u_games\%4\%1" -c "mount e c:\doorway"
                -c "mount f c:\doorsrv\node%3" -c "e:" -c "bnu" -c "DOORWAY.EXE SYSF
                CFG\%1.cfg" -securemode -socket %2 -c "exit"
             */
            string Arguments = "-telnet -conf " + DOSBoxConf + " -socket " + _CT.NodeInfo.Connection.GetSocket().Handle.ToInt32().ToString();
            if (Globals.Debug) _CT.UpdateStatus("Executing " + DOSBoxExe + " " + Arguments);

            // Start the process
            using (RMProcess P = new RMProcess()) {
                P.ProcessWaitEvent += _CT.OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(DOSBoxExe, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _CT.NodeInfo.Door.WindowStyle;

                P.StartAndWait(PSI);
            }
        }

        private void RunDoorDOSEMU(string command, string parameters) {
            if (Globals.Debug) _CT.UpdateStatus("DEBUG: DOSEMU launching " + command + " " + parameters);

            PseudoTerminal pty = null;
            Mono.Unix.UnixStream us = null;
            int WaitStatus;

            try {
                bool DataTransferred = false;
                int LoopsSinceIO = 0;
                Exception ReadException = null;

                // If we're running a batch file, add a CALL to it
                if (command.ToUpper().Contains(".BAT")) command = "call " + command;

                string[] ExternalBat = new string[] { "@echo off", "lredir g: linux\\fs" + ProcessUtils.StartupPath, "set path=%path%;g:\\dosutils", "fossil.com", "share.com", "ansi.com", "g:", command + " " + parameters, "exitemu" };
                FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "external.bat"), String.Join("\r\n", ExternalBat), RMEncoding.Ansi);

                string[] Arguments = new string[] { "HOME=" + ProcessUtils.StartupPath, "HOME=" + ProcessUtils.StartupPath, "QUIET=1", "DOSDRIVE_D=" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString()), "/usr/bin/nice", "-n19", "/usr/bin/dosemu.bin", "-Ivideo { none }", "-Ikeystroke \\r", "-Iserial { virtual com 1 }", "-t", "-Ed:external.bat", "-o" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dosemu.log") };//, "2> /gamesrv/NODE" + _CT.NodeInfo.Node.ToString() + "/DOSEMU_BOOT.LOG" }; // TODO add configuration variable so this path is not hardcoded
                if (Globals.Debug) _CT.UpdateStatus("Executing /usr/bin/env " + string.Join(" ", Arguments));

                lock (Globals.PrivilegeLock) {
                    try {
                        Globals.NeedRoot();
                        pty = PseudoTerminal.Open(null, "/usr/bin/env", Arguments, "/tmp", 80, 25, false, false, false);
                        us = new Mono.Unix.UnixStream(pty.FileDescriptor, false);
                    } finally {
                        Globals.DropRoot(_Config.UnixUser);
                    }
                }

                new Thread(delegate (object p) {
                    // Send data from door to user
                    try {
                        byte[] Buffer = new byte[10240];
                        int NumRead = 0;

                        while (!_CT.QuitThread()) {
                            NumRead = us.Read(Buffer, 0, Buffer.Length);
                            if (NumRead > 0) {
                                _CT.NodeInfo.Connection.WriteBytes(Buffer, NumRead);
                                DataTransferred = true;
                            }
                        }
                    } catch (Exception ex) {
                        ReadException = ex;
                    }
                }).Start();

                // Check if we need to run cpulimit
                if (File.Exists(StringUtils.PathCombine(ProcessUtils.StartupPath, "cpulimit.sh"))) Process.Start(StringUtils.PathCombine(ProcessUtils.StartupPath, "cpulimit.sh"), pty.ChildPid.ToString());

                // Loop until something happens
                while (!_CT.QuitThread()) // NB: Was simply _Stop before
                {
                    DataTransferred = false;

                    // Check for exception in read thread
                    if (ReadException != null) return;

                    // Check for dropped carrier
                    if (!_CT.NodeInfo.Connection.Connected) {
                        int Sleeps = 0;

                        _CT.UpdateStatus("User hung-up while in external program");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGHUP);
                        while ((Sleeps++ < 5) && (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0)) {
                            Thread.Sleep(1000);
                        }
                        if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0) {
                            _CT.UpdateStatus("Process still active after waiting 5 seconds");
                        }
                        return;
                    }

                    // Send data from user to door
                    if (_CT.NodeInfo.Connection.CanRead()) {
                        // Write the text to the program
                        byte[] Bytes = _CT.NodeInfo.Connection.ReadBytes();
                        for (int i = 0; i < Bytes.Length; i++) {
                            us.WriteByte((byte)Bytes[i]);
                            us.Flush();
                        }

                        DataTransferred = true;
                    }

                    // Checks to perform when there was no I/O
                    if (!DataTransferred) {
                        LoopsSinceIO++;

                        // Only check process termination after 300 milliseconds of no I/O
                        // to allow for last minute reception of output from DOS programs
                        if (LoopsSinceIO >= 3) {
                            // Check if door terminated
                            if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) != 0) {
                                break;
                            }
                        }

                        // Let's make sure the socket is up
                        // Sending will trigger a socket d/c detection
                        if (LoopsSinceIO >= 300) {
                            switch (_CT.NodeInfo.ConnectionType) {
                                case ConnectionType.RLogin:
                                    _CT.NodeInfo.Connection.Write("\0");
                                    break;
                                case ConnectionType.Telnet:
                                    ((TelnetConnection)_CT.NodeInfo.Connection).SendGoAhead();
                                    break;
                                case ConnectionType.WebSocket:
                                    _CT.NodeInfo.Connection.Write("\0");
                                    break;
                            }
                        }

                        // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                        _CT.NodeInfo.Connection.CanRead(100);
                    } else {
                        LoopsSinceIO = 0;
                    }
                }
            } finally {
                // Terminate process if it hasn't closed yet
                if (pty != null) {
                    if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0) {
                        _CT.UpdateStatus("Terminating process");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGKILL);
                    }
                    pty.Dispose();
                }
            }
        }

        private void RunDoorNative(string command, string parameters) {
            if (Globals.Debug) _CT.UpdateStatus("DEBUG: Natively launching " + command + " " + parameters);
            using (RMProcess P = new RMProcess()) {
                P.ProcessWaitEvent += _CT.OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(command, parameters);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _CT.NodeInfo.Door.WindowStyle;

                P.StartAndWait(PSI);
            }
        }

        struct sbbsexec_start_t {
            public uint Mode;
            public IntPtr Event;
        }

        private unsafe void RunDoorSBBSEXECNT(string command, string parameters, int forceQuitDelay) {
            if (Globals.Debug) _CT.UpdateStatus("DEBUG: SBBSEXECNT launching " + command + " " + parameters);

            // SBBSEXEC constants
            const uint LoopsBeforeYield = 10;
            const uint SBBSEXEC_MODE_FOSSIL = 0;
            const uint XTRN_IO_BUF_LEN = 10000;

            // Initialize variables
            IntPtr HangUpEvent = new IntPtr();
            IntPtr HungUpEvent = new IntPtr();
            int LoopsSinceIO = 0;
            RMProcess P = null;
            IntPtr ReadSlot = new IntPtr();
            IntPtr WriteSlot = new IntPtr();

            // Initialize filename variables
            string EnvFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dosxtrn.env");
            string RetFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dosxtrn.ret");
            string W32DoorFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "w32door.run");

            try {
                // Create temporary environment file
                FileUtils.FileWriteAllText(EnvFile, Environment.GetEnvironmentVariable("COMSPEC") + " /C " + command + " " + parameters);

                // Create a hungup event for when user drops carrier
                HungUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hungup" + _CT.NodeInfo.Node.ToString());
                if (HungUpEvent == IntPtr.Zero) {
                    RMLog.Error("CreateEvent() failed to create HungUpEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Create a hangup event (for when the door requests to drop DTR)
                HangUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hangup" + _CT.NodeInfo.Node.ToString());
                if (HangUpEvent == IntPtr.Zero) {
                    RMLog.Error("CreateEvent() failed to create HangUpEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Create a read mail slot
                ReadSlot = NativeMethods.CreateMailslot("\\\\.\\mailslot\\sbbsexec\\rd" + _CT.NodeInfo.Node.ToString(), XTRN_IO_BUF_LEN, 0, IntPtr.Zero);
                if (ReadSlot == IntPtr.Zero) {
                    RMLog.Error("CreateMailslot() failed to create ReadSlot: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Start the process
                string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "DOSXTRN.EXE");
                string Arguments = StringUtils.ExtractShortPathName(EnvFile) + " NT " + _CT.NodeInfo.Node.ToString() + " " + SBBSEXEC_MODE_FOSSIL.ToString() + " " + LoopsBeforeYield.ToString();
                ProcessStartInfo PSI = new ProcessStartInfo(FileName, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _CT.NodeInfo.Door.WindowStyle;
                P = RMProcess.Start(PSI);

                if (P == null) {
                    RMLog.Error("Error launching " + FileName + " " + Arguments);
                    return;
                }

                // Loop until something happens
                bool DataTransferred = false;
                while (!_CT.QuitThread()) // NB: Was simply _Stop before
                {
                    DataTransferred = false;

                    // Check for dropped carrier
                    if (!_CT.NodeInfo.Connection.Connected) {
                        _CT.UpdateStatus("User hung-up while in external program");
                        NativeMethods.SetEvent(HungUpEvent);

                        // Wait up to forceQuitDelay seconds for the process to terminate
                        for (int i = 0; i < forceQuitDelay; i++) {
                            if (P.HasExited) return;
                            P.WaitForExit(1000);
                        }
                        _CT.UpdateStatus("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                        return;
                    }

                    // Send data from user to door
                    if (_CT.NodeInfo.Connection.CanRead()) {
                        // If our writeslot doesnt exist yet, create it
                        if (WriteSlot == IntPtr.Zero) {
                            // Create A Write Mail Slot
                            WriteSlot = NativeMethods.CreateFile("\\\\.\\mailslot\\sbbsexec\\wr" + _CT.NodeInfo.Node.ToString(), NativeMethods.FileAccess.GenericWrite, NativeMethods.FileShare.Read, IntPtr.Zero, NativeMethods.CreationDisposition.OpenExisting, NativeMethods.CreateFileAttributes.Normal, IntPtr.Zero);
                            int LastWin32Error = Marshal.GetLastWin32Error();
                            if (WriteSlot == IntPtr.Zero) {
                                RMLog.Error("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                                return;
                            } else if (WriteSlot.ToInt32() == -1) {
                                if (LastWin32Error == 2) {
                                    // ERROR_FILE_NOT_FOUND - User must have hit a key really fast to trigger this!
                                    RMLog.Warning("CreateFile() failed to find WriteSlot: \\\\.\\mailslot\\sbbsexec\\wr" + _CT.NodeInfo.Node.ToString());
                                    WriteSlot = IntPtr.Zero;
                                    Thread.Sleep(100);
                                } else {
                                    RMLog.Error("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                                    return;
                                }
                            }
                        }

                        // Write the text to the program
                        if (WriteSlot != IntPtr.Zero) {
                            byte[] BufBytes = _CT.NodeInfo.Connection.PeekBytes();
                            uint BytesWritten = 0;
                            bool Result = NativeMethods.WriteFile(WriteSlot, BufBytes, (uint)BufBytes.Length, out BytesWritten, null);
                            int LastWin32Error = Marshal.GetLastWin32Error();
                            if (Result) {
                                _CT.NodeInfo.Connection.ReadBytes((int)BytesWritten);
                                DataTransferred = true;
                            } else {
                                RMLog.Error("Error calling WriteFile(): " + LastWin32Error.ToString());
                                return;
                            }
                        }
                    }

                    // Send data from door to user
                    uint BytesRead = 0;
                    int MaxMessageSize = 0;
                    int MessageCount = 0;
                    int NextSize = 0;
                    int ReadTimeout = 0;
                    if (NativeMethods.GetMailslotInfo(ReadSlot, ref MaxMessageSize, ref NextSize, ref MessageCount, ref ReadTimeout)) {
                        byte[] BufBytes = new byte[XTRN_IO_BUF_LEN];
                        int BufPtr = 0;

                        // If a message is waiting, get it
                        for (int i = 0; i < MessageCount; i++) {
                            // Read the next message
                            fixed (byte* p = BufBytes) {
                                if (NativeMethods.ReadFile(ReadSlot, p + BufPtr, (uint)(BufBytes.Length - BufPtr), out BytesRead, null)) {
                                    if (BytesRead > 0) {
                                        BufPtr += (int)BytesRead;

                                        // If we have filled the buffer, break
                                        if (BufPtr >= BufBytes.Length) break;
                                    }
                                }
                            }
                        }

                        // If we read something, write it to the user
                        if (BufPtr > 0) {
                            _CT.NodeInfo.Connection.WriteBytes(BufBytes, BufPtr);
                            DataTransferred = true;
                        }
                    }

                    // Checks to perform when there was no I/O
                    if (!DataTransferred) {
                        // Numer of loop iterations with no I/O
                        LoopsSinceIO++;

                        // Only check process termination after 300 milliseconds of no I/O
                        // to allow for last minute reception of output from DOS programs
                        if (LoopsSinceIO >= 3) {
                            if ((_CT.NodeInfo.Door.WatchDTR) && (NativeMethods.WaitForSingleObject(HangUpEvent, 0) == NativeMethods.WAIT_OBJECT_0)) {
                                _CT.UpdateStatus("External program requested hangup (dropped DTR)");
                                _CT.NodeInfo.Connection.Close();

                                // Wait up to forceQuitDelay seconds for the process to terminate
                                for (int i = 0; i < forceQuitDelay; i++) {
                                    if (P.HasExited) return;
                                    P.WaitForExit(1000);
                                }
                                _CT.UpdateStatus("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                                return;
                            }

                            if (P.HasExited) {
                                _CT.UpdateStatus("External terminated with exit code: " + P.ExitCode);
                                break;
                            }

                            // Watch for a W32DOOR.RUN file to be created in the node directory
                            // If it gets created, it's our signal that a DOS BBS package wants us to launch a W32 door
                            // W32DOOR.RUN will contain two lines, the first is the command to run, the second is the parameters
                            if (File.Exists(W32DoorFile)) {
                                try {
                                    if (Globals.Debug) _CT.UpdateStatus("DEBUG: w32door.run found");
                                    string[] W32DoorRunLines = FileUtils.FileReadAllLines(W32DoorFile);
                                    ConvertDoorSysToDoor32Sys(W32DoorRunLines[0]);
                                    RunDoorNative(W32DoorRunLines[1], W32DoorRunLines[2]);
                                } finally {
                                    FileUtils.FileDelete(W32DoorFile);
                                }
                            }

                        }

                        // Let's make sure the socket is up
                        // Sending will trigger a socket d/c detection
                        if (LoopsSinceIO % 300 == 0) {
                            switch (_CT.NodeInfo.ConnectionType) {
                                case ConnectionType.RLogin:
                                    _CT.NodeInfo.Connection.Write("\0");
                                    break;
                                case ConnectionType.Telnet:
                                    ((TelnetConnection)_CT.NodeInfo.Connection).SendGoAhead();
                                    break;
                                case ConnectionType.WebSocket:
                                    _CT.NodeInfo.Connection.Write("\0");
                                    break;
                            }
                        }

                        // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                        _CT.NodeInfo.Connection.CanRead(100);
                    } else {
                        LoopsSinceIO = 0;
                    }
                }
            } finally {
                try {
                    // Terminate process if it hasn't closed yet
                    if ((P != null) && !P.HasExited) {
                        RMLog.Error("Door still running, performing a force quit");
                        P.Kill();
                    }
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Unable to perform force quit");
                }

                // Free unmanaged resources
                if (WriteSlot != IntPtr.Zero) NativeMethods.CloseHandle(WriteSlot);
                if (ReadSlot != IntPtr.Zero) NativeMethods.CloseHandle(ReadSlot);
                if (HangUpEvent != IntPtr.Zero) NativeMethods.CloseHandle(HangUpEvent);
                if (HungUpEvent != IntPtr.Zero) NativeMethods.CloseHandle(HungUpEvent);

                // Delete .ENV and .RET files
                FileUtils.FileDelete(EnvFile);
                FileUtils.FileDelete(RetFile);
            }
        }

        private string TranslateCLS(string command) {
            List<KeyValuePair<string, string>> CLS = new List<KeyValuePair<string, string>>();
            CLS.Add(new KeyValuePair<string, string>("**ALIAS", _CT.NodeInfo.User.Alias));
            CLS.Add(new KeyValuePair<string, string>("DOOR32", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "door32.sys"))));
            CLS.Add(new KeyValuePair<string, string>("DOORSYS", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "door.sys"))));
            CLS.Add(new KeyValuePair<string, string>("DOORFILE", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "doorfile.sr"))));
            CLS.Add(new KeyValuePair<string, string>("DORINFOx", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo" + _CT.NodeInfo.Node.ToString() + ".def"))));
            CLS.Add(new KeyValuePair<string, string>("DORINFO1", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo1.def"))));
            CLS.Add(new KeyValuePair<string, string>("DORINFO", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _CT.NodeInfo.Node.ToString(), "dorinfo.def"))));
            CLS.Add(new KeyValuePair<string, string>("HANDLE", _CT.NodeInfo.Connection.Handle.ToString()));
            CLS.Add(new KeyValuePair<string, string>("IPADDRESS", _CT.NodeInfo.Connection.GetRemoteIP()));
            CLS.Add(new KeyValuePair<string, string>("MINUTESLEFT", _CT.NodeInfo.MinutesLeft.ToString()));
            CLS.Add(new KeyValuePair<string, string>("NODE", _CT.NodeInfo.Node.ToString()));
            CLS.Add(new KeyValuePair<string, string>("**PASSWORD", _CT.NodeInfo.User.PasswordHash));
            CLS.Add(new KeyValuePair<string, string>("SECONDSLEFT", _CT.NodeInfo.SecondsLeft.ToString()));
            CLS.Add(new KeyValuePair<string, string>("SOCKETHANDLE", _CT.NodeInfo.Connection.Handle.ToString()));
            CLS.Add(new KeyValuePair<string, string>("**USERNAME", _CT.NodeInfo.User.Alias));
            foreach (DictionaryEntry DE in _CT.NodeInfo.User.AdditionalInfo) {
                CLS.Add(new KeyValuePair<string, string>("**" + DE.Key.ToString(), DE.Value.ToString()));
            }

            // Perform translation
            for (int i = 0; i < CLS.Count; i++) {
                if (CLS[i].Value != null) {
                    command = command.Replace("*" + CLS[i].Key.ToString().ToUpper(), CLS[i].Value.ToString());
                }
            }
            return command;
        }
    }
}
