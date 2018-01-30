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
        private ClientThread _ClientThread;

        public RunDoor(ClientThread clientThread) {
            _ClientThread = clientThread;
        }

        private void CreateNodeDirectory() {
            Directory.CreateDirectory(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString()));

            // Create string list
            List<string> Sl = new List<string>();

            // Create DOOR.SYS
            Sl.Clear();
            Sl.Add("COM1:");                                                                // 1 - Comm Port
            Sl.Add("57600");                                                                // 2 - Connection Baud Rate
            Sl.Add("8");                                                                    // 3 - Parity
            Sl.Add(_ClientThread.NodeInfo.Node.ToString());                                 // 4 - Current Node Number
            Sl.Add("57600");                                                                // 5 - Locked Baud Rate
            Sl.Add("Y");                                                                    // 6 - Screen Display
            Sl.Add("Y");                                                                    // 7 - Printer Toggle
            Sl.Add("Y");                                                                    // 8 - Page Bell
            Sl.Add("Y");                                                                    // 9 - Caller Alarm
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                                      // 10 - User's Real Name
            Sl.Add("City, State");                                                          // 11 - User's Location
            Sl.Add("555-555-5555");                                                         // 12 - User's Home Phone #
            Sl.Add("555-555-5555");                                                         // 13 - User's Work Phone #
            Sl.Add("PASSWORD");                                                             // 14 - User's Password
            Sl.Add(_ClientThread.NodeInfo.User.AccessLevel.ToString());                     // 15 - User's Access Level
            Sl.Add("1");                                                                    // 16 - User's Total Calls
            Sl.Add("00/00/00");                                                             // 17 - User's Last Call Date
            Sl.Add(_ClientThread.NodeInfo.SecondsLeft.ToString());                          // 18 - Users's Seconds Left This Call
            Sl.Add(_ClientThread.NodeInfo.MinutesLeft.ToString());                          // 19 - User's Minutes Left This Call (I love redundancy!)
            Sl.Add("GR");                                                                   // 20 - Graphics Mode GR=Graphics, NG=No Graphics, 7E=7-bit
            Sl.Add("24");                                                                   // 21 - Screen Length
            Sl.Add("N");                                                                    // 22 - Expert Mode
            Sl.Add("");                                                                     // 23 - Conferences Registered In
            Sl.Add("");                                                                     // 24 - Conference Exited To Door From
            Sl.Add("00/00/00");                                                             // 25 - User's Expiration Date
            Sl.Add((_ClientThread.NodeInfo.User.UserId - 1).ToString());                    // 26 - User's Record Position (0 based)
            Sl.Add("Z");                                                                    // 27 - User's Default XFer Protocol
            Sl.Add("0");                                                                    // 28 - Total Uploads
            Sl.Add("0");                                                                    // 29 - Total Downloads
            Sl.Add("0");                                                                    // 30 - Total Downloaded Today (kB)
            Sl.Add("0");                                                                    // 31 - Daily Download Limit (kB)
            Sl.Add("00/00/00");                                                             // 32 - User's Birthday
            Sl.Add(StringUtils.ExtractShortPathName(ProcessUtils.StartupPath));             // 33 - Path To User File
            Sl.Add(StringUtils.ExtractShortPathName(ProcessUtils.StartupPath));             // 34 - Path To GEN Directory
            Sl.Add(Config.Instance.SysopFirstName + " " + Config.Instance.SysopLastName);   // 35 - SysOp's Name
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                                      // 36 - User's Alias
            Sl.Add("00:00");                                                                // 37 - Next Event Time
            Sl.Add("Y");                                                                    // 38 - Error Correcting Connection
            Sl.Add(_ClientThread.NodeInfo.TerminalType == TerminalType.ASCII ? "N" : "Y");  // 39 - ANSI Supported
            Sl.Add("Y");                                                                    // 40 - Use Record Locking
            Sl.Add("7");                                                                    // 41 - Default BBS Colour
            Sl.Add("0");                                                                    // 42 - Time Credits (In Minutes)
            Sl.Add("00/00/00");                                                             // 43 - Last New File Scan
            Sl.Add("00:00");                                                                // 44 - Time Of This Call
            Sl.Add("00:00");                                                                // 45 - Time Of Last Call
            Sl.Add("0");                                                                    // 46 - Daily File Limit
            Sl.Add("0");                                                                    // 47 - Files Downloaded Today
            Sl.Add("0");                                                                    // 48 - Total Uploaded (kB)
            Sl.Add("0");                                                                    // 49 - Total Downloaded (kB)
            Sl.Add("No Comment");                                                           // 50 - User's Comment
            Sl.Add("0");                                                                    // 51 - Total Doors Opened
            Sl.Add("0");                                                                    // 52 - Total Messages Left
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "door.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOOR32.SYS
            Sl.Clear();
            Sl.Add("2");                                                    // 1 - Comm Type (0=Local, 1=Serial, 2=Telnet)
            Sl.Add(_ClientThread.NodeInfo.Connection.Handle.ToString());    // 2 - Comm Or Socket Handle
            Sl.Add("57600");                                                // 3 - Baud Rate
            Sl.Add(ProcessUtils.ProductName + " v" + GameSrv.Version);      // 4 - BBSID (Software Name & Version
            Sl.Add(_ClientThread.NodeInfo.User.UserId.ToString());          // 5 - User's Record Position (1 based)
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                      // 6 - User's Real Name
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                      // 7 - User's Handle/Alias
            Sl.Add(_ClientThread.NodeInfo.User.AccessLevel.ToString());     // 8 - User's Access Level
            Sl.Add(_ClientThread.NodeInfo.MinutesLeft.ToString());          // 9 - User's Time Left (In Minutes)
            switch (_ClientThread.NodeInfo.TerminalType)                    // 10 - Emulation (0=Ascii, 1=Ansi, 2=Avatar, 3=RIP, 4=MaxGfx)
            {
                case TerminalType.ANSI: Sl.Add("1"); break;
                case TerminalType.ASCII: Sl.Add("0"); break;
                case TerminalType.RIP: Sl.Add("3"); break;
            }
            Sl.Add(_ClientThread.NodeInfo.Node.ToString());                 // 11 - Current Node Number
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "door32.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOORFILE.SR
            Sl.Clear();
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                                      // Complete name or handle of user
            Sl.Add(_ClientThread.NodeInfo.TerminalType == TerminalType.ASCII ? "0" : "1");  // ANSI status:  1 = yes, 0 = no, -1 = don't know
            Sl.Add("1");                                                                    // IBM Graphic characters:  1 = yes, 0 = no, -1 = unknown
            Sl.Add("24");                                                                   // Page length of screen, in lines.  Assume 25 if unknown
            Sl.Add("57600");                                                                // Baud Rate:  300, 1200, 2400, 9600, 19200, etc.
            Sl.Add("1");                                                                    // Com Port:  1, 2, 3, or 4; 0 if local.
            Sl.Add(_ClientThread.NodeInfo.MinutesLeft.ToString());                          // Time Limit:  (in minutes); -1 if unknown.
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                                      // Real name (the same as line 1 if not known)
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "doorfile.sr"), String.Join("\r\n", Sl.ToArray()));

            // Create DORINFO.DEF
            Sl.Clear();
            Sl.Add(Config.Instance.BBSName);                                                // 1 - BBS Name
            Sl.Add(Config.Instance.SysopFirstName);                                         // 2 - Sysop's First Name
            Sl.Add(Config.Instance.SysopLastName);                                          // 3 - Sysop's Last Name
            Sl.Add("COM1");                                                                 // 4 - Comm Number in COMxxx Form
            Sl.Add("57600 BAUD,N,8,1");                                                     // 5 - Baud Rate in 57600 BAUD,N,8,1 Form
            Sl.Add("0");                                                                    // 6 - Networked?
            Sl.Add(_ClientThread.NodeInfo.User.Alias);                                      // 7 - User's First Name / Alias
            Sl.Add("");                                                                     // 8 - User's Last Name
            Sl.Add("City, State");                                                          // 9 - User's Location (City, State, etc.)
            Sl.Add(_ClientThread.NodeInfo.TerminalType == TerminalType.ASCII ? "0" : "1");  // 10 - User's Emulation (0=Ascii, 1=Ansi)
            Sl.Add(_ClientThread.NodeInfo.User.AccessLevel.ToString());                     // 11 - User's Access Level
            Sl.Add(_ClientThread.NodeInfo.MinutesLeft.ToString());                          // 12 - User's Time Left (In Minutes)
            Sl.Add("1");                                                                    // 13 - Fossil?
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo1.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo" + _ClientThread.NodeInfo.Node.ToString() + ".def"), String.Join("\r\n", Sl.ToArray()));
        }

        private void DeleteNodeDirectory() {
            if (!Helpers.Debug) {
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "door.sys"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "door32.sys"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "doorfile.sr"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo.def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo1.def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo" + _ClientThread.NodeInfo.Node.ToString() + ".def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dosemu.log"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "external.bat"));
                FileUtils.DirectoryDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString()));
            }
        }

        public void Run(string door) {
            _ClientThread.NodeInfo.Door = new DoorInfo(door);
            if (_ClientThread.NodeInfo.Door.Loaded) {
                Run();
            } else {
                RMLog.Error("Unable to find door: '" + door + "'");
            }
        }

        public void Run() {
            try {
                // Clear the buffers and reset the screen
                _ClientThread.NodeInfo.Connection.ReadString();
                _ClientThread.ClrScr();

                // Create the node directory and drop files
                CreateNodeDirectory();

                // Determine how to run the door
                if (((_ClientThread.NodeInfo.Door.Platform == OSUtils.Platform.Linux) && OSUtils.IsUnix) || ((_ClientThread.NodeInfo.Door.Platform == OSUtils.Platform.Windows) && OSUtils.IsWindows)) {
                    RunDoorNative(TranslateCLS(_ClientThread.NodeInfo.Door.Command), TranslateCLS(_ClientThread.NodeInfo.Door.Parameters));
                } else if ((_ClientThread.NodeInfo.Door.Platform == OSUtils.Platform.DOS) && OSUtils.IsWindows) {
                    if (ProcessUtils.Is64BitOperatingSystem) {
                        if (Helpers.IsDOSBoxInstalled()) {
                            RunDoorDOSBox(TranslateCLS(_ClientThread.NodeInfo.Door.Command), TranslateCLS(_ClientThread.NodeInfo.Door.Parameters));
                        } else {
                            RMLog.Error("DOS doors are not supported on 64bit Windows (unless you install DOSBox 0.73)");
                        }
                    } else {
                        (new RunDoorSBBSEXEC(_ClientThread)).Run(TranslateCLS(_ClientThread.NodeInfo.Door.Command), TranslateCLS(_ClientThread.NodeInfo.Door.Parameters), _ClientThread.NodeInfo.Door.ForceQuitDelay);
                    }
                } else if ((_ClientThread.NodeInfo.Door.Platform == OSUtils.Platform.DOS) && OSUtils.IsUnix) {
                    if (Helpers.IsDOSEMUInstalled()) {
                        RunDoorDOSEMU(TranslateCLS(_ClientThread.NodeInfo.Door.Command), TranslateCLS(_ClientThread.NodeInfo.Door.Parameters));
                    } else {
                        RMLog.Error("DOS doors are not supported on Linux (unless you install DOSEMU)");
                    }
                } else {
                    RMLog.Error("Unsure how to run door on current platform");
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error while running door '" + _ClientThread.NodeInfo.Door.Name + "'");
            } finally {
                // Clean up
                try {
                    _ClientThread.ClrScr();
                    _ClientThread.NodeInfo.Connection.SetBlocking(true); // In case native door disabled blocking sockets
                    DeleteNodeDirectory();
                } catch { /* Ignore */ }
            }
        }

        private void RunDoorDOSBox(string command, string parameters) {
            if (Helpers.Debug) _ClientThread.UpdateStatus("DEBUG: DOSBox launching " + command + " " + parameters);

            string DOSBoxConf = StringUtils.PathCombine("node" + _ClientThread.NodeInfo.Node.ToString(), "dosbox.conf");
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
            string Arguments = "-telnet -conf " + DOSBoxConf + " -socket " + _ClientThread.NodeInfo.Connection.GetSocket().Handle.ToInt32().ToString();
            if (Helpers.Debug) _ClientThread.UpdateStatus("Executing " + DOSBoxExe + " " + Arguments);

            // Start the process
            using (RMProcess P = new RMProcess()) {
                P.ProcessWaitEvent += _ClientThread.OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(DOSBoxExe, Arguments) {
                    WindowStyle = _ClientThread.NodeInfo.Door.WindowStyle,
                    WorkingDirectory = ProcessUtils.StartupPath,
                };
                P.StartAndWait(PSI);
            }
        }

        private void RunDoorDOSEMU(string command, string parameters) {
            if (Helpers.Debug) _ClientThread.UpdateStatus("DEBUG: DOSEMU launching " + command + " " + parameters);

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
                FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "external.bat"), String.Join("\r\n", ExternalBat), RMEncoding.Ansi);

                string[] Arguments = new string[] { "HOME=" + ProcessUtils.StartupPath, "HOME=" + ProcessUtils.StartupPath, "QUIET=1", "DOSDRIVE_D=" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString()), "/usr/bin/nice", "-n19", "/usr/bin/dosemu.bin", "-Ivideo { none }", "-Ikeystroke \\r", "-Iserial { virtual com 1 }", "-t", "-Ed:external.bat", "-o" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dosemu.log") };//, "2> /gamesrv/NODE" + _CT.NodeInfo.Node.ToString() + "/DOSEMU_BOOT.LOG" }; // TODO add configuration variable so this path is not hardcoded
                if (Helpers.Debug) _ClientThread.UpdateStatus("Executing /usr/bin/env " + string.Join(" ", Arguments));

                lock (Helpers.PrivilegeLock) {
                    try {
                        Helpers.NeedRoot();
                        pty = PseudoTerminal.Open(null, "/usr/bin/env", Arguments, "/tmp", 80, 25, false, false, false);
                        us = new Mono.Unix.UnixStream(pty.FileDescriptor, false);
                    } finally {
                        Helpers.DropRoot(Config.Instance.UnixUser);
                    }
                }

                new Thread(delegate (object p) {
                    // Send data from door to user
                    try {
                        byte[] Buffer = new byte[10240];
                        int NumRead = 0;

                        while (!_ClientThread.QuitThread()) {
                            NumRead = us.Read(Buffer, 0, Buffer.Length);
                            if (NumRead > 0) {
                                _ClientThread.NodeInfo.Connection.WriteBytes(Buffer, NumRead);
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
                while (!_ClientThread.QuitThread()) // NB: Was simply _Stop before
                {
                    DataTransferred = false;

                    // Check for exception in read thread
                    if (ReadException != null) return;

                    // Check for dropped carrier
                    if (!_ClientThread.NodeInfo.Connection.Connected) {
                        int Sleeps = 0;

                        _ClientThread.UpdateStatus("User hung-up while in external program");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGHUP);
                        while ((Sleeps++ < 5) && (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0)) {
                            Thread.Sleep(1000);
                        }
                        if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0) {
                            _ClientThread.UpdateStatus("Process still active after waiting 5 seconds");
                        }
                        return;
                    }

                    // Send data from user to door
                    if (_ClientThread.NodeInfo.Connection.CanRead()) {
                        // Write the text to the program
                        byte[] Bytes = _ClientThread.NodeInfo.Connection.ReadBytes();
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
                            switch (_ClientThread.NodeInfo.ConnectionType) {
                                case ConnectionType.RLogin:
                                    _ClientThread.NodeInfo.Connection.Write("\0");
                                    break;
                                case ConnectionType.Telnet:
                                    ((TelnetConnection)_ClientThread.NodeInfo.Connection).SendGoAhead();
                                    break;
                                case ConnectionType.WebSocket:
                                    _ClientThread.NodeInfo.Connection.Write("\0");
                                    break;
                            }
                        }

                        // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                        _ClientThread.NodeInfo.Connection.CanRead(100);
                    } else {
                        LoopsSinceIO = 0;
                    }
                }
            } finally {
                // Terminate process if it hasn't closed yet
                if (pty != null) {
                    if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0) {
                        _ClientThread.UpdateStatus("Terminating process");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGKILL);
                    }
                    pty.Dispose();
                }
            }
        }

        public void RunDoorNative(string command, string parameters) {
            if (Helpers.Debug) _ClientThread.UpdateStatus("DEBUG: Natively launching " + command + " " + parameters);
            using (RMProcess P = new RMProcess()) {
                P.ProcessWaitEvent += _ClientThread.OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(command, parameters) {
                    WindowStyle = _ClientThread.NodeInfo.Door.WindowStyle,
                    WorkingDirectory = ProcessUtils.StartupPath,
                };
                P.StartAndWait(PSI);
            }
        }

        private string TranslateCLS(string command) {
            List<KeyValuePair<string, string>> CLS = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("**ALIAS", _ClientThread.NodeInfo.User.Alias),
                new KeyValuePair<string, string>("DOOR32", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "door32.sys"))),
                new KeyValuePair<string, string>("DOORSYS", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "door.sys"))),
                new KeyValuePair<string, string>("DOORFILE", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "doorfile.sr"))),
                new KeyValuePair<string, string>("DORINFOx", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo" + _ClientThread.NodeInfo.Node.ToString() + ".def"))),
                new KeyValuePair<string, string>("DORINFO1", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo1.def"))),
                new KeyValuePair<string, string>("DORINFO", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dorinfo.def"))),
                new KeyValuePair<string, string>("HANDLE", _ClientThread.NodeInfo.Connection.Handle.ToString()),
                new KeyValuePair<string, string>("IPADDRESS", _ClientThread.NodeInfo.Connection.GetRemoteIP()),
                new KeyValuePair<string, string>("MINUTESLEFT", _ClientThread.NodeInfo.MinutesLeft.ToString()),
                new KeyValuePair<string, string>("NODE", _ClientThread.NodeInfo.Node.ToString()),
                new KeyValuePair<string, string>("**PASSWORD", _ClientThread.NodeInfo.User.PasswordHash),
                new KeyValuePair<string, string>("SECONDSLEFT", _ClientThread.NodeInfo.SecondsLeft.ToString()),
                new KeyValuePair<string, string>("SOCKETHANDLE", _ClientThread.NodeInfo.Connection.Handle.ToString()),
                new KeyValuePair<string, string>("**USERNAME", _ClientThread.NodeInfo.User.Alias),
            };
            foreach (DictionaryEntry DE in _ClientThread.NodeInfo.User.AdditionalInfo) {
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
