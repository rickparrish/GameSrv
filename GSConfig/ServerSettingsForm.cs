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
        Config _Config = new Config();

        public frmServerSettings()
        {
            InitializeComponent();

            PopulateIPAddresses();

            txtBBSName.Text = _Config.BBSName;
            txtSysopFirstName.Text = _Config.SysopFirstName;
            txtSysopLastName.Text = _Config.SysopLastName;
            txtSysopEmail.Text = _Config.SysopEmail;
            txtFirstNode.Text = _Config.FirstNode.ToString();
            txtLastNode.Text = _Config.LastNode.ToString();
            txtTimePerCall.Text = _Config.TimePerCall.ToString();
            if (_Config.TelnetServerIP != "0.0.0.0")
            {
                if (!cboTelnetServerIP.Items.Contains(_Config.TelnetServerIP)) cboTelnetServerIP.Items.Add(_Config.TelnetServerIP);
                cboTelnetServerIP.Text = _Config.TelnetServerIP;
            }
            txtTelnetServerPort.Text = _Config.TelnetServerPort.ToString();
            if (_Config.RLoginServerIP != "0.0.0.0")
            {
                if (!cboRLoginServerIP.Items.Contains(_Config.RLoginServerIP)) cboRLoginServerIP.Items.Add(_Config.RLoginServerIP);
                cboRLoginServerIP.Text = _Config.RLoginServerIP;
            }
            txtRLoginServerPort.Text = _Config.RLoginServerPort.ToString();
            if (_Config.WebSocketServerIP != "0.0.0.0")
            {
                if (!cboWebSocketServerIP.Items.Contains(_Config.WebSocketServerIP)) cboWebSocketServerIP.Items.Add(_Config.WebSocketServerIP);
                cboWebSocketServerIP.Text = _Config.WebSocketServerIP;
            }
            txtWebSocketServerPort.Text = _Config.WebSocketServerPort.ToString();
            if (_Config.FlashSocketPolicyServerIP != "0.0.0.0")
            {
                if (!cboFlashSocketPolicyServerIP.Items.Contains(_Config.FlashSocketPolicyServerIP)) cboFlashSocketPolicyServerIP.Items.Add(_Config.FlashSocketPolicyServerIP);
                cboFlashSocketPolicyServerIP.Text = _Config.FlashSocketPolicyServerIP;
            }
            txtFlashSocketPolicyServerPort.Text = _Config.FlashSocketPolicyServerPort.ToString();
            txtTimeFormatLog.Text = _Config.TimeFormatLog;
            txtTimeFormatUI.Text = _Config.TimeFormatUI;
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
                    cboFlashSocketPolicyServerIP.Items.Add(ip.ToString());
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
                if ((cboFlashSocketPolicyServerIP.SelectedIndex != 0) && (!Dialog.ValidateIsIPAddress(cboFlashSocketPolicyServerIP))) return;
                if (!Dialog.ValidateIsInRange(txtFlashSocketPolicyServerPort, 0, 65535)) return;

                _Config.BBSName = txtBBSName.Text.Trim();
                _Config.SysopFirstName = txtSysopFirstName.Text.Trim();
                _Config.SysopLastName = txtSysopLastName.Text.Trim();
                _Config.SysopEmail = txtSysopEmail.Text.Trim();
                _Config.FirstNode = int.Parse(txtFirstNode.Text.Trim());
                _Config.LastNode = int.Parse(txtLastNode.Text.Trim());
                _Config.TimePerCall = int.Parse(txtTimePerCall.Text.Trim());
                _Config.TelnetServerIP = (cboTelnetServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboTelnetServerIP.Text;
                _Config.TelnetServerPort = int.Parse(txtTelnetServerPort.Text.Trim());
                _Config.RLoginServerIP = (cboRLoginServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboRLoginServerIP.Text;
                _Config.RLoginServerPort = int.Parse(txtRLoginServerPort.Text.Trim());
                _Config.WebSocketServerIP = (cboWebSocketServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboWebSocketServerIP.Text;
                _Config.WebSocketServerPort = int.Parse(txtWebSocketServerPort.Text.Trim());
                _Config.FlashSocketPolicyServerIP = (cboFlashSocketPolicyServerIP.SelectedIndex == 0) ? "0.0.0.0" : cboFlashSocketPolicyServerIP.Text;
                _Config.FlashSocketPolicyServerPort = int.Parse(txtFlashSocketPolicyServerPort.Text.Trim());
                _Config.TimeFormatLog = txtTimeFormatLog.Text.Trim();
                _Config.TimeFormatUI = txtTimeFormatUI.Text.Trim();
                _Config.Save();
            }
            catch (Exception ex)
            {
                Dialog.Error("An unexpected error has occured, and your changes have not been saved.\r\n\r\nPlease try again, or edit CONFIG\\GAMESRV.INI by hand\r\n\r\nException message: " + ex.ToString(), "Unhandled Exception");
            }

            DialogResult = DialogResult.OK;
        }
    }
}
