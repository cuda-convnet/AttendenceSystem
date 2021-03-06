﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Xml;
using AttendanceSystemAlpha.Properties;
using AttendenceSystem_Alp;
using AttendenceSystem_Alp.PC;
using AutoUpdaterDotNET;
using HDFingerPrintHelper;
using RemObjects.DataAbstract;
using Helpers;
using Telerik.WinControls.UI;
//数据类型 C#-> C++
using FP_HANDLE = System.IntPtr;
using int8_t = System.Char;
using int16_t = System.Int16;
using int32_t = System.Int32;
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using INT = System.Int32;
using UINT = System.UInt32;
//数据类型 C#-> C++


namespace AttendanceSystemAlpha
{
    public partial class MainForm : Telerik.WinControls.UI.RadForm
    {
        
        #region Private fields
        private DataModule fDataModule;
        private DataTable _propertiesTable;
        private LoginForm _loginForm;
        private RadFrmShowClasses _frmShowClasses;
        private RadfrmChooseClasses _frmChooseClasses;
        private string _teacherName = "";
/*
        private string _currentPasswd = "AAC0A9DAA4185875786C9ED154F0DECE";
*/
        
        private DataTable dmTable;
        private DataTable xsidTable;
        private DataTable xkTable;
/*
        private int _buffDatabaseNum;
*/
        private Briefcase _propertieBriefcase;
        private DataTable _mngPropertiesTable;
        private Briefcase _mngPropertieBriefcase;
        private DateTime classTime;
        private string jsid = "";
        
        private string _mngTeacherName;
/*
        private string mngCurrentPasswd;
*/
        private Briefcase _mngchooseClassBriefcase;
        private DataTable _mngdmTable;
        private DataTable _mngSKtable;
/*
        private DataTable jieciDisplayTable;
*/
        private long JieCi;
        private string _mngClassName;
        FP_HANDLE FpHandle;

        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object[] propertyValue);
        private volatile Boolean ContinueOpration = false;
        private int nRet = 0;
        private ushort _fingerprinterVerifyId = 0;
        private ushort _fingerprinterScore = 0;
        private ManualRollCall _frmmanualRollCall;
        private Briefcase ChooseClassBriefcase;
        private long CurrentRollCallJieci;
        //XML日志
        #endregion
        //private static readonly object logLocker = new object();
        //private static XmlDocument _doc = new XmlDocument();
        private bool isDoingRollCall = false;
        //static void Log(string logname, string details)
        //{
        //    lock (logLocker)
        //    {
        //        var el = (XmlElement)_doc.DocumentElement.AppendChild(_doc.CreateElement("Detail"));
        //        el.SetAttribute("Logname", logname);
        //        el.AppendChild(_doc.CreateElement("details")).InnerText = details;
        //        _doc.Save("logs.txt");
        //    }
        //}
        //XML日志
        public MainForm()
        {
            InitializeComponent();
            //if (File.Exists("logs.txt"))
            //    _doc.Load("logs.txt");
            //else
            //{
            //    var root = _doc.CreateElement("Logs");
            //    _doc.AppendChild(root);
            //}
            this.panel18.BringToFront();

            xsidTable = new DataTable("学生信息");
            fDataModule = new DataModule();
            
            if (System.IO.File.Exists(Properties.Settings.Default.PropertiesBriefcaseFolder)) return;
            try
            {
                if (!Directory.Exists(string.Format(Properties.Settings.Default.OfflineFolder, " ")))
                Directory.CreateDirectory(string.Format(Properties.Settings.Default.OfflineFolder, " "));
                Briefcase propertiesBriefcase = new FileBriefcase(".\\Resources\\Properties.daBriefcase");
                DataTable bClistTable = new DataTable("PropertiesTable");
                    
                //DataRow bflistRow = null;
                if (!bClistTable.Columns.Contains("开课编号"))
                {
                    bClistTable.Columns.Add("开课编号", type: Type.GetType("System.String"));
                    bClistTable.Columns.Add("教师姓名", type: Type.GetType("System.String"));
                    bClistTable.Columns.Add("开课名称", type: Type.GetType("System.String"));
                }
                    
                propertiesBriefcase.AddTable(bClistTable);
                    
                propertiesBriefcase.WriteBriefcase();
            }
            catch (Exception exception)
            {
                MessageBox.Show("出现错误： " + exception.Message);
                return;
            }
            
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
           
            this.Visible = false;
            //平板和笔记本的区别
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.MaximumSize = new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
                this.WindowState = FormWindowState.Maximized;
            }
            
            //平板和笔记本的区别
            //this.Width = 1280;
            //this.Height = 775;
            string __serverUrl = File.ReadAllText(@"ServerUrl.txt");
            fDataModule.setServerURL(__serverUrl);
            Properties.Settings.Default.ServerUrl = __serverUrl;
            
            _frmShowClasses = new RadFrmShowClasses(fDataModule);
            _frmChooseClasses = new RadfrmChooseClasses();
            
            //**********饼图*********//

            List<string> xData = new List<string>() { "实到：0", "未到:0" };
            List<int> yData = new List<int>() {0 , 50 };
            //chart1.Series[0]["PieLabelStyle"] = "Outside";//将文字移到外侧
            //chart1.Series[0]["PieLineColor"] = "Black";//绘制黑色的连线。
            chart1.Series[0].Points.DataBindXY(xData, yData);
            chart2.Series[0].Points.DataBindXY(xData, yData);
            //***********饼图*********//
            this.Visible = true;
            SetMngControlInvisible();
            panel19.Visible = panel22.Visible = false;

            this.toolStripVersionLabel.Text = string.Format("   版本号：{0}" , Assembly.GetExecutingAssembly().GetName().Version.ToString());//显示版本号

            string updateurl = File.ReadAllText(@"UpdateServer.txt");//获得升级地址

