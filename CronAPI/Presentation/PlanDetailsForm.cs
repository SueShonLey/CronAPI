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
    public partial class PlanDetailsForm : Form
    {
        EasyCrud easy = DB.easy;
        private static int _PlanId = -1;
        public PlanDetailsForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置计划
        /// </summary>
        /// <param name="id"></param>
        public static void SetPlanId(string id)
        {
            _PlanId = Convert.ToInt32(id);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlanDetailsForm_Load(object sender, EventArgs e)
        {
            GetQuery();
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void GetQuery()
        {
            var list = easy.GetFreeSql().Select<PlanRecord>()
                                        .Where(x => x.PlanId == _PlanId)
                                        .OrderByDescending(x => x.EndTime)
                                        .Take(200)
                                        .ToList<PlanRecordDto>();
            this.Text = $"【{list.FirstOrDefault()?.Name}】计划详情";
            dataGridView1.SetCommonWithCell<PlanRecordDto>(new ControlExtensions.DataDisplayEntityCell<PlanRecordDto>
            {
                DataList = list,
                HeadtextList = new List<(System.Linq.Expressions.Expression<Func<PlanRecordDto, object>> Feild, string TitileName, int Width)>
                {
                    (x=> x.Method , "请求",60),
                    (x=> x.isSucessStr , "状态",60),
                    (x=> x.Result , "返回结果",250),
                    (x=> x.StartTime , "开始时间",180),
                    (x=> x.EndTime , "结束时间",180),
                    (x=> x.CostTime , "用时(ms)",100),
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
        /// 点击按钮
        /// </summary>
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var entity = dataGridView1.GetCommonByButton<PlanRecordDto>("复制结果",e);
            if (entity != null)
            {
                entity.Result.JsonBeautify().ToClipboard();
                this.PopUpTips("结果已复制到您的粘贴板中！");
            }
        }
    }
}
