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
    public class RunDoorSBBSEXEC : IDisposable {
        private ClientThread _ClientThread;

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

        // Filename variables
        string EnvFile;
        string RetFile;
        string W32DoorFile;

        // Door parameters
        string _Command;
        string _Parameters;
        int _ForceQuitDelay;

        public RunDoorSBBSEXEC(ClientThread clientThread) {
            _ClientThread = clientThread;

            // Initialize filename variables
            EnvFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dosxtrn.env");
            RetFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "dosxtrn.ret");
            W32DoorFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _ClientThread.NodeInfo.Node.ToString(), "w32door.run");
        }

        private void CheckForW32DoorRun() {
            // Watch for a W32DOOR.RUN file to be created in the node directory
            // If it gets created, it's our signal that a DOS BBS package wants us to launch a W32 door
            // W32DOOR.RUN will contain two lines, the first is the command to run, the second is the parameters
            if (File.Exists(W32DoorFile)) {
                try {
                    if (Helpers.Debug)
                        _ClientThread.UpdateStatus("DEBUG: w32door.run found");
                    string[] W32DoorRunLines = FileUtils.FileReadAllLines(W32DoorFile);
                    ConvertDoorSysToDoor32Sys(W32DoorRunLines[0]);
                    // TODOX A way to do this without making RunDoorNative public?
                    (new RunDoor(_ClientThread)).RunDoorNative(W32DoorRunLines[1], W32DoorRunLines[2]);
                } finally {
                    FileUtils.FileDelete(W32DoorFile);
                }
            }
        }

        private void CleanUp() {
            // Delete .ENV and .RET files
            FileUtils.FileDelete(EnvFile);
            FileUtils.FileDelete(RetFile);
        }

        private void ConvertDoorSysToDoor32Sys(string doorSysPath) {
            string[] DoorSysLines = FileUtils.FileReadAllLines(doorSysPath);
            List<string> Door32SysLines = new List<string>()
            {
                "2", // Telnet
                _ClientThread.NodeInfo.Connection.Handle.ToString(), // Socket
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

        private bool DoorDroppedDTR() {
            if ((_ClientThread.NodeInfo.Door.WatchDTR) && (NativeMethods.WaitForSingleObject(HangUpEvent, 0) == NativeMethods.WAIT_OBJECT_0)) {
                _ClientThread.UpdateStatus("External program requested hangup (dropped DTR)");
                _ClientThread.NodeInfo.Connection.Close();

                // Wait up to forceQuitDelay seconds for the process to terminate
                for (int i = 0; i < _ForceQuitDelay; i++) {
                    if (P.HasExited) {
                        return true;
                    }
                    P.WaitForExit(1000);
                }
                _ClientThread.UpdateStatus("Process still active after waiting " + _ForceQuitDelay.ToString() + " seconds");
                return true;
            } else {
                return false;
            }
        }

        private bool Initialize() {
            // Create temporary environment file
            FileUtils.FileWriteAllText(EnvFile, Environment.GetEnvironmentVariable("COMSPEC") + " /C " + _Command + " " + _Parameters);

            // Create a hungup event for when user drops carrier
            HungUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hungup" + _ClientThread.NodeInfo.Node.ToString());
            if (HungUpEvent == IntPtr.Zero) {
                RMLog.Error("CreateEvent() failed to create HungUpEvent: " + Marshal.GetLastWin32Error().ToString());
                return false;
            }

            // Create a hangup event (for when the door requests to drop DTR)
            HangUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hangup" + _ClientThread.NodeInfo.Node.ToString());
            if (HangUpEvent == IntPtr.Zero) {
                RMLog.Error("CreateEvent() failed to create HangUpEvent: " + Marshal.GetLastWin32Error().ToString());
                return false;
            }

            // Create a read mail slot
            ReadSlot = NativeMethods.CreateMailslot("\\\\.\\mailslot\\sbbsexec\\rd" + _ClientThread.NodeInfo.Node.ToString(), XTRN_IO_BUF_LEN, 0, IntPtr.Zero);
            if (ReadSlot == IntPtr.Zero) {
                RMLog.Error("CreateMailslot() failed to create ReadSlot: " + Marshal.GetLastWin32Error().ToString());
                return false;
            }

            return true;
        }

        public unsafe void Run(string command, string parameters, int forceQuitDelay) {
            if (Helpers.Debug)
                _ClientThread.UpdateStatus("DEBUG: SBBSEXECNT launching " + command + " " + parameters);

            _Command = command;
            _Parameters = parameters;
            _ForceQuitDelay = forceQuitDelay;

            try {
                // Initialize variables
                if (Initialize()) {
                    // Start the door
                    if (StartProcess()) {
                        // Loop until something happens
                        bool DataTransferred = false;
                        while (!_ClientThread.QuitThread()) // NB: Was simply _Stop before
                        {
                            // Check for dropped carrier
                            if (UserHungUp()) {
                                return;
                            }

                            // Send data from user to door
                            DataTransferred = TransmitFromUserToDoor(out bool UserToDoorError);
                            if (UserToDoorError) {
                                return;
                            }

                            // Send data from door to user
                            DataTransferred |= TransmitFromDoorToUser(out bool DoorToUserError);
                            if (DoorToUserError) {
                                return;
                            }

                            // Checks to perform when there was no I/O
                            if (!DataTransferred) {
                                // Numer of loop iterations with no I/O
                                LoopsSinceIO++;

                                // Only check process termination after 300 milliseconds of no I/O
                                // to allow for last minute reception of output from DOS programs
                                if (LoopsSinceIO >= 3) {
                                    // Check if door is requesting a hangup (dropped DTR)
                                    if (DoorDroppedDTR()) {
                                        return;
                                    }

                                    // Check if door terminated 
                                    if (P.HasExited) {
                                        _ClientThread.UpdateStatus("External terminated with exit code: " + P.ExitCode);
                                        return;
                                    }

                                    // Check if door is trying to launch a native door
                                    CheckForW32DoorRun();
                                }

                                // Let's make sure the socket is up
                                // Sending will trigger a socket d/c detection
                                if (LoopsSinceIO % 300 == 0) {
                                    SendPing();
                                }

                                // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                                _ClientThread.NodeInfo.Connection.CanRead(100);
                            } else {
                                LoopsSinceIO = 0;
                            }
                        }
                    }
                }
            } finally {
                // Stop the process, if it's still running
                StopProcess();

                // Free unmanaged resources
                CleanUp();
            }
        }

        private void SendPing() {
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

        private bool StartProcess() {
            // Start the process
            string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "DOSXTRN.EXE");
            string Arguments = StringUtils.ExtractShortPathName(EnvFile) + " NT " + _ClientThread.NodeInfo.Node.ToString() + " " + SBBSEXEC_MODE_FOSSIL.ToString() + " " + LoopsBeforeYield.ToString();
            ProcessStartInfo PSI = new ProcessStartInfo(FileName, Arguments) {
                WindowStyle = _ClientThread.NodeInfo.Door.WindowStyle,
                WorkingDirectory = ProcessUtils.StartupPath,
            };
            P = RMProcess.Start(PSI);

            if (P == null) {
                RMLog.Error("Error launching " + FileName + " " + Arguments);
                return false;
            } else {
                return true;
            }
        }

        private void StopProcess() {
            try {
                // Terminate process if it hasn't closed yet
                if ((P != null) && !P.HasExited) {
                    RMLog.Error("Door still running, performing a force quit");
                    P.Kill();
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to perform force quit");
            }
        }

        private unsafe bool TransmitFromDoorToUser(out bool error) {
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
                                if (BufPtr >= BufBytes.Length)
                                    break;
                            }
                        }
                    }
                }

                // If we read something, write it to the user
                if (BufPtr > 0) {
                    _ClientThread.NodeInfo.Connection.WriteBytes(BufBytes, BufPtr);
                    error = false;
                    return true;
                } else {
                    error = false;
                    return false;
                }
            } else {
                error = false;
                return false;
            }
        }

        private unsafe bool TransmitFromUserToDoor(out bool error) {
            if (_ClientThread.NodeInfo.Connection.CanRead()) {
                // If our writeslot doesnt exist yet, create it
                if (WriteSlot == IntPtr.Zero) {
                    // Create A Write Mail Slot
                    WriteSlot = NativeMethods.CreateFile("\\\\.\\mailslot\\sbbsexec\\wr" + _ClientThread.NodeInfo.Node.ToString(), NativeMethods.FileAccess.GenericWrite, NativeMethods.FileShare.Read, IntPtr.Zero, NativeMethods.CreationDisposition.OpenExisting, NativeMethods.CreateFileAttributes.Normal, IntPtr.Zero);
                    int LastWin32Error = Marshal.GetLastWin32Error();
                    if (WriteSlot == IntPtr.Zero) {
                        RMLog.Error("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                        error = true;
                        return false;
                    } else if (WriteSlot.ToInt32() == -1) {
                        if (LastWin32Error == 2) {
                            // ERROR_FILE_NOT_FOUND - User must have hit a key really fast to trigger this!
                            RMLog.Warning("CreateFile() failed to find WriteSlot: \\\\.\\mailslot\\sbbsexec\\wr" + _ClientThread.NodeInfo.Node.ToString());
                            WriteSlot = IntPtr.Zero;
                            Thread.Sleep(100);
                        } else {
                            RMLog.Error("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                            error = true;
                            return false;
                        }
                    }
                }

                // Write the text to the program
                if (WriteSlot != IntPtr.Zero) {
                    byte[] BufBytes = _ClientThread.NodeInfo.Connection.PeekBytes();
                    bool Result = NativeMethods.WriteFile(WriteSlot, BufBytes, (uint)BufBytes.Length, out uint BytesWritten, null);
                    int LastWin32Error = Marshal.GetLastWin32Error();
                    if (Result) {
                        _ClientThread.NodeInfo.Connection.ReadBytes((int)BytesWritten);
                        error = false;
                        return true;
                    } else {
                        RMLog.Error("Error calling WriteFile(): " + LastWin32Error.ToString());
                        error = true;
                        return false;
                    }
                } else {
                    error = false;
                    return false;
                }
            } else {
                error = false;
                return false;
            }
        }

        private bool UserHungUp() {
            if (!_ClientThread.NodeInfo.Connection.Connected) {
                _ClientThread.UpdateStatus("User hung-up while in external program");
                NativeMethods.SetEvent(HungUpEvent);

                // Wait up to forceQuitDelay seconds for the process to terminate
                for (int i = 0; i < _ForceQuitDelay; i++) {
                    if (P.HasExited) {
                        return false;
                    }
                    P.WaitForExit(1000);
                }
                _ClientThread.UpdateStatus("Process still active after waiting " + _ForceQuitDelay.ToString() + " seconds");
                return true;
            }

            return false;
        }

        #region IDisposable Support
        private bool _Disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.
                if (WriteSlot != IntPtr.Zero)
                    NativeMethods.CloseHandle(WriteSlot);
                if (ReadSlot != IntPtr.Zero)
                    NativeMethods.CloseHandle(ReadSlot);
                if (HangUpEvent != IntPtr.Zero)
                    NativeMethods.CloseHandle(HangUpEvent);
                if (HungUpEvent != IntPtr.Zero)
                    NativeMethods.CloseHandle(HungUpEvent);

                _Disposed = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~RunDoorSBBSEXEC() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
