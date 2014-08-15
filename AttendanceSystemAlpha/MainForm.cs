﻿using System;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using AttendenceSystemClientBeta;
using AttendenceSystem_Alp;
using AttendenceSystem_Alp.PC;
using RemObjects.DataAbstract.Linq;
using 
namespace AttendanceSystemAlpha
{
    public partial class MainForm : Telerik.WinControls.UI.RadForm
    {
        private LoginForm loginForm;
        #region Private fields
        private DataModule fDataModule;
        #endregion

        public MainForm()
        {
            this.InitializeComponent();

            this.fDataModule = new DataModule();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            loginForm = new LoginForm(fDataModule);
        }

        private void mainPageView_MouseUp(object sender, MouseEventArgs e)
        {
            if (mainPageView.SelectedPage.Name == "viewpageLoadData")
            {
                if (loginForm.IsLogin()) //todo :将kktablequery写出来
                {
                    this.lbTeacherName.Text = fDataModule.getTeacherName();
                    this.clboxClassnames.Items.Clear();

                    foreach (KKTABLE_05 kktable05 in this.fDataModule.Context.KKTABLE_05)
                    {
                        clboxClassnames.Items.Add(kktable05.KKNAME);
                    }
                }
                else
                {
                    loginForm.Show();
                }
            }
        }

        private void rbtnFinish_Click(object sender, EventArgs e)
        {
            
        }

        private void rbtnCancel_Click(object sender, EventArgs e)
        {
            mainPageView.TabIndex = 0;
        }
    }
}