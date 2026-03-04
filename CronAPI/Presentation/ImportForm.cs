using CronAPI.Domain.Dto;
using CronAPI.Infrastructure.Method.Extension;
using CronAPI.Infrastructure.Method.Static;
using CronAPI.Infrastructure.ORM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinformLib;
using static CronAPI.Domain.Entity.Model;

namespace CronAPI.Presentation
{
    public partial class ImportForm : Form
    {
        private string _Path { get; set; }
        public ImportForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 下载模板
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            var excelTemplatePath = @"./Domain/Resource/ExcelTemplate/导入模板.xlsx";
            var path = FileExtentions.PopUpFolder();//文件夹位置
                                                    // 校验文件夹是否选择有效
            if (string.IsNullOrEmpty(path))
            {
                this.PopUpTips("未选择目标文件夹，操作取消！");
                return;
            }
            try
            {
                //1. 拼接新文件名：导入模板_时间戳.xlsx
                string newFileName = $"CronAPI_PlanImport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.xlsx";
                // 2. 拼接目标文件完整路径
                string targetFilePath = Path.Combine(path, newFileName);

                // 3. 校验原模板文件是否存在
                if (!File.Exists(excelTemplatePath))
                {
                    throw new FileNotFoundException("Excel模板文件不存在", path);
                }

                // 4. 复制文件到目标文件夹（覆盖同名文件，如需避免覆盖可先判断文件是否存在）
                File.Copy(excelTemplatePath, targetFilePath, true);

                FileExtentions.OpenFolder(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportForm_Load(object sender, EventArgs e)
        {
            panel1.ReceiveFiles((path) =>
            {
                _Path = path;
                Settips($"已导入文件:{path}!\n请点击开始导入");
                button2.Enabled = true;
            });
        }

        private void Settips(string data)
        {
            label3.Text = data;
        }

        /// <summary>
        /// 开始导入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_Path))
            {
                this.PopUpTips("请先拖动内容文件到下方！");
                return;
            }
            Task.Run(() =>
            {
                try
                {
                    var list = EasyExcel.ReadFirstExcel(_Path).datas;
                    var insertList = new List<PlanInfo>();
                    int index = 1;
                    foreach (DataRow row in list.Rows)
                    {
                        if (!EasyQuartz.CheckCron(row[3].ToString()))
                        {
                            this.UISafeInvoke(() =>
                            {
                                MessageBox.Show($"【导入失败】Cron表达式错误！请检查第{index}行：{row[3].ToString()}");
                            });
                            return;
                        }
                        insertList.Add(new PlanInfo
                        {
                            Name = row[0].ToString(),
                            RequestMethod = Convert.ToInt32(row[1]),
                            RequestPath = row[2].ToString(),
                            Cron = row[3].ToString(),
                            RetryMaxTimes = Convert.ToInt32(row[4]),
                            RetryIntervalTime = Convert.ToInt32(row[5]),
                            IsEnable = Convert.ToInt32(row[6]),
                            IsQueue = Convert.ToInt32(row[7]),
                            Remark = row[8].ToString()
                        });
                        index++;
                    }
                    DB.easy.Insert(insertList);
                    _Path = string.Empty;
                    this.UISafeInvoke(() =>
                    {
                        button2.Enabled = false;
                        MessageBox.Show("导入成功！");
                        Settips("导入成功！");
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败！{ex.Message}!");
                }

            });

        }
    }
}
