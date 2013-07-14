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
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Security;
using System.Windows.Forms;

namespace RandM.GameSrv
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "frm"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "frm")]
    public partial class frmMain : Form
    {
        private int _CharWidth = 0;
        private GameSrv _GameSrv = new GameSrv();
        private FormWindowState _LastWindowState = FormWindowState.Normal;

        public frmMain()
        {
            InitializeComponent();

            // Determine the character width of our font
            using (Graphics G = this.CreateGraphics())
            {
                G.PageUnit = GraphicsUnit.Pixel;
                _CharWidth = G.MeasureString("..", this.Font).ToSize().Width - G.MeasureString(".", this.Font).ToSize().Width;
            }

            // Init titles
            this.Text = "GameSrv WFC Screen v" + GameSrv.Version;
            lblGameSrvWFCScreen.Tag = this.Text; // Update tag because text contains "Press [F1] For Help"
            Tray.Text = this.Text;

            // Display copyright message(s)
            StatusText(Globals.Copyright, false);

            // Update time
            tmrUpdateDisplay_Tick(null, EventArgs.Empty);
            tmrUpdateDisplay.Interval = 10000;

            // Init GameSrv object
            _GameSrv.AggregatedStatusMessageEvent += new EventHandler<StringEventArgs>(GameSrv_AggregatedStatusMessageEvent);
            _GameSrv.ConnectionCountChangeEvent += new EventHandler<IntEventArgs>(GameSrv_ConnectionCountChangeEvent);
            _GameSrv.LogOnEvent += new EventHandler<NodeEventArgs>(GameSrv_LogOnEvent);
            _GameSrv.StatusEvent += new EventHandler<StatusEventArgs>(GameSrv_StatusEvent); ;
            _GameSrv.Start();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
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

        private void frmMain_Resize(object sender, EventArgs e)
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
            UpdateTrayIcon();
        }

        private void GameSrv_AggregatedStatusMessageEvent(object sender, StringEventArgs e)
        {
            StatusText(e.Text);
        }

        private void GameSrv_LogOnEvent(object sender, NodeEventArgs e)
        {
            if (lblLast.InvokeRequired)
            {
                lblLast.Invoke(new MethodInvoker(delegate { GameSrv_LogOnEvent(sender, e); }));
            }
            else
            {
                lblLast.Text = e.NodeInfo.User.Alias + " (" + e.NodeInfo.Connection.GetRemoteIP() + ":" + e.NodeInfo.Connection.GetRemotePort() + ")";
                lblOn.Text = DateTime.Now.ToString("dddd MMMM dd, yyyy") + "  " + DateTime.Now.ToString(_GameSrv.TimeFormatUI).ToLower();
                lblType.Text = e.NodeInfo.ConnectionType.ToString();

                switch (e.NodeInfo.ConnectionType)
                {
                    case ConnectionType.RLogin: lblRLogin.Text = (Convert.ToInt32(lblRLogin.Text) + 1).ToString(); break;
                    case ConnectionType.Telnet: lblTelnet.Text = (Convert.ToInt32(lblTelnet.Text) + 1).ToString(); break;
                    case ConnectionType.WebSocket: lblWebSocket.Text = (Convert.ToInt32(lblWebSocket.Text) + 1).ToString(); break;
                }
            }
        }

        private void GameSrv_StatusEvent(object sender, StatusEventArgs e)
        {
            UpdateTrayIcon();
        }

        private void StatusText(string message)
        {
            if (rtbStatus.InvokeRequired)
            {
                rtbStatus.Invoke(new MethodInvoker(delegate { StatusText(message); }));
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
                rtbStatus.SelectionHangingIndent = Time.Length * _CharWidth;
                rtbStatus.AppendText(Time, Color.LightGray);
            }
            else
            {
                rtbStatus.SelectionHangingIndent = 0;
            }

            // TODO Make colours configurable
                // TODO Certain things may be green:
                // "User hung-up while in external program"
                // "No carrier detected (maybe it was a 'ping'?)"
                // "External program requested hangup (dropped DTR)"
            Color TextColour = Color.LightGray;
            if (message.Contains("ERROR") || message.Contains("EXCEPTION"))
            {
                TextColour = Color.Red;
            }
            else if (message.Contains("WARNING"))
            {
                TextColour = Color.Yellow;
            }
            else if (message.Contains("DEBUG"))
            {
                TextColour = Color.Cyan;
            }

            rtbStatus.AppendText(message + "\r\n", TextColour);
            rtbStatus.SelectionStart = rtbStatus.Text.Length;
            rtbStatus.ScrollToCaret();
        }

        private void TLP_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            // Colour the background of certain cells blue
            if ((e.Row <= 2) || ((e.Row == TLP.RowCount - 1) && (e.Column == TLP.ColumnCount - 1)))
            {
                e.Graphics.FillRectangle(Brushes.Blue, e.CellBounds);
            }
        }

        private void tmrUpdateDisplay_Tick(object sender, EventArgs e)
        {
            tmrUpdateDisplay.Stop();

            // Update time
            lblTime.Text = DateTime.Now.ToString(_GameSrv.TimeFormatUI).ToLower();
            lblDate.Text = DateTime.Now.ToString("dddd MMMM dd, yyyy");

            // Toggle F1 message
            string Temp = lblGameSrvWFCScreen.Tag.ToString();
            lblGameSrvWFCScreen.Tag = lblGameSrvWFCScreen.Text;
            lblGameSrvWFCScreen.Text = Temp;

            // Restart timer
            tmrUpdateDisplay.Start();
        }

        private void Tray_DoubleClick(object sender, EventArgs e)
        {
            if (!this.Visible) this.Show();
            if (!this.Focused) this.Activate();
            this.WindowState = _LastWindowState;
            Tray.Visible = false;
        }

        private void UpdateTrayIcon()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate { UpdateTrayIcon(); }));
            }
            else
            {
                switch (_GameSrv.Status)
                {
                    case ServerStatus.Paused:
                        this.Icon = Properties.Resources.GameSrv16_32Paused;
                        break;
                    case ServerStatus.Resumed:
                    case ServerStatus.Started:
                        this.Icon = (_GameSrv.ConnectionCount == 0) ? Properties.Resources.GameSrv16_32Started : Properties.Resources.GameSrv16_32InUse;
                        break;
                    case ServerStatus.Stopped:
                        this.Icon = Properties.Resources.GameSrv16_32Stopped;
                        break;
                }
                Tray.Icon = this.Icon;
            }
        }

        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    StatusText("", false);
                    StatusText("GameSrv WFC Screen Help", false);
                    StatusText("-=-=-=-=-=-=-=-=-=-=-=-", false);
                    StatusText("F1 = Help (this screen)", false);
                    StatusText("P  = Pause (reject new connections, leave existing connections alone)", false);
                    StatusText("Q  = Quit (shut down and terminate existing connections)", false);
                    StatusText("", false);
                    break;
                case Keys.C:
                    if (!e.Alt && !e.Control)
                    {
                        Process.Start(StringUtils.PathCombine(ProcessUtils.StartupPath, "GameSrvConfig.exe"));
                    }
                    break;
                case Keys.P:
                    if (!e.Alt && !e.Control)
                    {
                        _GameSrv.Pause();
                    }
                    break;
                case Keys.Q:
                    // Check if we're already stopped (or are stopping)
                    this.Close();
                    //if ((_GameSrv.Status != ServerStatus.Stopped) && (_GameSrv.Status != ServerStatus.Stopping))
                    //{
                    //    int ConnectionCount = _GameSrv.ConnectionCount;
                    //    if (ConnectionCount > 0)
                    //    {
                    //        WriteLn("");
                    //        WriteLn("There are " + ConnectionCount.ToString() + " active connections.");
                    //        WriteLn("Are you sure you want to quit [y/N]: ");
                    //        Ch = Crt.ReadKey();
                    //        if (Ch.ToString().ToUpper() != "Y")
                    //        {
                    //            WriteLn("Cancelling quit request.");
                    //            continue;
                    //        }
                    //    }
                    //}

                    //_GameSrv.Stop();
                    //Quit = true;
                    break;
            }
        }
    }
}
