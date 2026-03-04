using CronAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinformLib;

namespace CronAPI.Presentation
{
    public partial class AboutAutherForm : Form
    {
        public AboutAutherForm()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                // 1. 目标URL（可替换为任意网页）
                string targetUrl = "https://www.baidu.com";

                // 2. 调用系统默认浏览器打开URL
                // ProcessStartInfo：配置进程启动参数
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true // 关键：启用ShellExecute才能调用默认浏览器
                };

                // 启动进程（打开浏览器）
                Process.Start(psi);

                // 可选：标记链接为“已访问”（更新颜色）
                linkLabel1.LinkVisited = true;
            }
            catch (Exception ex)
            {
                // 异常处理（比如浏览器未安装、URL无效等）
                MessageBox.Show($"打开网页失败：{ex.Message}\n请检查浏览器是否正常或URL是否有效",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AboutAutherForm_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 查看流程图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            FileExtentions.OpenFile(".\\Domain\\Resource\\Pics\\tips.png");
        }
    }
}
