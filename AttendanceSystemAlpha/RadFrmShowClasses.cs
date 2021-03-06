﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AttendenceSystem_Alp;
using AttendenceSystem_Alp.PC;
using Telerik.WinControls;

namespace AttendanceSystemAlpha
{
    public partial class RadFrmShowClasses : Telerik.WinControls.UI.RadForm
    {
        private long ____KKNO = 0;
        private DataModule ____fDataModule = null;
        private OfflinePasswdForm offlinePasswd = null;
        public RadFrmShowClasses(DataModule fDataModule)
        {
            ____fDataModule = fDataModule;
            offlinePasswd = new OfflinePasswdForm();
            InitializeComponent();
        }

        

        private void RadFrmShowClasses_Load(object sender, EventArgs e)
        {
            this.Width = 854;
            this.Height = 416;
            listBox1.DataSource = ____fDataModule.Context.JSANDKKVIEWRO;
            listBox1.DisplayMember = "KKNAME";
            listBox1.ValueMember = "KKNO";
            
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RadFrmShowClasses_SizeChanged(object sender, EventArgs e)
        {
            
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            offlinePasswd = new OfflinePasswdForm();
            offlinePasswd.ShowDialog(); //获得离线密码
            if (offlinePasswd.DialogResult == DialogResult.Cancel)
            {
                return;
            }
            ListBox.SelectedObjectCollection checkedToDownload = listBox1.SelectedItems;
            //选择的那门课程

            ____fDataModule.ServerToBriefcase
                (Properties.Settings.Default.CurrentDownloadPasswd, checkedToDownload);
            //开始下载课程信息
        }
    }
}
