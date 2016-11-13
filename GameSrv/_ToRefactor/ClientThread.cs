/*
  GameSrv: A BBS Door Game Server
  Copyright (C) 2002-2014  Rick Parrish, R&M Software

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

 
  This file also contains portions of the Synchronet BBS software
  that have been ported from the original C code to C#.  Those portions are
  Copyright Rob Swindell - http://www.synchro.net/copyright.html
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using RandM.RMLib;
using Unix;
using System.Net.Sockets;
using System.Timers;
using System.Linq;

namespace RandM.GameSrv {
    public class ClientThread : RMThread {
        private Config _Config = new Config();
        private string _CurrentMenu;
        private Dictionary<char, MenuOption> _CurrentMenuOptions = new Dictionary<char, MenuOption>();
        private string _LastDisplayFile = "";
        private List<string> _Log = new List<string>();
        private object _LogLock = new object();
        private System.Timers.Timer _LogTimer = new System.Timers.Timer();
        private NodeInfo _NodeInfo = new NodeInfo();
        private Random _R = new Random();
        private string _Status = "";

        // TODOZ Add a Disconnect event of some sort to allow a sysop to disconnect another node
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<WhoIsOnlineEventArgs> WhoIsOnlineEvent = null; // TODOX Gotta be a better way to get

        public ClientThread() {
            _LogTimer.Interval = 60000; // 1 minute
            _LogTimer.Elapsed += LogTimer_Elapsed;
            _LogTimer.Start();
        }

        protected override void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // dispose managed state (managed objects).
                    if (_NodeInfo.Connection != null) _NodeInfo.Connection.Dispose();

                    if (_LogTimer != null) {
                        _LogTimer.Stop();
                        _LogTimer.Dispose();
                    }
                }

                // free unmanaged resources (unmanaged objects)
                // set large fields to null.

                // Call the base dispose
                base.Dispose(disposing);
            }
        }

        private void AddToLog(string logMessage) {
            lock (_LogLock) {
                _Log.Add(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + logMessage);
            }
        }

        public string Alias {
            get { return (_NodeInfo.User.Alias == null) ? "" : _NodeInfo.User.Alias; }
        }

        private bool AuthenticateRLogin() {
            string UserName = ((RLoginConnection)_NodeInfo.Connection).ServerUserName;
            string Password = ((RLoginConnection)_NodeInfo.Connection).ClientUserName;
            string TerminalType = ((RLoginConnection)_NodeInfo.Connection).TerminalType;

            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password)) {
                // RLogin requires both fields
                if (_Config.RLoginPromptForCredentialsOnFailedLogin) {
                    return AuthenticateTelnet();
                } else {
                    DisplayAnsi("RLOGIN_INVALID");
                    return false;
                }
            } else {
                // Check if we're requesting a door
                if (TerminalType.ToLower().StartsWith("xtrn=")) {
                    _NodeInfo.Door = new DoorInfo(TerminalType.Substring(5)); // 5 = strip off leading xtrn=
                    if (!_NodeInfo.Door.Loaded) {
                        // Requested door was not found
                        DisplayAnsi("RLOGIN_INVALID_XTRN");
                        return false;
                    }
                }

                // Check if the username is valid
                _NodeInfo.User = new UserInfo(UserName);
                if (_NodeInfo.User.Loaded) {
                    // Yep, so validate the password
                    if (_Config.RLoginValidatePassword) {
                        if (!_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper)) {
                            // Password is bad
                            if (_Config.RLoginPromptForCredentialsOnFailedLogin) {
                                return AuthenticateTelnet();
                            } else {
                                DisplayAnsi("RLOGIN_INVALID_PASSWORD");
                                return false;
                            }
                        }
                    }

                    // TODOX Add option where password is validated at the server-level instead of user level
                    //       That would allow someone to allow RLogin to anybody, but only if they had the right password
                } else if (_Config.RLoginPromptForCredentialsOnFailedLogin) {
                    // Nope, and we want to prompt for credentials when the alias isn't found
                    return AuthenticateTelnet();
                } else {
                    // Nope, so perform the new user process with given username and password
                    if (_Config.RLoginSkipNewUserPrompts) {
                        // We're going to just directly register the user since sysop wants to skip the prompts
                        if (IsBannedUser(UserName)) {
                            RMLog.Warning("RLogin user not allowed due to banned alias: '" + UserName + "'");
                            return false;
                        } else {
                            lock (Globals.RegistrationLock) {
                                if (_NodeInfo.User.StartRegistration(Alias)) {
                                    _NodeInfo.User.SetPassword(Password, _Config.PasswordPepper);
                                    Config C = new Config();
                                    _NodeInfo.User.UserId = C.NextUserId++;
                                    _NodeInfo.User.SaveRegistration();
                                    C.Save();
                                } else {
                                    // TODOZ This user lost the race (_NodeInfo.User.Loaded returned false above, so the user should have been free to register, but between then and .StartRegistration the alias was taken)
                                    //       Not sure what to do in this case, aside from log that it happened
                                    RMLog.Warning("RLogin user lost a race condition and couldn't register as '" + UserName + "'");
                                }
                            }
                        }
                    } else {
                        return Register(UserName, Password);
                    }
                }
            }

            // If we get here, login is ok
            return true;
        }

        private bool AuthenticateTelnet() {
            DisplayAnsi("LOGON_HEADER");

            int FailedAttempts = 0;
            while (FailedAttempts++ < 3) {
                // Get alias
                if (Globals.Debug) AddToLog("Entering Alias");
                UpdateStatus("Entering Alias");
                DisplayAnsi("LOGON_ENTER_ALIAS");
                if (Globals.Debug) _NodeInfo.Connection.ReadEvent += Connection_ReadEvent;
                string Alias = ReadLn().Trim();
                if (Globals.Debug) {
                    _NodeInfo.Connection.ReadEvent -= Connection_ReadEvent;
                    FlushLog();
                }

                // Make sure we should still proceed
                if (QuitThread()) return false;

                if (Alias.ToUpper() == "NEW") {
                    bool CanRegister = false;

                    if (string.IsNullOrEmpty(_Config.NewUserPassword)) {
                        CanRegister = true;
                    } else {
                        // Get new user password
                        UpdateStatus("Entering New User Password");
                        DisplayAnsi("NEWUSER_ENTER_NEWUSER_PASSWORD");
                        string NewUserPassword = ReadLn('*').Trim();
                        _NodeInfo.Connection.WriteLn();

                        CanRegister = NewUserPassword == _Config.NewUserPassword;
                    }

                    // Make sure we should still proceed
                    if (QuitThread()) return false;

                    if (CanRegister) {
                        // Trying to register as a new user
                        UpdateStatus("Registering as new user");
                        return Register();
                    } else {
                        UpdateStatus("Entered invalid newuser password");
                    }
                } else if (IsBannedUser(Alias)) {
                    //List<string> BannedIPs = new List<string>();

                    // Load existing banned ips, if file exists
                    //string BannedIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-ips.txt");
                    //if (File.Exists(BannedIPsFileName)) BannedIPs.AddRange(FileUtils.FileReadAllLines(BannedIPsFileName));

                    // Add new banned ip
                    //BannedIPs.Add(_NodeInfo.Connection.GetRemoteIP());

                    // Save updated banned ip list
                    //FileUtils.FileWriteAllLines(BannedIPsFileName, BannedIPs.ToArray());

                    // Add to temp ban list
                    Globals.AddTempIgnoredIP(_NodeInfo.Connection.GetRemoteIP());

                    RMLog.Warning("IP banned for trying to log in as " + Alias);
                    DisplayAnsi("USER_BANNED");
                    return false;
                } else if (!string.IsNullOrEmpty(Alias)) {
                    UpdateStatus("Logging on as " + Alias);

                    // Get password
                    UpdateStatus("Entering Password");
                    DisplayAnsi("LOGON_ENTER_PASSWORD");
                    string Password = ReadLn('*').Trim();

                    // Make sure we should still proceed
                    if (QuitThread()) return false;

                    UpdateStatus("Validating Credentials");
                    _NodeInfo.User = new UserInfo(Alias);
                    if (_NodeInfo.User.Loaded && (_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper))) {
                        // Successfully loaded user info
                        return true;
                    } else {
                        DisplayAnsi("LOGON_INVALID");
                    }
                }
            }

            // If we get here, we failed all 3 logon attempts
            DisplayAnsi("LOGON_FAILED");
            return false;
        }

        private void ClrScr() {
            switch (_NodeInfo.TerminalType) {
                case TerminalType.ANSI:
                    _NodeInfo.Connection.Write(Ansi.TextAttr(7) + Ansi.ClrScr() + Ansi.GotoXY(1, 1));
                    break;
                case TerminalType.ASCII:
                    _NodeInfo.Connection.Write("\r\n\x0C");
                    break;
                case TerminalType.RIP:
                    _NodeInfo.Connection.Write("\r\n!|*" + Ansi.TextAttr(7) + Ansi.ClrScr() + Ansi.GotoXY(1, 1));
                    break;
            }
        }

        private void Connection_ReadEvent(object sender, StringEventArgs e) {
            AddToLog("ReadEvent " + e.Text);
        }

        private void ConvertDoorSysToDoor32Sys(string doorSysPath) {
            string[] DoorSysLines = FileUtils.FileReadAllLines(doorSysPath);
            List<string> Door32SysLines = new List<string>()
            {
                "2", // Telnet
                _NodeInfo.Connection.Handle.ToString(), // Socket
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
            Directory.CreateDirectory(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString()));

            // Create string list
            List<string> Sl = new List<string>();

            // Create DOOR.SYS
            Sl.Clear();
            Sl.Add("COM1:");                                                    // 1 - Comm Port
            Sl.Add("57600");                                                    // 2 - Connection Baud Rate
            Sl.Add("8");                                                        // 3 - Parity
            Sl.Add(_NodeInfo.Node.ToString());                                  // 4 - Current Node Number
            Sl.Add("57600");                                                    // 5 - Locked Baud Rate
            Sl.Add("Y");                                                        // 6 - Screen Display
            Sl.Add("Y");                                                        // 7 - Printer Toggle
            Sl.Add("Y");                                                        // 8 - Page Bell
            Sl.Add("Y");                                                        // 9 - Caller Alarm
            Sl.Add(_NodeInfo.User.Alias);                                       // 10 - User's Real Name
            Sl.Add("City, State");                                              // 11 - User's Location
            Sl.Add("555-555-5555");                                             // 12 - User's Home Phone #
            Sl.Add("555-555-5555");                                             // 13 - User's Work Phone #
            Sl.Add("PASSWORD");                                                 // 14 - User's Password
            Sl.Add(_NodeInfo.User.AccessLevel.ToString());                      // 15 - User's Access Level
            Sl.Add("1");                                                        // 16 - User's Total Calls
            Sl.Add("00/00/00");                                                 // 17 - User's Last Call Date
            Sl.Add(SecondsLeft().ToString());                                   // 18 - Users's Seconds Left This Call
            Sl.Add(MinutesLeft().ToString());                                   // 19 - User's Minutes Left This Call (I love redundancy!)
            Sl.Add("GR");                                                       // 20 - Graphics Mode GR=Graphics, NG=No Graphics, 7E=7-bit
            Sl.Add("24");                                                       // 21 - Screen Length
            Sl.Add("N");                                                        // 22 - Expert Mode
            Sl.Add("");                                                         // 23 - Conferences Registered In
            Sl.Add("");                                                         // 24 - Conference Exited To Door From
            Sl.Add("00/00/00");                                                 // 25 - User's Expiration Date
            Sl.Add((_NodeInfo.User.UserId - 1).ToString());                     // 26 - User's Record Position (0 based)
            Sl.Add("Z");                                                        // 27 - User's Default XFer Protocol
            Sl.Add("0");                                                        // 28 - Total Uploads
            Sl.Add("0");                                                        // 29 - Total Downloads
            Sl.Add("0");                                                        // 30 - Total Downloaded Today (kB)
            Sl.Add("0");                                                        // 31 - Daily Download Limit (kB)
            Sl.Add("00/00/00");                                                 // 32 - User's Birthday
            Sl.Add(StringUtils.ExtractShortPathName(ProcessUtils.StartupPath)); // 33 - Path To User File
            Sl.Add(StringUtils.ExtractShortPathName(ProcessUtils.StartupPath)); // 34 - Path To GEN Directory
            Sl.Add(_Config.SysopFirstName + " " + _Config.SysopLastName);       // 35 - SysOp's Name
            Sl.Add(_NodeInfo.User.Alias);                                       // 36 - User's Alias
            Sl.Add("00:00");                                                    // 37 - Next Event Time
            Sl.Add("Y");                                                        // 38 - Error Correcting Connection
            Sl.Add(_NodeInfo.TerminalType == TerminalType.ASCII ? "N" : "Y");   // 39 - ANSI Supported
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
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOOR32.SYS
            Sl.Clear();
            Sl.Add("2");                                                            // 1 - Comm Type (0=Local, 1=Serial, 2=Telnet)
            Sl.Add(_NodeInfo.Connection.Handle.ToString());                         // 2 - Comm Or Socket Handle
            Sl.Add("57600");                                                        // 3 - Baud Rate
            Sl.Add(ProcessUtils.ProductName + " v" + GameSrv.Version);              // 4 - BBSID (Software Name & Version
            Sl.Add(_NodeInfo.User.UserId.ToString());                               // 5 - User's Record Position (1 based)
            Sl.Add(_NodeInfo.User.Alias);                                           // 6 - User's Real Name
            Sl.Add(_NodeInfo.User.Alias);                                           // 7 - User's Handle/Alias
            Sl.Add(_NodeInfo.User.AccessLevel.ToString());                          // 8 - User's Access Level
            Sl.Add(MinutesLeft().ToString());                                       // 9 - User's Time Left (In Minutes)
            switch (_NodeInfo.TerminalType)                                         // 10 - Emulation (0=Ascii, 1=Ansi, 2=Avatar, 3=RIP, 4=MaxGfx)
            {
                case TerminalType.ANSI: Sl.Add("1"); break;
                case TerminalType.ASCII: Sl.Add("0"); break;
                case TerminalType.RIP: Sl.Add("3"); break;
            }
            Sl.Add(_NodeInfo.Node.ToString());                                      // 11 - Current Node Number
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door32.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOORFILE.SR
            Sl.Clear();
            Sl.Add(_NodeInfo.User.Alias);                                       // Complete name or handle of user
            Sl.Add(_NodeInfo.TerminalType == TerminalType.ASCII ? "0" : "1");   // ANSI status:  1 = yes, 0 = no, -1 = don't know
            Sl.Add("1");                                                        // IBM Graphic characters:  1 = yes, 0 = no, -1 = unknown
            Sl.Add("24");                                                       // Page length of screen, in lines.  Assume 25 if unknown
            Sl.Add("57600");                                                    // Baud Rate:  300, 1200, 2400, 9600, 19200, etc.
            Sl.Add("1");                                                        // Com Port:  1, 2, 3, or 4; 0 if local.
            Sl.Add(MinutesLeft().ToString());                                   // Time Limit:  (in minutes); -1 if unknown.
            Sl.Add(_NodeInfo.User.Alias);                                       // Real name (the same as line 1 if not known)
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "doorfile.sr"), String.Join("\r\n", Sl.ToArray()));

            // Create DORINFO.DEF
            Sl.Clear();
            Sl.Add(_Config.BBSName);                                           // 1 - BBS Name
            Sl.Add(_Config.SysopFirstName);                                    // 2 - Sysop's First Name
            Sl.Add(_Config.SysopLastName);                                     // 3 - Sysop's Last Name
            Sl.Add("COM1");                                                    // 4 - Comm Number in COMxxx Form
            Sl.Add("57600 BAUD,N,8,1");                                        // 5 - Baud Rate in 57600 BAUD,N,8,1 Form
            Sl.Add("0");                                                       // 6 - Networked?
            Sl.Add(_NodeInfo.User.Alias);                                      // 7 - User's First Name / Alias
            Sl.Add("");                                                        // 8 - User's Last Name
            Sl.Add("City, State");                                             // 9 - User's Location (City, State, etc.)
            Sl.Add(_NodeInfo.TerminalType == TerminalType.ASCII ? "0" : "1");  // 10 - User's Emulation (0=Ascii, 1=Ansi)
            Sl.Add(_NodeInfo.User.AccessLevel.ToString());                     // 11 - User's Access Level
            Sl.Add(MinutesLeft().ToString());                                  // 12 - User's Time Left (In Minutes)
            Sl.Add("1");                                                       // 13 - Fossil?
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo1.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo" + _NodeInfo.Node.ToString() + ".def"), String.Join("\r\n", Sl.ToArray()));
        }

        private void DeleteNodeDirectory() {
            if (!Globals.Debug) {
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door.sys"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door32.sys"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "doorfile.sr"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo.def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo1.def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo" + _NodeInfo.Node.ToString() + ".def"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosemu.log"));
                FileUtils.FileDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "external.bat"));
                FileUtils.DirectoryDelete(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString()));
            }
        }

        public bool DisplayAnsi(string fileName) {
            return DisplayAnsi(fileName, false);
        }

        public bool DisplayAnsi(string fileName, bool pauseAtEnd) {
            if (string.IsNullOrEmpty(fileName)) {
                return false;
            } else {
                List<string> FilesToCheck = new List<string>();
                if (_NodeInfo.TerminalType == TerminalType.RIP) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".rip"));
                if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".ans"));
                FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".asc"));

                for (int i = 0; i < FilesToCheck.Count; i++) {
                    if (File.Exists(FilesToCheck[i])) return DisplayFile(FilesToCheck[i], false, pauseAtEnd, false);
                }
            }

            return false;
        }

        private void DisplayCurrentMenu() {
            // Generate list of possible menus to look for, prioritized by access level specific and terminal type specific
            List<string> FilesToCheck = new List<string>();

            // Access level specific
            if (_NodeInfo.TerminalType == TerminalType.RIP) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".rip"));
            if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".ans"));
            FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".asc"));

            // No specified access level
            if (_NodeInfo.TerminalType == TerminalType.RIP) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".rip"));
            if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".ans"));
            FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".asc"));

            // Check if any of the above files exists
            for (int i = 0; i < FilesToCheck.Count; i++) {
                if (File.Exists(FilesToCheck[i])) {
                    DisplayFile(FilesToCheck[i], true, false, false);
                    return;
                }
            }

            // None of the files existed, displayed canned menu
            if (_NodeInfo.TerminalType == TerminalType.ASCII) {
                // Clear the screen
                ClrScr();

                // Display menu header
                _NodeInfo.Connection.WriteLn("ÄÄÄ´°±² " + _CurrentMenu.ToUpper() + " MENU ²±°ÃÄÄÄ");
                _NodeInfo.Connection.WriteLn("ÚÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄ¿");
                _NodeInfo.Connection.WriteLn("ÃÄÅÄÉÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ»ÄÅÄ´");

                // Display current menu options
                int Row = 1;
                _NodeInfo.Connection.Write((0 == Row % 2) ? "ÃÄÅÄº " : "ÃÄÅÄº ");
                _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenu.ToUpper() + " MENU OPTIONS", ' ', 30 * 2 + 8));
                _NodeInfo.Connection.WriteLn((0 == Row++ % 2) ? "ºÄÅÄ´" : "ºÄÅÄ´");
                Row = DisplayCurrentMenuOptions(Row);

                // Display menu footer
                if (0 == Row % 2) {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄÈÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼ÄÅÄ´");
                    _NodeInfo.Connection.WriteLn("ÀÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÙ");
                } else {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄÈÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼ÄÅÄ´");
                    _NodeInfo.Connection.WriteLn("ÀÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÙ");
                }

                // Still part of the footer
                _NodeInfo.Connection.Write("[" + StringUtils.SecToHMS(SecondsLeft()) + "] Select: ");
            } else {
                // Clear the screen
                ClrScr();

                // Display menu header
                _NodeInfo.Connection.WriteLn("\x1B" + "[1;30mÄ" + "\x1B" + "[0mÄ" + "\x1B" + "[1mÄ´" + "\x1B" + "[0;34m°±²" + "\x1B" + "[1;44;37m " + _CurrentMenu.ToUpper() + " MENU " + "\x1B" + "[0;34m²±°" + "\x1B" + "[1;37mÃÄ" + "\x1B" + "[0mÄ" + "\x1B" + "[1;30mÄ");
                _NodeInfo.Connection.WriteLn("\x1B" + "[37mÚÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m¿");
                _NodeInfo.Connection.WriteLn("\x1B" + "[1mÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mÉÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ" + "\x1B" + "[1;30m»Ä" + "\x1B" + "[37mÅÄ´");

                // Display current menu options
                int Row = 1;
                _NodeInfo.Connection.Write((0 == Row % 2) ? "ÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mº " : "\x1B" + "[1mÃÄÅÄ" + "\x1B" + "[0mº " + "\x1B" + "[1;37m");
                _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenu.ToUpper() + " MENU OPTIONS", ' ', 30 * 2 + 8));
                _NodeInfo.Connection.WriteLn((0 == Row++ % 2) ? "\x1B" + "[1;30mºÄ" + "\x1B" + "[37mÅÄ´" : "\x1B" + "[1;30mºÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                Row = DisplayCurrentMenuOptions(Row);

                // Display menu footer
                if (0 == Row % 2) {
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mÈ" + "\x1B" + "[1;30mÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼Ä" + "\x1B" + "[37mÅÄ´");
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÀ" + "\x1B" + "[0mÄ" + "\x1B" + "[1mÁÄ" + "\x1B" + "[30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[37mÄ" + "\x1B" + "[0mÙ");
                } else {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄ" + "\x1B" + "[0mÈ" + "\x1B" + "[1;30mÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼Ä" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÀ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[37mÄÙ");
                }

                // Still part of the footer
                _NodeInfo.Connection.Write("\x1B" + "[1;30m[" + StringUtils.SecToHMS(SecondsLeft()) + "]" + "\x1B" + "[37m Select: " + "\x1B" + "[0m");
            }
        }

        private int DisplayCurrentMenuOptions(int row) {
            ArrayList HotKeys = new ArrayList();
            foreach (KeyValuePair<char, MenuOption> KV in _CurrentMenuOptions) {
                HotKeys.Add(KV.Key);
            }
            HotKeys.Sort();

            int i = 0;
            while (i <= HotKeys.Count - 1) {
                if (_NodeInfo.TerminalType == TerminalType.ASCII) {
                    // Display left side
                    _NodeInfo.Connection.Write((0 == row % 2) ? "ÃÄÅÄº " : "ÃÄÅÄº ");

                    // Display left hotkey and description
                    _NodeInfo.Connection.Write(HotKeys[i].ToString() + "] ");
                    _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");

                    // Check if that was the last item, or if there is one more
                    if (i >= HotKeys.Count) {
                        // Display blank space (that was the last item)
                        _NodeInfo.Connection.Write(new string(' ', 30 + 4));
                    } else {
                        // Display right hotkey and description
                        _NodeInfo.Connection.Write(HotKeys[i].ToString() + "] ");
                        _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");
                    }

                    // Display right side
                    _NodeInfo.Connection.WriteLn((0 == row++ % 2) ? "ºÄÅÄ´" : "ºÄÅÄ´");
                } else {
                    // Display left side
                    _NodeInfo.Connection.Write((0 == row % 2) ? "ÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mº " : "\x1B" + "[1mÃÄÅÄ" + "\x1B" + "[0mº ");

                    // Display left hotkey and description
                    _NodeInfo.Connection.Write("\x1B" + "[1;33m" + HotKeys[i].ToString() + "\x1B" + "[1;30m] " + "\x1B" + "[0;37m");
                    _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");

                    // Check if that was the last item, or if there is one more
                    if (i >= HotKeys.Count) {
                        // Display blank space (that was the last item)
                        _NodeInfo.Connection.Write(new string(' ', 30 + 4));
                    } else {
                        // Display right hotkey and description
                        _NodeInfo.Connection.Write("\x1B" + "[1;33m" + HotKeys[i].ToString() + "\x1B" + "[1;30m] " + "\x1B" + "[0;37m");
                        _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");
                    }

                    // Display right side
                    _NodeInfo.Connection.WriteLn((0 == row++ % 2) ? "\x1B" + "[1;30mºÄ" + "\x1B" + "[37mÅÄ´" : "\x1B" + "[1;30mºÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                }
            }

            return row;
        }

        private bool DisplayFile(string fileName, bool clearAtBeginning, bool pauseAtEnd, bool pauseAfter24) {
            try {
                _LastDisplayFile = fileName;

                if (clearAtBeginning) {
                    // Clear the screen
                    ClrScr();
                }

                // Translate the slashes accordingly
                if (OSUtils.IsWindows) {
                    fileName = fileName.Replace("/", "\\");
                } else if (OSUtils.IsUnix) {
                    fileName = fileName.Replace("\\", "/");
                }

                // If file starts with @, then it's an index file and we want to choose a random row from it
                if (fileName.StartsWith("@")) {
                    // Strip @
                    fileName = fileName.Substring(1);

                    if (File.Exists(fileName)) {
                        // Read index file
                        string[] FileNames = FileUtils.FileReadAllLines(fileName, RMEncoding.Ansi);

                        // Pick random filename
                        fileName = FileNames[_R.Next(0, FileNames.Length)];
                        _LastDisplayFile = fileName;

                        // Translate the slashes accordingly
                        if (OSUtils.IsWindows) {
                            fileName = fileName.Replace("/", "\\");
                        } else if (OSUtils.IsUnix) {
                            fileName = fileName.Replace("\\", "/");
                        }
                    } else {
                        return false;
                    }
                }

                // Check if we need to pick an extension based on the user's terminal type (do this if the file doesn't exist, and doesn't have an extension)
                if (!File.Exists(fileName) && !Path.HasExtension(fileName)) {
                    List<string> FileNamesWithExtension = new List<string>();
                    if (_NodeInfo.TerminalType == TerminalType.RIP) {
                        FileNamesWithExtension.Add(fileName + ".rip");
                    }
                    if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) {
                        FileNamesWithExtension.Add(fileName + ".ans");
                    }
                    FileNamesWithExtension.Add(fileName + ".asc");

                    foreach (string FileNameWithExtension in FileNamesWithExtension) {
                        if (File.Exists(FileNameWithExtension)) {
                            fileName = FileNameWithExtension;
                            break;
                        }
                    }
                }

                if (File.Exists(fileName)) {
                    string TranslatedText = TranslateMCI(FileUtils.FileReadAllText(fileName, RMEncoding.Ansi), fileName);

                    // Check if we need to filter out a SAUCE record
                    if (TranslatedText.Contains("\x1A")) TranslatedText = TranslatedText.Substring(0, TranslatedText.IndexOf("\x1A"));

                    // Check if the file contains manual pauses
                    if (TranslatedText.Contains("{PAUSE}")) {
                        // When the file contains {PAUSE} statements then pauseAfter24 is ignored
                        string[] Pages = TranslatedText.Split(new string[] { "{PAUSE}" }, StringSplitOptions.None);
                        for (int i = 0; i < Pages.Length; i++) {
                            _NodeInfo.Connection.Write(Pages[i]);
                            if (i < (Pages.Length - 1)) ReadChar();
                        }
                    } else {
                        // Check if we want to pause every 24 lines
                        if (pauseAfter24) {
                            // Yep, so split the file on CRLF
                            string[] TranslatedLines = TranslatedText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            if (TranslatedLines.Length <= 24) {
                                // But if the file is less than 24 lines, then just send it all at once
                                _NodeInfo.Connection.Write(TranslatedText);
                            } else {
                                // More than 24 lines, do it the hard way
                                for (int i = 0; i < TranslatedLines.Length; i++) {
                                    _NodeInfo.Connection.Write(TranslatedLines[i]);
                                    if (i < TranslatedLines.Length - 1) _NodeInfo.Connection.WriteLn();

                                    // TODOZ This doesn't work when a single line of output is spread across multiple lines of input
                                    //      Should somehow count the number of lines scrolled, and pause after 24
                                    if ((i % 24 == 23) && (i < TranslatedLines.Length - 1)) {
                                        _NodeInfo.Connection.Write("<more>");
                                        var Ch = ReadChar();
                                        _NodeInfo.Connection.Write("\b\b\b\b\b\b      \b\b\b\b\b\b");
                                        if (Ch.ToString().ToUpper() == "Q") return true;
                                    }
                                }
                            }
                        } else {
                            // Nope, so just send it as is.
                            _NodeInfo.Connection.Write(TranslatedText);
                        }
                    }

                    if (pauseAtEnd) {
                        ReadChar();
                        ClrScr();
                    }
                    return true;
                } else {
                    return false;
                }
            } catch (IOException ioex) {
                RMLog.Exception(ioex, "Unable to display '" + fileName + "'");
                return false;
            }
        }

        protected override void Execute() {
            try {
                // Make telnet connections convert CRLF to CR
                _NodeInfo.Connection.LineEnding = "\r";
                _NodeInfo.Connection.StripLF = true;

                _NodeInfo.Door = new DoorInfo("RUNBBS");
                if (_NodeInfo.Door.Loaded) {
                    _NodeInfo.User.Alias = "Anonymous";
                    NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, "Running RUNBBS.INI process", NodeEventType.LogOn));

                    _NodeInfo.SecondsThisSession = 86400; // RUNBBS.BAT can run for 24 hours
                    RunDoor();
                } else {
                    // Handle authentication based on the connection type
                    bool Authed = false;
                    switch (_NodeInfo.ConnectionType) {
                        case ConnectionType.RLogin: Authed = AuthenticateRLogin(); break;
                        case ConnectionType.Telnet: Authed = AuthenticateTelnet(); break;
                        case ConnectionType.WebSocket: Authed = AuthenticateTelnet(); break;
                    }

                    if ((Authed) && (!QuitThread())) {
                        // Update the user's time remaining
                        _NodeInfo.UserLoggedOn = true;
                        _NodeInfo.SecondsThisSession = _Config.TimePerCall * 60;
                        NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, "Logging on", NodeEventType.LogOn));

                        // Check if RLogin is requesting to launch a door immediately via the xtrn= command
                        if ((_NodeInfo.Door != null) && _NodeInfo.Door.Loaded) {
                            RunDoor();
                            Thread.Sleep(2500);
                        } else {
                            // Do the logon process
                            UpdateStatus("Running Logon Process");
                            HandleLogOnProcess();

                            // Make sure we should still proceed
                            if (QuitThread()) return;

                            // Do the logoff process
                            UpdateStatus("Running Logoff Process");
                            HandleLogOffProcess();

                            // Make sure we should still proceed
                            if (QuitThread()) return;

                            DisplayAnsi("LOGOFF");
                            Thread.Sleep(2500);
                        }
                    }
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in ClientThread::Execute()");
            } finally {
                // Try to close the connection
                try { _NodeInfo.Connection.Close(); } catch { /* Ignore */ }

                // Try to free the node
                try { NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, "Logging off", NodeEventType.LogOff)); } catch { /* Ignore */ }

                FlushLog();
            }
        }

        private void FlushLog() {
            lock (_LogLock) {
                // Flush log to disk
                if (_Log.Count > 0) {
                    try {
                        FileUtils.FileAppendAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "logs", "node" + _NodeInfo.Node.ToString() + ".log"), string.Join(Environment.NewLine, _Log.ToArray()) + Environment.NewLine);
                        _Log.Clear();
                    } catch (Exception ex) {
                        RMLog.Exception(ex, "Unable to update node" + _NodeInfo.Node.ToString() + ".log");
                    }
                }
            }
        }

        private void GetCurrentMenu() {
            // Clear the dictionary
            _CurrentMenuOptions.Clear();

            // Find out what hotkeys the menu has
            string[] HotKeys = MenuOption.GetHotKeys(_CurrentMenu);

            // Get the data for each hotkey
            for (int i = 0; i < HotKeys.Length; i++) {
                char HotKey = HotKeys[i].ToString().ToUpper()[0];

                try {
                    MenuOption MO = new MenuOption(_CurrentMenu, HotKey);
                    if ((MO.Loaded) && (_NodeInfo.User.AccessLevel >= MO.RequiredAccess)) {
                        bool CanAdd = true;

                        // If the option is a door, check if the door can be run on the current platform
                        if (MO.Action == Action.RunDoor) {
                            DoorInfo DI = new DoorInfo(MO.Parameters);
                            if (DI.Loaded) {
                                // Determine how to run the door
                                if (DI.Platform == OSUtils.Platform.DOS) {
                                    if (OSUtils.IsWindows) {
                                        if (ProcessUtils.Is64BitOperatingSystem) {
                                            // DOS doors are OK on 64bit Windows if DOSBox is installed
                                            CanAdd = Globals.IsDOSBoxInstalled();
                                        } else {
                                            // DOS doors are OK on 32bit Windows
                                            CanAdd = true;
                                        }
                                    } else if (OSUtils.IsUnix) {
                                        // DOS doors are OK on Linux if DOSEMU is installed
                                        CanAdd = Globals.IsDOSEMUInstalled();
                                    } else {
                                        // DOS doors are not OK on unknown platforms
                                        CanAdd = false;
                                    }
                                } else if (DI.Platform == OSUtils.Platform.Linux) {
                                    // Linux doors are OK on Linux
                                    CanAdd = OSUtils.IsUnix;
                                } else if (DI.Platform == OSUtils.Platform.Windows) {
                                    // Windows doors are OK on Windows
                                    CanAdd = OSUtils.IsWindows;
                                }
                            } else {
                                // Can't load door, so don't display it
                                CanAdd = false;
                            }
                        }

                        if (CanAdd) _CurrentMenuOptions.Add(HotKey, MO);
                    }
                } catch (ArgumentException aex) {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RMLog.Exception(aex, "Unable to load '" + _CurrentMenu + "' menu option for '" + HotKey + "'");
                }
            }
        }

        private void HandleLogOffProcess() {
            // Get all the sections from the ini file and sort them
            string[] Processes = LogOffProcess.GetProcesses();

            // Loop through the options, and run the ones we allow here
            bool ExitFor = false;
            for (int i = 0; i < Processes.Length; i++) {
                try {
                    LogOffProcess LP = new LogOffProcess(Processes[i]);
                    if ((LP.Loaded) && (!QuitThread())) {
                        switch (LP.Action) {
                            case Action.Disconnect:
                            case Action.DisplayFile:
                            case Action.DisplayFileMore:
                            case Action.DisplayFilePause:
                            case Action.Pause:
                            case Action.RunDoor:
                                MenuOption MO = new MenuOption("", '\0');
                                MO.Action = LP.Action;
                                MO.Name = LP.Name;
                                MO.Parameters = LP.Parameters;
                                MO.RequiredAccess = LP.RequiredAccess;
                                ExitFor = HandleMenuOption(MO);
                                break;
                        }
                        if (ExitFor) {
                            break;
                        }
                    }
                } catch (ArgumentException aex) {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RMLog.Exception(aex, "Error during logoff process '" + Processes[i] + "'");
                }
            }
        }

        private void HandleLogOnProcess() {
            string[] Processes = LogOnProcess.GetProcesses();
            Action LastAction = Action.None;

            // Loop through the options, and run the ones we allow here
            bool ExitFor = false;
            for (int i = 0; i < Processes.Length; i++) {
                try {
                    LogOnProcess LP = new LogOnProcess(Processes[i]);
                    if ((LP.Loaded) && (!QuitThread())) {
                        LastAction = LP.Action;

                        switch (LP.Action) {
                            case Action.Disconnect:
                            case Action.DisplayFile:
                            case Action.DisplayFileMore:
                            case Action.DisplayFilePause:
                            case Action.MainMenu:
                            case Action.Pause:
                            case Action.RunDoor:
                                MenuOption MO = new MenuOption("", '\0');
                                MO.Action = LP.Action;
                                MO.Name = LP.Name;
                                MO.Parameters = LP.Parameters;
                                MO.RequiredAccess = LP.RequiredAccess;
                                ExitFor = HandleMenuOption(MO);
                                break;
                        }
                        if (ExitFor) {
                            break;
                        }
                    }
                } catch (Exception ex) {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RMLog.Exception(ex, "Error during logon process '" + Processes[i] + "'");
                }
            }
        }

        private bool HandleMenuOption(MenuOption menuOption) {
            if (_NodeInfo.User.AccessLevel >= menuOption.RequiredAccess) {
                switch (menuOption.Action) {
                    case Action.ChangeMenu:
                        UpdateStatus("Changing to " + menuOption.Parameters.ToUpper() + " menu");
                        _CurrentMenu = menuOption.Parameters.ToUpper();
                        return false;
                    case Action.Disconnect:
                        UpdateStatus("Disconnecting");
                        _NodeInfo.Connection.Close();
                        return true;
                    case Action.DisplayFile:
                        UpdateStatus("Displaying " + menuOption.Parameters);
                        DisplayFile(menuOption.Parameters, true, false, false);
                        if (menuOption.Parameters != _LastDisplayFile) UpdateStatus(" displayed " + _LastDisplayFile);
                        return false;
                    case Action.DisplayFileMore:
                        UpdateStatus("Displaying " + menuOption.Parameters + " (with more)");
                        DisplayFile(menuOption.Parameters, true, true, true);
                        if (menuOption.Parameters != _LastDisplayFile) UpdateStatus(" displayed " + _LastDisplayFile + " (with more)");
                        return false;
                    case Action.DisplayFilePause:
                        UpdateStatus("Displaying " + menuOption.Parameters + " (with pause)");
                        DisplayFile(menuOption.Parameters, true, true, false);
                        if (menuOption.Parameters != _LastDisplayFile) UpdateStatus(" displayed " + _LastDisplayFile + " (with pause)");
                        return false;
                    case Action.LogOff:
                        UpdateStatus("Logging off");
                        return true;
                    case Action.MainMenu:
                        UpdateStatus("Changing to " + menuOption.Parameters.ToUpper() + " menu");
                        _CurrentMenu = menuOption.Parameters.ToUpper();
                        MainMenu();
                        return true;
                    case Action.Pause:
                        UpdateStatus("Pausing for " + menuOption.Parameters + " seconds");
                        Thread.Sleep(int.Parse(menuOption.Parameters));
                        return false;
                    case Action.RunDoor:
                        UpdateStatus("Running " + menuOption.Parameters);
                        RunDoor(menuOption.Parameters);
                        return false;
                    case Action.Telnet:
                        UpdateStatus("Telnetting to " + menuOption.Parameters);
                        Telnet(menuOption.Parameters);
                        return false;
                }
            }

            return false;
        }

        public string IPAddress {
            get { return _NodeInfo.Connection.GetRemoteIP(); }
        }

        private bool IsBannedUser(string alias) {
            try {
                alias = alias.Trim().ToLower();
                if (string.IsNullOrEmpty(alias)) return false; // Don't ban for blank inputs

                string BannedUsersFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-users.txt");
                if (File.Exists(BannedUsersFileName)) {
                    string[] BannedUsers = FileUtils.FileReadAllLines(BannedUsersFileName);
                    foreach (string BannedUser in BannedUsers) {
                        if (BannedUser.StartsWith(";")) continue;

                        if (BannedUser.Trim().ToLower() == alias) return true;
                    }
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to validate alias against banned-users.txt");
            }

            // If we get here, it's an OK name
            return false;
        }

        void LogTimer_Elapsed(object sender, ElapsedEventArgs e) {
            _LogTimer.Stop();
            FlushLog();
            _LogTimer.Start();
        }

        private void MainMenu() {
            bool ExitWhile = false;
            while ((!ExitWhile) && (!QuitThread())) {
                // Get current menu options
                GetCurrentMenu();

                // Show menu options
                DisplayCurrentMenu();

                // Send node event message
                UpdateStatus("At " + _CurrentMenu.ToUpper() + " menu");

                string HotKey = ReadChar().ToString().ToUpper();
                if (!string.IsNullOrEmpty(HotKey) && !QuitThread()) {
                    if (_CurrentMenuOptions.ContainsKey(HotKey[0])) {
                        MenuOption MO = _CurrentMenuOptions[HotKey[0]];
                        switch (MO.Action) {
                            case Action.ChangeMenu:
                            case Action.Disconnect:
                            case Action.DisplayFile:
                            case Action.DisplayFileMore:
                            case Action.DisplayFilePause:
                            case Action.LogOff:
                            case Action.RunDoor:
                            case Action.Telnet:
                                ExitWhile = HandleMenuOption(MO);
                                break;
                        }
                    }
                }
            }
        }

        private int MinutesLeft() {
            return SecondsLeft() / 60;
        }

        private void OnDoorWait(object sender, RMProcessStartAndWaitEventArgs e) {
            e.Stop = QuitThread();
        }

        private bool QuitThread() {
            if (_Stop) return true;
            if (!_NodeInfo.Connection.Connected) return true;
            if (_NodeInfo.Connection.ReadTimedOut) return true;
            if (SecondsLeft() <= 0) return true;
            return false;
        }

        private char? ReadChar() {
            char? Result = null;

            Result = _NodeInfo.Connection.ReadChar(ReadTimeout());
            if (Result == null) {
                if ((!_Stop) && (_NodeInfo.Connection.Connected)) {
                    if (SecondsLeft() > 0) {
                        // User has time left so they timed out
                        DisplayAnsi("EXCEEDED_IDLE_LIMIT");
                        Thread.Sleep(2500);
                        _NodeInfo.Connection.Close();
                    } else {
                        // User has no time left, so they exceeded the call limit
                        DisplayAnsi("EXCEEDED_CALL_LIMIT");
                        Thread.Sleep(2500);
                        _NodeInfo.Connection.Close();
                    }
                }
            }

            return Result;
        }

        private string ReadLn() {
            // Call the main SocketReadLn() indicating no password character
            return ReadLn('\0');
        }

        private string ReadLn(char passwordChar) {
            string Result = _NodeInfo.Connection.ReadLn(passwordChar, ReadTimeout());
            if ((_NodeInfo.Connection.ReadTimedOut) && (!_Stop) && (_NodeInfo.Connection.Connected)) {
                if (SecondsLeft() > 0) {
                    // User has time left so they timed out
                    DisplayAnsi("EXCEEDED_IDLE_LIMIT");
                    Thread.Sleep(2500);
                    _NodeInfo.Connection.Close();
                } else {
                    // User has no time left, so they exceeded the call limit
                    DisplayAnsi("EXCEEDED_CALL_LIMIT");
                    Thread.Sleep(2500);
                    _NodeInfo.Connection.Close();
                }
            }

            return Result;
        }

        private int ReadTimeout() {
            return Math.Min(5 * 60, SecondsLeft()) * 1000;
        }

        private bool Register() {
            return Register(null, null);
        }

        private bool Register(string defaultUserName, string defaultPassword) {
            bool Registered = false;

            try {
                DisplayAnsi("NEWUSER_HEADER");

                // Get an alias
                string Alias = "";
                if (string.IsNullOrEmpty(defaultUserName)) {
                    GetAlias:
                    Alias = "";
                    while ((string.IsNullOrEmpty(Alias)) || (Alias.ToUpper() == "NEW")) {
                        DisplayAnsi("NEWUSER_ENTER_ALIAS");
                        Alias = ReadLn().Trim();
                        if (QuitThread()) return false;
                    }

                    // StartRegistration will check if the alias already exists, and if not, reserve it so there's no race condition for two people registering at the same time and both wanting the same alias
                    if (IsBannedUser(Alias) || !_NodeInfo.User.StartRegistration(Alias)) {
                        // Alias has already been taken
                        DisplayAnsi("NEWUSER_ENTER_ALIAS_DUPLICATE");
                        goto GetAlias;
                    }
                } else {
                    Alias = defaultUserName;
                }

                // Get their password
                GetPassword:
                string Password = "";
                if (string.IsNullOrEmpty(defaultPassword)) {
                    while (string.IsNullOrEmpty(Password)) {
                        DisplayAnsi("NEWUSER_ENTER_PASSWORD");
                        Password = ReadLn('*').Trim();
                        if (QuitThread()) return false;
                    }
                    _NodeInfo.User.SetPassword(Password, _Config.PasswordPepper);

                    // Confirm their password
                    DisplayAnsi("NEWUSER_ENTER_PASSWORD_CONFIRM");
                    Password = ReadLn('*').Trim();
                    if (QuitThread()) return false;

                    if (!_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper)) {
                        DisplayAnsi("NEWUSER_ENTER_PASSWORD_MISMATCH");
                        goto GetPassword;
                    }
                } else {
                    Password = defaultPassword;
                }

                // Loop through the questions
                string[] Questions = NewUserQuestion.GetQuestions();
                for (int i = 0; i < Questions.Length; i++) {
                    NewUserQuestion Question = new NewUserQuestion(Questions[i]);

                    GetAnswer:
                    // Display prompt
                    if (DisplayAnsi("NEWUSER_ENTER_" + Questions[i])) {
                        // Get answer
                        string Answer = ReadLn().Trim();
                        if (QuitThread()) return false;

                        // Check if answer is required
                        if ((Question.Required) && (string.IsNullOrEmpty(Answer))) goto GetAnswer;

                        // Check if answer requires validation
                        if (!string.IsNullOrEmpty(Answer)) {
                            bool Valid = true;
                            switch (Question.Validate) {
                                case ValidationType.Email:
                                    if (!StringUtils.IsValidEmailAddress(Answer)) Valid = false;
                                    break;
                                case ValidationType.Numeric:
                                    double Temp = 0;
                                    if (!double.TryParse(Answer, out Temp)) Valid = false;
                                    break;
                                case ValidationType.TwoWords:
                                    if (!Answer.Contains(" ")) Valid = false;
                                    break;
                            }
                            if (!Valid) {
                                if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_INVALID")) _NodeInfo.Connection.WriteLn("Input is not valid!");
                                goto GetAnswer;
                            }
                        }

                        // Check if answer requires confirmation
                        if (Question.Confirm) {
                            if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_CONFIRM")) _NodeInfo.Connection.Write("Please re-enter: ");

                            string Confirm = ReadLn().Trim();
                            if (QuitThread()) return false;

                            // Check if confirmation matches
                            if (Confirm != Answer) {
                                if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_MISMATCH")) _NodeInfo.Connection.WriteLn("Text does not match!");
                                goto GetAnswer;
                            }
                        }

                        // If we get here, the answer is valid, so save it
                        _NodeInfo.User.AdditionalInfo[Questions[i]] = Answer;
                    } else {
                        RMLog.Error("Unable to prompt new user for '" + Questions[i] + "' since ansi\\newuser_enter_" + Questions[i].ToLower() + ".ans is missing");
                    }
                }

                Registered = true;
                return Registered;
            } finally {
                if (Registered) {
                    lock (Globals.RegistrationLock) {
                        Config C = new Config();
                        _NodeInfo.User.UserId = C.NextUserId++;
                        _NodeInfo.User.SaveRegistration();
                        C.Save();
                    }

                    DisplayAnsi("NEWUSER_SUCCESS", true);
                } else {
                    _NodeInfo.User.AbortRegistration();
                }
            }
        }

        private void RunDoor(string door) {
            _NodeInfo.Door = new DoorInfo(door);
            if (_NodeInfo.Door.Loaded) {
                RunDoor();
            } else {
                RMLog.Error("Unable to find door: '" + door + "'");
            }
        }

        private void RunDoor() {
            try {
                // Clear the buffers and reset the screen
                _NodeInfo.Connection.ReadString();
                ClrScr();

                // Create the node directory and drop files
                CreateNodeDirectory();

                // Determine how to run the door
                if (((_NodeInfo.Door.Platform == OSUtils.Platform.Linux) && OSUtils.IsUnix) || ((_NodeInfo.Door.Platform == OSUtils.Platform.Windows) && OSUtils.IsWindows)) {
                    RunDoorNative(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters));
                } else if ((_NodeInfo.Door.Platform == OSUtils.Platform.DOS) && OSUtils.IsWindows) {
                    if (ProcessUtils.Is64BitOperatingSystem) {
                        if (Globals.IsDOSBoxInstalled()) {
                            RunDoorDOSBox(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters));
                        } else {
                            RMLog.Error("DOS doors are not supported on 64bit Windows (unless you install DOSBox 0.73)");
                        }
                    } else {
                        if (Environment.OSVersion.Platform == PlatformID.Win32Windows) {
                            RunDoorSBBSEXEC9x(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters), _NodeInfo.Door.ForceQuitDelay);
                        } else {
                            RunDoorSBBSEXECNT(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters), _NodeInfo.Door.ForceQuitDelay);
                        }
                    }
                } else if ((_NodeInfo.Door.Platform == OSUtils.Platform.DOS) && OSUtils.IsUnix) {
                    if (Globals.IsDOSEMUInstalled()) {
                        // TODOZ Doesn't this allow a door to hang if the user hangs up?  We need some method to force-quit it!
                        RunDoorDOSEMU(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters));
                    } else {
                        RMLog.Error("DOS doors are not supported on Linux (unless you install DOSEMU)");
                    }
                } else {
                    RMLog.Error("Unsure how to run door on current platform");
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error while running door '" + _NodeInfo.Door.Name + "'");
            } finally {
                // Clean up
                try {
                    ClrScr();
                    _NodeInfo.Connection.SetBlocking(true); // In case native door disabled blocking sockets
                    DeleteNodeDirectory();
                } catch { /* Ignore */ }
            }
        }

        private void RunDoorDOSBox(string command, string parameters) {
            if (Globals.Debug) UpdateStatus("DEBUG: DOSBox launching " + command + " " + parameters);

            string DOSBoxConf = StringUtils.PathCombine("node" + _NodeInfo.Node.ToString(), "dosbox.conf");
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
            string Arguments = "-telnet -conf " + DOSBoxConf + " -socket " + _NodeInfo.Connection.GetSocket().Handle.ToInt32().ToString();
            if (Globals.Debug) UpdateStatus("Executing " + DOSBoxExe + " " + Arguments);

            // Start the process
            using (RMProcess P = new RMProcess()) {
                P.ProcessWaitEvent += OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(DOSBoxExe, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;

                P.StartAndWait(PSI);
            }
        }

        private void RunDoorDOSEMU(string command, string parameters) {
            if (Globals.Debug) UpdateStatus("DEBUG: DOSEMU launching " + command + " " + parameters);

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
                FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "external.bat"), String.Join("\r\n", ExternalBat), RMEncoding.Ansi);

                string[] Arguments = new string[] { "HOME=" + ProcessUtils.StartupPath, "HOME=" + ProcessUtils.StartupPath, "QUIET=1", "DOSDRIVE_D=" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString()), "/usr/bin/nice", "-n19", "/usr/bin/dosemu.bin", "-Ivideo { none }", "-Ikeystroke \\r", "-Iserial { virtual com 1 }", "-t", "-Ed:external.bat", "-o" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosemu.log") };//, "2> /gamesrv/NODE" + _NodeInfo.Node.ToString() + "/DOSEMU_BOOT.LOG" }; // TODO add configuration variable so this path is not hardcoded
                if (Globals.Debug) UpdateStatus("Executing /usr/bin/env " + string.Join(" ", Arguments));

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

                        while (!QuitThread()) {
                            NumRead = us.Read(Buffer, 0, Buffer.Length);
                            if (NumRead > 0) {
                                _NodeInfo.Connection.WriteBytes(Buffer, NumRead);
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
                while (!_Stop) // TODOZ Check inside loop for other things QuitThread() would check
                {
                    DataTransferred = false;

                    // Check for exception in read thread
                    if (ReadException != null) return;

                    // Check for dropped carrier
                    if (!_NodeInfo.Connection.Connected) {
                        int Sleeps = 0;

                        UpdateStatus("User hung-up while in external program");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGHUP);
                        while ((Sleeps++ < 5) && (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0)) {
                            Thread.Sleep(1000);
                        }
                        if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0) {
                            UpdateStatus("Process still active after waiting 5 seconds");
                        }
                        return;
                    }

                    // Send data from user to door
                    if (_NodeInfo.Connection.CanRead()) {
                        // Write the text to the program
                        byte[] Bytes = _NodeInfo.Connection.ReadBytes();
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
                            switch (_NodeInfo.ConnectionType) {
                                case ConnectionType.RLogin:
                                    _NodeInfo.Connection.Write("\0");
                                    break;
                                case ConnectionType.Telnet:
                                    ((TelnetConnection)_NodeInfo.Connection).SendGoAhead();
                                    break;
                                case ConnectionType.WebSocket:
                                    _NodeInfo.Connection.Write("\0");
                                    break;
                            }
                        }

                        // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                        _NodeInfo.Connection.CanRead(100);
                    } else {
                        LoopsSinceIO = 0;
                    }
                }
            } finally {
                // Terminate process if it hasn't closed yet
                if (pty != null) {
                    if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0) {
                        UpdateStatus("Terminating process");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGKILL);
                    }
                    pty.Dispose();
                }
            }
        }

        private void RunDoorNative(string command, string parameters) {
            if (Globals.Debug) UpdateStatus("DEBUG: Natively launching " + command + " " + parameters);
            using (RMProcess P = new RMProcess()) {
                P.ProcessWaitEvent += OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(command, parameters);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;

                P.StartAndWait(PSI);
            }
        }

        struct sbbsexec_start_t {
            public uint Mode;
            public IntPtr Event;
        }

        private unsafe void RunDoorSBBSEXEC9x(string command, string parameters, int forceQuitDelay) {
            if (Globals.Debug) UpdateStatus("DEBUG: SBBSEXEC9x launching " + command + " " + parameters);

            // SBBSEXEC constants
            const uint LoopsBeforeYield = 10;
            const uint SBBSEXEC_MODE_FOSSIL = 0;
            const uint SBBSEXEC_IOCTL_START = 0x8001;
            const uint SBBSEXEC_IOCTL_COMPLETE = 0x8002;
            const uint SBBSEXEC_IOCTL_READ = 0x8003;
            const uint SBBSEXEC_IOCTL_WRITE = 0x8004;
            const uint SBBSEXEC_IOCTL_DISCONNECT = 0x8005;
            const uint SBBSEXEC_IOCTL_STOP = 0x8006;
            const uint XTRN_IO_BUF_LEN = 10000;

            // Initialize variables
            uint BytesRead = 0;
            IntPtr BytesWritten = IntPtr.Zero;
            bool DataTransferred = false;
            int LoopsSinceIO = 0;
            RMProcess P = null;
            IntPtr ReadBuffer = IntPtr.Zero;
            sbbsexec_start_t Start = new sbbsexec_start_t();
            IntPtr StartEvent = IntPtr.Zero;
            IntPtr StartPtr = IntPtr.Zero;
            uint StartSize = 0;
            IntPtr VM = IntPtr.Zero;
            string VMValue = "";
            IntPtr VxD = IntPtr.Zero;

            // Initialize filename variables
            string EnvFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosxtrn.env");
            string RetFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosxtrn.ret");

            try {
                // Create temporary environment file
                FileUtils.FileWriteAllText(EnvFile, Environment.GetEnvironmentVariable("COMSPEC") + " /C " + command + " " + parameters);

                // Load vxd to intercept interrupts
                VxD = NativeMethods.CreateFile("\\\\.\\" + StringUtils.PathCombine(ProcessUtils.StartupPath, "sbbsexec.vxd"), NativeMethods.FileAccess.None, NativeMethods.FileShare.None, IntPtr.Zero, NativeMethods.CreationDisposition.New, NativeMethods.CreateFileAttributes.DeleteOnClose, IntPtr.Zero);
                int LastWin32Error = Marshal.GetLastWin32Error();
                if (VxD == (IntPtr)NativeMethods.INVALID_HANDLE_VALUE) {
                    RMLog.Error("CreateFile() failed to load VxD: " + LastWin32Error.ToString());
                    return;
                }

                StartEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, null);
                if (StartEvent == IntPtr.Zero) {
                    RMLog.Error("CreateEvent() failed to create StartEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }
                Start.Event = NativeMethods.OpenVxDHandle(StartEvent);
                Start.Mode = SBBSEXEC_MODE_FOSSIL;

                StartPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Start));
                Marshal.StructureToPtr(Start, StartPtr, true);
                StartSize = (uint)Marshal.SizeOf(Start);

                if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_START, StartPtr, StartSize, IntPtr.Zero, 0, out BytesRead, IntPtr.Zero)) {
                    RMLog.Error("Error executing SBBSEXEC_IOCTL_START: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Start the process
                string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "DOSXTRN.EXE");
                string Arguments = StringUtils.ExtractShortPathName(EnvFile) + " 95 " + _NodeInfo.Node.ToString() + " " + Start.Mode.ToString() + " " + LoopsBeforeYield.ToString();
                ProcessStartInfo PSI = new ProcessStartInfo(FileName, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;
                P = RMProcess.Start(PSI);

                if (P == null) {
                    RMLog.Error("Error launching " + FileName + " " + Arguments);
                    return;
                }

                // Wait for notification from VXD that new VM has started
                if (NativeMethods.WaitForSingleObject(StartEvent, 5000) != (uint)NativeMethods.WAIT_OBJECT_0) {
                    RMLog.Error("WaitForSingleObject() timeout while waiting for StartEvent");
                    return;
                }

                // Mark as closed
                NativeMethods.CloseHandle(StartEvent);
                StartEvent = IntPtr.Zero;

                VM = Marshal.AllocHGlobal(sizeof(uint));
                if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_COMPLETE, IntPtr.Zero, 0, VM, sizeof(uint), out BytesRead, IntPtr.Zero)) {
                    RMLog.Error("Error executing SBBSEXEC_IOCTL_COMPLETE: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }
                VMValue = RMEncoding.Ansi.GetString(BitConverter.GetBytes(Marshal.ReadInt32(VM)));

                // Loop until something happens
                BytesWritten = Marshal.AllocHGlobal(sizeof(uint));
                ReadBuffer = Marshal.AllocHGlobal((int)XTRN_IO_BUF_LEN);
                while (!_Stop) // TODOZ Check inside loop for other things QuitThread() would check
                {
                    DataTransferred = false;

                    // Check for dropped carrier
                    if (!_NodeInfo.Connection.Connected) {
                        UpdateStatus("User hung-up while in external program");

                        if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_DISCONNECT, VM, sizeof(uint), IntPtr.Zero, 0, out BytesRead, IntPtr.Zero)) {
                            RMLog.Error("Error executing SBBSEXEC_IOCTL_DISCONNECT: " + Marshal.GetLastWin32Error().ToString());
                            return;
                        }

                        // Wait up to forceQuitDelay seconds for the process to terminate
                        for (int i = 0; i < forceQuitDelay; i++) {
                            if (P.HasExited) return;
                            P.WaitForExit(1000);
                        }
                        UpdateStatus("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                        return;
                    }

                    // Write to VxD (send data from user to door)
                    if (_NodeInfo.Connection.CanRead()) {
                        IntPtr InBuf = IntPtr.Zero;
                        try {
                            string ToSend = VMValue + _NodeInfo.Connection.PeekString();
                            InBuf = Marshal.StringToHGlobalAnsi(ToSend);

                            if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_WRITE, InBuf, (uint)ToSend.Length, BytesWritten, sizeof(uint), out BytesRead, IntPtr.Zero)) {
                                RMLog.Error("Error executing SBBSEXEC_IOCTL_WRITE: " + Marshal.GetLastWin32Error().ToString());
                                return;
                            }

                            // Since we only peeked above, now we want to actually read the number of bytes the VxD accepted
                            _NodeInfo.Connection.ReadBytes(Marshal.ReadInt32(BytesWritten));
                            DataTransferred = true;
                        } finally {
                            Marshal.ZeroFreeGlobalAllocAnsi(InBuf);
                        }
                    }

                    // Read from VxD (send data from door to user)
                    if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_READ, VM, sizeof(uint), ReadBuffer, XTRN_IO_BUF_LEN, out BytesRead, IntPtr.Zero)) {
                        RMLog.Error("Error executing SBBSEXEC_IOCTL_READ: " + Marshal.GetLastWin32Error().ToString());
                        return;
                    }

                    // If we read something, write it to the user
                    if (BytesRead > 0) {
                        _NodeInfo.Connection.Write(Marshal.PtrToStringAnsi(ReadBuffer, (int)BytesRead));
                        DataTransferred = true;
                    }

                    // Checks to perform when there was no I/O
                    if (!DataTransferred) {
                        // Numer of loop iterations with no I/O
                        LoopsSinceIO++;

                        // Only check process termination after 300 milliseconds of no I/O
                        // to allow for last minute reception of output from DOS programs
                        if (LoopsSinceIO >= 3) {
                            // Check if process terminated
                            if (P.HasExited) return;
                        }

                        // Only send telnet GA every 30 seconds of no I/O
                        if (LoopsSinceIO % 300 == 0) {
                            switch (_NodeInfo.ConnectionType) {
                                case ConnectionType.RLogin:
                                    _NodeInfo.Connection.Write("\0");
                                    break;
                                case ConnectionType.Telnet:
                                    ((TelnetConnection)_NodeInfo.Connection).SendGoAhead();
                                    break;
                                case ConnectionType.WebSocket:
                                    _NodeInfo.Connection.Write("\0");
                                    break;
                            }
                        }

                        // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                        _NodeInfo.Connection.CanRead(100);
                    } else {
                        LoopsSinceIO = 0;
                    }
                }
            } finally {
                if ((VxD != IntPtr.Zero) && (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_STOP, VM, sizeof(uint), IntPtr.Zero, 0, out BytesRead, IntPtr.Zero))) {
                    RMLog.Error("Error executing SBBSEXEC_IOCTL_STOP: " + Marshal.GetLastWin32Error().ToString());
                }

                // Terminate process if it hasn't closed yet
                try {
                    if ((P != null) && !P.HasExited) {
                        RMLog.Error("Door still running, performing a force quit");
                        P.Kill();
                    }
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Unable to perform force quit");
                }

                // Free unmanaged resources
                if (ReadBuffer != IntPtr.Zero) Marshal.FreeHGlobal(ReadBuffer);
                if (BytesWritten != IntPtr.Zero) Marshal.FreeHGlobal(BytesWritten);
                if (VM != IntPtr.Zero) Marshal.FreeHGlobal(VM);
                if (StartPtr != IntPtr.Zero) Marshal.FreeHGlobal(StartPtr);
                if (StartEvent != IntPtr.Zero) NativeMethods.CloseHandle(StartEvent);
                if (VxD != IntPtr.Zero) NativeMethods.CloseHandle(VxD);

                // Delete .ENV and .RET files
                FileUtils.FileDelete(EnvFile);
                FileUtils.FileDelete(RetFile);
            }
        }

        private unsafe void RunDoorSBBSEXECNT(string command, string parameters, int forceQuitDelay) {
            if (Globals.Debug) UpdateStatus("DEBUG: SBBSEXECNT launching " + command + " " + parameters);

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
            string EnvFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosxtrn.env");
            string RetFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosxtrn.ret");
            string W32DoorFile = StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "w32door.run");

            try {
                // Create temporary environment file
                FileUtils.FileWriteAllText(EnvFile, Environment.GetEnvironmentVariable("COMSPEC") + " /C " + command + " " + parameters);

                // Create a hungup event for when user drops carrier
                HungUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hungup" + _NodeInfo.Node.ToString());
                if (HungUpEvent == IntPtr.Zero) {
                    RMLog.Error("CreateEvent() failed to create HungUpEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Create a hangup event (for when the door requests to drop DTR)
                HangUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hangup" + _NodeInfo.Node.ToString());
                if (HangUpEvent == IntPtr.Zero) {
                    RMLog.Error("CreateEvent() failed to create HangUpEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Create a read mail slot
                ReadSlot = NativeMethods.CreateMailslot("\\\\.\\mailslot\\sbbsexec\\rd" + _NodeInfo.Node.ToString(), XTRN_IO_BUF_LEN, 0, IntPtr.Zero);
                if (ReadSlot == IntPtr.Zero) {
                    RMLog.Error("CreateMailslot() failed to create ReadSlot: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Start the process
                string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "DOSXTRN.EXE");
                string Arguments = StringUtils.ExtractShortPathName(EnvFile) + " NT " + _NodeInfo.Node.ToString() + " " + SBBSEXEC_MODE_FOSSIL.ToString() + " " + LoopsBeforeYield.ToString();
                ProcessStartInfo PSI = new ProcessStartInfo(FileName, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;
                P = RMProcess.Start(PSI);

                if (P == null) {
                    RMLog.Error("Error launching " + FileName + " " + Arguments);
                    return;
                }

                // Loop until something happens
                bool DataTransferred = false;
                while (!_Stop) // TODOZ Check inside loop for other things QuitThread() would check
                {
                    DataTransferred = false;

                    // Check for dropped carrier
                    if (!_NodeInfo.Connection.Connected) {
                        UpdateStatus("User hung-up while in external program");
                        NativeMethods.SetEvent(HungUpEvent);

                        // Wait up to forceQuitDelay seconds for the process to terminate
                        for (int i = 0; i < forceQuitDelay; i++) {
                            if (P.HasExited) return;
                            P.WaitForExit(1000);
                        }
                        UpdateStatus("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                        return;
                    }

                    // Send data from user to door
                    if (_NodeInfo.Connection.CanRead()) {
                        // If our writeslot doesnt exist yet, create it
                        if (WriteSlot == IntPtr.Zero) {
                            // Create A Write Mail Slot
                            WriteSlot = NativeMethods.CreateFile("\\\\.\\mailslot\\sbbsexec\\wr" + _NodeInfo.Node.ToString(), NativeMethods.FileAccess.GenericWrite, NativeMethods.FileShare.Read, IntPtr.Zero, NativeMethods.CreationDisposition.OpenExisting, NativeMethods.CreateFileAttributes.Normal, IntPtr.Zero);
                            int LastWin32Error = Marshal.GetLastWin32Error();
                            if (WriteSlot == IntPtr.Zero) {
                                RMLog.Error("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                                return;
                            } else if (WriteSlot.ToInt32() == -1) {
                                if (LastWin32Error == 2) {
                                    // ERROR_FILE_NOT_FOUND - User must have hit a key really fast to trigger this!
                                    RMLog.Warning("CreateFile() failed to find WriteSlot: \\\\.\\mailslot\\sbbsexec\\wr" + _NodeInfo.Node.ToString());
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
                            byte[] BufBytes = _NodeInfo.Connection.PeekBytes();
                            uint BytesWritten = 0;
                            bool Result = NativeMethods.WriteFile(WriteSlot, BufBytes, (uint)BufBytes.Length, out BytesWritten, null);
                            int LastWin32Error = Marshal.GetLastWin32Error();
                            if (Result) {
                                _NodeInfo.Connection.ReadBytes((int)BytesWritten);
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
                            _NodeInfo.Connection.WriteBytes(BufBytes, BufPtr);
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
                            if ((_NodeInfo.Door.WatchDTR) && (NativeMethods.WaitForSingleObject(HangUpEvent, 0) == NativeMethods.WAIT_OBJECT_0)) {
                                UpdateStatus("External program requested hangup (dropped DTR)");
                                _NodeInfo.Connection.Close();

                                // Wait up to forceQuitDelay seconds for the process to terminate
                                for (int i = 0; i < forceQuitDelay; i++) {
                                    if (P.HasExited) return;
                                    P.WaitForExit(1000);
                                }
                                UpdateStatus("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                                return;
                            }

                            if (P.HasExited) {
                                UpdateStatus("External terminated with exit code: " + P.ExitCode);
                                break;
                            }

                            // Watch for a W32DOOR.RUN file to be created in the node directory
                            // If it gets created, it's our signal that a DOS BBS package wants us to launch a W32 door
                            // W32DOOR.RUN will contain two lines, the first is the command to run, the second is the parameters
                            if (File.Exists(W32DoorFile)) {
                                try {
                                    if (Globals.Debug) UpdateStatus("DEBUG: w32door.run found");
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
                            switch (_NodeInfo.ConnectionType) {
                                case ConnectionType.RLogin:
                                    _NodeInfo.Connection.Write("\0");
                                    break;
                                case ConnectionType.Telnet:
                                    ((TelnetConnection)_NodeInfo.Connection).SendGoAhead();
                                    break;
                                case ConnectionType.WebSocket:
                                    _NodeInfo.Connection.Write("\0");
                                    break;
                            }
                        }

                        // Delay for 100ms (unless the user hits a key, in which case break the delay early)
                        _NodeInfo.Connection.CanRead(100);
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

        private int SecondsLeft() {
            return _NodeInfo.SecondsThisSession - (int)DateTime.Now.Subtract(_NodeInfo.TimeOn).TotalSeconds;
        }

        public void Start(int node, TcpConnection connection, ConnectionType connectionType, TerminalType terminalType) {
            if (connection == null) throw new ArgumentNullException("connection");

            _NodeInfo.TerminalType = terminalType;
            _NodeInfo.Node = node;
            _NodeInfo.Connection = connection;
            _NodeInfo.ConnectionType = connectionType;

            base.Start();
        }

        public string Status {
            get { return _Status; }
        }

        public override void Stop() {
            // Close the socket so that any waits on ReadLn(), ReadChar(), etc, will not block
            _NodeInfo.Connection.Close();

            base.Stop();
        }

        private void Telnet(string hostname) {
            bool _RLogin = false;
            string _RLoginClientUserName = "TODO";
            string _RLoginServerUserName = "TODO";
            string _RLoginTerminalType = "TODO";

            ClrScr();
            _NodeInfo.Connection.WriteLn();
            _NodeInfo.Connection.Write(" Connecting to remote server...");

            TcpConnection RemoteServer = null;
            if (_RLogin) {
                RemoteServer = new RLoginConnection();
            } else {
                RemoteServer = new TelnetConnection();
            }

            // Sanity check on the port
            int Port = 23;
            WebUtils.ParseHostPort(hostname, ref hostname, ref Port);
            if ((Port < 1) || (Port > 65535)) {
                Port = (_RLogin) ? 513 : 23;
            }

            if (RemoteServer.Connect(hostname, Port)) {
                bool CanContinue = true;
                if (_RLogin) {
                    // Send rlogin header
                    RemoteServer.Write("\0" + _RLoginClientUserName + "\0" + _RLoginServerUserName + "\0" + _RLoginTerminalType + "\0");

                    // Wait up to 5 seconds for a response
                    char? Ch = RemoteServer.ReadChar(5000);
                    if ((Ch == null) || (Ch != '\0')) {
                        CanContinue = false;
                        _NodeInfo.Connection.WriteLn("failed!");
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn(" Looks like the remote server doesn't accept RLogin connections.");
                    }
                }

                if (CanContinue) {
                    _NodeInfo.Connection.WriteLn("connected!");

                    bool UserAborted = false;
                    while (!UserAborted && RemoteServer.Connected && !QuitThread()) {
                        bool Yield = true;

                        // See if the server sent anything to the client
                        if (RemoteServer.CanRead()) {
                            _NodeInfo.Connection.Write(RemoteServer.ReadString());
                            Yield = false;
                        }

                        // See if the client sent anything to the server
                        if (_NodeInfo.Connection.CanRead()) {
                            //string ToSend = "";
                            //while (_NodeInfo.Connection.KeyPressed())
                            //{
                            //    byte B = (byte)_NodeInfo.Connection.ReadByte();
                            //    if (B == 29)
                            //    {
                            //        // Ctrl-]
                            //        RemoteServer.Close();
                            //        UserAborted = true;
                            //        break;
                            //    }
                            //    else
                            //    {
                            //        ToSend += (char)B;
                            //    }
                            //}
                            //RemoteServer.Write(ToSend);

                            RemoteServer.Write(_NodeInfo.Connection.ReadString());
                            Yield = false;
                        }

                        // See if we need to yield
                        if (Yield) Crt.Delay(1);
                    }

                    if (UserAborted) {
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn(" User hit CTRL-] to disconnect from server.");
                        ReadChar();
                    } else if ((_NodeInfo.Connection.Connected) && (!RemoteServer.Connected)) {
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn(" Remote server closed the connection.");
                        ReadChar();
                    }
                }
            } else {
                _NodeInfo.Connection.WriteLn("failed!");
                _NodeInfo.Connection.WriteLn();
                _NodeInfo.Connection.WriteLn(" Looks like the remote server isn't online, please try back later.");
            }
        }

        private string TranslateCLS(string command) {
            List<KeyValuePair<string, string>> CLS = new List<KeyValuePair<string, string>>();
            CLS.Add(new KeyValuePair<string, string>("**ALIAS", _NodeInfo.User.Alias));
            CLS.Add(new KeyValuePair<string, string>("DOOR32", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door32.sys"))));
            CLS.Add(new KeyValuePair<string, string>("DOORSYS", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door.sys"))));
            CLS.Add(new KeyValuePair<string, string>("DOORFILE", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "doorfile.sr"))));
            CLS.Add(new KeyValuePair<string, string>("DORINFOx", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo" + _NodeInfo.Node.ToString() + ".def"))));
            CLS.Add(new KeyValuePair<string, string>("DORINFO1", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo1.def"))));
            CLS.Add(new KeyValuePair<string, string>("DORINFO", StringUtils.ExtractShortPathName(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo.def"))));
            CLS.Add(new KeyValuePair<string, string>("HANDLE", _NodeInfo.Connection.Handle.ToString()));
            CLS.Add(new KeyValuePair<string, string>("IPADDRESS", _NodeInfo.Connection.GetRemoteIP()));
            CLS.Add(new KeyValuePair<string, string>("MINUTESLEFT", MinutesLeft().ToString()));
            CLS.Add(new KeyValuePair<string, string>("NODE", _NodeInfo.Node.ToString()));
            CLS.Add(new KeyValuePair<string, string>("**PASSWORD", _NodeInfo.User.PasswordHash));
            CLS.Add(new KeyValuePair<string, string>("SECONDSLEFT", SecondsLeft().ToString()));
            CLS.Add(new KeyValuePair<string, string>("SOCKETHANDLE", _NodeInfo.Connection.Handle.ToString()));
            CLS.Add(new KeyValuePair<string, string>("**USERNAME", _NodeInfo.User.Alias));
            foreach (DictionaryEntry DE in _NodeInfo.User.AdditionalInfo) {
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

        private string TranslateMCI(string text, string fileName) {
            StringDictionary MCI = new StringDictionary();
            MCI.Add("ACCESSLEVEL", _NodeInfo.User.AccessLevel.ToString());
            MCI.Add("ALIAS", _NodeInfo.User.Alias);
            MCI.Add("BBSNAME", _Config.BBSName);
            MCI.Add("DATE", DateTime.Now.ToShortDateString());
            MCI.Add("FILENAME", fileName.Replace(StringUtils.PathCombine(ProcessUtils.StartupPath, ""), ""));
            MCI.Add("GSDIR", StringUtils.PathCombine(ProcessUtils.StartupPath, ""));
            MCI.Add("MENUNAME", _CurrentMenu);
            MCI.Add("NODE", _NodeInfo.Node.ToString());
            MCI.Add("OPERATINGSYSTEM", OSUtils.GetNameAndVersion());
            MCI.Add("SYSOPEMAIL", _Config.SysopEmail);
            MCI.Add("SYSOPNAME", _Config.SysopFirstName + " " + _Config.SysopLastName);
            MCI.Add("TIME", DateTime.Now.ToShortTimeString());
            MCI.Add("TIMELEFT", StringUtils.SecToHMS(SecondsLeft()));
            foreach (DictionaryEntry DE in _NodeInfo.User.AdditionalInfo) {
                MCI.Add(DE.Key.ToString(), DE.Value.ToString());
            }
            if (text.Contains("WHOSONLINE_")) {
                EventHandler<WhoIsOnlineEventArgs> Handler = WhoIsOnlineEvent;
                if (Handler != null) {
                    WhoIsOnlineEventArgs WOEA = new WhoIsOnlineEventArgs();
                    Handler(this, WOEA);
                    foreach (DictionaryEntry DE in WOEA.WhoIsOnline) {
                        MCI.Add(DE.Key.ToString(), DE.Value.ToString());
                    }
                }
            }

            // Perform MCI Translations
            foreach (DictionaryEntry DE in MCI) {
                if (DE.Value != null) {
                    text = text.Replace("{" + DE.Key.ToString().ToUpper() + "}", DE.Value.ToString());
                    for (int PadWidth = 1; PadWidth <= 80; PadWidth++) {
                        // Now translate anything that needs right padding
                        text = text.Replace("{" + DE.Key.ToString().ToUpper() + PadWidth.ToString() + "}", StringUtils.PadRight(DE.Value.ToString(), ' ', PadWidth));

                        // And now translate anything that needs left padding
                        text = text.Replace("{" + PadWidth.ToString() + DE.Key.ToString().ToUpper() + '}', StringUtils.PadLeft(DE.Value.ToString(), ' ', PadWidth));
                    }
                }
            }

            return text;
        }

        private void UpdateStatus(string newStatus) {
            _Status = newStatus;
            NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, newStatus, NodeEventType.StatusChange));
        }

        private class LogOffProcess : ConfigHelper {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Parameters { get; set; }
            public int RequiredAccess { get; set; }

            private LogOffProcess() {
                // Don't let the user instantiate this without a constructor
            }

            public LogOffProcess(string section)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "logoffprocess.ini")) {
                Name = "";
                Action = Action.None;
                Parameters = "";
                RequiredAccess = 0;

                Load(section);
            }

            public static string[] GetProcesses() {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "logoffprocess.ini"))) {
                    return Ini.ReadSections();
                }
            }
        }

        private class LogOnProcess : ConfigHelper {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Parameters { get; set; }
            public int RequiredAccess { get; set; }

            private LogOnProcess() {
                // Don't let the user instantiate this without a constructor
            }

            public LogOnProcess(string section)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "logonprocess.ini")) {
                Name = "";
                Action = Action.None;
                Parameters = "";
                RequiredAccess = 0;

                Load(section);
            }

            public static string[] GetProcesses() {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "logonprocess.ini"))) {
                    return Ini.ReadSections();
                }
            }
        }

        private class MenuOption : ConfigHelper {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Parameters { get; set; }
            public int RequiredAccess { get; set; }

            private MenuOption() {
                // Don't let the user instantiate this without a constructor
            }

            public MenuOption(string menu, char hotKey)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("menus", menu.ToLower() + ".ini")) {
                Name = "";
                Action = Action.None;
                Parameters = "";
                RequiredAccess = 0;

                Load(hotKey.ToString());
            }

            public static string[] GetHotKeys(string menu) {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("menus", menu.ToLower() + ".ini")))) {
                    return Ini.ReadSections();
                }
            }
        }

        private class NewUserQuestion : ConfigHelper {
            public bool Confirm { get; set; }
            public bool Required { get; set; }
            public ValidationType Validate { get; set; }

            private NewUserQuestion() {
                // Don't let the user instantiate this without a constructor
            }

            public NewUserQuestion(string question)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "newuser.ini")) {
                Confirm = false;
                Required = false;
                Validate = ValidationType.None;

                Load(question);
            }

            public static string[] GetQuestions() {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("config", "newuser.ini")))) {
                    // Return all the sections in newuser.ini, except for [alias] and [password] since they're reserved
                    return Ini.ReadSections().Where(x => (x.ToLower() != "alias") && (x.ToLower() != "password")).ToArray();
                }
            }
        }

        private enum Action {
            None,
            ChangeMenu,
            Disconnect,
            DisplayFile,
            DisplayFileMore,
            DisplayFilePause,
            LogOff,
            MainMenu,
            Pause,
            RunDoor,
            Telnet
        }

        private enum ValidationType {
            Email,
            None,
            Numeric,
            TwoWords
        }
    }
}
