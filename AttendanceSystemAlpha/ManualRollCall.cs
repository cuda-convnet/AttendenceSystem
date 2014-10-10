﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web.Script.Services;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AttendenceSystem_Alp;
using AttendenceSystem_Alp.PC;
using RemObjects.DataAbstract;
using Telerik.WinControls;

namespace AttendanceSystemAlpha
{
    public partial class ManualRollCall : Telerik.WinControls.UI.RadForm
    {
        private long jieci;
        private Briefcase classBriefcase;
        private DataTable _dmTable;
        private DateTime _rollCallTime;
        private DataTable displayTable;
        private int currentLine = 0;
        public ManualRollCall(Briefcase _classBriefcase , long _jieci ,ref DataTable dmTable , DateTime RollcallTime)
        {
            InitializeComponent();
            this.classBriefcase = _classBriefcase;
            this.jieci = _jieci;
            _dmTable = dmTable;
            _rollCallTime = RollcallTime;
        }

        private void ManualRollCall_Load(object sender, EventArgs e)
        {
            displayTable = new DataTable();
            if (!displayTable.Columns.Contains("到课状态"))
            {
                displayTable.Columns.Add("姓名", typeof(string));
                displayTable.Columns.Add("学号", typeof(string));
                displayTable.Columns.Add("到课状态", typeof(string));
            }
            foreach (DataRow Row in _dmTable.Rows)
            {
                DataRow displayRow = displayTable.NewRow();
                if (Row["XSID"] != DBNull.Value)
                {
                    displayRow["学号"] = Convert.ToString(Row["XSID"]);
                }
                displayRow["到课状态"] = Convert.ToString(Row["DKZT"]);
                switch (Convert.ToString(Row["DKZT"]))
                {
                    case "0":
                        {
                            displayRow["到课状态"] = "正常到课";
                            break;
                        }
                    case "1":
                        {
                            displayRow["到课状态"] = "迟到";
                            break;
                        }
                    case "2":
                        {
                            displayRow["到课状态"] = "早退";
                            break;
                        }
                    case "3":
                        {
                            displayRow["到课状态"] = "旷课";
                            break;
                        }
                    case "4":
                        {
                            displayRow["到课状态"] = "请假";
                            break;
                        }
                    case "5":
                        {
                            displayRow["到课状态"] = "未签到";
                            break;
                        }
                }
                if (Row["XSNAME"] != DBNull.Value)
                {
                    displayRow["姓名"] = Row["XSNAME"].ToString();
                }
                
                //resaultRow["到课时间"] = Convert.ToDateTime(Row["DMSJ1"]);\
                displayTable.Rows.Add(displayRow);
            }
            GridStudentName.DataSource = displayTable;
            
            currentLine = GridStudentName.CurrentRow.Index;
            if (GridStudentName.SelectedRows.Any())
            {
                btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
            }

        }
        /// <summary>
        /// 更改一个点名记录
        /// </summary>
        /// <param name="dkzt">到课状态 0 正常到课 1 迟到 2 早退 3 旷课 4 请假 </param>
        /// <param name="isArrive">学生是否来上课 true 来上课 false 没有来上课</param>
        private void ChangeOnedmtableRecord(Int16 dkzt, bool isArrive)
        {
            DataRow[] dmRows;
            DataRow[] displayRows;
            lock (ThreadLocker.CallingBriefcaseLocker)
            {
                dmRows =   _dmTable.Select("XSID = '" + GridStudentName.CurrentRow.Cells[1].Value.ToString() + "'");
                if (dmRows.Any())
                {
                    dmRows.First().BeginEdit();
                    dmRows.First()["DKZT"] = dkzt;
                    
                    if (isArrive && dkzt == 1)
                    {
                        dmRows.First()["DMSJ1"] = DateTime.Now;
                    }
                    else
                    {
                        dmRows.First()["DMSJ1"] = _rollCallTime;
                    }
                    
                    dmRows.First().EndEdit();
                    displayRows = displayTable.Select("学号 = '" + GridStudentName.CurrentRow.Cells[1].Value + "'");
                    if (displayRows.Any())
                    {
                        displayRows.First().BeginEdit();
                        switch (dkzt)
                        {
                            case 0:
                                {
                                    displayRows.First()["到课状态"] = "正常到课";
                                    break;
                                }
                            case 1:
                                {
                                    displayRows.First()["到课状态"] = "迟到";
                                    break;
                                }
                            case 2:
                                {
                                    displayRows.First()["到课状态"] = "早退";
                                    break;
                                }
                            case 3:
                                {
                                    displayRows.First()["到课状态"] = "旷课";
                                    break;
                                }
                            case 4:
                            {
                                displayRows.First()["到课状态"] = "请假";
                                break;
                            }
                            case 5:
                            {
                                displayRows.First()["到课状态"] = "未签到";
                                break;
                            }
                        }
                        displayRows.First().EndEdit();
                    }
                }
                _dmTable =
                    OfflineHelper.TableListToDataTable(
                        Helpers.EnumerableExtension.ToList<DMTABLE_08_NOPIC_VIEW>(_dmTable), _dmTable.TableName);
                classBriefcase.AddTable(_dmTable);
                classBriefcase.WriteBriefcase();
                
            }
        }

