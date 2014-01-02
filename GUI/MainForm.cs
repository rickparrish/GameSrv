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

namespace RandM.GameSrv
{
    public partial class MainForm : Form
    {
        private int _CharWidth = 0;
        private GameSrv _GameSrv = new GameSrv();
        private FormWindowState _LastWindowState = FormWindowState.Normal;

        public MainForm()
        {
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

            // Determine the character width of our font
            using (Graphics G = this.CreateGraphics())
            {
                G.PageUnit = GraphicsUnit.Pixel;
                _CharWidth = G.MeasureString("..", this.Font).ToSize().Width - G.MeasureString(".", this.Font).ToSize().Width;
            }

            // Init titles
            this.Text = "GameSrv GUI v" + GameSrv.Version;
            Tray.Text = this.Text;

            // Display copyright message(s)
            StatusText(Globals.Copyright, false);

            // Init GameSrv object
            _GameSrv.AggregatedStatusMessageEvent += new EventHandler<StringEventArgs>(GameSrv_AggregatedStatusMessageEvent);
            _GameSrv.ConnectionCountChangeEvent += new EventHandler<IntEventArgs>(GameSrv_ConnectionCountChangeEvent);
            _GameSrv.LogOffEvent += GameSrv_LogOffEvent;
            _GameSrv.LogOnEvent += GameSrv_LogOnEvent;
            _GameSrv.NodeEvent += GameSrv_NodeEvent;
            _GameSrv.StatusEvent += GameSrv_StatusEvent;
            _GameSrv.Start();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // User initiated closure should be confirmed if there are active sessions
            if ((e.CloseReason == CloseReason.FormOwnerClosing) || (e.CloseReason == CloseReason.UserClosing))
            {
                // Check if we're already stopped (or are stopping)
                if ((_GameSrv.Status != ServerStatus.Stopped) && (_GameSrv.Status != ServerStatus.Stopping))
                {
                    int ConnectionCount = _GameSrv.ConnectionCount;
                    if (ConnectionCount > 0)
                    {
                        if (Dialog.YesNo("There are " + ConnectionCount.ToString() + " active connections.\r\n\r\nAre you sure you want to quit?", "Confirm quit") == DialogResult.No)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }

            // If we get here it's OK to quit, so check if we're already stopped (or are stopping)
            if ((_GameSrv.Status != ServerStatus.Stopped) && (_GameSrv.Status != ServerStatus.Stopping))
            {
                _GameSrv.Stop();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                Tray.Visible = true;
            }
            else
            {
                _LastWindowState = this.WindowState;
            }
        }

        private void GameSrv_ConnectionCountChangeEvent(object sender, IntEventArgs e)
        {
            UpdateButtonsAndTrayIcon();
        }

        private void GameSrv_AggregatedStatusMessageEvent(object sender, StringEventArgs e)
        {
            StatusText(e.Text);
        }

        void GameSrv_LogOffEvent(object sender, NodeEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { GameSrv_LogOffEvent(sender, e); }));
            }
            else
            {
                ListViewItem LVI = lvNodes.Items[e.NodeInfo.Node - _GameSrv.FirstNode];
                LVI.SubItems[1].Text = "";
                LVI.SubItems[2].Text = "";
                LVI.SubItems[3].Text = "";
                LVI.SubItems[4].Text = "Waiting for a caller...";
            }
        }

