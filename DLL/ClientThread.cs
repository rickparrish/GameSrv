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

namespace RandM.GameSrv
{
    public class ClientThread : RMThread, IDisposable
    {
        private Config _Config = new Config();
        private string _CurrentMenu;
        private Dictionary<char, MenuOption> _CurrentMenuOptions = new Dictionary<char, MenuOption>();
        private bool _Disposed = false;
        private string _LastDisplayFile = "";
        private NodeInfo _NodeInfo = new NodeInfo();
        private Random _R = new Random();
        private StringDictionary _RLoginVariables = new StringDictionary();
        private string _Status = "";

        // TODO Add a Disconnect event of some sort to allow a sysop to disconnect another node
        public event EventHandler<StringEventArgs> ErrorMessageEvent = null;
        public event EventHandler<ExceptionEventArgs> ExceptionEvent = null;
        public event EventHandler<NodeEventArgs> LogOffEvent = null;
        public event EventHandler<NodeEventArgs> LogOnEvent = null;
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<StringEventArgs> WarningMessageEvent = null;
        public event EventHandler<WhoIsOnlineEventArgs> WhoIsOnlineEvent = null;

        ~ClientThread()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_Disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (_NodeInfo.Connection != null) _NodeInfo.Connection.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _Disposed = true;
            }
        }

        public string Alias
        {
            get { return (_NodeInfo.User.Alias == null) ? "" : _NodeInfo.User.Alias; }
        }

        private bool AuthenticateRLogin()
        {
            // Check for door request/web information in terminal type string
            string[] TerminalTypeEntries = ((RLoginConnection)_NodeInfo.Connection).TerminalType.Split(';');
            foreach (string TerminalTypeEntry in TerminalTypeEntries)
            {
                if (TerminalTypeEntry.Contains("="))
                {
                    string[] KeyValue = TerminalTypeEntry.Split('=');
                    string EntryValue = string.Join("=", KeyValue, 1, KeyValue.Length - 1).Trim();
                    if (!string.IsNullOrEmpty(EntryValue)) _RLoginVariables.Add(KeyValue[0], EntryValue);
                }
            }

            // Now based on the rlogin mode, handle the rest of the details
            if (_RLoginVariables.ContainsKey("mode") && (_RLoginVariables["mode"] == "web"))
            {
                return AuthenticateRLoginWeb(((RLoginConnection)_NodeInfo.Connection).ClientUserName, ((RLoginConnection)_NodeInfo.Connection).ServerUserName);
            }
            else
            {
                return AuthenticateRLoginClassic(((RLoginConnection)_NodeInfo.Connection).ServerUserName);
            }
        }

        private bool AuthenticateRLoginClassic(string serverUserName)
        {
            // Check if the username is valid
            _NodeInfo.User = new UserInfo(serverUserName);
            if (!_NodeInfo.User.Loaded)
            {
                // Nope, so register them and let them in
                if (_NodeInfo.User.StartRegistration(serverUserName))
                {
                    _NodeInfo.User.SetPassword(StringUtils.RandomString(10), _Config.PasswordPepper);
                    lock (Globals.RegistrationLock)
                    {
                        Config C = new Config();
                        _NodeInfo.User.UserId = C.NextUserId++;
                        _NodeInfo.User.SaveRegistration();
                        C.Save();
                    }
                }
            }

            // Classic RLogin is always successful since there's no password validation
            return true;
        }

        private bool AuthenticateRLoginWeb(string clientUserName, string serverUserName)
        {
            // Check if the username is valid
            _NodeInfo.User = new UserInfo(serverUserName);
            if (_NodeInfo.User.Loaded)
            {
                // Yep, so validate the password
                if (_RLoginVariables.ContainsKey("callback"))
                {
                    // Web connection means password is already hashed
                    if (_NodeInfo.User.PasswordHash != clientUserName)
                    {
                        // Password is bad
                        DisplayAnsi("RLOGIN_INVALID");
                        return false;
                    }
                }
                else
                {
                    // Not a web connection, so password should be plain text
                    if (!_NodeInfo.User.ValidatePassword(clientUserName, _Config.PasswordPepper))
                    {
                        // Password is bad
                        DisplayAnsi("RLOGIN_INVALID");
                        return false;
                    }
                }
            }
            else
            {
                // Nope, so perform the new user process
                return Register();
            }

            // If we get here, login is ok
            return true;
        }

        private bool AuthenticateTelnet()
        {
            DisplayAnsi("LOGON_HEADER");

            int FailedAttempts = 0;
            while (FailedAttempts++ < 3)
            {
                // Get alias
                RaiseNodeEvent("Entering Alias");
                DisplayAnsi("LOGON_ENTER_ALIAS");
                string Alias = ReadLn().Trim();

                // Make sure we should still proceed
                if (QuitThread()) return false;

                if (Alias.ToUpper() == "NEW")
                {
                    bool CanRegister = false;

                    if (string.IsNullOrEmpty(_Config.NewUserPassword))
                    {
                        CanRegister = true;
                    }
                    else
                    {
                        // Get new user password
                        RaiseNodeEvent("Entering New User Password");
                        DisplayAnsi("NEWUSER_ENTER_NEWUSER_PASSWORD");
                        string NewUserPassword = ReadLn('*').Trim();
                        _NodeInfo.Connection.WriteLn();

                        CanRegister = NewUserPassword == _Config.NewUserPassword;
                    }

                    // Make sure we should still proceed
                    if (QuitThread()) return false;

                    if (CanRegister)
                    {
                        // Trying to register as a new user
                        RaiseNodeEvent("Registering as new user");
                        return Register();
                    }
                    else
                    {
                        RaiseNodeEvent("Entered invalid newuser password");
                    }
                }
                else
                {
                    RaiseNodeEvent("Logging on as " + Alias);

                    // Get password
                    RaiseNodeEvent("Entering Password");
                    DisplayAnsi("LOGON_ENTER_PASSWORD");
                    string Password = ReadLn('*').Trim();

                    // Make sure we should still proceed
                    if (QuitThread()) return false;

                    RaiseNodeEvent("Validating Credentials");
                    _NodeInfo.User = new UserInfo(Alias);
                    if (_NodeInfo.User.Loaded && (_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper)))
                    {
                        // Successfully loaded user info
                        return true;
                    }
                    else
                    {
                        DisplayAnsi("LOGON_INVALID");
                    }
                }
            }

            // If we get here, we failed all 3 logon attempts
            DisplayAnsi("LOGON_FAILED");
            return false;
        }

        private void ClrScr()
        {
            switch (_NodeInfo.TerminalType)
            {
                case TerminalType.Ansi:
                    _NodeInfo.Connection.Write(Ansi.TextAttr(7) + Ansi.ClrScr() + Ansi.GotoXY(1, 1));
                    break;
                case TerminalType.Ascii:
                    _NodeInfo.Connection.Write("\r\n\x0C");
                    break;
                case TerminalType.Rip:
                    _NodeInfo.Connection.Write("\r\n!|*" + Ansi.TextAttr(7) + Ansi.ClrScr() + Ansi.GotoXY(1, 1));
                    break;
            }
        }

        private void CreateNodeDirectory()
        {
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
            Sl.Add(_NodeInfo.TerminalType == TerminalType.Ascii ? "N" : "Y");   // 39 - ANSI Supported
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
                case TerminalType.Ansi: Sl.Add("1"); break;
                case TerminalType.Ascii: Sl.Add("0"); break;
                case TerminalType.Rip: Sl.Add("3"); break;
            }
            Sl.Add(_NodeInfo.Node.ToString());                                      // 11 - Current Node Number
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "door32.sys"), String.Join("\r\n", Sl.ToArray()));

            // Create DOORFILE.SR
            Sl.Clear();
            Sl.Add(_NodeInfo.User.Alias);                                       // Complete name or handle of user
            Sl.Add(_NodeInfo.TerminalType == TerminalType.Ascii ? "0" : "1");   // ANSI status:  1 = yes, 0 = no, -1 = don't know
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
            Sl.Add(_NodeInfo.TerminalType == TerminalType.Ascii ? "0" : "1");  // 10 - User's Emulation (0=Ascii, 1=Ansi)
            Sl.Add(_NodeInfo.User.AccessLevel.ToString());                     // 11 - User's Access Level
            Sl.Add(MinutesLeft().ToString());                                  // 12 - User's Time Left (In Minutes)
            Sl.Add("1");                                                       // 13 - Fossil?
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo1.def"), String.Join("\r\n", Sl.ToArray()));
            FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dorinfo" + _NodeInfo.Node.ToString() + ".def"), String.Join("\r\n", Sl.ToArray()));
        }

        private void DeleteNodeDirectory()
        {
            if (!Globals.Debug)
            {
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

        public bool DisplayAnsi(string fileName)
        {
            return DisplayAnsi(fileName, false);
        }

        public bool DisplayAnsi(string fileName, bool pauseAtEnd)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            else
            {
                List<string> FilesToCheck = new List<string>();
                if (_NodeInfo.TerminalType == TerminalType.Rip) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".rip"));
                if ((_NodeInfo.TerminalType == TerminalType.Rip) || (_NodeInfo.TerminalType == TerminalType.Ansi)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".ans"));
                FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".asc"));

                for (int i = 0; i < FilesToCheck.Count; i++)
                {
                    if (File.Exists(FilesToCheck[i])) return DisplayFile(FilesToCheck[i], false, pauseAtEnd, false);
                }
            }

            return false;
        }

        private void DisplayCurrentMenu()
        {
            // Generate list of possible menus to look for, prioritized by access level specific and terminal type specific
            List<string> FilesToCheck = new List<string>();

            // Access level specific
            if (_NodeInfo.TerminalType == TerminalType.Rip) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".rip"));
            if ((_NodeInfo.TerminalType == TerminalType.Rip) || (_NodeInfo.TerminalType == TerminalType.Ansi)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".ans"));
            FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".asc"));

            // No specified access level
            if (_NodeInfo.TerminalType == TerminalType.Rip) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".rip"));
            if ((_NodeInfo.TerminalType == TerminalType.Rip) || (_NodeInfo.TerminalType == TerminalType.Ansi)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".ans"));
            FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".asc"));

            // Check if any of the above files exists
            for (int i = 0; i < FilesToCheck.Count; i++)
            {
                if (File.Exists(FilesToCheck[i]))
                {
                    DisplayFile(FilesToCheck[i], true, false, false);
                    return;
                }
            }

            // None of the files existed, displayed canned menu
            if (_NodeInfo.TerminalType == TerminalType.Ascii)
            {
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
                if (0 == Row % 2)
                {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄÈÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼ÄÅÄ´");
                    _NodeInfo.Connection.WriteLn("ÀÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÙ");
                }
                else
                {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄÈÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼ÄÅÄ´");
                    _NodeInfo.Connection.WriteLn("ÀÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÙ");
                }

                // Still part of the footer
                _NodeInfo.Connection.Write("[" + StringUtils.SecToHMS(SecondsLeft()) + "] Select: ");
            }
            else
            {
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
                if (0 == Row % 2)
                {
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mÈ" + "\x1B" + "[1;30mÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼Ä" + "\x1B" + "[37mÅÄ´");
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÀ" + "\x1B" + "[0mÄ" + "\x1B" + "[1mÁÄ" + "\x1B" + "[30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[37mÄ" + "\x1B" + "[0mÙ");
                }
                else
                {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄ" + "\x1B" + "[0mÈ" + "\x1B" + "[1;30mÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼Ä" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÀ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[37mÄÙ");
                }

                // Still part of the footer
                _NodeInfo.Connection.Write("\x1B" + "[1;30m[" + StringUtils.SecToHMS(SecondsLeft()) + "]" + "\x1B" + "[37m Select: " + "\x1B" + "[0m");
            }
        }

        private int DisplayCurrentMenuOptions(int row)
        {
            ArrayList HotKeys = new ArrayList();
            foreach (KeyValuePair<char, MenuOption> KV in _CurrentMenuOptions)
            {
                HotKeys.Add(KV.Key);
            }
            HotKeys.Sort();

            int i = 0;
            while (i <= HotKeys.Count - 1)
            {
                if (_NodeInfo.TerminalType == TerminalType.Ascii)
                {
                    // Display left side
                    _NodeInfo.Connection.Write((0 == row % 2) ? "ÃÄÅÄº " : "ÃÄÅÄº ");

                    // Display left hotkey and description
                    _NodeInfo.Connection.Write(HotKeys[i].ToString() + "] ");
                    _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");

                    // Check if that was the last item, or if there is one more
                    if (i >= HotKeys.Count)
                    {
                        // Display blank space (that was the last item)
                        _NodeInfo.Connection.Write(new string(' ', 30 + 4));
                    }
                    else
                    {
                        // Display right hotkey and description
                        _NodeInfo.Connection.Write(HotKeys[i].ToString() + "] ");
                        _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");
                    }

                    // Display right side
                    _NodeInfo.Connection.WriteLn((0 == row++ % 2) ? "ºÄÅÄ´" : "ºÄÅÄ´");
                }
                else
                {
                    // Display left side
                    _NodeInfo.Connection.Write((0 == row % 2) ? "ÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mº " : "\x1B" + "[1mÃÄÅÄ" + "\x1B" + "[0mº ");

                    // Display left hotkey and description
                    _NodeInfo.Connection.Write("\x1B" + "[1;33m" + HotKeys[i].ToString() + "\x1B" + "[1;30m] " + "\x1B" + "[0;37m");
                    _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");

                    // Check if that was the last item, or if there is one more
                    if (i >= HotKeys.Count)
                    {
                        // Display blank space (that was the last item)
                        _NodeInfo.Connection.Write(new string(' ', 30 + 4));
                    }
                    else
                    {
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

        private bool DisplayFile(string fileName, bool clearAtBeginning, bool pauseAtEnd, bool pauseAfter24)
        {
            try
            {
                _LastDisplayFile = fileName;

                if (clearAtBeginning)
                {
                    // Clear the screen
                    ClrScr();
                }

                // Translate the slashes accordingly
                if (OSUtils.IsWindows)
                {
                    fileName = fileName.Replace("/", "\\");
                }
                else if (OSUtils.IsUnix)
                {
                    fileName = fileName.Replace("\\", "/");
                }

                // If file starts with @, then it's an index file and we want to choose a random row from it
                if (fileName.StartsWith("@"))
                {
                    // Strip @
                    fileName = fileName.Substring(1);

                    if (File.Exists(fileName))
                    {
                        // Read index file
                        string[] FileNames = FileUtils.FileReadAllLines(fileName, RMEncoding.Ansi);

                        // Pick random filename
                        fileName = FileNames[_R.Next(0, FileNames.Length)];
                        _LastDisplayFile = fileName;

                        // Translate the slashes accordingly
                        if (OSUtils.IsWindows)
                        {
                            fileName = fileName.Replace("/", "\\");
                        }
                        else if (OSUtils.IsUnix)
                        {
                            fileName = fileName.Replace("\\", "/");
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                // Check if we need to pick an extension based on the user's terminal type (do this if the file doesn't exist, and doesn't have an extension)
                if (!File.Exists(fileName) && !Path.HasExtension(fileName))
                {
                    List<string> FileNamesWithExtension = new List<string>();
                    if (_NodeInfo.TerminalType == TerminalType.Rip)
                    {
                        FileNamesWithExtension.Add(fileName + ".rip");
                    }
                    if ((_NodeInfo.TerminalType == TerminalType.Rip) || (_NodeInfo.TerminalType == TerminalType.Ansi))
                    {
                        FileNamesWithExtension.Add(fileName + ".ans");
                    }
                    FileNamesWithExtension.Add(fileName + ".asc");

                    foreach (string FileNameWithExtension in FileNamesWithExtension)
                    {
                        if (File.Exists(FileNameWithExtension))
                        {
                            fileName = FileNameWithExtension;
                            break;
                        }
                    }
                }

                if (File.Exists(fileName))
                {
                    string TranslatedText = TranslateMCI(FileUtils.FileReadAllText(fileName, RMEncoding.Ansi), fileName);

                    // Check if we need to filter out a SAUCE record
                    if (TranslatedText.Contains("\x1A")) TranslatedText = TranslatedText.Substring(0, TranslatedText.IndexOf("\x1A"));

                    // Check if the file contains manual pauses
                    if (TranslatedText.Contains("{PAUSE}"))
                    {
                        // When the file contains {PAUSE} statements then pauseAfter24 is ignored
                        string[] Pages = TranslatedText.Split(new string[] { "{PAUSE}" }, StringSplitOptions.None);
                        for (int i = 0; i < Pages.Length; i++)
                        {
                            _NodeInfo.Connection.Write(Pages[i]);
                            if (i < (Pages.Length - 1)) ReadChar();
                        }
                    }
                    else
                    {
                        // Check if we want to pause every 24 lines
                        if (pauseAfter24)
                        {
                            // Yep, so split the file on CRLF
                            string[] TranslatedLines = TranslatedText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            if (TranslatedLines.Length <= 24)
                            {
                                // But if the file is less than 24 lines, then just send it all at once
                                _NodeInfo.Connection.Write(TranslatedText);
                            }
                            else
                            {
                                // More than 24 lines, do it the hard way
                                for (int i = 0; i < TranslatedLines.Length; i++)
                                {
                                    _NodeInfo.Connection.Write(TranslatedLines[i]);
                                    if (i < TranslatedLines.Length - 1) _NodeInfo.Connection.WriteLn();

                                    // TODO This doesn't work when a single line of output is spread across multiple lines of input
                                    //      Should somehow count the number of lines scrolled, and pause after 24
                                    if ((i % 24 == 23) && (i < TranslatedLines.Length - 1))
                                    {
                                        _NodeInfo.Connection.Write("<more>");
                                        var Ch = ReadChar();
                                        _NodeInfo.Connection.Write("\b\b\b\b\b\b      \b\b\b\b\b\b");
                                        if (Ch.ToString().ToUpper() == "Q") return true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Nope, so just send it as is.
                            _NodeInfo.Connection.Write(TranslatedText);
                        }
                    }

                    if (pauseAtEnd)
                    {
                        ReadChar();
                        ClrScr();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (IOException ioex)
            {
                RaiseExceptionEvent("I/O exception displaying " + fileName + ": " + ioex.Message, ioex);
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override void Execute()
        {
            try
            {
                // Make telnet connections convert CRLF to CR
                _NodeInfo.Connection.LineEnding = "\r";
                _NodeInfo.Connection.StripLF = true;

                _NodeInfo.Door = new DoorInfo("RUNBBS");
                if (_NodeInfo.Door.Loaded)
                {
                    _NodeInfo.User.Alias = "Anonymous";
                    RaiseLogOnEvent("Running RUNBBS.INI process");

                    _NodeInfo.SecondsThisSession = 86400; // RUNBBS.BAT can run for 24 hours
                    RunDoor();
                }
                else
                {
                    // Handle authentication based on the connection type
                    bool Authed = false;
                    switch (_NodeInfo.ConnectionType)
                    {
                        case ConnectionType.RLogin: Authed = AuthenticateRLogin(); break;
                        case ConnectionType.Telnet: Authed = AuthenticateTelnet(); break;
                        case ConnectionType.WebSocket: Authed = AuthenticateTelnet(); break;
                    }

                    if ((Authed) && (!QuitThread()))
                    {
                        // Update the user's time remaining
                        _NodeInfo.UserLoggedOn = true;
                        _NodeInfo.SecondsThisSession = _Config.TimePerCall * 60;
                        RaiseLogOnEvent();

                        // TODO RECORD THE LOGIN SO A LAST 10 CALLERS FEATURE CAN BE IMPLEMENTED

                        // Check if RLogin is requesting to launch a door immediately via the xtrn= command
                        if (_RLoginVariables.ContainsKey("xtrn"))
                        {
                            RunDoor(_RLoginVariables["xtrn"]);
                            Thread.Sleep(2500);
                        }
                        else
                        {
                            // Do the logon process
                            RaiseNodeEvent("Running Logon Process");
                            HandleLogOnProcess();

                            // Make sure we should still proceed
                            if (QuitThread()) return;

                            // Do the logoff process
                            RaiseNodeEvent("Running Logoff Process");
                            HandleLogOffProcess();

                            // Make sure we should still proceed
                            if (QuitThread()) return;

                            DisplayAnsi("LOGOFF");
                            Thread.Sleep(2500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionEvent("Unhandled Exception in ClientThread::Execute(): " + ex.Message, ex);
            }
            finally
            {
                // Try to close the connection
                try { _NodeInfo.Connection.Close(); }
                catch { /* Ignore */ }

                // Try to free the node
                try { RaiseLogOffEvent(); }
                catch { /* Ignore */ }
            }
        }

        private void GetCurrentMenu()
        {
            // Clear the dictionary
            _CurrentMenuOptions.Clear();

            // Find out what hotkeys the menu has
            string[] HotKeys = MenuOption.GetHotKeys(_CurrentMenu);

            // Get the data for each hotkey
            for (int i = 0; i < HotKeys.Length; i++)
            {
                char HotKey = HotKeys[i].ToString().ToUpper()[0];

                try
                {
                    MenuOption MO = new MenuOption(_CurrentMenu, HotKey);
                    if ((MO.Loaded) && (_NodeInfo.User.AccessLevel >= MO.RequiredAccess))
                    {
                        bool CanAdd = true;

                        // If the option is a door, check if the door can be run on the current platform
                        if (MO.Action == Action.RunDoor)
                        {
                            DoorInfo DI = new DoorInfo(MO.Parameters);
                            if (DI.Loaded)
                            {
                                // Determine how to run the door
                                if (DI.Native)
                                {
                                    // Native is always OK
                                }
                                else if (OSUtils.IsWindows)
                                {
                                    if (ProcessUtils.Is64BitOperatingSystem)
                                    {
                                        // DOS doors are not OK on 64bit Windows if DOSBox is installed
                                        CanAdd = IsDOSBoxInstalled();
                                    }
                                    else
                                    {
                                        // DOS doors are OK on 32bit Windows
                                    }
                                }
                                else if (OSUtils.IsUnix)
                                {
                                    // DOS doors are OK on Linux if DOSEMU is installed
                                    CanAdd = IsDOSEMUInstalled();
                                }
                                else
                                {
                                    // DOS doors are not OK on unknown platforms
                                    CanAdd = false;
                                }
                            }
                            else
                            {
                                // Can't load door, so don't display it
                                CanAdd = false;
                            }
                        }

                        if (CanAdd) _CurrentMenuOptions.Add(HotKey, MO);
                    }
                }
                catch (ArgumentException aex)
                {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RaiseExceptionEvent("Exception while loading '" + _CurrentMenu + "' menu option for '" + HotKey + "'", aex);
                }
            }
        }

        private void HandleLogOffProcess()
        {
            // Get all the sections from the ini file and sort them
            string[] Processes = LogOffProcess.GetProcesses();

            // Loop through the options, and run the ones we allow here
            bool ExitFor = false;
            for (int i = 0; i < Processes.Length; i++)
            {
                try
                {
                    LogOffProcess LP = new LogOffProcess(Processes[i]);
                    if ((LP.Loaded) && (!QuitThread()))
                    {
                        switch (LP.Action)
                        {
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
                        if (ExitFor)
                        {
                            break;
                        }
                    }
                }
                catch (ArgumentException aex)
                {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RaiseExceptionEvent("Exception executing logoff process '" + Processes[i] + "'", aex);
                }
            }
        }

        private void HandleLogOnProcess()
        {
            string[] Processes = LogOnProcess.GetProcesses();
            Action LastAction = Action.None;

            // Loop through the options, and run the ones we allow here
            bool ExitFor = false;
            for (int i = 0; i < Processes.Length; i++)
            {
                try
                {
                    LogOnProcess LP = new LogOnProcess(Processes[i]);
                    if ((LP.Loaded) && (!QuitThread()))
                    {
                        LastAction = LP.Action;

                        switch (LP.Action)
                        {
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
                        if (ExitFor)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RaiseExceptionEvent("Exception executing logon process '" + Processes[i] + "'", ex);

                    // If the exception was at the main menu, then exit the loop so we don't continue on to other items (like the twit menu)
                    if (LastAction == Action.MainMenu)
                    {
                        // TODO Display error message and pause
                        return;
                    }
                }
            }
        }

        private bool HandleMenuOption(MenuOption menuOption)
        {
            if (_NodeInfo.User.AccessLevel >= menuOption.RequiredAccess)
            {
                switch (menuOption.Action)
                {
                    case Action.ChangeMenu:
                        RaiseNodeEvent("Changing to " + menuOption.Parameters.ToUpper() + " menu");
                        _CurrentMenu = menuOption.Parameters.ToUpper();
                        return false;
                    case Action.Disconnect:
                        RaiseNodeEvent("Disconnecting");
                        _NodeInfo.Connection.Close();
                        return true;
                    case Action.DisplayFile:
                        RaiseNodeEvent("Displaying " + menuOption.Parameters);
                        DisplayFile(menuOption.Parameters, true, false, false);
                        if (menuOption.Parameters != _LastDisplayFile) RaiseNodeEvent(" displayed " + _LastDisplayFile);
                        return false;
                    case Action.DisplayFileMore:
                        RaiseNodeEvent("Displaying " + menuOption.Parameters + " (with more)");
                        DisplayFile(menuOption.Parameters, true, true, true);
                        if (menuOption.Parameters != _LastDisplayFile) RaiseNodeEvent(" displayed " + _LastDisplayFile + " (with more)");
                        return false;
                    case Action.DisplayFilePause:
                        RaiseNodeEvent("Displaying " + menuOption.Parameters + " (with pause)");
                        DisplayFile(menuOption.Parameters, true, true, false);
                        if (menuOption.Parameters != _LastDisplayFile) RaiseNodeEvent(" displayed " + _LastDisplayFile + " (with pause)");
                        return false;
                    case Action.LogOff:
                        RaiseNodeEvent("Logging off");
                        return true;
                    case Action.MainMenu:
                        RaiseNodeEvent("Changing to " + menuOption.Parameters.ToUpper() + " menu");
                        _CurrentMenu = menuOption.Parameters.ToUpper();
                        MainMenu();
                        return true;
                    case Action.Pause:
                        RaiseNodeEvent("Pausing for " + menuOption.Parameters + " seconds");
                        Thread.Sleep(int.Parse(menuOption.Parameters));
                        return false;
                    case Action.RunDoor:
                        RaiseNodeEvent("Running " + menuOption.Parameters);
                        RunDoor(menuOption.Parameters);
                        return false;
                    case Action.Telnet:
                        RaiseNodeEvent("Telnetting to " + menuOption.Parameters);
                        _NodeInfo.Door = new DoorInfo("");
                        _NodeInfo.Door.Command = "bin\\TelnetDoor.exe";
                        _NodeInfo.Door.Native = true;
                        _NodeInfo.Door.Parameters = "-D*DOOR32 -S" + menuOption.Parameters;
                        RunDoor();
                        return false;
                }
            }

            return false;
        }

        public string IPAddress
        {
            get { return _NodeInfo.Connection.GetRemoteIP(); }
        }

        private bool IsDOSBoxInstalled()
        {
            return File.Exists(@"C:\Program Files (x86)\DOSBox-0.73\DOSBox.exe"); // TODO add configuration variable so this path is not hardcoded
        }

        private bool IsDOSEMUInstalled()
        {
            return File.Exists("/usr/bin/dosemu.bin"); // TODO add configuration variable so this path is not hardcoded
        }

        private void MainMenu()
        {
            bool ExitWhile = false;
            while ((!ExitWhile) && (!QuitThread()))
            {
                // Get current menu options
                GetCurrentMenu();

                // Show menu options
                DisplayCurrentMenu();

                // Send node event message
                RaiseNodeEvent("At " + _CurrentMenu.ToUpper() + " menu");

                string HotKey = ReadChar().ToString().ToUpper();
                if (!string.IsNullOrEmpty(HotKey) && !QuitThread())
                {
                    if (_CurrentMenuOptions.ContainsKey(HotKey[0]))
                    {
                        MenuOption MO = _CurrentMenuOptions[HotKey[0]];
                        switch (MO.Action)
                        {
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

        private int MinutesLeft()
        {
            return SecondsLeft() / 60;
        }

        private void OnDoorWait(object sender, RMProcessStartAndWaitEventArgs e)
        {
            e.Stop = QuitThread();
        }

        private bool QuitThread()
        {
            if (_Stop) return true;
            if (!_NodeInfo.Connection.Connected) return true;
            if (_NodeInfo.Connection.ReadTimedOut) return true;
            if (SecondsLeft() <= 0) return true;
            return false;
        }

        private void RaiseErrorMessageEvent(string message)
        {
            EventHandler<StringEventArgs> Handler = ErrorMessageEvent;
            if (Handler != null) Handler(this, new StringEventArgs(message));
        }

        private void RaiseExceptionEvent(string message, Exception ex)
        {
            EventHandler<ExceptionEventArgs> Handler = ExceptionEvent;
            if (Handler != null) Handler(this, new ExceptionEventArgs(message, ex));
        }

        private void RaiseLogOffEvent()
        {
            EventHandler<NodeEventArgs> Handler = LogOffEvent;
            if (Handler != null) Handler(this, new NodeEventArgs(_NodeInfo, "Logging off", _Stop));
        }

        private void RaiseLogOnEvent()
        {
            RaiseLogOnEvent("Logging on");
        }

        private void RaiseLogOnEvent(string status)
        {
            EventHandler<NodeEventArgs> Handler = LogOnEvent;
            if (Handler != null) Handler(this, new NodeEventArgs(_NodeInfo, status, _Stop));
        }

        private void RaiseNodeEvent(string AStatus)
        {
            _Status = AStatus;
            EventHandler<NodeEventArgs> Handler = NodeEvent;
            if (Handler != null) Handler(this, new NodeEventArgs(_NodeInfo, AStatus, _Stop));
        }

        private void RaiseWarningMessageEvent(string message)
        {
            EventHandler<StringEventArgs> Handler = WarningMessageEvent;
            if (Handler != null) Handler(this, new StringEventArgs(message));
        }

        private char? ReadChar()
        {
            char? Result = null;

            Result = _NodeInfo.Connection.ReadChar(ReadTimeout());
            if (Result == null)
            {
                if ((!_Stop) && (_NodeInfo.Connection.Connected))
                {
                    if (SecondsLeft() > 0)
                    {
                        // User has time left so they timed out
                        DisplayAnsi("EXCEEDED_IDLE_LIMIT");
                        Thread.Sleep(2500);
                        _NodeInfo.Connection.Close();
                    }
                    else
                    {
                        // User has no time left, so they exceeded the call limit
                        DisplayAnsi("EXCEEDED_CALL_LIMIT");
                        Thread.Sleep(2500);
                        _NodeInfo.Connection.Close();
                    }
                }
            }

            return Result;
        }

        private string ReadLn()
        {
            // Call the main SocketReadLn() indicating no password character
            return ReadLn('\0');
        }

        private string ReadLn(char passwordChar)
        {
            string Result = _NodeInfo.Connection.ReadLn(passwordChar, ReadTimeout());
            if ((_NodeInfo.Connection.ReadTimedOut) && (!_Stop) && (_NodeInfo.Connection.Connected))
            {
                if (SecondsLeft() > 0)
                {
                    // User has time left so they timed out
                    DisplayAnsi("EXCEEDED_IDLE_LIMIT");
                    Thread.Sleep(2500);
                    _NodeInfo.Connection.Close();
                }
                else
                {
                    // User has no time left, so they exceeded the call limit
                    DisplayAnsi("EXCEEDED_CALL_LIMIT");
                    Thread.Sleep(2500);
                    _NodeInfo.Connection.Close();
                }
            }

            return Result;
        }

        private int ReadTimeout()
        {
            return Math.Min(5 * 60, SecondsLeft()) * 1000;
        }

        private bool Register()
        {
            bool Registered = false;

            try
            {
                DisplayAnsi("NEWUSER_HEADER");

                // Get an alias
            GetAlias:
                string Alias = "";
                while ((string.IsNullOrEmpty(Alias)) || (Alias.ToUpper() == "NEW"))
                {
                    DisplayAnsi("NEWUSER_ENTER_ALIAS");
                    Alias = ReadLn().Trim();
                    if (QuitThread()) return false;
                }

                // StartRegistration will check if the alias already exists, and if not, reserve it so there's no race condition for two people registering at the same time and both wanting the same alias
                if (!_NodeInfo.User.StartRegistration(Alias))
                {
                    // Alias has already been taken
                    DisplayAnsi("NEWUSER_ENTER_ALIAS_DUPLICATE");
                    goto GetAlias;
                }

                // Get their password
            GetPassword:
                if (_RLoginVariables.ContainsKey("callback"))
                {
                    _NodeInfo.User.SetPassword(StringUtils.RandomString(10), _Config.PasswordPepper);
                }
                else
                {
                    string Password = "";
                    while (string.IsNullOrEmpty(Password))
                    {
                        DisplayAnsi("NEWUSER_ENTER_PASSWORD");
                        Password = ReadLn('*').Trim();
                        if (QuitThread()) return false;
                    }
                    _NodeInfo.User.SetPassword(Password, _Config.PasswordPepper);
                }

                // Confirm their password (if it's not an rlogin web connection)
                if (!_RLoginVariables.ContainsKey("callback"))
                {
                    DisplayAnsi("NEWUSER_ENTER_PASSWORD_CONFIRM");
                    string Password = ReadLn('*').Trim();
                    if (QuitThread()) return false;

                    if (!_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper))
                    {
                        DisplayAnsi("NEWUSER_ENTER_PASSWORD_MISMATCH");
                        goto GetPassword;
                    }
                }

                // Loop through the questions
                string[] Questions = NewUserQuestion.GetQuestions();
                for (int i = 0; i < Questions.Length; i++)
                {
                    // Check if we have the value supplied by an rlogin web connection
                    if (_RLoginVariables.ContainsKey(Questions[i]))
                    {
                        _NodeInfo.User.AdditionalInfo[Questions[i]] = _RLoginVariables[Questions[i]];
                    }
                    else
                    {
                        NewUserQuestion Question = new NewUserQuestion(Questions[i]);

                    GetAnswer:
                        // Display prompt
                        if (DisplayAnsi("NEWUSER_ENTER_" + Questions[i]))
                        {
                            // Get answer
                            string Answer = ReadLn().Trim();
                            if (QuitThread()) return false;

                            // Check if answer is required
                            if ((Question.Required) && (string.IsNullOrEmpty(Answer))) goto GetAnswer;

                            // Check if answer requires validation
                            if (!string.IsNullOrEmpty(Answer))
                            {
                                bool Valid = true;
                                switch (Question.Validate)
                                {
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
                                if (!Valid)
                                {
                                    if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_INVALID")) _NodeInfo.Connection.WriteLn("Input is not valid!");
                                    goto GetAnswer;
                                }
                            }

                            // Check if answer requires confirmation
                            if (Question.Confirm)
                            {
                                if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_CONFIRM")) _NodeInfo.Connection.Write("Please re-enter: ");

                                string Confirm = ReadLn().Trim();
                                if (QuitThread()) return false;

                                // Check if confirmation matches
                                if (Confirm != Answer)
                                {
                                    if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_MISMATCH")) _NodeInfo.Connection.WriteLn("Text does not match!");
                                    goto GetAnswer;
                                }
                            }

                            // If we get here, the answer is valid, so save it
                            _NodeInfo.User.AdditionalInfo[Questions[i]] = Answer;
                        }
                        else
                        {
                            RaiseErrorMessageEvent("Unable to prompt new user for '" + Questions[i] + "' since ansi\\newuser_enter_" + Questions[i].ToLower() + ".ans is missing");
                        }
                    }
                }

                Registered = SendRLoginWebCallback(); // Will only perform callback if necessary
                if (!Registered) DisplayAnsi("RLOGIN_CALLBACK_ERROR");

                return Registered;
            }
            finally
            {
                if (Registered)
                {
                    lock (Globals.RegistrationLock)
                    {
                        Config C = new Config();
                        _NodeInfo.User.UserId = C.NextUserId++;
                        _NodeInfo.User.SaveRegistration();
                        C.Save();
                    }

                    DisplayAnsi("NEWUSER_SUCCESS", true);
                }
                else
                {
                    _NodeInfo.User.AbortRegistration();
                }
            }
        }

        private void RunDoor(string door)
        {
            _NodeInfo.Door = new DoorInfo(door);
            if (_NodeInfo.Door.Loaded)
            {
                RunDoor();
            }
            else
            {
                RaiseErrorMessageEvent("Unable to find door: '" + door + "'");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void RunDoor()
        {
            if (Globals.Debug) RaiseNodeEvent("DEBUG: Launching " + TranslateCLS(_NodeInfo.Door.Command) + " " + TranslateCLS(_NodeInfo.Door.Parameters));

            try
            {
                // Clear the buffers and reset the screen
                _NodeInfo.Connection.ReadString();
                ClrScr();

                // Create the node directory and drop files
                CreateNodeDirectory();

                // Determine how to run the door
                if (_NodeInfo.Door.Native)
                {
                    // Start the process
                    using (RMProcess P = new RMProcess())
                    {
                        P.ProcessWaitEvent += OnDoorWait;

                        ProcessStartInfo PSI = new ProcessStartInfo(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters));
                        PSI.WorkingDirectory = ProcessUtils.StartupPath;
                        PSI.WindowStyle = _NodeInfo.Door.WindowStyle;

                        P.StartAndWait(PSI);
                    }
                }
                else if (OSUtils.IsWindows)
                {
                    if (ProcessUtils.Is64BitOperatingSystem)
                    {

                        if (IsDOSBoxInstalled())
                        {
                            RunDoorDOSBox(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters));
                        }
                        else
                        {
                            RaiseErrorMessageEvent("DOS doors are not supported on 64bit Windows (unless you install DOSBox 0.73)");
                        }
                    }
                    else
                    {
                        if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                        {
                            RunDoorSBBSEXEC9x(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters), _NodeInfo.Door.ForceQuitDelay);
                        }
                        else
                        {
                            RunDoorSBBSEXECNT(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters), _NodeInfo.Door.ForceQuitDelay);
                        }
                    }
                }
                else if (OSUtils.IsUnix)
                {
                    if (IsDOSEMUInstalled())
                    {
                        // TODO Doesn't this allow a door to hang if the user hangs up?  We need some method to force-quit it!
                        RunDoorDOSEMU(TranslateCLS(_NodeInfo.Door.Command), TranslateCLS(_NodeInfo.Door.Parameters));
                    }
                    else
                    {
                        RaiseErrorMessageEvent("DOS doors are not supported on Linux (unless you install DOSEMU)");
                    }
                }
                else
                {
                    RaiseErrorMessageEvent("Unable to run non-native door on current platform");
                }
            }
            catch (Exception ex)
            {
                RaiseExceptionEvent("Unhandled Exception while running door '" + _NodeInfo.Door.Name + "': " + ex.Message, ex);
            }
            finally
            {
                // Clean up
                try
                {
                    ClrScr();
                    _NodeInfo.Connection.SetBlocking(true); // In case native door disabled blocking sockets
                    DeleteNodeDirectory();
                }
                catch { /* Ignore */ }
            }
        }

        private void RunDoorDOSBox(string command, string parameters)
        {
            string DOSBoxConf = StringUtils.PathCombine("node" + _NodeInfo.Node.ToString(), "dosbox.conf");
            string DOSBoxExe = @"C:\Program Files (x86)\DOSBox-0.73\DOSBox.exe"; // TODO add configuration variable so this path is not hardcoded

            // Copy base dosbox.conf 
            FileUtils.FileDelete(DOSBoxConf);
            FileUtils.FileCopy("dosbox.conf", DOSBoxConf);

            // If we're running a batch file, add a CALL to it
            if (command.ToUpper().Contains(".BAT")) command = "call " + command;

            string[] ExternalBat = new string[] { "mount c " + StringUtils.ExtractShortPathName(ProcessUtils.StartupPath), "C:", command + " " + parameters, "exit" };
            FileUtils.FileAppendAllText(DOSBoxConf, string.Join("\r\n", ExternalBat));

            string Arguments = "-telnet -conf " + DOSBoxConf + " -socket " + _NodeInfo.Connection.GetSocket().Handle.ToInt32().ToString();
            if (Globals.Debug) RaiseNodeEvent("Executing " + DOSBoxExe + " " + Arguments);

            // Start the process
            using (RMProcess P = new RMProcess())
            {
                P.ProcessWaitEvent += OnDoorWait;

                ProcessStartInfo PSI = new ProcessStartInfo(DOSBoxExe, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;

                P.StartAndWait(PSI);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void RunDoorDOSEMU(string command, string parameters)
        {
            PseudoTerminal pty = null;
            Mono.Unix.UnixStream us = null;
            int WaitStatus;

            try
            {
                bool DataTransferred = false;
                int LoopsSinceIO = 0;
                Exception ReadException = null;

                // If we're running a batch file, add a CALL to it
                if (command.ToUpper().Contains(".BAT")) command = "call " + command;

                string[] ExternalBat = new string[] { "@echo off", "lredir g: linux\\fs" + ProcessUtils.StartupPath, "set path=%path%;g:\\dosutils", "fossil.com", "share.com", "ansi.com", "g:", command + " " + parameters, "exitemu" };
                FileUtils.FileWriteAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "external.bat"), String.Join("\r\n", ExternalBat), RMEncoding.Ansi);

                string[] Arguments = new string[] { "HOME=" + ProcessUtils.StartupPath, "HOME=" + ProcessUtils.StartupPath, "QUIET=1", "DOSDRIVE_D=" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString()), "/usr/bin/nice", "-n19", "/usr/bin/dosemu.bin", "-Ivideo { none }", "-Ikeystroke \\r", "-Iserial { virtual com 1 }", "-Ed:external.bat", "-o" + StringUtils.PathCombine(ProcessUtils.StartupPath, "node" + _NodeInfo.Node.ToString(), "dosemu.log") };//, "2> /gamesrv/NODE" + _NodeInfo.Node.ToString() + "/DOSEMU_BOOT.LOG" }; // TODO add configuration variable so this path is not hardcoded
                if (Globals.Debug) RaiseNodeEvent("Executing /usr/bin/env " + string.Join(" ", Arguments));

                lock (Globals.PrivilegeLock)
                {
                    try
                    {
                        Globals.NeedRoot();
                        pty = PseudoTerminal.Open(null, "/usr/bin/env", Arguments, "/tmp", 80, 25, false, false, false);
                        us = new Mono.Unix.UnixStream(pty.FileDescriptor, false);
                    }
                    finally
                    {
                        Globals.DropRoot(_Config.UnixUser);
                    }
                }

                new Thread(delegate(object p)
                {
                    // Send data from door to user
                    try
                    {
                        byte[] Buffer = new byte[10240];
                        int NumRead = 0;

                        while (!QuitThread())
                        {
                            NumRead = us.Read(Buffer, 0, Buffer.Length);
                            if (NumRead > 0)
                            {
                                _NodeInfo.Connection.WriteBytes(Buffer, NumRead);
                                DataTransferred = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReadException = ex;
                    }
                }).Start();

                // Check if we need to run cpulimit
                if (File.Exists(StringUtils.PathCombine(ProcessUtils.StartupPath, "cpulimit.sh"))) Process.Start(StringUtils.PathCombine(ProcessUtils.StartupPath, "cpulimit.sh"), pty.ChildPid.ToString());

                // Loop until something happens
                while (!_Stop) // TODO Check inside loop for other things QuitThread() would check
                {
                    DataTransferred = false;

                    // Check for exception in read thread
                    if (ReadException != null) return;

                    // Check for dropped carrier
                    if (!_NodeInfo.Connection.Connected)
                    {
                        int Sleeps = 0;

                        RaiseNodeEvent("User hung-up while in external program");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGHUP);
                        while ((Sleeps++ < 5) && (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0))
                        {
                            Thread.Sleep(1000);
                        }
                        if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0)
                        {
                            RaiseNodeEvent("Process still active after waiting 5 seconds");
                        }
                        return;
                    }

                    // Send data from user to door
                    if (_NodeInfo.Connection.CanRead())
                    {
                        // Write the text to the program
                        byte[] Bytes = _NodeInfo.Connection.ReadBytes();
                        for (int i = 0; i < Bytes.Length; i++)
                        {
                            us.WriteByte((byte)Bytes[i]);
                            us.Flush();
                        }

                        DataTransferred = true;
                    }

                    // Checks to perform when there was no I/O
                    if (!DataTransferred)
                    {
                        LoopsSinceIO++;

                        // Only check process termination after 300 milliseconds of no I/O
                        // to allow for last minute reception of output from DOS programs
                        if (LoopsSinceIO >= 3)
                        {
                            // Check if door terminated
                            if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) != 0)
                            {
                                break;
                            }
                        }

                        // Let's make sure the socket is up
                        // Sending will trigger a socket d/c detection
                        if (LoopsSinceIO >= 300)
                        {
                            switch (_NodeInfo.ConnectionType)
                            {
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
                    }
                    else
                    {
                        LoopsSinceIO = 0;
                    }
                }
            }
            finally
            {
                // Terminate process if it hasn't closed yet
                if (pty != null)
                {
                    if (Mono.Unix.Native.Syscall.waitpid(pty.ChildPid, out WaitStatus, Mono.Unix.Native.WaitOptions.WNOHANG) == 0)
                    {
                        RaiseNodeEvent("Terminating process");
                        Mono.Unix.Native.Syscall.kill(pty.ChildPid, Mono.Unix.Native.Signum.SIGKILL);
                    }
                    pty.Dispose();
                }
            }
        }

        struct sbbsexec_start_t
        {
            public uint Mode;
            public IntPtr Event;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private unsafe void RunDoorSBBSEXEC9x(string command, string parameters, int forceQuitDelay)
        {
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

            try
            {
                // Create temporary environment file
                FileUtils.FileWriteAllText(EnvFile, Environment.GetEnvironmentVariable("COMSPEC") + " /C " + command + " " + parameters);

                // Load vxd to intercept interrupts
                VxD = NativeMethods.CreateFile("\\\\.\\" + StringUtils.PathCombine(ProcessUtils.StartupPath, "sbbsexec.vxd"), NativeMethods.FileAccess.None, NativeMethods.FileShare.None, IntPtr.Zero, NativeMethods.CreationDisposition.New, NativeMethods.CreateFileAttributes.DeleteOnClose, IntPtr.Zero);
                int LastWin32Error = Marshal.GetLastWin32Error();
                if (VxD == (IntPtr)NativeMethods.INVALID_HANDLE_VALUE)
                {
                    RaiseErrorMessageEvent("CreateFile() failed to load VxD: " + LastWin32Error.ToString());
                    return;
                }

                StartEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, null);
                if (StartEvent == IntPtr.Zero)
                {
                    RaiseErrorMessageEvent("CreateEvent() failed to create StartEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }
                Start.Event = NativeMethods.OpenVxDHandle(StartEvent);
                Start.Mode = SBBSEXEC_MODE_FOSSIL;

                StartPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Start));
                Marshal.StructureToPtr(Start, StartPtr, true);
                StartSize = (uint)Marshal.SizeOf(Start);

                if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_START, StartPtr, StartSize, IntPtr.Zero, 0, out BytesRead, IntPtr.Zero))
                {
                    RaiseErrorMessageEvent("Error executing SBBSEXEC_IOCTL_START: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Start the process
                string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "DOSXTRN.EXE");
                string Arguments = StringUtils.ExtractShortPathName(EnvFile) + " 95 " + _NodeInfo.Node.ToString() + " " + Start.Mode.ToString() + " " + LoopsBeforeYield.ToString();
                ProcessStartInfo PSI = new ProcessStartInfo(FileName, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;
                P = RMProcess.Start(PSI);

                if (P == null)
                {
                    RaiseErrorMessageEvent("Error launching " + FileName + " " + Arguments);
                    return;
                }

                // Wait for notification from VXD that new VM has started
                if (NativeMethods.WaitForSingleObject(StartEvent, 5000) != (uint)NativeMethods.WAIT_OBJECT_0)
                {
                    RaiseErrorMessageEvent("WaitForSingleObject() timeout while waiting for StartEvent");
                    return;
                }

                // Mark as closed
                NativeMethods.CloseHandle(StartEvent);
                StartEvent = IntPtr.Zero;

                VM = Marshal.AllocHGlobal(sizeof(uint));
                if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_COMPLETE, IntPtr.Zero, 0, VM, sizeof(uint), out BytesRead, IntPtr.Zero))
                {
                    RaiseErrorMessageEvent("Error executing SBBSEXEC_IOCTL_COMPLETE: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }
                VMValue = RMEncoding.Ansi.GetString(BitConverter.GetBytes(Marshal.ReadInt32(VM)));

                // Loop until something happens
                BytesWritten = Marshal.AllocHGlobal(sizeof(uint));
                ReadBuffer = Marshal.AllocHGlobal((int)XTRN_IO_BUF_LEN);
                while (!_Stop)// TODO Check inside loop for other things QuitThread() would check
                {
                    DataTransferred = false;

                    // Check for dropped carrier
                    if (!_NodeInfo.Connection.Connected)
                    {
                        RaiseNodeEvent("User hung-up while in external program");

                        if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_DISCONNECT, VM, sizeof(uint), IntPtr.Zero, 0, out BytesRead, IntPtr.Zero))
                        {
                            RaiseErrorMessageEvent("Error executing SBBSEXEC_IOCTL_DISCONNECT: " + Marshal.GetLastWin32Error().ToString());
                            return;
                        }

                        // Wait up to forceQuitDelay seconds for the process to terminate
                        for (int i = 0; i < forceQuitDelay; i++)
                        {
                            if (P.HasExited) return;
                            P.WaitForExit(1000);
                        }
                        RaiseNodeEvent("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                        return;
                    }

                    // Write to VxD (send data from user to door)
                    if (_NodeInfo.Connection.CanRead())
                    {
                        IntPtr InBuf = IntPtr.Zero;
                        try
                        {
                            string ToSend = VMValue + _NodeInfo.Connection.PeekString();
                            InBuf = Marshal.StringToHGlobalAnsi(ToSend);

                            if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_WRITE, InBuf, (uint)ToSend.Length, BytesWritten, sizeof(uint), out BytesRead, IntPtr.Zero))
                            {
                                RaiseErrorMessageEvent("Error executing SBBSEXEC_IOCTL_WRITE: " + Marshal.GetLastWin32Error().ToString());
                                return;
                            }

                            // Since we only peeked above, now we want to actually read the number of bytes the VxD accepted
                            _NodeInfo.Connection.ReadBytes(Marshal.ReadInt32(BytesWritten));
                            DataTransferred = true;
                        }
                        finally
                        {
                            Marshal.ZeroFreeGlobalAllocAnsi(InBuf);
                        }
                    }

                    // Read from VxD (send data from door to user)
                    if (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_READ, VM, sizeof(uint), ReadBuffer, XTRN_IO_BUF_LEN, out BytesRead, IntPtr.Zero))
                    {
                        RaiseErrorMessageEvent("Error executing SBBSEXEC_IOCTL_READ: " + Marshal.GetLastWin32Error().ToString());
                        return;
                    }

                    // If we read something, write it to the user
                    if (BytesRead > 0)
                    {
                        _NodeInfo.Connection.Write(Marshal.PtrToStringAnsi(ReadBuffer, (int)BytesRead));
                        DataTransferred = true;
                    }

                    // Checks to perform when there was no I/O
                    if (!DataTransferred)
                    {
                        // Numer of loop iterations with no I/O
                        LoopsSinceIO++;

                        // Only check process termination after 300 milliseconds of no I/O
                        // to allow for last minute reception of output from DOS programs
                        if (LoopsSinceIO >= 3)
                        {
                            // Check if process terminated
                            if (P.HasExited) return;
                        }

                        // Only send telnet GA every 30 seconds of no I/O
                        if (LoopsSinceIO % 300 == 0)
                        {
                            switch (_NodeInfo.ConnectionType)
                            {
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
                    }
                    else
                    {
                        LoopsSinceIO = 0;
                    }
                }
            }
            finally
            {
                if ((VxD != IntPtr.Zero) && (!NativeMethods.DeviceIoControl(VxD, SBBSEXEC_IOCTL_STOP, VM, sizeof(uint), IntPtr.Zero, 0, out BytesRead, IntPtr.Zero)))
                {
                    RaiseErrorMessageEvent("Error executing SBBSEXEC_IOCTL_STOP: " + Marshal.GetLastWin32Error().ToString());
                }

                // Terminate process if it hasn't closed yet
                if ((P != null) && !P.HasExited)
                {
                    RaiseErrorMessageEvent("Door still running, performing a force quit");
                    P.Kill();
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private unsafe void RunDoorSBBSEXECNT(string command, string parameters, int forceQuitDelay)
        {
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

            try
            {
                // Create temporary environment file
                FileUtils.FileWriteAllText(EnvFile, Environment.GetEnvironmentVariable("COMSPEC") + " /C " + command + " " + parameters);

                // Create a hungup event for when user drops carrier
                HungUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hungup" + _NodeInfo.Node.ToString());
                if (HungUpEvent == IntPtr.Zero)
                {
                    RaiseErrorMessageEvent("CreateEvent() failed to create HungUpEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Create a hangup event (for when the door requests to drop DTR)
                HangUpEvent = NativeMethods.CreateEvent(IntPtr.Zero, true, false, "sbbsexec_hangup" + _NodeInfo.Node.ToString());
                if (HangUpEvent == IntPtr.Zero)
                {
                    RaiseErrorMessageEvent("CreateEvent() failed to create HangUpEvent: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Create a read mail slot
                ReadSlot = NativeMethods.CreateMailslot("\\\\.\\mailslot\\sbbsexec\\rd" + _NodeInfo.Node.ToString(), XTRN_IO_BUF_LEN, 0, IntPtr.Zero);
                if (ReadSlot == IntPtr.Zero)
                {
                    RaiseErrorMessageEvent("CreateMailslot() failed to create ReadSlot: " + Marshal.GetLastWin32Error().ToString());
                    return;
                }

                // Start the process
                string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "DOSXTRN.EXE");
                string Arguments = StringUtils.ExtractShortPathName(EnvFile) + " NT " + _NodeInfo.Node.ToString() + " " + SBBSEXEC_MODE_FOSSIL.ToString() + " " + LoopsBeforeYield.ToString();
                ProcessStartInfo PSI = new ProcessStartInfo(FileName, Arguments);
                PSI.WorkingDirectory = ProcessUtils.StartupPath;
                PSI.WindowStyle = _NodeInfo.Door.WindowStyle;
                P = RMProcess.Start(PSI);

                if (P == null)
                {
                    RaiseErrorMessageEvent("Error launching " + FileName + " " + Arguments);
                    return;
                }

                // Loop until something happens
                bool DataTransferred = false;
                while (!_Stop)// TODO Check inside loop for other things QuitThread() would check
                {
                    DataTransferred = false;

                    // Check for dropped carrier
                    if (!_NodeInfo.Connection.Connected)
                    {
                        RaiseNodeEvent("User hung-up while in external program");
                        NativeMethods.SetEvent(HungUpEvent);

                        // Wait up to forceQuitDelay seconds for the process to terminate
                        for (int i = 0; i < forceQuitDelay; i++)
                        {
                            if (P.HasExited) return;
                            P.WaitForExit(1000);
                        }
                        RaiseNodeEvent("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                        return;
                    }

                    // Send data from user to door
                    if (_NodeInfo.Connection.CanRead())
                    {
                        // If our writeslot doesnt exist yet, create it
                        if (WriteSlot == IntPtr.Zero)
                        {
                            // Create A Write Mail Slot
                            WriteSlot = NativeMethods.CreateFile("\\\\.\\mailslot\\sbbsexec\\wr" + _NodeInfo.Node.ToString(), NativeMethods.FileAccess.GenericWrite, NativeMethods.FileShare.Read, IntPtr.Zero, NativeMethods.CreationDisposition.OpenExisting, NativeMethods.CreateFileAttributes.Normal, IntPtr.Zero);
                            int LastWin32Error = Marshal.GetLastWin32Error();
                            if (WriteSlot == IntPtr.Zero)
                            {
                                RaiseErrorMessageEvent("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                                return;
                            }
                            else if (WriteSlot.ToInt32() == -1)
                            {
                                if (LastWin32Error == 2)
                                {
                                    // ERROR_FILE_NOT_FOUND - User must have hit a key really fast to trigger this!
                                    RaiseWarningMessageEvent("CreateFile() failed to find WriteSlot: \\\\.\\mailslot\\sbbsexec\\wr" + _NodeInfo.Node.ToString());
                                    WriteSlot = IntPtr.Zero;
                                    Thread.Sleep(100);
                                }
                                else
                                {
                                    RaiseErrorMessageEvent("CreateFile() failed to create WriteSlot: " + LastWin32Error.ToString());
                                    return;
                                }
                            }
                        }

                        // Write the text to the program
                        if (WriteSlot != IntPtr.Zero)
                        {
                            byte[] BufBytes = _NodeInfo.Connection.PeekBytes();
                            uint BytesWritten = 0;
                            bool Result = NativeMethods.WriteFile(WriteSlot, BufBytes, (uint)BufBytes.Length, out BytesWritten, null);
                            int LastWin32Error = Marshal.GetLastWin32Error();
                            if (Result)
                            {
                                _NodeInfo.Connection.ReadBytes((int)BytesWritten);
                                DataTransferred = true;
                            }
                            else
                            {
                                RaiseErrorMessageEvent("Error calling WriteFile(): " + LastWin32Error.ToString());
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
                    if (NativeMethods.GetMailslotInfo(ReadSlot, ref MaxMessageSize, ref NextSize, ref  MessageCount, ref ReadTimeout))
                    {
                        byte[] BufBytes = new byte[XTRN_IO_BUF_LEN];
                        int BufPtr = 0;

                        // If a message is waiting, get it
                        for (int i = 0; i < MessageCount; i++)
                        {
                            // Read the next message
                            fixed (byte* p = BufBytes)
                            {
                                if (NativeMethods.ReadFile(ReadSlot, p + BufPtr, (uint)(BufBytes.Length - BufPtr), out BytesRead, null))
                                {
                                    if (BytesRead > 0)
                                    {
                                        BufPtr += (int)BytesRead;

                                        // If we have filled the buffer, break
                                        if (BufPtr >= BufBytes.Length) break;
                                    }
                                }
                            }
                        }

                        // If we read something, write it to the user
                        if (BufPtr > 0)
                        {
                            _NodeInfo.Connection.WriteBytes(BufBytes, BufPtr);
                            DataTransferred = true;
                        }
                    }

                    // Checks to perform when there was no I/O
                    if (!DataTransferred)
                    {
                        // Numer of loop iterations with no I/O
                        LoopsSinceIO++;

                        // Only check process termination after 300 milliseconds of no I/O
                        // to allow for last minute reception of output from DOS programs
                        if (LoopsSinceIO >= 3)
                        {
                            if ((_NodeInfo.Door.WatchDTR) && (NativeMethods.WaitForSingleObject(HangUpEvent, 0) == NativeMethods.WAIT_OBJECT_0))
                            {
                                RaiseNodeEvent("External program requested hangup (dropped DTR)");
                                _NodeInfo.Connection.Close();

                                // Wait up to forceQuitDelay seconds for the process to terminate
                                for (int i = 0; i < forceQuitDelay; i++)
                                {
                                    if (P.HasExited) return;
                                    P.WaitForExit(1000);
                                }
                                RaiseNodeEvent("Process still active after waiting " + forceQuitDelay.ToString() + " seconds");
                                return;
                            }

                            if (P.HasExited)
                            {
                                RaiseNodeEvent("External terminated with exit code: " + P.ExitCode);
                                break;
                            }
                        }

                        // Let's make sure the socket is up
                        // Sending will trigger a socket d/c detection
                        if (LoopsSinceIO % 300 == 0)
                        {
                            switch (_NodeInfo.ConnectionType)
                            {
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
                    }
                    else
                    {
                        LoopsSinceIO = 0;
                    }
                }
            }
            finally
            {
                // Terminate process if it hasn't closed yet
                if ((P != null) && !P.HasExited)
                {
                    RaiseErrorMessageEvent("Door still running, performing a force quit");
                    P.Kill();
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

        private int SecondsLeft()
        {
            return _NodeInfo.SecondsThisSession - (int)DateTime.Now.Subtract(_NodeInfo.TimeOn).TotalSeconds;
        }

        private bool SendRLoginWebCallback()
        {
            if (_RLoginVariables.ContainsKey("callback"))
            {
                // Send the callback to the requested server
                NameValueCollection NVC = new NameValueCollection();
                NVC.Add("CallbackId", _RLoginVariables["callbackid"]);
                NVC.Add("Alias", _NodeInfo.User.Alias);
                NVC.Add("PasswordHash", _NodeInfo.User.PasswordHash);
                foreach (DictionaryEntry DE in _NodeInfo.User.AdditionalInfo)
                {
                    NVC.Add(DE.Key.ToString(), DE.Value.ToString());
                }
                return WebUtils.HttpPost(_RLoginVariables["callback"], NVC).Contains("SUCCESS");
            }

            // If we get here, no callback was necessary
            return true;
        }

        public void Start(int node, TcpConnection connection, ConnectionType connectionType, TerminalType terminalType)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            _NodeInfo.TerminalType = terminalType;
            _NodeInfo.Node = node;
            _NodeInfo.Connection = connection;
            _NodeInfo.ConnectionType = connectionType;

            base.Start();
        }

        public string Status
        {
            get { return _Status; }
        }

        public override void Stop()
        {
            // Close the socket so that any waits on ReadLn(), ReadChar(), etc, will not block
            _NodeInfo.Connection.Close();

            base.Stop();
        }

        private string TranslateCLS(string command)
        {
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
            foreach (DictionaryEntry DE in _NodeInfo.User.AdditionalInfo)
            {
                CLS.Add(new KeyValuePair<string, string>("**" + DE.Key.ToString(), DE.Value.ToString()));
            }

            // Perform translation
            for (int i = 0; i < CLS.Count; i++)
            {
                if (CLS[i].Value != null)
                {
                    command = command.Replace("*" + CLS[i].Key.ToString().ToUpper(), CLS[i].Value.ToString());
                }
            }
            return command;
        }

        private string TranslateMCI(string text, string fileName)
        {
            // TODO @USERLASTCALL@

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
            foreach (DictionaryEntry DE in _NodeInfo.User.AdditionalInfo)
            {
                MCI.Add(DE.Key.ToString(), DE.Value.ToString());
            }
            if (text.Contains("WHOSONLINE_"))
            {
                EventHandler<WhoIsOnlineEventArgs> Handler = WhoIsOnlineEvent;
                if (Handler != null)
                {
                    WhoIsOnlineEventArgs WOEA = new WhoIsOnlineEventArgs();
                    Handler(this, WOEA);
                    foreach (DictionaryEntry DE in WOEA.WhoIsOnline)
                    {
                        MCI.Add(DE.Key.ToString(), DE.Value.ToString());
                    }
                }
            }

            // Perform MCI Translations
            foreach (DictionaryEntry DE in MCI)
            {
                if (DE.Value != null)
                {
                    text = text.Replace("{" + DE.Key.ToString().ToUpper() + "}", DE.Value.ToString());
                    for (int PadWidth = 1; PadWidth <= 80; PadWidth++)
                    {
                        // Now translate anything that needs right padding
                        text = text.Replace("{" + DE.Key.ToString().ToUpper() + PadWidth.ToString() + "}", StringUtils.PadRight(DE.Value.ToString(), ' ', PadWidth));

                        // And now translate anything that needs left padding
                        text = text.Replace("{" + PadWidth.ToString() + DE.Key.ToString().ToUpper() + '}', StringUtils.PadLeft(DE.Value.ToString(), ' ', PadWidth));
                    }
                }
            }

            return text;
        }

        private class LogOffProcess : ConfigHelper
        {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Parameters { get; set; }
            public int RequiredAccess { get; set; }

            private LogOffProcess()
            {
                // Don't let the user instantiate this without a constructor
            }

            public LogOffProcess(string section)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "logoffprocess.ini"))
            {
                Name = "";
                Action = Action.None;
                Parameters = "";
                RequiredAccess = 0;

                Load(section);
            }

            static public string[] GetProcesses()
            {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "logoffprocess.ini")))
                {
                    return Ini.ReadSections();
                }
            }
        }

        private class LogOnProcess : ConfigHelper
        {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Parameters { get; set; }
            public int RequiredAccess { get; set; }

            private LogOnProcess()
            {
                // Don't let the user instantiate this without a constructor
            }

            public LogOnProcess(string section)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "logonprocess.ini"))
            {
                Name = "";
                Action = Action.None;
                Parameters = "";
                RequiredAccess = 0;

                Load(section);
            }

            static public string[] GetProcesses()
            {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "logonprocess.ini")))
                {
                    return Ini.ReadSections();
                }
            }
        }

        private class MenuOption : ConfigHelper
        {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Parameters { get; set; }
            public int RequiredAccess { get; set; }

            private MenuOption()
            {
                // Don't let the user instantiate this without a constructor
            }

            public MenuOption(string menu, char hotKey)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("menus", menu.ToLower() + ".ini"))
            {
                Name = "";
                Action = Action.None;
                Parameters = "";
                RequiredAccess = 0;

                Load(hotKey.ToString());
            }

            static public string[] GetHotKeys(string menu)
            {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("menus", menu.ToLower() + ".ini"))))
                {
                    return Ini.ReadSections();
                }
            }
        }

        private class NewUserQuestion : ConfigHelper
        {
            public bool Confirm { get; set; }
            public bool Required { get; set; }
            public ValidationType Validate { get; set; }

            private NewUserQuestion()
            {
                // Don't let the user instantiate this without a constructor
            }

            public NewUserQuestion(string question)
                : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "newuser.ini"))
            {
                Confirm = false;
                Required = false;
                Validate = ValidationType.None;

                Load(question);
            }

            static public string[] GetQuestions()
            {
                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("config", "newuser.ini"))))
                {
                    return Ini.ReadSections();
                }
            }
        }

        private enum Action
        {
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

        private enum ValidationType
        {
            Email,
            None,
            Numeric,
            TwoWords
        }
    }
}
