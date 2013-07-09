using RandM.GameSrv;
using RandM.RMLib;
using RandM.RMLibUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Upgrade
{
    public partial class frmMain : Form
    {
        private string SQL = "";
        private string _PasswordPepper;

        public frmMain()
        {
            InitializeComponent();
        }

        private void AddToLog(string message)
        {
            richTextBox1.AppendText(message + "\r\n");
        }

        private void cmdUpgrade_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog OFD = new OpenFileDialog())
                {
                    OFD.FileName = "GameSrv.db";
                    OFD.Filter = "GameSrv.db|GameSrv.db";
                    if (OFD.ShowDialog() == DialogResult.OK)
                    {
                        richTextBox1.Clear();
                        using (RMSQLiteConnection DB = new RMSQLiteConnection(Path.GetDirectoryName(OFD.FileName), Path.GetFileName(OFD.FileName), false))
                        {
                            SQL = "SELECT Value FROM ConfigTbl WHERE Option = '_DBVERSION'";
                            if (DB.ExecuteScalar(SQL).ToString() == "6")
                            {
                                // Values that can be harvested for gamesrv.ini
                                HandleGameSrvDotIni(DB);

                                // Values that can be harvested for logoffprocess.ini (LogoutProcessTbl order by StepNumber)
                                HandleLogOffOrOnProcessDotIni(DB, "off", "out");
                                HandleLogOffOrOnProcessDotIni(DB, "on", "in");

                                // Values that can be harvested for doors
                                HandleDoors(DB);

                                // Values that can be harvested for menus
                                HandleMenus(DB);

                                // Values that can be harvested for users
                                HandleUsers(DB);
                            }
                            else
                            {
                                Dialog.Error("Sorry, that file doesn't appear to be from a GameSrv v10.04.02 install", "Upgrade aborted");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dialog.Error("Error upgrading: " + ex.Message, "Upgrade failed");
            }
        }

        private string CommandToAction(string command)
        {
            switch (command)
            {
                case "CHANGE_MENU": return "ChangeMenu";
                case "DISCONNECT": return "Disconnect";
                case "DISPLAY_FILE": return "DisplayFile";
                case "DISPLAY_FILE_PAUSE": return "DisplayFilePause";
                case "EXEC": return "RunDoor";
                case "EXEC_NETFOSS": return "RunDoor";
                case "EXEC_MSYNCFOS": return "RunDoor";
                case "LOGOUT": return "Logoff";
                case "MAINMENU": return "MainMenu";
                case "PAUSE": return "Pause";
            }

            return command;
        }

        private string GetSafeDoorFileName(string name)
        {
            return Regex.Replace(name, "[^a-zA-Z0-9 _-]", "").Replace(" ", "_");
        }

        private void HandleDoors(RMSQLiteConnection DB)
        {
            AddToLog("Importing settings for doors.ini");
            SQL = "SELECT * FROM MenuTbl WHERE Command IN ('EXEC', 'EXEC_NETFOSS', 'EXEC_MSYNCFOS')";
            DB.ExecuteReader(SQL);
            while (DB.Reader.Read())
            {
                string Name = DB.Reader["Description"].ToString();
                string[] CommandAndParameters = DB.Reader["Parameters"].ToString().Split(' ');
                string Command = CommandAndParameters[0];
                string Parameters = string.Join(" ", CommandAndParameters, 1, CommandAndParameters.Length - 1);
                bool Native = DB.Reader["Command"].ToString() == "EXEC";

                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "doors", GetSafeDoorFileName(Name) + ".ini")))
                {
                    Ini.WriteString("DOOR", "Name", Name);
                    Ini.WriteString("DOOR", "Command", Command);
                    Ini.WriteString("DOOR", "Parameters", Parameters);
                    Ini.WriteString("DOOR", "Native", Native.ToString());
                    Ini.WriteString("DOOR", "ForceQuitDelay", "5");
                    Ini.WriteString("DOOR", "WindowStyle", "Minimized");
                }

                AddToLog(" - Added Name = " + Name);
                AddToLog("         Command = " + Command);
                AddToLog("         Parameters = " + Parameters);
                AddToLog("         Native = " + Native.ToString());
            }
            DB.Reader.Close();
            AddToLog("");
        }

        private void HandleGameSrvDotIni(RMSQLiteConnection DB)
        {
            AddToLog("Importing settings for gamesrv.ini");
            using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "gamesrv.ini")))
            {
                string BBSName = DB.ExecuteScalar("SELECT Value FROM ConfigTbl WHERE Option = 'BBSName'").ToString();
                Ini.WriteString("CONFIGURATION", "BBSName", BBSName);
                AddToLog(" - BBSName = " + BBSName);

                string SysopName = DB.ExecuteScalar("SELECT Value FROM ConfigTbl WHERE Option = 'SysOpName'").ToString();
                string SysopFirstName = SysopName;
                string SysopLastName = "";
                if (SysopName.Trim().Contains(" "))
                {
                    string[] SysopNameElements = SysopName.Split(' ');
                    SysopFirstName = SysopNameElements[0];
                    SysopLastName = string.Join(" ", SysopNameElements, 1, SysopNameElements.Length - 1);
                }
                Ini.WriteString("CONFIGURATION", "SysopFirstName", SysopFirstName);
                AddToLog(" - SysopFirstName = " + SysopFirstName);
                Ini.WriteString("CONFIGURATION", "SysopLastName", SysopLastName);
                AddToLog(" - SysopLastName = " + SysopLastName);

                string SysopEmail = DB.ExecuteScalar("SELECT Value FROM ConfigTbl WHERE Option = 'SysOpEmailAddress'").ToString();
                Ini.WriteString("CONFIGURATION", "SysopEmail", SysopEmail);
                AddToLog(" - SysopEmail = " + SysopEmail);

                int FirstNode = Convert.ToInt32(DB.ExecuteScalar("SELECT Value FROM ConfigTbl WHERE Option = 'FirstNode'"));
                int Nodes = Convert.ToInt32(DB.ExecuteScalar("SELECT Value FROM ConfigTbl WHERE Option = 'Nodes'"));
                int LastNode = FirstNode + Nodes - 1;
                Ini.WriteString("CONFIGURATION", "FirstNode", FirstNode.ToString());
                AddToLog(" - FirstNode = " + FirstNode.ToString());
                Ini.WriteString("CONFIGURATION", "LastNode", LastNode.ToString());
                AddToLog(" - LastNode = " + LastNode.ToString());

                string TimePerCall = DB.ExecuteScalar("SELECT MinutesPerCall FROM GroupTbl WHERE GroupID = 2").ToString();
                Ini.WriteString("CONFIGURATION", "TimePerCall", TimePerCall);
                AddToLog(" - TimePerCall = " + TimePerCall);

                string NextUserId = DB.ExecuteScalar("SELECT MAX(UserID) + 1 FROM UserTbl").ToString();
                Ini.WriteString("CONFIGURATION", "NextUserId", NextUserId);
                AddToLog(" - NextUserId = " + NextUserId);

                if (DB.ExecuteScalar("SELECT COUNT(*) FROM ServerThreadTbl WHERE ConnectionType = 2").ToString() != "0")
                {
                    string TelnetServerIP = DB.ExecuteScalar("SELECT LocalAddress FROM ServerThreadTbl WHERE ConnectionType = 2 ORDER BY ServerThreadID LIMIT 1").ToString();
                    Ini.WriteString("CONFIGURATION", "TelnetServerIP", TelnetServerIP);
                    AddToLog(" - TelnetServerIP = " + TelnetServerIP);

                    string TelnetServerPort = DB.ExecuteScalar("SELECT LocalPort FROM ServerThreadTbl WHERE ConnectionType = 2 ORDER BY ServerThreadID LIMIT 1").ToString();
                    Ini.WriteString("CONFIGURATION", "TelnetServerPort", TelnetServerPort);
                    AddToLog(" - TelnetServerPort = " + TelnetServerPort);
                }

                if (DB.ExecuteScalar("SELECT COUNT(*) FROM ServerThreadTbl WHERE ConnectionType = 1").ToString() != "0")
                {
                    string RLoginServerIP = DB.ExecuteScalar("SELECT LocalAddress FROM ServerThreadTbl WHERE ConnectionType = 1 ORDER BY ServerThreadID LIMIT 1").ToString();
                    Ini.WriteString("CONFIGURATION", "RLoginServerIP", RLoginServerIP);
                    AddToLog(" - RLoginServerIP = " + RLoginServerIP);

                    string RLoginServerPort = DB.ExecuteScalar("SELECT LocalPort FROM ServerThreadTbl WHERE ConnectionType = 1 ORDER BY ServerThreadID LIMIT 1").ToString();
                    Ini.WriteString("CONFIGURATION", "RLoginServerPort", RLoginServerPort);
                    AddToLog(" - RLoginServerPort = " + RLoginServerPort);
                }

                _PasswordPepper = (chkPlaintextPasswords.Checked) ? "DISABLE" : StringUtils.RandomString(100);
                Ini.WriteString("CONFIGURATION", "PasswordPepper", _PasswordPepper);
                AddToLog(" - PasswordPepper = " + _PasswordPepper);
            }
            AddToLog("");
        }

        private void HandleLogOffOrOnProcessDotIni(RMSQLiteConnection DB, string offOrOn, string inOrOut)
        {
            AddToLog("Importing settings for log" + offOrOn + "process.ini");
            using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "log" + offOrOn + "process.ini")))
            {
                // Erase old sections
                string[] Sections = Ini.ReadSections();
                foreach (string Section in Sections)
                {
                    Ini.EraseSection(Section);
                }

                SQL = "SELECT * FROM Log" + inOrOut + "ProcessTbl ORDER BY StepNumber";
                DB.ExecuteReader(SQL);
                while (DB.Reader.Read())
                {
                    string Name = "Log" + offOrOn + "Process" + DB.Reader["StepNumber"].ToString();
                    string Action = CommandToAction(DB.Reader["Command"].ToString());
                    string Parameters = DB.Reader["Parameters"].ToString();
                    string RequiredAccess = DB.Reader["RequiredAccess"].ToString();

                    if (Action == DB.Reader["Command"].ToString())
                    {
                        AddToLog(" - Ignoring command that no longer exists (" + Action + ")");
                    }
                    else
                    {
                        // See if we need to add the logon/off process as a door
                        if (Action == "RunDoor")
                        {
                            string[] CommandAndParameters = DB.Reader["Parameters"].ToString().Split(' ');
                            string Command = CommandAndParameters[0];
                            string DoorParameters = string.Join(" ", CommandAndParameters, 1, CommandAndParameters.Length - 1);
                            bool Native = DB.Reader["Command"].ToString() == "EXEC";

                            using (IniFile DoorIni = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "doors", GetSafeDoorFileName(Name) + ".ini")))
                            {
                                DoorIni.WriteString("DOOR", "Name", Name);
                                DoorIni.WriteString("DOOR", "Command", Command);
                                DoorIni.WriteString("DOOR", "Parameters", DoorParameters);
                                DoorIni.WriteString("DOOR", "Native", Native.ToString());
                                DoorIni.WriteString("DOOR", "ForceQuitDelay", "5");
                                DoorIni.WriteString("DOOR", "WindowStyle", "Minimized");
                            }

                            AddToLog(" - Added Door = " + Name);
                            AddToLog("         Command = " + Command);
                            AddToLog("         Parameters = " + DoorParameters);
                            AddToLog("         Native = " + Native.ToString());

                            // Override settings to be used below
                            Action = "RunDoor";
                            Parameters = Name;
                        }
                    
                        Ini.WriteString(Name, "Name", Name);
                        Ini.WriteString(Name, "Action", Action);
                        Ini.WriteString(Name, "Parameters", Parameters);
                        Ini.WriteString(Name, "RequiredAccess", RequiredAccess);

                        AddToLog(" - Added Name = " + Name);
                        AddToLog("         Action = " + Action);
                        AddToLog("         Parameters = " + Parameters);
                        AddToLog("         RequiredAccess = " + RequiredAccess);
                    }
                }
                DB.Reader.Close();
            }
            AddToLog("");
        }

        private void HandleMenus(RMSQLiteConnection DB)
        {
            AddToLog("Importing menus");

            // Get menu names
            List<string> MenuNames = new List<string>();
            SQL = "SELECT DISTINCT MenuName FROM MenuTbl WHERE MenuName <> 'GLOBAL' ORDER BY MenuName";
            DB.ExecuteReader(SQL);
            while (DB.Reader.Read())
            {
                MenuNames.Add(DB.Reader["MenuName"].ToString());
            }
            DB.Reader.Close();

            // Erase old menus
            foreach (string MenuName in MenuNames)
            {
                string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", MenuName.ToLower().Replace(" ", "_") + ".ini");
                if (File.Exists(FileName)) FileUtils.FileDelete(FileName);
            }

            // Add hotkeys to menus
            SQL = "SELECT * FROM MenuTbl ORDER BY MenuName, HotKey";
            DB.ExecuteReader(SQL);
            while (DB.Reader.Read())
            {
                string HotKey = DB.Reader["HotKey"].ToString();
                string Name = DB.Reader["Description"].ToString();
                string Action = CommandToAction(DB.Reader["Command"].ToString());
                string Parameters = DB.Reader["Parameters"].ToString();
                string RequiredAccess = DB.Reader["RequiredAccess"].ToString();

                // Modify parameters, for those that need it
                if (Action == "ChangeMenu")
                {
                    Parameters = Parameters.Replace(" ", "_");
                }
                else if (Action == "RunDoor")
                {
                    Parameters = GetSafeDoorFileName(Name);
                }

                if (Action == DB.Reader["Command"].ToString())
                {
                    AddToLog(" - Ignoring command that no longer exists (" + Action + ")");
                }
                else
                {
                    List<string> MenuNamesToUpdate = new List<string>();
                    if (DB.Reader["MenuName"].ToString().ToUpper() == "GLOBAL")
                    {
                        foreach (string MenuName in MenuNames)
                        {
                            // Don't add the global CHANGE_MENU commands to the menu we're wanting to change to
                            // i.e. Don't add a CHANGE_MENU MAIN to the MAIN menu
                            if ((Action != "ChangeMenu") || (Parameters.ToUpper() != MenuName.ToUpper()))
                            {
                                MenuNamesToUpdate.Add(MenuName);
                            }
                        }
                    }
                    else
                    {
                        MenuNamesToUpdate.Add(DB.Reader["MenuName"].ToString());
                    }

                    foreach (string MenuNameToUpdate in MenuNamesToUpdate)
                    {
                        using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", MenuNameToUpdate.ToLower().Replace(" ", "_") + ".ini")))
                        {
                            Ini.WriteString(HotKey, "Name", Name);
                            Ini.WriteString(HotKey, "Action", Action);
                            Ini.WriteString(HotKey, "Parameters", Parameters);
                            Ini.WriteString(HotKey, "RequiredAccess", RequiredAccess);

                            AddToLog(" - Added Menu = " + MenuNameToUpdate);
                            AddToLog("         HotKey = " + HotKey);
                            AddToLog("         Name = " + Name);
                            AddToLog("         Action = " + Action);
                            AddToLog("         Parameters = " + Parameters);
                            AddToLog("         RequiredAccess = " + RequiredAccess);
                        }
                    }
                }

            }
            DB.Reader.Close();

            // RecordID, MenuName, HotKey, Description, Command, Parameters, RequiredAccess
            AddToLog("");
        }

        private void HandleUsers(RMSQLiteConnection DB)
        {
            AddToLog("Importing users");
            SQL = "SELECT * FROM UserTbl U INNER JOIN GroupTbl G ON U.GroupID = G.GroupID";
            DB.ExecuteReader(SQL);
            while (DB.Reader.Read())
            {
                // UserID, GroupID, RLoginHostID, UserName, Password, RealName, Email, EmailVerification, EmailVerified, RegistrationDate
                string AccessLevel = DB.Reader["AccessLevel"].ToString();
                string Alias = DB.Reader["UserName"].ToString();
                string PasswordSalt = StringUtils.RandomString(100);
                string PasswordHash = UserInfo.GetPasswordHash(DB.Reader["Password"].ToString(), PasswordSalt, _PasswordPepper);
                string UserId = DB.Reader["UserID"].ToString();

                using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "users", UserInfo.SafeAlias(Alias.ToLower()) + ".ini")))
                {
                    Ini.WriteString("USER", "AccessLevel", AccessLevel);
                    Ini.WriteString("USER", "Alias", Alias);
                    Ini.WriteString("USER", "PasswordSalt", PasswordSalt);
                    Ini.WriteString("USER", "PasswordHash", PasswordHash);
                    Ini.WriteString("USER", "UserId", UserId);

                    AddToLog(" - Added Alias = " + Alias);
                    AddToLog("         AccessLevel = " + AccessLevel);
                    AddToLog("         PasswordSalt = " + PasswordSalt);
                    AddToLog("         PasswordHash = " + PasswordHash);
                    AddToLog("         UserId = " + UserId);
                    AddToLog("");
                }
            }
            DB.Reader.Close();
        }
    }
}