        private void GameSrv_LogOnEvent(object sender, NodeEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { GameSrv_LogOnEvent(sender, e); }));
            }
            else
            {
                ListViewItem LVINodes = lvNodes.Items[e.NodeInfo.Node - _GameSrv.FirstNode];
                LVINodes.SubItems[1].Text = e.NodeInfo.ConnectionType.ToString();
                LVINodes.SubItems[2].Text = e.NodeInfo.Connection.GetRemoteIP();
                LVINodes.SubItems[3].Text = e.NodeInfo.User.Alias;
                LVINodes.SubItems[4].Text = e.Status;
                // TODO Show time user signed on at in listview?

                ListViewItem LVIHistory = new ListViewItem(e.NodeInfo.Node.ToString());
                LVIHistory.SubItems.Add(e.NodeInfo.ConnectionType.ToString());
                LVIHistory.SubItems.Add(e.NodeInfo.Connection.GetRemoteIP());
                LVIHistory.SubItems.Add(e.NodeInfo.User.Alias);
                LVIHistory.SubItems.Add(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
                lvHistory.Items.Insert(0, LVIHistory);
                // TODO Add column to hold logoff date (and maybe also session duration)?

                // Keep a counter for number of connections
                switch (e.NodeInfo.ConnectionType)
                {
                    case ConnectionType.RLogin: lblRLoginCount.Text = (Convert.ToInt32(lblRLoginCount.Text) + 1).ToString(); break;
                    case ConnectionType.Telnet: lblTelnetCount.Text = (Convert.ToInt32(lblTelnetCount.Text) + 1).ToString(); break;
                    case ConnectionType.WebSocket: lblWebSocketCount.Text = (Convert.ToInt32(lblWebSocketCount.Text) + 1).ToString(); break;
                }
            }
        }

        void GameSrv_NodeEvent(object sender, NodeEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { GameSrv_NodeEvent(sender, e); }));
            }
            else
            {
                // Skip out of it's not a true status message
                if ((e.Status.StartsWith("ERROR:")) || e.Status.StartsWith("EXCEPTION:") || (e.Status.StartsWith("WARNING:")) || (e.Status.StartsWith("DEBUG:"))) return;

                ListViewItem LVI = lvNodes.Items[e.NodeInfo.Node - _GameSrv.FirstNode];
                LVI.SubItems[1].Text = e.NodeInfo.ConnectionType.ToString();
                LVI.SubItems[2].Text = e.NodeInfo.Connection.GetRemoteIP();
                LVI.SubItems[3].Text = e.NodeInfo.User.Alias;
                LVI.SubItems[4].Text = e.Status;
            }
        }

        private void GameSrv_StatusEvent(object sender, StatusEventArgs e)
        {
            UpdateButtonsAndTrayIcon();
        }

        void mnuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void StatusText(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new MethodInvoker(delegate { StatusText(message); }));
            }
            else
            {
                StatusText(message, true);
            }
        }

        private void StatusText(string message, bool prefixWithTime)
        {
            if (prefixWithTime)
            {
                string Time = DateTime.Now.ToString(_GameSrv.TimeFormatUI) + "  ";
                rtbLog.SelectionHangingIndent = Time.Length * _CharWidth;
                rtbLog.AppendText(Time, Color.Black);
            }
            else
            {
                rtbLog.SelectionHangingIndent = 0;
            }

            // TODO Make colours configurable
            // TODO Certain things may be green:
            // "User hung-up while in external program"
            // "No carrier detected (maybe it was a 'ping'?)"
            // "External program requested hangup (dropped DTR)"
            Color TextColour = Color.Black;
            if (message.Contains("ERROR:") || message.Contains("EXCEPTION:"))
            {
                TextColour = Color.Red;
            }
            else if (message.Contains("WARNING:"))
            {
                TextColour = Color.Orange;
            }
            else if (message.Contains("DEBUG:"))
            {
                TextColour = Color.DarkCyan;
            }

            rtbLog.AppendText(message + "\r\n", TextColour);
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }

        private void Tray_DoubleClick(object sender, EventArgs e)
        {
            if (!this.Visible) this.Show();
            if (!this.Focused) this.Activate();
            this.WindowState = _LastWindowState;
            Tray.Visible = false;
        }

        void tsbDisconnect_Click(object sender, EventArgs e)
        {
            if (lvNodes.SelectedItems.Count == 0)
            {
                Dialog.Error("Please select a node to disconnect first", "Error");
            }
            else
            {
                if (Dialog.NoYes("Are you sure you want to disconnect this user?", "Confirm disconnect") == DialogResult.Yes)
                {
                    _GameSrv.DisconnectNode(Convert.ToInt32(lvNodes.SelectedItems[0].SubItems[0].Text));
                }
            }
        }

        void tsbPause_Click(object sender, EventArgs e)
        {
            _GameSrv.Pause();
        }

        void tsbSetup_Click(object sender, EventArgs e)
        {
            Process.Start(StringUtils.PathCombine(ProcessUtils.StartupPath, "GameSrvConfig.exe"));
        }

        void tsbStart_Click(object sender, EventArgs e)
        {
            _GameSrv.Start();
        }

        void tsbStop_Click(object sender, EventArgs e)
        {
            _GameSrv.Stop();
        }

        private void UpdateButtonsAndTrayIcon()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { UpdateButtonsAndTrayIcon(); }));
            }
            else
            {
                switch (_GameSrv.Status)
                {
                    case ServerStatus.Paused:
                        tsbStart.Enabled = true;
                        tsbPause.Enabled = true;
                        tsbStop.Enabled = true;
                        tsbDisconnect.Enabled = true;
                        this.Icon = Properties.Resources.GameSrv16_32Paused;
                        break;
                    case ServerStatus.Resumed:
                    case ServerStatus.Started:
                        tsbStart.Enabled = false;
                        tsbPause.Enabled = true;
                        tsbStop.Enabled = true;
                        tsbDisconnect.Enabled = true;
                        this.Icon = (_GameSrv.ConnectionCount == 0) ? Properties.Resources.GameSrv16_32Started : Properties.Resources.GameSrv16_32InUse;

                        // Only add if we haven't previously added
                        if (lvNodes.Items.Count == 0)
                        {
                            for (int i = _GameSrv.FirstNode; i <= _GameSrv.LastNode; i++)
                            {
                                ListViewItem LVI = new ListViewItem(i.ToString());
                                LVI.SubItems.Add("");
                                LVI.SubItems.Add("");
                                LVI.SubItems.Add("");
                                LVI.SubItems.Add("Waiting for a caller...");
                                lvNodes.Items.Add(LVI);
                            }
                        }

                        break;
                    case ServerStatus.Stopped:
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