            AutoUpdater.Start(updateurl);//开始升级操作
           
        }


        private void mainPageView_MouseUp(object sender, MouseEventArgs e)
        {
          
            switch (mainPageView.SelectedPage.Name)
            {
                case "viewpageLoadData":
                    
                    break;
                case "viewpageCall":
                    
                    break;
                case "viewpageDataManagement":
                    if (!Directory.Exists(string.Format(Properties.Settings.Default.OfflineFolder, "")) || Directory.GetFiles(string.Format(Properties.Settings.Default.OfflineFolder, "")).Length == 0)
                    {
                        MessageBox.Show("没有离线数据 请先下载离线数据");
                    }
                    else
                    {
                        _mngPropertieBriefcase = new FileBriefcase(Properties.Settings.Default.PropertiesBriefcaseFolder, true);
                        _mngPropertiesTable = _mngPropertieBriefcase.FindTable("PropertiesTable");
                        
                    }
                    
                    break;
            }
        }


        /// <summary>
        /// 结束点名按钮函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radButton1_Click(object sender, EventArgs e)
        {//结束点名按钮函数
            DialogResult dr2 = 
                MessageBox.Show("确定结束点名吗？", "确认结束点名", 
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            
            if (dr2 != DialogResult.OK) return;

            DialogResult dr3 = 
                MessageBox.Show("结束点名之后本节课将不能再次点名", 
                "确认结束点名", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            
            if (dr3 != DialogResult.OK) return;

            ////DataTable ClassStatusTable = frmChooseClasses._chooseClassBriefcase.FindTable("ClassStatus");
            ////// todo manangement
            //// DataRow mngClassStatusRow = ClassStatusTable.Select("Table编号 = '" + frmChooseClasses.Jieci.ToString() + "'")
            ////        .First();
            ////mngClassStatusRow.BeginEdit();
            ////mngClassStatusRow["签到情况"] = "已签到";
            ////mngClassStatusRow.EndEdit();
            //frmChooseClasses._chooseClassBriefcase.AddTable(ClassStatusTable);
            //frmChooseClasses._chooseClassBriefcase.WriteBriefcase();
            DataTable sktable = FormChooseClassParams.
                ChooseClassBriefcase.FindTable("SKTABLE");//从"选择课程窗口参数类"里获取SKTABLE

            DataRow skRow = null;//初始化skrow 该变量用于遍历变量,并且用于提交更改本地数据.

            skRow = sktable.Select
                ("SKNO = '" + FormChooseClassParams.Jieci + "'").First();
            //获取到节次编号(上课编号)之后,从SKTABLE(离线过的上课表)中获取需要上传的那节课的详细信息

            skRow.BeginEdit();//开始编辑这行信息
            skRow["SKDATE"] = DateTimePicker1.Value;//写入上课时间
            skRow["SKZT"] = 1; //更改上课状态为1(已经考勤)
            skRow.EndEdit();

            lock (ThreadLocker.CallingBriefcaseLocker)
            {
                FormChooseClassParams.
                    ChooseClassBriefcase.
                    AddTable(OfflineHelper.
                    TableListToDataTable
                    (EnumerableExtension.ToList<SKTABLE_07_VIEW>(sktable), "SKTABLE"));
                //将更改过的SKTABLE添加到本地数据中 并挂起更改

                FormChooseClassParams.
                    ChooseClassBriefcase.WriteBriefcase();//写入数据
            }
            
            //**********初始化界面***********//
            radButton1.Enabled = false;

            rbtnStartcall.Enabled = true;
            lbStudentClass.Text = "";
            lbStudentId.Text = "";
            lbStudentXy.Text = "";
            lbStudentName.Text = "";
            lbDczt.Text = "";
            lbDcsj.Text = "";
            pboxPhoto.Image = Resources.attendance_list_icon;
            ContinueOpration = false;
            isDoingRollCall = false;
            if (ContinueOpration)
            {
                HDFingerprintHelper.FpCloseUsb(FpHandle);
            }
            lbStudentName.Text = "学生姓名";
            panel19.Visible = panel22.Visible = false;
            radButton1.Enabled = false;
            rbtnStartcall.Enabled = true;
            BtnManualRollCall.Enabled = false;
            //**********初始化界面***********//
        }
        /// <summary>
        ///统计正常上课学生数据
        /// </summary>
        /// <param name="dmTable">已经离线的点名表</param>
        /// <returns>正常上课学生人数</returns>
        private static int CountArriveSudentNumber(DataTable dmTable)
        {
            DataRow[] dmRows;

            dmRows = dmTable.Select("DKZT = '0'");

            return dmRows.Count();
        }

        /// <summary>
        /// 统计迟到学生人数
        /// </summary>
        /// <param name="dmTable">已经离线的点名表</param>
        /// <returns>迟到人数</returns>
        private static int CountLateStudentNumber(DataTable dmTable)
        {
            DataRow[] dmRows;
            dmRows = dmTable.Select( "DKZT = '1'");
            return dmRows.Count();
        }

        /// <summary>
        /// 统计早退学生人数
        /// </summary>
        /// <param name="dmTable">已经离线的点名表</param>
        /// <returns></returns>
        private static int CountLeaveEarly(DataTable dmTable)
        {
            DataRow[] dmRows;
            dmRows = dmTable.Select("DKZT = '2'");
            return dmRows.Count();
        }

        /// <summary>
        /// 统计缺勤人数
        /// </summary>
        /// <param name="dmTable">已经离线的点名表</param>
        /// <returns></returns>
        private static int CountAbsentStudent(DataTable dmTable)
        {
            DataRow[] dmRows;

            dmRows = dmTable.Select("DKZT = 3 or DKZT = 5");

            return dmRows.Count();
        }
        
        /// <summary>
        /// 显示已经选择的课程的信息
        /// </summary>
        private void ShowOfflineInformations()
        {
            //DataTable mngxkTable = null;
            DataTable dtResault = new DataTable();//建立临时的datatable  用于底部gridview 的显示

            if (!dtResault.Columns.Contains("到课状态"))//为临时的datatable添加字段
            {
                dtResault.Columns.Add("姓名", typeof (string));

                dtResault.Columns.Add("到课状态", typeof (string));

                dtResault.Columns.Add("学号", typeof (string));

                dtResault.Columns.Add("到课时间", typeof (DateTime));
            }

            //mngxkTable = mngchooseClassBriefcase.FindTable("XKTABLE_VIEW1");//从briefcase中将选课表拉出来
            //mngGridView.DataSource = mngdmTable;
            int _studentTotal = 0;//学生总数

            int _sdrs = 0;//实到人数

            double _dkpercent = 0.0;

            _studentTotal = _mngdmTable.Rows.Count;
            //_studentTotal = (CountArriveSudentNumber(mngdmTable) + CountLateStudentNumber(mngdmTable) +
            //                CountLeaveEarly(mngdmTable) + CountAbsentStudent(mngdmTable));//todo: 直接数人数
            _sdrs = (CountArriveSudentNumber(_mngdmTable) + CountLateStudentNumber(_mngdmTable));
            _dkpercent = (_sdrs / Convert.ToDouble(_studentTotal));
            //lbMngStudentTotal.Text = _studentTotal.ToString();
            //lbMngsdrs.Text = _sdrs.ToString();
            //lbMngDkpercent.Text = string.Format("{0:P}", _dkpercent);
            foreach (DataRow Row in _mngdmTable.Rows)
            {
                DataRow resaultRow = dtResault.NewRow();
                resaultRow["学号"] = Convert.ToString(Row["XSID"]);
                resaultRow["到课状态"] = Convert.ToString(Row["DKZT"]);
                switch (Convert.ToString(Row["DKZT"]))
                {
                    case "0":
                    {
                        resaultRow["到课状态"] = "正常到课";
                        break;
                    }
                    case "1":
                    {
                        resaultRow["到课状态"] = "迟到";
                        break;
                    }
                    case "2":
                    {
                        resaultRow["到课状态"] = "早退";
                        break;
                    }
                    case"3":
                    {
                        resaultRow["到课状态"] = "旷课";
                        break;
                    }
                    case "4":
                    {
                        resaultRow["到课状态"] = "请假";
                        break;
                    }
                    case "5":
                    {
                        resaultRow["到课状态"] = "未签到";
                        break;
                    }
                }
                resaultRow["姓名"] = Row["XSNAME"].ToString();
                //resaultRow["到课时间"] = Convert.ToDateTime(Row["DMSJ1"]);\
                if (Row["DMSJ1"] != DBNull.Value)
                {
                    resaultRow["到课时间"] = Convert.ToDateTime(Row["DMSJ1"]);
                }

                dtResault.Rows.Add(resaultRow);
            }
           
            mngGridView.DataSource = dtResault;
            //lbMngOfflineStatus.Text = "未提交";
            //**********饼图*********//

            List<string> xData = new List<string>() { "实到：" + _sdrs + "人", "未到" + (_studentTotal - _sdrs) + "人" };
            List<int> yData = new List<int>() { _sdrs, _studentTotal-_sdrs };
            //chart1.Series[0]["PieLabelStyle"] = "Outside";//将文字移到外侧
            //chart1.Series[0]["PieLineColor"] = "Black";//绘制黑色的连线。
            chart2.Series[0].Points.DataBindXY(xData, yData);
            //***********饼图*********//
            
        }

      
        
        private void radButton2_Click(object sender, EventArgs e)
        {//上传考勤数据
            try
            {
                _loginForm = new LoginForm(fDataModule , jsid); // todo get userID

                _loginForm.ShowDialog();//显示登录窗口

                if (!_loginForm.IsLogin()) return;//若登录失败 则退出该函数

                ProgressHelper.StartProgressThread();//开始显示上传进度

                int __count = _mngdmTable.Rows.Count; // 用于进度条的显示

                int i = 0;

                foreach (DataRow dr in _mngdmTable.Rows)
                {
                    //遍历每一行点名记录 更改本地数据 并挂起上传操作
                    dr.BeginEdit();

                    dr["POSTDATE"] = DateTime.Now;//本地数据修改:上传时间

                    dr["POSTMANNO"] = Convert.ToDecimal(fDataModule.GetUserID());//上传人员编号

                    if ((Int16)dr["DKZT"] == 5)//到课状态 5(未考勤) 强制转换成 3(旷课)
                    {
                        dr["DKZT"] = (Int16)3;
                    } 

                    dr.EndEdit();//结束编辑

                    ProgressHelper.SetProgress((int) (20*(++i/__count)));//设置进度条显示
                }

                ProgressHelper.SetProgress(20);//progress:  0-20

                var dmtableList = 
                    EnumerableExtension.
                    ToList<DMTABLE_08_NOPIC_VIEW>(_mngdmTable);
                //将dmtable转换成list 以便向服务器上传考勤结果

                _mngchooseClassBriefcase.
                    AddTable
                    (OfflineHelper.
                    TableListToDataTable(dmtableList,JieCi.ToString()));

                i = 0;//用于进度条的显示

                foreach (var dmtable08 in dmtableList)
                {
                    fDataModule.UpdateDmtable(dmtable08); // dmtable update完成

                    ProgressHelper.SetProgress(20+ (int) (70*(++i/__count)));
                }
                fDataModule.ApplyChanges(); //提交更改

                ProgressHelper.SetProgress(90);

                //mngSKtable = _chooseClassBriefcase.FindTable() // todo update sktable 点名方式 早退人数
                long _skno = JieCi;

                fDataModule.GetSktableQueryUpload(_skno);

                if (!fDataModule.Context.SKTABLE_07_VIEW.Any()) // 选择 sktable需要上传的那一列
                {
                    throw new Exception("数据库异常 找不到该教师的相关信息 \n请重试或者联系管理员");
                }

                //rowSktable07:需要上传的那一列
                SKTABLE_07_VIEW rowSktable07 = fDataModule.Context.SKTABLE_07_VIEW.First();
                rowSktable07.EDITDATE = DateTime.Now;
                //rowSktable07.DMFS = Convert.ToInt16(2);
                rowSktable07.EDITMANNO = Convert.ToInt64(fDataModule.GetUserID());
                
                rowSktable07.CDRS = Convert.ToInt16(CountLateStudentNumber(_mngdmTable));//统计迟到人数

                rowSktable07.KKRS = Convert.ToInt16(CountAbsentStudent(_mngdmTable));//统计旷课人数

                rowSktable07.ZCRS = Convert.ToInt16(CountArriveSudentNumber(_mngdmTable));//统计正常人数
                
                
                
                //SKTABLE_07 rowSktable07 = new SKTABLE_07();
                //rowSktable07.EDITDATE = DateTime.Now;
                rowSktable07.DMFS = Convert.ToInt16(1); // 一次点名

                rowSktable07.RZFS = Convert.ToInt16(2); // 指纹认证

                rowSktable07.ZTRS = 0;
                //rowSktable07.ZTRS = Convert.ToInt16(CountLeaveEarly(mngdmTable));

                rowSktable07.SKDATE =
                    Convert.ToDateTime(
                        _mngSKtable.Select("SKNO = '" + rowSktable07.SKNO.ToString() + "'").First()["SKDATE"]);
                rowSktable07.SKZT = 1; //签到状态变为1  (已经签到)


                fDataModule.UpdateSktable(rowSktable07); // sktable 提交完成

                ProgressHelper.SetProgress(95);//进度条

                fDataModule.ApplyChanges();//提交更改

                DataTable mngClassStatusTable = 
                    _mngchooseClassBriefcase.FindTable("ClassStatus");//更改考勤状态
                
                DataRow mngClassStatusRow = 
                    mngClassStatusTable.Select("Table编号 = '" + rowSktable07.SKNO + "'")
                        .First();//获取该堂课考勤状态的那一行
                    
                mngClassStatusRow.BeginEdit();//开始编辑这一行

                mngClassStatusRow["离线数据提交情况"] = "已提交";//更改离线数据提交情况

                mngClassStatusRow.EndEdit();//结束编辑

                _mngchooseClassBriefcase.AddTable(mngClassStatusTable);//保存更改之后的数据

                _mngchooseClassBriefcase.WriteBriefcase();//保存更改

                ProgressHelper.SetProgress(100);

                ProgressHelper.StopProgressThread();

                ProgressHelper.SetProgress(0);

                //lbMngOfflineStatus.Text = "数据提交成功";

                toolStripOperationStatus.Text = "数据提交成功";

                MessageBox.Show("数据提交成功");

                jsid = "";

                _loginForm.Close();//结束登录窗口

                //开始刷新指纹信息
                DialogResult dr2 = MessageBox.Show("需要刷新此门课程的指纹信息吗", "刷新指纹信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr2 == DialogResult.OK)
                {
                    fDataModule.RefreshStudentInformation(rowSktable07.KKNO.Value, _mngchooseClassBriefcase);
                    MessageBox.Show("刷新指纹信息成功");
                }
                //完成刷新指纹信息

            }
            catch (Exception exception)
            {
                ProgressHelper.StopProgressThread();
                ProgressHelper.SetProgress(0);
                //lbMngOfflineStatus.Text = "数据提交失败 请将以下信息提供给管理员：" +exception.Message;
                MessageBox.Show("数据提交异常 请将以下信息提供给管理员：" + exception.Message);
                return;
            }
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripTimeLabel.Text = DateTime.Now.ToString();
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            _loginForm = new LoginForm(fDataModule);
            _loginForm.ShowDialog(); // 登录
            if (_loginForm.IsLogin())   // 判断登录结果
            {
                _frmShowClasses.ShowDialog();
            }
            if (!File.Exists(Properties.Settings.Default.PropertiesBriefcaseFolder)) return;
            Briefcase ____briefcase = new FileBriefcase(Properties.Settings.Default.PropertiesBriefcaseFolder, true);
            DataTable ____datatable = ____briefcase.FindTable("PropertiesTable");
            lboxClassName.DataSource = ____datatable;
            this.lboxClassName.DisplayMember = "开课名称";
            lboxClassName.ValueMember = "开课编号";
            _loginForm.Close();
        }

        private void rbtnStartcall_Click_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.NeedUpload = false;
            xsidTable = new DataTable("学生信息");
            
            if (!Directory.Exists(string.Format(Properties.Settings.Default.OfflineFolder, "")) || System.IO.Directory.GetFiles(string.Format(Properties.Settings.Default.OfflineFolder, "")).Length == 0)
            {
                MessageBox.Show("没有离线数据 请先下载离线数据");
                return;
            }
            toolStripOperationStatus.Text = "开始点名";
            _frmChooseClasses.ShowDialog(); // 获得各种信息 弹窗 
            if (!FormChooseClassParams.Flag) return;
            dmTable = FormChooseClassParams.DmTable;
            //Log("已选择课程", frmChooseClasses.Jieci.ToString());
            _propertieBriefcase = FormChooseClassParams.PropertieBriefcase;
            ChooseClassBriefcase = FormChooseClassParams.ChooseClassBriefcase;
            if (!xsidTable.Columns.Contains("学生学号"))
            {
                xsidTable.Columns.Add("学生学号");
            }
            if (!xsidTable.Columns.Contains("指纹识别号"))
            {
                xsidTable.Columns.Add("指纹识别号");
            }

            //选择上课时间
            FrmChooseDate frmChooseDate = new FrmChooseDate(FormChooseClassParams.SjSkdate, FormChooseClassParams.YdSkdate);
            frmChooseDate.ShowDialog();
            if (frmChooseDate.isChanged)
            {
                DateTimePicker1.Value = frmChooseDate.dt;
                frmChooseDate.Close();
            }
            else
            {

                frmChooseDate.Close();
                toolStripOperationStatus.Text = "您取消了点名操作";
                return;
            }
            //选择上课时间
            xkTable = FormChooseClassParams.ChooseClassBriefcase.FindTable("XKTABLE_VIEW1");

            int _initCount = 0;
            while ((FpHandle = HDFingerprintHelper.FpOpenUsb(0xFFFFFFFF, 1000)) == IntPtr.Zero)
            {
                    DialogResult dr2 = MessageBox.Show("初始化指纹仪失败，\n点击【确定】，重新初始化指纹仪\n点击【取消】停止使用指纹仪，并开始手动考勤", "确认开启手动考勤", MessageBoxButtons.OKCancel, MessageBoxIcon.Question); //指纹仪如果没有初始化成功，则手动考勤
                    if (dr2 == DialogResult.OK) continue; //不用指纹仪考勤
                    InitWithoutFingerPrint();
                    StartManualRollCall();
                    return;

            }// 初始化指纹仪
            //Log("指纹仪初始化完成", FpHandle.ToString());
            
            uint16_t fingerId = 0;
            nRet =  HDFingerprintHelper.FpEmpty(FpHandle, 0); // 清空指纹仪
            if (nRet != 0)
            {
                
                DialogResult dr2 = MessageBox.Show("指纹仪初始化失败 错误代码:" + nRet.ToString()+"是否开始手动考勤？", "指纹仪初始化失败", MessageBoxButtons.OKCancel, MessageBoxIcon.Question); //指纹仪如果没有初始化成功，则手动考勤
                if (dr2 != DialogResult.OK) return;
                InitWithoutFingerPrint();
                //不用指纹仪考勤的函数应该写在这里
                //
                return;
            }
            ProgressHelper.StartProgressThread();
            ProgressHelper.SetProgress(10);
            int __count = xkTable.Rows.Count;
            int i = 0;
            foreach (DataRow dataRows in xkTable.Rows.Cast<DataRow>().Where(dataRows => dataRows["ZW2"] != DBNull.Value))
            {
                

                try
                {
                    nRet = HDFingerprintHelper.Download1Fingerprint(FpHandle, dataRows["ZW2"].ToString(), fingerId); // 下载一条指纹字符串到指纹仪中
                    if (nRet != 0)
                    {
                        ProgressHelper.StopProgressThread();

                        DialogResult dr2 = MessageBox.Show("下载指纹时出错，是否开始手动考勤？", "确认开启手动考勤", MessageBoxButtons.OKCancel, MessageBoxIcon.Question); //指纹仪如果没有初始化成功，则手动考勤
                        if (dr2 != DialogResult.OK) return;
                        InitWithoutFingerPrint();
                        StartManualRollCall();
                        //不用指纹仪考勤的初始化函数应该写在这里
                        //
                        return;
                    }
                    DataRow xsidRow = xsidTable.NewRow();
                    xsidRow["学生学号"] = dataRows["XSID"].ToString();
                    xsidRow["指纹识别号"] = fingerId.ToString();
                    xsidTable.Rows.Add(xsidRow);
                    fingerId++; // 指纹编号递增
                    //Log("已下载指纹", dataRows["XSID"].ToString());
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
                //ProgressHelper.SetProgress((int)(80 * (++i / __count)));
                ProgressHelper.SetProgress((int)Math.Ceiling((20.0 + 80.0 * ((double)i / (double)__count))));
                i += 1;
            }
            lbTeacherName.Text = FormChooseClassParams.TeacherName;
            lbClassName.Text = FormChooseClassParams.ClassName;
            preparedTime.Value = FormChooseClassParams.SjSkdate;
            rbtnStartcall.Enabled = false;
            radButton1.Enabled = true;
            panel19.Visible = panel22.Visible = true; // 设置信息区域可见
            //**********饼图*********//
            lbYdrs.Text = FormChooseClassParams.ChooseClassBriefcase.Properties[Properties.Settings.Default.PropertiesTotalStudentNumber];
            List<string> xData = new List<string>() { "实到：0 人", "未到：" + FormChooseClassParams.DmTable.Rows.Count + "人" };
            List<int> yData = new List<int>() { 0, FormChooseClassParams.DmTable.Rows.Count };
            //chart1.Series[0]["PieLabelStyle"] = "Outside";//将文字移到外侧
            //chart1.Series[0]["PieLineColor"] = "Black";//绘制黑色的连线。
            chart1.Series[0].Points.DataBindXY(xData, yData);
            //***********饼图*********//
            //this.lbYdrs.Text = frmChooseClasses.DmTable.Rows.Count.ToString();
            //this.lbSdrs.Text = "0";
            //this.lbMngDkpercent.Text = "0.00%"; // 这段代码是用来初始化label显示的。
            ContinueOpration = true;
            isDoingRollCall = true;
            
            Thread verifyThread = new Thread(VerifyFingerprint);
            verifyThread.IsBackground = true;
            verifyThread.Start();
            //Log("开始验证指纹", frmChooseClasses.Jieci.ToString());
            ProgressHelper.SetProgress(100);
            
            radButton1.Enabled = true;
            rbtnStartcall.Enabled = false;
            BtnManualRollCall.Enabled = true;
            ProgressHelper.StopProgressThread();
        }

        private void rbtnMngShowInformation_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.NeedUpload = true;
            _frmChooseClasses.ShowDialog();
            if (!FormChooseClassParams.Flag) return;
            _mngdmTable = FormChooseClassParams.DmTable;
            _mngTeacherName = FormChooseClassParams.TeacherName;
            JieCi = FormChooseClassParams.Jieci;
            _mngClassName = FormChooseClassParams.ClassName;
            dateTimePicker2.Value = FormChooseClassParams.SjSkdate;
            _mngchooseClassBriefcase = FormChooseClassParams.ChooseClassBriefcase;
            jsid = _mngchooseClassBriefcase.Properties[GlobalParams.PropertiesTeacherID]; // 获取教师工号 用于上传登录
            ShowOfflineInformations(); // 离线数据显示
            mngGridView.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            mngGridView.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            mngGridView.Columns["到课时间"].Width = 75;
            //*********以上测试良好 ****************//
            lbMngTeacherName.Text = _mngTeacherName;
            lbMngClassName.Text = _mngClassName;
            lbMngjieci.Text = string.Format("第{0}节", FormChooseClassParams.Jieci);
            lbMngCallMethod.Text = "指纹点名";
            
            radButton2.Enabled = true;
            _mngSKtable = _mngchooseClassBriefcase.FindTable("SKTABLE");
            
            SetMngControlVisible();
        }

        

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            panel18.Visible = false;
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox2.Image = Properties.Resources.Exig_mouse_off;
            pictureBox2.BorderStyle = BorderStyle.Fixed3D;
        }

        private void pictureBox2_MouseEnter(object sender, EventArgs e)
        {
            pictureBox2.Image = Properties.Resources.Exit_mouse_on;
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            pictureBox2.Image = Properties.Resources.Exig_mouse_off;
            pictureBox2.BorderStyle = BorderStyle.None;
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox2.Image = Properties.Resources.Exit_mouse_on;
            pictureBox2.BorderStyle = BorderStyle.None;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (isDoingRollCall)
            {
                MessageBox.Show("请先结束点名");
                return;
            }
            DialogResult dr2 = MessageBox.Show("确定退出吗？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (dr2 != DialogResult.OK) return;
            this.Close();
            Application.Exit();
        }

        private void panel18_Paint(object sender, PaintEventArgs e)
        {
            if (!File.Exists(Properties.Settings.Default.PropertiesBriefcaseFolder)) return;
            Briefcase ____briefcase = new FileBriefcase(Properties.Settings.Default.PropertiesBriefcaseFolder, true);
            DataTable ____datatable = ____briefcase.FindTable("PropertiesTable");
            lboxClassName.DataSource = ____datatable;
            this.lboxClassName.DisplayMember = "开课名称";
            lboxClassName.ValueMember = "开课编号";
        }

        private void VerifyFingerprint()
        {
            
            while (ContinueOpration)
            {
                DataTable classTable = _propertieBriefcase.FindTable("ClassNameTable"); // 班级表
                int sdrs = 0;
                string XSID = "";
                string xsName = "";
                byte[] xszpBytes = null;

                //Log("检测手指是否按下", frmChooseClasses.Jieci.ToString());
                nRet =  HDFingerprintHelper.StartVerify(FpHandle, "fingerprint.bmp", ref  _fingerprinterVerifyId, ref  _fingerprinterScore,
                   3000); // 新的指纹仪验证语句 如果没有检测到指纹 返回值为9 即没有搜索到指纹
                //Log("检测到手指按下", string.Format("编号{0} , 指纹识别质量{1}", FingerprinterVerifyID, FingerprinterScore));
                if (File.Exists("fingerprint.bmp"))
                {
                    FileStream fs = new FileStream("fingerprint.bmp", FileMode.Open, FileAccess.Read, FileShare.Read);
                    BinaryReader br = new BinaryReader(fs);
                    MemoryStream ms = new MemoryStream(br.ReadBytes((int)fs.Length));
                    try
                    {
                        SetControlPropertyThreadSafe(pboxPhoto, "Image", new object[] { Image.FromStream(ms) });
                    }
                    catch (Exception)
                    {
                         //Log("写入指纹图像","failed" );
                    }
                    fs.Close();
                    //Log("写入指纹图像","success" );
                }
                
                DataRow[] xsidRows;
                DataRow[] dmRows;
                DataRow[] xkRows;

                classTime = DateTimePicker1.Value;
                 xsidRows = xsidTable.Select("指纹识别号 = '" + _fingerprinterVerifyId.ToString() + "'");
                 
                SoundPlayer player; // 声音 player 声明
                try
                {
                    switch (nRet)
                    {
                        case 0: // 这个分支被lock了 lock对象：ThreadLocker.CallingBriefcaseLocker
                        {
                            //Log("指纹搜索成功正在查找学生信息", "Success");

                            lock (ThreadLocker.CallingBriefcaseLocker)
                            {
                                XSID = xsidRows.First()["学生学号"].ToString();
                                dmRows = dmTable.Select("XSID = '" + XSID + "'");
                                //Log("学生信息查找成功", XSID);
                                // Briefcase briefcase =
                                // new FileBriefcase(string.Format(Properties.Settings.Default.OfflineFolder, cbboxClassname.SelectedValue), true);

                                dmRows.First().BeginEdit();
                                //dmRows.First()["DMSJ1"] = DateTime.Now; //Convert.ToInt16(1);


                                if ((DateTime.Compare(DateTimePicker1.Value, DateTime.Now.AddMinutes(15)) > 0) || (DateTime.Compare(DateTimePicker1.Value.AddHours(2), DateTime.Now) < 0))
                                {
                                    //Log("未到点名时间", DateTime.Now.ToString());
                                    dmRows.First().EndEdit();
                                    SetControlPropertyThreadSafe(lbStudentName , "Text" , new object[]{"还未到点名时间"});
                                    player = new SoundPlayer(Resources.beepFail);
                                    player.Play();//播放声音
                                    player.Dispose();
                                    continue;
                                }

                                Boolean errFlag = dmTable.Rows.Cast<DataRow>().Any(dr => dr["DMSJ1"] != DBNull.Value && DateTime.Compare((DateTime) dr["DMSJ1"], DateTime.Now) > 0); // 判断当前时间是否在数据库最大的时间之前。如果是，该变量应为true
                                if (errFlag)
                                { // 如果为false，跳出循环
                                    //Log("时间错误：本次签到时间比之前已经签到的时间都提前", DateTime.Now.ToString());
                                    player = new SoundPlayer(Resources.beepFail);
                                    player.Play();//播放声音
                                    player.Dispose();
                                    SetControlPropertyThreadSafe(lbStudentClass, "Text", new object[] { "" });
                                    SetControlPropertyThreadSafe(lbStudentId, "Text", new object[] { "" });
                                    SetControlPropertyThreadSafe(lbStudentXy, "Text", new object[] { "" });
                                    SetControlPropertyThreadSafe(lbStudentName, "Text", new object[] { "时间错误" });
                                    SetControlPropertyThreadSafe(lbDczt, "Text", new object[] { "" });
                                    SetControlPropertyThreadSafe(lbDcsj, "Text", new object[] { "" });
                                    continue;
                                }


                                player = new SoundPlayer(Resources.beepSuccess);
                                player.Play(); // 播放声音
                                player.Dispose();
                    
                                if (dmRows.First()["DMSJ1"] == DBNull.Value || (Convert.ToDateTime(dmRows.First()["DMSJ1"]) > DateTime.Now)) // 判断是否在点名时间范围内
                                {
                                    dmRows.First()["DMSJ1"] = DateTime.Now;
                                }

                                if (DateTimePicker1.Value > Convert.ToDateTime(dmRows.First()["DMSJ1"]))
                                {
                                    dmRows.First()["DKZT"] = 0;
                                    //lbDczt.Text = "按时到课";
                                    SetControlPropertyThreadSafe(lbDczt, "Text", new object[] { "按时到课" });
                                }
                                else
                                {
                                    dmRows.First()["DKZT"] = 1;
                                    //lbDczt.Text = "迟到";
                                    SetControlPropertyThreadSafe(lbDczt, "Text", new object[] { "迟到" });
                                }
                                dmRows.First().EndEdit();

                                //briefcase.RemoveTable(GlobalParams.SKNO); //briefcase直接addtable 代表更新
                                //briefcase.WriteBriefcase();


                                //dmTable.TableName = GlobalParams.SKNO;
                                dmTable = OfflineHelper.TableListToDataTable(EnumerableExtension.ToList<DMTABLE_08_NOPIC_VIEW>(dmTable),
                                    FormChooseClassParams.Jieci.ToString());
                                FormChooseClassParams.ChooseClassBriefcase.AddTable(dmTable);
                                FormChooseClassParams.ChooseClassBriefcase.Properties[Settings.Default.PropertiesLastCheckin] = FormChooseClassParams.Jieci.ToString();
                                FormChooseClassParams.ChooseClassBriefcase.WriteBriefcase();//写入briefcase
                                sdrs = CountArriveSudentNumber(dmTable) + CountLateStudentNumber(dmTable);
                                //显示信息
                                xkRows = xkTable.Select("XSID = '" + XSID + "'");

                                DataRow bjRow = classTable.Select("BJID = '" + xkRows.First()["BJID"].ToString() + "'").First();

                                //lbStudentXy.Text = bjRow["XYNAME"].ToString();
                                SetControlPropertyThreadSafe(lbStudentXy, "Text", new object[] { bjRow["XYNAME"].ToString() });
                                //lbStudentClass.Text = bjRow["BJNAME"].ToString();
                                SetControlPropertyThreadSafe(lbStudentClass, "Text", new object[] { bjRow["BJNAME"].ToString() });

                                xsName = xkRows.First()["XSNAME"].ToString();
                                if (xkRows.First()["XSZP"] != DBNull.Value)
                                {
                                    xszpBytes = (byte[])xkRows.First()["XSZP"];
                                    Stream ms = new MemoryStream(xszpBytes);
                                    ms.Write(xszpBytes, 0, xszpBytes.Length);
                                    //pboxPhoto.Image = Image.FromStream(ms);
                                    SetControlPropertyThreadSafe(pboxPhoto, "Image", new object[] { Image.FromStream(ms) });
                                }
                                else
                                {
                                    //pboxPhoto.Image = Properties.Resources.attendance_list_icon;
                                    SetControlPropertyThreadSafe(pboxPhoto, "Image", new object[] { Resources.attendance_list_icon });
                                }

                                //lbStudentName.Text = xsName;
                                SetControlPropertyThreadSafe(lbStudentName, "Text", new object[] { xsName });
                                //lbStudentId.Text = XSID;
                                SetControlPropertyThreadSafe(lbStudentId, "Text", new object[] { XSID });
                                //lbDcsj.Text = Convert.ToDateTime(dmRows.First()["DMSJ1"]).ToString("t", DateTimeFormatInfo.InvariantInfo);
                                SetControlPropertyThreadSafe(lbDcsj, "Text", new object[] { Convert.ToDateTime(dmRows.First()["DMSJ1"]).ToString("t", DateTimeFormatInfo.InvariantInfo) });

                                
                                //lbYdrs.Text = dmTable.Rows.Count.ToString();
                                //***********************更新label显示**********************************//已删除 代码供参考
                                //SetControlPropertyThreadSafe(lbYdrs, "Text", new object[] { dmTable.Rows.Count.ToString() });
                                ////lbDKPercent.Text = (Convert.ToDouble(sdrs) / Convert.ToDouble(lbYdrs.Text)).ToString("0.00%");
                                //SetControlPropertyThreadSafe(lbDKPercent, "Text", new object[] { (Convert.ToDouble(sdrs) / Convert.ToDouble(lbYdrs.Text)).ToString("0.00%") });
                                ////lbSdrs.Text = sdrs.ToString();
                                //SetControlPropertyThreadSafe(lbSdrs, "Text", new object[] { sdrs.ToString() });

                                ////lbCdrs.Text = CountLateStudentNumber(dmTable).ToString();
                                //SetControlPropertyThreadSafe(lbCdrs, "Text", new object[] { CountLateStudentNumber(dmTable).ToString() });//已删除 代码供参考
                                //***********************更新label显示**********************************
                                //**********饼图*********//

                                List<string> xData = new List<string>() { "实到:" + sdrs + "人", "未到" + (dmTable.Rows.Count-sdrs) + "人" };

                                List<int> yData = new List<int>() { sdrs, dmTable.Rows.Count - sdrs };
                                chart1.Invoke((MethodInvoker) delegate { chart1.Series[0].Points.DataBindXY(xData, yData); });
                                //***********饼图*********//
                                HDFingerprintHelper.LiftUrFinger(FpHandle , 5000);
                                
                            }//Locker:ThreadLocker.CallingBriefcaseLocker
                        }
                            break;
                        case 9:
                            player = new SoundPlayer(Resources.beepFail);
                            player.Play();//播放声音
                            player.Dispose();
                            SetControlPropertyThreadSafe(lbStudentClass, "Text", new object[] { "" });
                            SetControlPropertyThreadSafe(lbStudentId, "Text", new object[] { "" });
                            SetControlPropertyThreadSafe(lbStudentXy, "Text", new object[] { "" });
                            SetControlPropertyThreadSafe(lbStudentName, "Text", new object[] { "请重扫指纹" });
                            SetControlPropertyThreadSafe(lbDczt, "Text", new object[] { "" });
                            SetControlPropertyThreadSafe(lbDcsj, "Text", new object[] { "" });
                            break;
                        default:
                            if (!ContinueOpration)
                            {
                                player = new SoundPlayer(Resources.beepSuccess);
                                player.Play();//播放声音
                                player.Dispose();
                                //lbStudentClass.Text = "";
                                SetControlPropertyThreadSafe(lbStudentClass, "Text", new object[] { "" });
                                //lbStudentId.Text = "";
                                SetControlPropertyThreadSafe(lbStudentId, "Text", new object[] { "" });
                                //lbStudentXy.Text = "";
                                SetControlPropertyThreadSafe(lbStudentXy, "Text", new object[] { "" });
                                //lbStudentName.Text = "请重扫指纹";
                                SetControlPropertyThreadSafe(lbStudentName, "Text", new object[] { "学生姓名" });
                                //lbDczt.Text = "";
                                SetControlPropertyThreadSafe(lbDczt, "Text", new object[] { "" });
                                //lbDcsj.Text = "";
                                SetControlPropertyThreadSafe(lbDcsj, "Text", new object[] { "" });
                                //pboxPhoto.Image = Properties.Resources.attendance_list_icon;
                                //SetControlPropertyThreadSafe(pboxPhoto, "Image", new object[] { Properties.Resources.attendance_list_icon });
                            }
                            else
                            {
                                this.Invoke(new Action(() =>
                                {
                                    DialogResult dr2 = MessageBox.Show("指纹仪故障 错误代码:" + nRet + "\n点击【确定】，重新初始化指纹仪\n点击【取消】停止使用指纹仪，并开始手动考勤", "指纹仪故障", MessageBoxButtons.OKCancel, MessageBoxIcon.Question); //指纹仪如果没有初始化成功，则手动考勤
                                    if (dr2 != DialogResult.OK)
                                    {
                                        ContinueOpration = false;
                                        HDFingerprintHelper.FpCloseUsb(FpHandle);
                                        StartManualRollCall();
                                    }
                                    else if ((FpHandle = HDFingerprintHelper.FpOpenUsb(0xFFFFFFFF, 1000)) != IntPtr.Zero)
                                    {
                                        MessageBox.Show(this, "指纹仪初始化成功");
                                    }
                                }));
                                
                                while (FpHandle == IntPtr.Zero)
                                {
                                    FpHandle = HDFingerprintHelper.FpOpenUsb(0xFFFFFFFF, 1000);
                                    if (FpHandle == IntPtr.Zero)
                                    {
                                        this.Invoke(new Action(() =>
                                        {
                                            DialogResult dr2 = MessageBox.Show("指纹仪初始化失败 " + "\n点击【确定】，重新初始化指纹仪\n点击【取消】停止使用指纹仪，并开始手动考勤", "指纹仪故障", MessageBoxButtons.OKCancel, MessageBoxIcon.Question); //指纹仪如果没有初始化成功，则手动考勤
                                            if (dr2 != DialogResult.OK)
                                            {
                                                ContinueOpration = false;
                                                HDFingerprintHelper.FpCloseUsb(FpHandle);
                                                StartManualRollCall();
                                            }
                                        }));
                                    }
                                    else
                                    {
                                        this.Invoke(new Action(() => { MessageBox.Show(this, "指纹仪初始化成功"); }));
                                    }
                                }
                                
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    this.Invoke(new Action(() => { MessageBox.Show(this, "出现了一个错误。请将错误代码报告给管理员。错误代码：" + e.Message + "单击 确定 继续点名操作"); }));
                }
            }
        }
        public static void SetControlPropertyThreadSafe(Control control, string propertyName, object[] propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe), new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control,  propertyValue );
            }
        }
        

        
        private void DateTimePicker1_MouseUp(object sender, MouseEventArgs e)
        {
            
        }

        /// <summary>
        /// 将数据管理控件设置为不可见
        /// </summary>
        private void SetMngControlInvisible()
        {
            PnMngClassInfo.Visible = false;
            mngGridView.Visible = false;
        }
        /// <summary>
        /// 将数据管理控件设置为可见
        /// </summary>
        private void SetMngControlVisible()
        {
            PnMngClassInfo.Visible = true;
            mngGridView.Visible = true;
            
        }

        private void radButton6_Click(object sender, EventArgs e) // 删除课程按钮
        {
            if (!Directory.Exists(string.Format(Properties.Settings.Default.OfflineFolder, "")) || System.IO.Directory.GetFiles(string.Format(Properties.Settings.Default.OfflineFolder, "")).Length == 0)
            {
                MessageBox.Show("没有离线数据 请先下载离线数据");
                return;
            }

            var selectedClassId = lboxClassName.SelectedValue.ToString();

            ///////////////开始验证密码/////////////
            Briefcase classBriefcase =
                new FileBriefcase(
                    string.Format(Properties.Settings.Default.OfflineFolder, selectedClassId),
                    true); // 根据selectedClassId选中课程 并提取密码

            var currentPasswd = classBriefcase.Properties[Properties.Settings.Default.PropertiesBriefcasePasswd];//提取密码
            frmVerifyOfflinePasswd frmVerifyOfflinePasswd = new frmVerifyOfflinePasswd(currentPasswd);//开始验证密码
            frmVerifyOfflinePasswd.ShowDialog();
            if (frmVerifyOfflinePasswd.DialogResult == DialogResult.Cancel)
            {
                return;
            }
            ///////////////密码验证完毕/////////////
            
            DeleteClass(selectedClassId);

            RefreshlboxClassname();
            //第一步：删除课程
            //第二步：修改PropertiesBriefcase
            //第三步：


        }

        /// <summary>
        /// 删除一个已经下载的课程
        /// </summary>
        /// <param name="selectedID">选中的课程ID</param>
        private void DeleteClass(string selectedID)
        {
            File.Delete(string.Format(GlobalParams.CurrentOfflineDataFile, selectedID) +
                            ".daBriefcase");//删除该课程对应的Briefcase
            Briefcase propertiesBriefcase = new FileBriefcase(Properties.Settings.Default.PropertiesBriefcaseFolder, true);//实例化一个Briefcase ，用来对Briefcase进行更改

            DataTable propertiesDataTable = propertiesBriefcase.FindTable("PropertiesTable");//获取PropertiesBriefcase中的PropertiesTable（Datatable）

            propertiesDataTable.Rows.Remove(propertiesDataTable.Select("开课编号 = '" + selectedID + "'").First());

            propertiesBriefcase.AddTable(propertiesDataTable);//对PropertiesBriefcase进行更改并挂起该操作

            propertiesBriefcase.WriteBriefcase();//保存对PropertiesBriefcase的更改



        }

        /// <summary>
        /// 刷新 lboxClassname的显示
        /// </summary>
        private void RefreshlboxClassname()
        {
            Briefcase propertiesBriefcase = new FileBriefcase(Properties.Settings.Default.PropertiesBriefcaseFolder, true);//实例化一个Briefcase ，用来对Briefcase进行更改

            DataTable propertiesDataTable = propertiesBriefcase.FindTable("PropertiesTable");//获取PropertiesBriefcase中的PropertiesTable（Datatable）

            this.lboxClassName.DataSource = propertiesDataTable;
        }
        //手动签到部分->>
        private void BtnManualRollCall_Click(object sender, EventArgs e)
        {
            StartManualRollCall();
        }

        private void StartManualRollCall()
        {
            Briefcase classBriefcase =
                new FileBriefcase(
                    string.Format(Properties.Settings.Default.OfflineFolder, Properties.Settings.Default.CurrentRollCallClassNO),
                    true); // 根据properties中的CurrentRollCallClassNO选中课程 并提取密码

            string currentPasswd = classBriefcase.Properties[Properties.Settings.Default.PropertiesBriefcasePasswd];
            frmVerifyOfflinePasswd frmVerifyOfflinePasswd = new frmVerifyOfflinePasswd(currentPasswd);
            frmVerifyOfflinePasswd.ShowDialog();
            if (frmVerifyOfflinePasswd.DialogResult == DialogResult.Cancel)
            {
                return;
            }
            lock (ThreadLocker.CallingBriefcaseLocker)
            {
                _frmmanualRollCall = new ManualRollCall(FormChooseClassParams.ChooseClassBriefcase, FormChooseClassParams.Jieci, dmTable,
                    DateTimePicker1.Value);
                _frmmanualRollCall.ShowDialog();
                dmTable = FormChooseClassParams.ChooseClassBriefcase.FindTable(FormChooseClassParams.Jieci.ToString());
                int sdrs = CountArriveSudentNumber(dmTable) + CountLateStudentNumber(dmTable);
                List<string> xData = new List<string>() {"实到:" + sdrs + "人", "未到" + (dmTable.Rows.Count - sdrs) + "人"};

                List<int> yData = new List<int>() {sdrs, dmTable.Rows.Count - sdrs};

                chart1.Invoke((MethodInvoker) (() => chart1.Series[0].Points.DataBindXY(xData, yData)));
            }
        }

        //<<-手动签到部分
        private void InitWithoutFingerPrint()
        {
            lbTeacherName.Text = FormChooseClassParams.TeacherName;
            lbClassName.Text = FormChooseClassParams.ClassName;
            preparedTime.Value = FormChooseClassParams.SjSkdate;
            rbtnStartcall.Enabled = false;
            radButton1.Enabled = true;
            panel19.Visible = panel22.Visible = true; // 设置信息区域可见
            //**********饼图*********//
            lbYdrs.Text = FormChooseClassParams.ChooseClassBriefcase.Properties[Properties.Settings.Default.PropertiesTotalStudentNumber];
            List<string> xData = new List<string>() { "实到：0 人", "未到：" + FormChooseClassParams.DmTable.Rows.Count + "人" };
            List<int> yData = new List<int>() { 0, FormChooseClassParams.DmTable.Rows.Count };
            //chart1.Series[0]["PieLabelStyle"] = "Outside";//将文字移到外侧
            //chart1.Series[0]["PieLineColor"] = "Black";//绘制黑色的连线。
            chart1.Series[0].Points.DataBindXY(xData, yData);
            ContinueOpration = false;
            isDoingRollCall = true;
            radButton1.Enabled = true;
            rbtnStartcall.Enabled = false;
            BtnManualRollCall.Enabled = true;
        }
    }
}
