using CronAPI.Domain.Dto;
using CronAPI.Domain.Enumeration;
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
    public partial class QueueManagerForm : Form
    {
        EasyCrud easy = DB.easy;
        public QueueManagerForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 查询
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            var queryPlanTypeId = (comboBox2.GetCommonSelectWithEntity<PlanInfo>())?.ID;
            var list = await easy.GetFreeSql()
                                    .Select<PlanQueue, PlanInfo>()
                                    .InnerJoin((x, y) => x.PlanId == y.ID)
                                    .WhereIf(queryPlanTypeId > 0, (x, y) => x.PlanId == queryPlanTypeId)
                                    .OrderBy((x, y) => x.CreateTime)
                                    .ToListAsync<PlanQueueInfoDto>((x, y) => new PlanQueueInfoDto
                                    {
                                        QueueId=x.Id,
                                        PlanName = y.Name,
                                        CreateTime = x.CreateTime,
                                        RequestPath = y.RequestPath,
                                        RequestMethodStr = (EnumHTTPMethod)y.RequestMethod == EnumHTTPMethod.GET ? "GET" : "POST",
                                    });
            dataGridView1.SetCommon<PlanQueueInfoDto>(list, new List<(System.Linq.Expressions.Expression<Func<PlanQueueInfoDto, object>> fields, string name, int width)>
            {
                (x=> x.PlanName,"计划名称",250),
                (x=> x.RequestMethodStr,"请求方式",100),
                (x=> x.RequestPath,"请求路径",440),
                (x=> x.CreateTime,"创建时间",180),
            }, new List<string> { "清空队列" });

        }

        private void QueueManagerForm_Load(object sender, EventArgs e)
        {
            //渲染下拉框
            var list = easy.GetList<PlanInfo>().ToList();
            list.Insert(0, new PlanInfo()
            {
                ID = -1,
                Name = "全部"
            });
            comboBox1.SetCommonWithEntity<PlanInfo>(list, x => x.Name);
            comboBox2.SetCommonWithEntity<PlanInfo>(list, x => x.Name);


            //查询
            button1_Click(sender, e);

            //标签全透明
            var allLabels = this.GetChildrenControls<Label>();
            allLabels.ForEach(x =>
            {
                x.BackColor = Color.Transparent;
                x.Font = new Font("宋体", 10f, FontStyle.Bold);
            });

        }

        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            var queryPlan = (comboBox1.GetCommonSelectWithEntity<PlanInfo>());
            if (queryPlan.ID < 0 )
            {
                if (this.PopUpDialog($"您确定要清空【所有】计划所有待消费的队列吗？"))
                {
                    easy.ExecuteNonQuery("DELETE FROM PlanQueue;");
                    this.PopUpTips($"已清空【所有】计划所有消费的队列");
                    button1_Click(sender, e);
                }
                return;
            }
            else if (this.PopUpDialog($"您确定要清空【{queryPlan?.Name}】计划所有待消费的队列吗？"))
            {
                var planId = queryPlan?.ID;
                easy.DeleteByExp<PlanQueue>(x => x.PlanId == planId);
                this.PopUpTips($"已清空【{queryPlan?.Name}】计划所有消费的队列");
                button1_Click(sender, e);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var entity = dataGridView1.GetCommonByButton<PlanQueueInfoDto>("清空队列", e);
            if (entity != null)
            {
                easy.DeleteByExp<PlanQueue>(x => x.Id == entity.QueueId);
                button1_Click(sender,e);
            }
        }
    }


}
