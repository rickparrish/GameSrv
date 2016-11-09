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
*/
namespace RandM.GameSrv
{
    partial class frmServerSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmServerSettings));
            this.label1 = new System.Windows.Forms.Label();
            this.txtBBSName = new System.Windows.Forms.TextBox();
            this.txtSysopFirstName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSysopLastName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSysopEmail = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtFirstNode = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtLastNode = new System.Windows.Forms.TextBox();
            this.txtTimePerCall = new System.Windows.Forms.TextBox();
            this.lblTimePerCall = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtTelnetServerPort = new System.Windows.Forms.TextBox();
            this.cboTelnetServerIP = new System.Windows.Forms.ComboBox();
            this.cboRLoginServerIP = new System.Windows.Forms.ComboBox();
            this.txtRLoginServerPort = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cboFlashSocketPolicyServerIP = new System.Windows.Forms.ComboBox();
            this.txtFlashSocketPolicyServerPort = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cboWebSocketServerIP = new System.Windows.Forms.ComboBox();
            this.txtWebSocketServerPort = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtTimeFormatLog = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.txtTimeFormatUI = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.lblTimeFormatLogSample = new System.Windows.Forms.Label();
            this.lblTimeFormatUISample = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.lblTimePerCallMinutes = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "BBS Name";
            // 
            // txtBBSName
            // 
            this.txtBBSName.Location = new System.Drawing.Point(12, 25);
            this.txtBBSName.Name = "txtBBSName";
            this.txtBBSName.Size = new System.Drawing.Size(200, 20);
            this.txtBBSName.TabIndex = 1;
            this.txtBBSName.Tag = "BBS Name";
            // 
            // txtSysopFirstName
            // 
            this.txtSysopFirstName.Location = new System.Drawing.Point(12, 64);
            this.txtSysopFirstName.Name = "txtSysopFirstName";
            this.txtSysopFirstName.Size = new System.Drawing.Size(200, 20);
            this.txtSysopFirstName.TabIndex = 2;
            this.txtSysopFirstName.Tag = "Sysop First Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Sysop First Name";
            // 
            // txtSysopLastName
            // 
            this.txtSysopLastName.Location = new System.Drawing.Point(12, 103);
            this.txtSysopLastName.Name = "txtSysopLastName";
            this.txtSysopLastName.Size = new System.Drawing.Size(200, 20);
            this.txtSysopLastName.TabIndex = 3;
            this.txtSysopLastName.Tag = "Sysop Last Name";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 87);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Sysop Last Name";
            // 
            // txtSysopEmail
            // 
            this.txtSysopEmail.Location = new System.Drawing.Point(12, 142);
            this.txtSysopEmail.Name = "txtSysopEmail";
            this.txtSysopEmail.Size = new System.Drawing.Size(200, 20);
            this.txtSysopEmail.TabIndex = 4;
            this.txtSysopEmail.Tag = "Sysop Email Address";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 126);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Sysop Email Address";
            // 
            // txtFirstNode
            // 
            this.txtFirstNode.Location = new System.Drawing.Point(12, 190);
            this.txtFirstNode.Name = "txtFirstNode";
            this.txtFirstNode.Size = new System.Drawing.Size(59, 20);
            this.txtFirstNode.TabIndex = 5;
            this.txtFirstNode.Tag = "Start Node Number";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 174);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(146, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Start and End Node Numbers";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(77, 193);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(16, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "to";
            // 
            // txtLastNode
            // 
            this.txtLastNode.Location = new System.Drawing.Point(99, 190);
            this.txtLastNode.Name = "txtLastNode";
            this.txtLastNode.Size = new System.Drawing.Size(59, 20);
            this.txtLastNode.TabIndex = 6;
            this.txtLastNode.Tag = "End Node Number";
            // 
            // txtTimePerCall
            // 
            this.txtTimePerCall.Location = new System.Drawing.Point(12, 238);
            this.txtTimePerCall.Name = "txtTimePerCall";
            this.txtTimePerCall.Size = new System.Drawing.Size(59, 20);
            this.txtTimePerCall.TabIndex = 7;
            this.txtTimePerCall.Tag = "Time Per Call";
            // 
            // lblTimePerCall
            // 
            this.lblTimePerCall.AutoSize = true;
            this.lblTimePerCall.Location = new System.Drawing.Point(12, 222);
            this.lblTimePerCall.Name = "lblTimePerCall";
            this.lblTimePerCall.Size = new System.Drawing.Size(69, 13);
            this.lblTimePerCall.TabIndex = 12;
            this.lblTimePerCall.Text = "Time Per Call";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(222, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(168, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Telnet Server IP Address and Port";
            // 
            // txtTelnetServerPort
            // 
            this.txtTelnetServerPort.Location = new System.Drawing.Point(363, 25);
            this.txtTelnetServerPort.Name = "txtTelnetServerPort";
            this.txtTelnetServerPort.Size = new System.Drawing.Size(59, 20);
            this.txtTelnetServerPort.TabIndex = 9;
            this.txtTelnetServerPort.Tag = "Telnet Server Port";
            // 
            // cboTelnetServerIP
            // 
            this.cboTelnetServerIP.FormattingEnabled = true;
            this.cboTelnetServerIP.ItemHeight = 13;
            this.cboTelnetServerIP.Items.AddRange(new object[] {
            "All IP addresses"});
            this.cboTelnetServerIP.Location = new System.Drawing.Point(222, 24);
            this.cboTelnetServerIP.Name = "cboTelnetServerIP";
            this.cboTelnetServerIP.Size = new System.Drawing.Size(135, 21);
            this.cboTelnetServerIP.TabIndex = 17;
            this.cboTelnetServerIP.Tag = "Telnet Server IP";
            this.cboTelnetServerIP.Text = "All IP addresses";
            // 
            // cboRLoginServerIP
            // 
            this.cboRLoginServerIP.FormattingEnabled = true;
            this.cboRLoginServerIP.Items.AddRange(new object[] {
            "All IP addresses"});
            this.cboRLoginServerIP.Location = new System.Drawing.Point(222, 63);
            this.cboRLoginServerIP.Name = "cboRLoginServerIP";
            this.cboRLoginServerIP.Size = new System.Drawing.Size(135, 21);
            this.cboRLoginServerIP.TabIndex = 10;
            this.cboRLoginServerIP.Tag = "RLogin Server IP";
            this.cboRLoginServerIP.Text = "All IP addresses";
            // 
            // txtRLoginServerPort
            // 
            this.txtRLoginServerPort.Location = new System.Drawing.Point(363, 64);
            this.txtRLoginServerPort.Name = "txtRLoginServerPort";
            this.txtRLoginServerPort.Size = new System.Drawing.Size(59, 20);
            this.txtRLoginServerPort.TabIndex = 11;
            this.txtRLoginServerPort.Tag = "RLogin Server Port";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(222, 48);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(172, 13);
            this.label9.TabIndex = 18;
            this.label9.Text = "RLogin Server IP Address and Port";
            // 
            // cboFlashSocketPolicyServerIP
            // 
            this.cboFlashSocketPolicyServerIP.FormattingEnabled = true;
            this.cboFlashSocketPolicyServerIP.Items.AddRange(new object[] {
            "All IP addresses"});
            this.cboFlashSocketPolicyServerIP.Location = new System.Drawing.Point(222, 141);
            this.cboFlashSocketPolicyServerIP.Name = "cboFlashSocketPolicyServerIP";
            this.cboFlashSocketPolicyServerIP.Size = new System.Drawing.Size(135, 21);
            this.cboFlashSocketPolicyServerIP.TabIndex = 14;
            this.cboFlashSocketPolicyServerIP.Tag = "Flash Policy Server IP";
            this.cboFlashSocketPolicyServerIP.Text = "All IP addresses";
            // 
            // txtFlashSocketPolicyServerPort
            // 
            this.txtFlashSocketPolicyServerPort.Location = new System.Drawing.Point(363, 142);
            this.txtFlashSocketPolicyServerPort.Name = "txtFlashSocketPolicyServerPort";
            this.txtFlashSocketPolicyServerPort.Size = new System.Drawing.Size(59, 20);
            this.txtFlashSocketPolicyServerPort.TabIndex = 15;
            this.txtFlashSocketPolicyServerPort.Tag = "Flash Policy Server Port";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(222, 126);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(194, 13);
            this.label10.TabIndex = 21;
            this.label10.Text = "Flash Policy Server IP Address and Port";
            // 
            // cboWebSocketServerIP
            // 
            this.cboWebSocketServerIP.FormattingEnabled = true;
            this.cboWebSocketServerIP.Items.AddRange(new object[] {
            "All IP addresses"});
            this.cboWebSocketServerIP.Location = new System.Drawing.Point(222, 102);
            this.cboWebSocketServerIP.Name = "cboWebSocketServerIP";
            this.cboWebSocketServerIP.Size = new System.Drawing.Size(135, 21);
            this.cboWebSocketServerIP.TabIndex = 12;
            this.cboWebSocketServerIP.Tag = "WebSocket Server IP";
            this.cboWebSocketServerIP.Text = "All IP addresses";
            // 
            // txtWebSocketServerPort
            // 
            this.txtWebSocketServerPort.Location = new System.Drawing.Point(363, 103);
            this.txtWebSocketServerPort.Name = "txtWebSocketServerPort";
            this.txtWebSocketServerPort.Size = new System.Drawing.Size(59, 20);
            this.txtWebSocketServerPort.TabIndex = 13;
            this.txtWebSocketServerPort.Tag = "WebSocket Server Port";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(222, 87);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(195, 13);
            this.label11.TabIndex = 24;
            this.label11.Text = "WebSocket Server IP Address and Port";
            // 
            // txtTimeFormatLog
            // 
            this.txtTimeFormatLog.Location = new System.Drawing.Point(222, 190);
            this.txtTimeFormatLog.Name = "txtTimeFormatLog";
            this.txtTimeFormatLog.Size = new System.Drawing.Size(86, 20);
            this.txtTimeFormatLog.TabIndex = 16;
            this.txtTimeFormatLog.Tag = "Log Time Format";
            this.txtTimeFormatLog.TextChanged += new System.EventHandler(this.txtTimeFormatLog_TextChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(222, 174);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(86, 13);
            this.label12.TabIndex = 27;
            this.label12.Text = "Log Time Format";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(333, 174);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(79, 13);
            this.label13.TabIndex = 29;
            this.label13.Text = "UI Time Format";
            // 
            // txtTimeFormatUI
            // 
            this.txtTimeFormatUI.Location = new System.Drawing.Point(336, 190);
            this.txtTimeFormatUI.Name = "txtTimeFormatUI";
            this.txtTimeFormatUI.Size = new System.Drawing.Size(86, 20);
            this.txtTimeFormatUI.TabIndex = 17;
            this.txtTimeFormatUI.Tag = "UI Time Format";
            this.txtTimeFormatUI.TextChanged += new System.EventHandler(this.txtTimeFormatUI_TextChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(222, 222);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(28, 13);
            this.label14.TabIndex = 31;
            this.label14.Text = "Log:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(222, 241);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(21, 13);
            this.label15.TabIndex = 32;
            this.label15.Text = "UI:";
            // 
            // lblTimeFormatLogSample
            // 
            this.lblTimeFormatLogSample.AutoSize = true;
            this.lblTimeFormatLogSample.Location = new System.Drawing.Point(256, 222);
            this.lblTimeFormatLogSample.Name = "lblTimeFormatLogSample";
            this.lblTimeFormatLogSample.Size = new System.Drawing.Size(109, 13);
            this.lblTimeFormatLogSample.TabIndex = 33;
            this.lblTimeFormatLogSample.Text = "Log sample date/time";
            // 
            // lblTimeFormatUISample
            // 
            this.lblTimeFormatUISample.AutoSize = true;
            this.lblTimeFormatUISample.Location = new System.Drawing.Point(256, 241);
            this.lblTimeFormatUISample.Name = "lblTimeFormatUISample";
            this.lblTimeFormatUISample.Size = new System.Drawing.Size(76, 13);
            this.lblTimeFormatUISample.TabIndex = 34;
            this.lblTimeFormatUISample.Text = "UI sample time";
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point(347, 263);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 19;
            this.button1.Text = "&Cancel";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(266, 263);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 18;
            this.button2.Text = "&Save";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label16
            // 
            this.label16.Location = new System.Drawing.Point(9, 268);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(203, 30);
            this.label16.TabIndex = 37;
            this.label16.Text = "These settings will require GameSrv to be restarted before they will take effect." +
    "";
            // 
            // lblTimePerCallMinutes
            // 
            this.lblTimePerCallMinutes.AutoSize = true;
            this.lblTimePerCallMinutes.Location = new System.Drawing.Point(77, 241);
            this.lblTimePerCallMinutes.Name = "lblTimePerCallMinutes";
            this.lblTimePerCallMinutes.Size = new System.Drawing.Size(49, 13);
            this.lblTimePerCallMinutes.TabIndex = 38;
            this.lblTimePerCallMinutes.Text = "(minutes)";
            // 
            // frmServerSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 298);
            this.Controls.Add(this.lblTimePerCallMinutes);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lblTimeFormatUISample);
            this.Controls.Add(this.lblTimeFormatLogSample);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.txtTimeFormatUI);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.txtTimeFormatLog);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.cboWebSocketServerIP);
            this.Controls.Add(this.txtWebSocketServerPort);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.cboFlashSocketPolicyServerIP);
            this.Controls.Add(this.txtFlashSocketPolicyServerPort);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.cboRLoginServerIP);
            this.Controls.Add(this.txtRLoginServerPort);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.cboTelnetServerIP);
            this.Controls.Add(this.txtTelnetServerPort);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtTimePerCall);
            this.Controls.Add(this.lblTimePerCall);
            this.Controls.Add(this.txtLastNode);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtFirstNode);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtSysopEmail);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtSysopLastName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtSysopFirstName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtBBSName);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmServerSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Server Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtBBSName;
        private System.Windows.Forms.TextBox txtSysopFirstName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSysopLastName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSysopEmail;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtFirstNode;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtLastNode;
        private System.Windows.Forms.TextBox txtTimePerCall;
        private System.Windows.Forms.Label lblTimePerCall;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtTelnetServerPort;
        private System.Windows.Forms.ComboBox cboTelnetServerIP;
        private System.Windows.Forms.ComboBox cboRLoginServerIP;
        private System.Windows.Forms.TextBox txtRLoginServerPort;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cboFlashSocketPolicyServerIP;
        private System.Windows.Forms.TextBox txtFlashSocketPolicyServerPort;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cboWebSocketServerIP;
        private System.Windows.Forms.TextBox txtWebSocketServerPort;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtTimeFormatLog;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtTimeFormatUI;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label lblTimeFormatLogSample;
        private System.Windows.Forms.Label lblTimeFormatUISample;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label lblTimePerCallMinutes;
    }
}