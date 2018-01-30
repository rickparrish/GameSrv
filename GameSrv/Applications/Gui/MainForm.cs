/*
  GameSrv: A BBS Door Game Server
  Copyright (C) Rick Parrish, R&M Software

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
using RandM.RMLib;
using RandM.RMLibUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RandM.GameSrv {
    public partial class MainForm : Form {
        private int _CharWidth = 0;
        private GameSrv _GameSrv = new GameSrv();
        private FormWindowState _LastWindowState = FormWindowState.Normal;

        public MainForm() {
            InitializeComponent();

            // Init events
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
            mnuFileExit.Click += mnuFileExit_Click;
            Tray.DoubleClick += Tray_DoubleClick;
            tsbDisconnect.Click += tsbDisconnect_Click;
            tsbPause.Click += tsbPause_Click;
            tsbSetup.Click += tsbSetup_Click;
            tsbStart.Click += tsbStart_Click;
            tsbStop.Click += tsbStop_Click;

            // Determine the character width of our font (used for indenting wrapped lines)
            using (Graphics G = this.CreateGraphics()) {
                G.PageUnit = GraphicsUnit.Pixel;
                _CharWidth = G.MeasureString("..", this.Font).ToSize().Width - G.MeasureString(".", this.Font).ToSize().Width;
            }

            // Init titles
            this.Text = "GameSrv GUI v" + GameSrv.Version;
            Tray.Text = this.Text;

            // Display copyright message(s)
            StatusText(Helpers.Copyright, Color.White, false);

            // Setup log handler
            RMLog.Handler += RMLog_Handler;

            // Init GameSrv object
            NodeManager.ConnectionCountChangeEvent += NodeManager_ConnectionCountChangeEvent;
            NodeManager.NodeEvent += NodeManager_NodeEvent;
            _GameSrv.StatusChangeEvent += GameSrv_StatusChangeEvent;
            _GameSrv.Start();
        }

        private void GameSrv_StatusChangeEvent(object sender, StatusEventArgs e) {
            UpdateButtonsAndTrayIcon();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            // User initiated closure should be confirmed if there are active sessions
            if ((e.CloseReason == CloseReason.FormOwnerClosing) || (e.CloseReason == CloseReason.UserClosing)) {
                // Check if we're already stopped (or are stopping)
                if ((_GameSrv.Status != GameSrvStatus.Stopped) && (_GameSrv.Status != GameSrvStatus.Stopping)) {
                    int ConnectionCount = NodeManager.ConnectionCount;
                    if (ConnectionCount > 0) {
                        if (Dialog.YesNo("There are " + ConnectionCount.ToString() + " active connections.\r\n\r\nAre you sure you want to quit?", "Confirm quit") == DialogResult.No) {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }

            // If we get here it's OK to quit, so check if we're already stopped (or are stopping)
            if (_GameSrv.Status != GameSrvStatus.Offline) {
                _GameSrv.Stop(true);
                _GameSrv.Dispose();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized) {
                // We hide the taskbar icon and show the tray icon when GameSrv is minimized
                this.Hide();
                Tray.Visible = true;
            } else {
                // We also record the last window state for non-minimize events, so we know what to restore to
                _LastWindowState = this.WindowState;
            }
        }

        private void mnuFileExit_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void NodeManager_ConnectionCountChangeEvent(object sender, IntEventArgs e) {
            UpdateButtonsAndTrayIcon();
        }

        private void NodeManager_NodeEvent(object sender, NodeEventArgs e) {
            if (this.InvokeRequired) {
                this.Invoke(new MethodInvoker(delegate { NodeManager_NodeEvent(sender, e); }));
            } else {
                if (e.EventType == NodeEventType.LogOff) {
                    UpdateButtonsAndTrayIcon();
                } else if (e.EventType == NodeEventType.LogOn) {
                    UpdateButtonsAndTrayIcon();

                    // Add history item
                    ListViewItem LVIHistory = new ListViewItem(e.NodeInfo.Node.ToString());
                    LVIHistory.SubItems.Add(e.NodeInfo.ConnectionType.ToString());
                    LVIHistory.SubItems.Add(e.NodeInfo.Connection.GetRemoteIP());
                    LVIHistory.SubItems.Add(e.NodeInfo.User.Alias);
                    LVIHistory.SubItems.Add(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
                    lvHistory.Items.Insert(0, LVIHistory);

                    // Keep a counter for number of connections
                    switch (e.NodeInfo.ConnectionType) {
                        case ConnectionType.RLogin:
                            lblRLoginCount.Text = (Convert.ToInt32(lblRLoginCount.Text) + 1).ToString();
                            break;
                        case ConnectionType.Telnet:
                            lblTelnetCount.Text = (Convert.ToInt32(lblTelnetCount.Text) + 1).ToString();
                            break;
                        case ConnectionType.WebSocket:
                            lblWebSocketCount.Text = (Convert.ToInt32(lblWebSocketCount.Text) + 1).ToString();
                            break;
                    }
                }

                // Update status
                ListViewItem LVI = lvNodes.Items[e.NodeInfo.Node - Config.Instance.FirstNode];
                LVI.SubItems[1].Text = (e.EventType == NodeEventType.LogOff ? "" : e.NodeInfo.ConnectionType.ToString());
                LVI.SubItems[2].Text = (e.EventType == NodeEventType.LogOff ? "" : e.NodeInfo.Connection.GetRemoteIP());
                LVI.SubItems[3].Text = (e.EventType == NodeEventType.LogOff ? "" : e.NodeInfo.User.Alias);
                LVI.SubItems[4].Text = (e.EventType == NodeEventType.LogOff ? "Waiting for a caller..." : e.Status);
            }
        }

        // TODOX Have entries in the INI file that define which colour to use for each type of message
        private void RMLog_Handler(object sender, RMLogEventArgs e) {
            switch (e.Level) {
                case LogLevel.Debug:
                    StatusText("DEBUG: " + e.Message, Color.LightCyan);
                    break;
                case LogLevel.Error:
                    StatusText("ERROR: " + e.Message, Color.Red);
                    break;
                case LogLevel.Info:
                    StatusText(e.Message, Color.LightGray);
                    break;
                case LogLevel.Trace:
                    StatusText("TRACE: " + e.Message, Color.DarkGray);
                    break;
                case LogLevel.Warning:
                    StatusText("WARNING: " + e.Message, Color.Yellow);
                    break;
                default:
                    StatusText("UNKNOWN: " + e.Message, Color.White);
                    break;
            }
        }

        private void StatusText(string message, Color foreColour, bool prefixWithTime = true) {
            if (this.InvokeRequired) {
                this.Invoke(new MethodInvoker(delegate { StatusText(message, foreColour, prefixWithTime); }));
            } else {

                if (prefixWithTime) {
                    string Time = DateTime.Now.ToString(Config.Instance.TimeFormatUI) + "  ";
                    rtbLog.SelectionHangingIndent = Time.Length * _CharWidth;
                    rtbLog.AppendText(Time, Color.LightGray);
                } else {
                    rtbLog.SelectionHangingIndent = 0;
                }

                rtbLog.AppendText(message + "\r\n", foreColour);
                rtbLog.SelectionStart = rtbLog.Text.Length;
                rtbLog.ScrollToCaret();
            }
        }

        private void Tray_DoubleClick(object sender, EventArgs e) {
            if (!this.Visible) this.Show();
            if (!this.Focused) this.Activate();
            this.WindowState = _LastWindowState;
            Tray.Visible = false;
        }

        void tsbDisconnect_Click(object sender, EventArgs e) {
            if (lvNodes.SelectedItems.Count == 0) {
                Dialog.Error("Please select a node to disconnect first", "ERROR: No node selected");
            } else {
                if (Dialog.NoYes("Are you sure you want to disconnect this user?", "Confirm disconnect") == DialogResult.Yes) {
                    NodeManager.DisconnectNode(Convert.ToInt32(lvNodes.SelectedItems[0].SubItems[0].Text));
                }
            }
        }

        void tsbPause_Click(object sender, EventArgs e) {
            _GameSrv.Pause();
        }

        void tsbSetup_Click(object sender, EventArgs e) {
            Process.Start(StringUtils.PathCombine(ProcessUtils.StartupPath, "GameSrvConfig.exe"));
        }

        void tsbStart_Click(object sender, EventArgs e) {
            _GameSrv.Start();
        }

        void tsbStop_Click(object sender, EventArgs e) {
            _GameSrv.Stop(false);
        }

        private void UpdateButtonsAndTrayIcon() {
            if (this.InvokeRequired) {
                this.Invoke(new MethodInvoker(delegate { UpdateButtonsAndTrayIcon(); }));
            } else {
                switch (_GameSrv.Status) {
                    case GameSrvStatus.Paused:
                        tsbStart.Enabled = true;
                        tsbPause.Enabled = true;
                        tsbStop.Enabled = true;
                        tsbDisconnect.Enabled = true;
                        this.Icon = Properties.Resources.GameSrv16_32Paused;
                        break;
                    case GameSrvStatus.Started:
                        tsbStart.Enabled = false;
                        tsbPause.Enabled = true;
                        tsbStop.Enabled = true;
                        tsbDisconnect.Enabled = true;
                        this.Icon = (NodeManager.ConnectionCount == 0) ? Properties.Resources.GameSrv16_32Started : Properties.Resources.GameSrv16_32InUse;

                        // Only add if we haven't previously added
                        if (lvNodes.Items.Count == 0) {
                            for (int i = Config.Instance.FirstNode; i <= Config.Instance.LastNode; i++) {
                                ListViewItem LVI = new ListViewItem(i.ToString());
                                LVI.SubItems.Add("");
                                LVI.SubItems.Add("");
                                LVI.SubItems.Add("");
                                LVI.SubItems.Add("Waiting for a caller...");
                                lvNodes.Items.Add(LVI);
                            }
                        }

                        break;
                    case GameSrvStatus.Stopped:
                        tsbStart.Enabled = true;
                        tsbPause.Enabled = false;
                        tsbStop.Enabled = false;
                        tsbDisconnect.Enabled = false;
                        this.Icon = Properties.Resources.GameSrv16_32Stopped;

                        lvNodes.Items.Clear();

                        break;
                }
                Tray.Icon = this.Icon;
            }
        }
    }
}
