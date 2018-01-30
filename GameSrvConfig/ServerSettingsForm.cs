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
using RandM.RMLibUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace RandM.GameSrv
{
    public partial class frmServerSettings : Form
    {
        public frmServerSettings()
        {
            InitializeComponent();

            PopulateIPAddresses();

            txtBBSName.Text = Config.Instance.BBSName;
            txtSysopFirstName.Text = Config.Instance.SysopFirstName;
            txtSysopLastName.Text = Config.Instance.SysopLastName;
            txtSysopEmail.Text = Config.Instance.SysopEmail;
            txtFirstNode.Text = Config.Instance.FirstNode.ToString();
            txtLastNode.Text = Config.Instance.LastNode.ToString();
            txtTimePerCall.Text = Config.Instance.TimePerCall.ToString();
            if (Config.Instance.TelnetServerIP != "0.0.0.0")
            {
                if (!cboTelnetServerIP.Items.Contains(Config.Instance.TelnetServerIP)) cboTelnetServerIP.Items.Add(Config.Instance.TelnetServerIP);
                cboTelnetServerIP.Text = Config.Instance.TelnetServerIP;
            }
            txtTelnetServerPort.Text = Config.Instance.TelnetServerPort.ToString();
            if (Config.Instance.RLoginServerIP != "0.0.0.0")
            {
                if (!cboRLoginServerIP.Items.Contains(Config.Instance.RLoginServerIP)) cboRLoginServerIP.Items.Add(Config.Instance.RLoginServerIP);
                cboRLoginServerIP.Text = Config.Instance.RLoginServerIP;
            }
            txtRLoginServerPort.Text = Config.Instance.RLoginServerPort.ToString();
            if (Config.Instance.WebSocketServerIP != "0.0.0.0")
            {
                if (!cboWebSocketServerIP.Items.Contains(Config.Instance.WebSocketServerIP)) cboWebSocketServerIP.Items.Add(Config.Instance.WebSocketServerIP);
                cboWebSocketServerIP.Text = Config.Instance.WebSocketServerIP;
            }
            txtWebSocketServerPort.Text = Config.Instance.WebSocketServerPort.ToString();
            txtTimeFormatLog.Text = Config.Instance.TimeFormatLog;
            txtTimeFormatUI.Text = Config.Instance.TimeFormatUI;
        }

        public void InitRunBBS()
        {
            lblTimePerCall.Visible = false;
            lblTimePerCallMinutes.Visible = false;
            txtTimePerCall.Visible = false;
        }

        private void PopulateIPAddresses()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    cboTelnetServerIP.Items.Add(ip.ToString());
                    cboRLoginServerIP.Items.Add(ip.ToString());
                    cboWebSocketServerIP.Items.Add(ip.ToString());
                }
            }
        }

        private void txtTimeFormatLog_TextChanged(object sender, EventArgs e)
        {
            try
            {
                lblTimeFormatLogSample.Text = DateTime.Now.ToString(txtTimeFormatLog.Text);
            }
            catch (Exception)
            {
                lblTimeFormatLogSample.Text = "Invalid format string";
            }
        }

        private void txtTimeFormatUI_TextChanged(object sender, EventArgs e)
        {
            try
            {
                lblTimeFormatUISample.Text = DateTime.Now.ToString(txtTimeFormatUI.Text);
            }
            catch (Exception)
            {
                lblTimeFormatUISample.Text = "Invalid format string";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Dialog.ValidateIsNotEmpty(txtBBSName)) return;
                if (!Dialog.ValidateIsNotEmpty(txtSysopFirstName)) return;
                if (!Dialog.ValidateIsNotEmpty(txtSysopLastName)) return;
                if (!Dialog.ValidateIsEmailAddress(txtSysopEmail)) return;
                if (!Dialog.ValidateIsInRange(txtFirstNode, 1, 255)) return;
                if (!Dialog.ValidateIsInRange(txtLastNode, 1, 255)) return;
                if (!Dialog.ValidateIsInRange(txtTimePerCall, 5, 1440)) return;
                if ((cboTelnetServerIP.SelectedIndex != 0) && (!Dialog.ValidateIsIPAddress(cboTelnetServerIP))) return;
                if (!Dialog.ValidateIsInRange(txtTelnetServerPort, 0, 65535)) return;
                if ((cboRLoginServerIP.SelectedIndex != 0) && (!Dialog.ValidateIsIPAddress(cboRLoginServerIP))) return;
                if (!Dialog.ValidateIsInRange(txtRLoginServerPort, 0, 65535)) return;
                if ((cboWebSocketServerIP.SelectedIndex != 0) && (!Dialog.ValidateIsIPAddress(cboWebSocketServerIP))) return;
                if (!Dialog.ValidateIsInRange(txtWebSocketServerPort, 0, 65535)) return;

                Config.Instance.BBSName = txtBBSName.Text.Trim();
                Config.Instance.SysopFirstName = txtSysopFirstName.Text.Trim();
                Config.Instance.SysopLastName = txtSysopLastName.Text.Trim();
                Config.Instance.SysopEmail = txtSysopEmail.Text.Trim();
                Config.Instance.FirstNode = int.Parse(txtFirstNode.Text.Trim());
                Config.Instance.LastNode = int.Parse(txtLastNode.Text.Trim());
                Config.Instance.TimePerCall = int.Parse(txtTimePerCall.Text.Trim());
                Config.Instance.TelnetServerIP = (cboTelnetServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboTelnetServerIP.Text;
                Config.Instance.TelnetServerPort = int.Parse(txtTelnetServerPort.Text.Trim());
                Config.Instance.RLoginServerIP = (cboRLoginServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboRLoginServerIP.Text;
                Config.Instance.RLoginServerPort = int.Parse(txtRLoginServerPort.Text.Trim());
                Config.Instance.WebSocketServerIP = (cboWebSocketServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboWebSocketServerIP.Text;
                Config.Instance.WebSocketServerPort = int.Parse(txtWebSocketServerPort.Text.Trim());
                Config.Instance.TimeFormatLog = txtTimeFormatLog.Text.Trim();
                Config.Instance.TimeFormatUI = txtTimeFormatUI.Text.Trim();
                Config.Instance.Save();
            }
            catch (Exception ex)
            {
                Dialog.Error("An unexpected error has occured, and your changes have not been saved.\r\n\r\nPlease try again, or edit CONFIG\\GAMESRV.INI by hand\r\n\r\nException message: " + ex.ToString(), "Unhandled Exception");
            }

            DialogResult = DialogResult.OK;
        }
    }
}
