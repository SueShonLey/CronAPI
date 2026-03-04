using CronAPI.Infrastructure.Method.Extension;
using CronAPI.Infrastructure.Method.Static;
using CronAPI.Infrastructure.ORM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinformLib;
using static CronAPI.Domain.Entity.Model;

namespace CronAPI.Presentation
{
    public partial class UrlSetForm : Form
    {
        EasyCrud easy = DB.easy;
        public UrlSetForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button1_Click(object sender, EventArgs e)
        {
            var queryText = textBox1.Text;
            var list = await easy.GetFreeSql()
                        .Select<ServerPort>()
                        .WhereIf(!string.IsNullOrEmpty(queryText), x => x.ServerUrl.Contains(queryText) || x.Name.Contains(queryText) || x.Remark.Contains(queryText))
                        .ToListAsync();
            dataGridView1.SetCommonWithCell<ServerPort>(new ControlExtensions.DataDisplayEntityCell<ServerPort>
            {
                DataList = list,
                HeadtextList = new List<(System.Linq.Expressions.Expression<Func<ServerPort, object>> Feild, string TitileName, int Width)>
                {
                    (x=>x.Name,"名称",200),
                    (x=>x.ServerUrl,"URL",400),
                    (x=>x.Remark,"备注",380)
                },
                ButtonList = new List<(string ButtonName, string TitileName, int Width)>
                {
                    ("删除","操作",80)
                },
                ColumnAction = (col) =>
                {
                    col.ReadOnly = false;
                }
            });
        }

        /// <summary>
        /// 新增
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            var dict = this.SetCustomizeForms(new CustomizeFormsExtentions.CustomizeFormInput
            {
                FormTitle = "URL设定",
                inputs = new List<CustomizeFormsExtentions.CustomizeValueInput>
                {
                    new CustomizeFormsExtentions.CustomizeValueInput
                    {
                       Label = "名称",
                       FormControlType = CustomizeFormsExtentions.FormControlType.InputBox
                    },
                    new CustomizeFormsExtentions.CustomizeValueInput
                    {
                       Label = "URL",
                       FormControlType = CustomizeFormsExtentions.FormControlType.InputBox,
                       DefaultValue = "http://127.0.0.1:8080"
                    },
                    new CustomizeFormsExtentions.CustomizeValueInput
                    {
                       Label = "备注",
                       FormControlType = CustomizeFormsExtentions.FormControlType.InputBox
                    },
                }
            });
            if (dict.Any())
            {
                var entity = new ServerPort
                {
                    Name = dict["名称"].ToString(),
                    ServerUrl = dict["URL"].ToString(),
                    Remark = dict["备注"].ToString()
                };
                easy.Insert(entity);
                button1_Click(sender, e);
            }
        }

        /// <summary>
        /// 保存更改
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            var dataList = dataGridView1.GetCommon<ServerPort>(new List<(System.Linq.Expressions.Expression<Func<ServerPort, object>> fields, string name)>
            {
                    (x=>x.Name,"名称"),
                    (x=>x.ServerUrl,"URL"),
                    (x => x.Remark, "备注")
            });
            easy.Update(dataList);
            button1_Click(sender, e);
        }

        private void UrlSetForm_Load(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var entity = dataGridView1.GetCommonByButton<ServerPort>("删除",e);
            if (entity != null)
            {
                easy.DeleteByExp<ServerPort>(x => x.Id == entity.Id);
                button1_Click(sender, e);
            }
        }
    }
}