        private void RefreshDisplay()
        {
            
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            if (btnChangeState.Text == "正常到课")
            {

                btnChangeState.Text = "迟到";
                ChangeOnedmtableRecord(1, true);
                //刷新label的显示
                if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
                {
                    lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                    btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                }
                
                //刷新label的显示
            }
            else if (btnChangeState.Text == "迟到")
            {
                btnChangeState.Text = "早退";
                ChangeOnedmtableRecord(2, false);
                //刷新label的显示
                if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
                {
                    lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                    btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                }
                //刷新label的显示
            }
            else if (btnChangeState.Text == "早退")
            {
                btnChangeState.Text = "旷课";
                ChangeOnedmtableRecord(3, false);
                //刷新label的显示
                if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
                {
                    lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                    btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                }
                //刷新label的显示
            }
            else if (btnChangeState.Text == "旷课")
            {
                btnChangeState.Text = "请假";
                ChangeOnedmtableRecord(4, false);
                //刷新label的显示
                if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
                {
                    lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                    btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                }
                //刷新label的显示
            }
            else if (btnChangeState.Text == "请假")
            {
                btnChangeState.Text = "正常到课";
                ChangeOnedmtableRecord(0, true);
                //刷新label的显示
                if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
                {
                    lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                    btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                }
            }
            else if (btnChangeState.Text == "未签到")
            {
                btnChangeState.Text = "正常到课";
                ChangeOnedmtableRecord(0, true);
                //刷新label的显示
                if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
                {
                    lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                    btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                }
            }

            
        }
        private void radButton7_Click(object sender, EventArgs e)
        {
            
        }

        private void GridStudentName_SelectionChanged(object sender, EventArgs e)
        {
            if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
            {
                lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
                currentLine = GridStudentName.CurrentRow.Index;
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (currentLine == (GridStudentName.Rows.Count - 1))
            {
                currentLine = 0;
            }
            else
            {
                currentLine++;
            }
            GridStudentName.Rows[currentLine].IsSelected = true;
            GridStudentName.CurrentRow = GridStudentName.Rows[currentLine];
            if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
            {
                lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            if (currentLine == 0)
            {
                currentLine = (GridStudentName.RowCount - 1);
            }
            else
            {
                currentLine--;
            }
            GridStudentName.Rows[currentLine].IsSelected = true;
            GridStudentName.CurrentRow = GridStudentName.Rows[currentLine];
            if (GridStudentName.SelectedRows.Any() && GridStudentName.CurrentRow.Cells[0].Value != DBNull.Value)
            {
                lbStudentName.Text = (string)GridStudentName.CurrentRow.Cells[0].Value;
                btnChangeState.Text = (string)GridStudentName.CurrentRow.Cells[2].Value;
            }
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            this.Hide();
            this.Height = 740;
            this.Width = 904;
        }

        private void ManualRollCall_VisibleChanged(object sender, EventArgs e)
        {
            displayTable = new DataTable();
            if (!displayTable.Columns.Contains("到课状态"))
            {
                displayTable.Columns.Add("姓名", typeof(string));
                displayTable.Columns.Add("学号", typeof(string));
                displayTable.Columns.Add("到课状态", typeof(string));
            }
            foreach (DataRow Row in _dmTable.Rows)
            {
                DataRow displayRow = displayTable.NewRow();
                if (Row["XSID"] != DBNull.Value)
                {
                    displayRow["学号"] = Convert.ToString(Row["XSID"]);
                }
                displayRow["到课状态"] = Convert.ToString(Row["DKZT"]);
                switch (Convert.ToString(Row["DKZT"]))
                {
                        
                    case "0":
                        {
                            displayRow["到课状态"] = "正常到课";
                            break;
                        }
                    case "1":
                        {
                            displayRow["到课状态"] = "迟到";
                            break;
                        }
                    case "2":
                        {
                            displayRow["到课状态"] = "早退";
                            break;
                        }
                    case "3":
                        {
                            displayRow["到课状态"] = "旷课";
                            break;
                        }
                    case "4":
                        {
                            displayRow["到课状态"] = "请假";
                            break;
                        }
                    case "5":
                        {
                            displayRow["到课状态"] = "未签到";
                            break;
                        }
                }
                if (Row["XSNAME"] != DBNull.Value)
                {
                    displayRow["姓名"] = Row["XSNAME"].ToString();
                }

                //resaultRow["到课时间"] = Convert.ToDateTime(Row["DMSJ1"]);\
                displayTable.Rows.Add(displayRow);
            }
            GridStudentName.DataSource = displayTable;

            currentLine = GridStudentName.CurrentRow.Index;
        }

    }
}
