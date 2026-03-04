using CronAPI.Domain.Dto;
using CronAPI.Domain.Enumeration;
using CronAPI.Infrastructure.Method.Extension;
using CronAPI.Infrastructure.Method.Static;
using CronAPI.Infrastructure.ORM;
using FreeSql;
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
    public partial class TimeRecordForm : Form
    {
        EasyCrud easy = DB.easy;
        public TimeRecordForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.TaskRun(() =>
            {
                //查询统计
                GetStatus();
            });
            var queryIssucess = comboBox1.SelectedIndex;
            var queryType = comboBox2.SelectedIndex;
            var queryText = textBox1.Text;
            var list = easy.GetFreeSql().Select<PlanRecord>()
                           .WhereIf(queryIssucess != 0, x => queryIssucess == 1 ? x.Issucess == 1 : x.Issucess == 0)
                           .WhereIf(queryType != 0, x => x.Type == queryType - 1)
                           .WhereIf(!string.IsNullOrEmpty(queryText), x => x.Name.Contains(queryText) || x.Result.Contains(queryText))
                            .OrderByDescending(x => x.EndTime)
                            .Take(200)
                            .ToList<PlanRecordDto>();
            this.Text = $"【{list.FirstOrDefault()?.Name}】计划详情";
            dataGridView1.SetCommonWithCell<PlanRecordDto>(new ControlExtensions.DataDisplayEntityCell<PlanRecordDto>
            {
                DataList = list,
                HeadtextList = new List<(System.Linq.Expressions.Expression<Func<PlanRecordDto, object>> Feild, string TitileName, int Width)>
                {
                    (x=> x.Name , "名称",110),
                    (x=> x.Method , "请求",60),
                    (x=> x.EndTime , "时间",180),
                    (x=> x.isSucessStr , "状态",60),
                    (x=> x.HTTPCode , "状态码",70),
                    (x=> x.TypeStr , "类型",90),
                    (x=> x.Result , "返回结果",260),
                    (x=> x.Msg , "备注",150),

                },
                ButtonList = new List<(string ButtonName, string TitileName, int Width)>
                {
                    ("复制结果","操作",110)
                },
                RowAction = (entity, row) =>
                {
                    if (entity.Issucess == 1)
                    {
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 153, 51);
                    }
                    else
                    {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                    }
                }
            });
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void TimeRecordForm_Load(object sender, EventArgs e)
        {
            //默认
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            //查询数据
            button1_Click(sender, e);

        }

        private void GetStatus()
        {
            var day = 7;
            var list = easy.GetFreeSql()
                            .Select<PlanRecord>()
                            .Where(x => x.EndTime > DateTime.Now.AddDays(-day))
                            .ToList<PlanRecord>();
            decimal rate = 0;
            int timerigger = 0;
            int manual = 0;
            int retry = 0;
            int queue = 0;
            if (list.Count != 0)
            {
                rate = Math.Round((decimal)list.Count(x => x.Issucess == 1) / (list.Count) * 100, 2);
                timerigger = list.Count(x => x.Type == EnumTimerType.Timing.GetHashCode());
                manual = list.Count(x => x.Type == EnumTimerType.Manual.GetHashCode());
                retry = list.Count(x => x.Type == EnumTimerType.ErrorRetry.GetHashCode());
                queue = list.Count(x => x.Type == EnumTimerType.Queue.GetHashCode());
            }
            string template = $"统计：近{day}天接口调用成功率：{rate}%，其中：定时触发{timerigger}次、手动触发{manual}次、错误重试{retry}次、排队触发{queue}次,总共调用了{list.Count}次。";
            this.UISafeInvoke(() =>
            {
                label4.Text = template;
            });
        }

        /// <summary>
        /// 导出结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var entity = dataGridView1.GetCommonByButton<PlanRecordDto>("复制结果", e);
            if (entity != null)
            {
                entity.Result.JsonBeautify().ToClipboard();
                this.PopUpTips("结果已复制到您的粘贴板中！");
            }
        }

        /// <summary>
        /// 清空记录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            if (this.PopUpDialog("您确定要清空所有的调用记录吗，该操作不会影响已设定的计划。"))
            {
                await easy.TruncateTableAsync<PlanRecord>();
                this.PopUpTips("已清空所有的调用记录！");
                button1_Click(sender, e);
            }
        }
    }
}
