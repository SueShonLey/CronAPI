using CronAPI.Applications.Services;
using CronAPI.Domain.Dto;
using CronAPI.Domain.Enumeration;
using CronAPI.Infrastructure.Method.Extension;
using CronAPI.Infrastructure.Method.Static;
using CronAPI.Infrastructure.ORM;
using CronAPI.Presentation;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Vml.Office;
using System.Collections.Concurrent;
using WinformLib;
using static CronAPI.Domain.Entity.Model;
using static CronAPI.Infrastructure.Method.Static.EasyQuartz;

namespace CronAPI
{
    public partial class MainForm : Form
    {
        EasyCrud easy = DB.easy;
        static ConcurrentDictionary<int, EasyQuartzJobManager<TimerTaskService>> condict = new ConcurrentDictionary<int, EasyQuartzJobManager<TimerTaskService>>();
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            //常见设置
            this.SetCommon();

            //左边菜单栏
            flowLayoutPanel1.AddButtonsNew(new FlowLayoutPanelExtentions.FlowLayOutListInput
            {
                NameList = new List<string> { "计划设定", "调用记录", "服务器设定", "队列管理", "关于作者" },
                VerticalSpacing = 50
            }, (index, btnName, btn) =>
            {
                var btnlist = flowLayoutPanel1.GetChildrenControls<Button>();
                foreach (Button item in btnlist)
                {
                    item.BackColor = Color.White;
                }
                btn.BackColor = Color.Orange;

                if (index == 0)
                {
                    panel1.SetCommonRecover(this);
                }
                else if (index == 1)
                {
                    panel1.SetCommon(new TimeRecordForm()
                    {
                        BackgroundImage = this.BackgroundImage,
                        BackgroundImageLayout = this.BackgroundImageLayout,
                        MaximizeBox = false,
                        FormBorderStyle = this.FormBorderStyle
                    });
                }
                else if (index == 2)
                {
                    panel1.SetCommon(new UrlSetForm()
                    {
                        BackgroundImage = this.BackgroundImage,
                        BackgroundImageLayout = this.BackgroundImageLayout,
                        MaximizeBox = false,
                        FormBorderStyle = this.FormBorderStyle
                    });
                }
                else if (index == 3)
                {
                    panel1.SetCommon(new QueueManagerForm()
                    {
                        BackgroundImage = this.BackgroundImage,
                        BackgroundImageLayout = this.BackgroundImageLayout,
                        MaximizeBox = false,
                        FormBorderStyle = this.FormBorderStyle,
                        Icon = this.Icon,
                    });
                }
                else if (index == 4)
                {
                    panel1.SetCommon(new AboutAutherForm()
                    {
                        BackgroundImage = this.BackgroundImage,
                        BackgroundImageLayout = this.BackgroundImageLayout,
                        MaximizeBox = false,
                        FormBorderStyle = this.FormBorderStyle
                    });
                }

            });
            flowLayoutPanel1.GetChildrenControls<Button>().FirstOrDefault().BackColor = Color.Orange;


            //设置默认控件
            panel1.SetCommonDefualt(this);

            //设置所有状态为挂起状态（计划表初始化）
            await easy.ExecuteNonQueryAsync("update PlanInfo set Status=0");

            //查询
            button1_Click(sender, e);

            //定时设定（定时调度任务）
            var allPlan = easy.GetList<PlanInfo>(x => x.IsEnable == 1).ToList();
            foreach (var item in allPlan)
            {
                var jobManager = EasyQuartzJobManager<TimerTaskService>.Create(
                    taskId: item.ID.ToString(),
                    taskName: "定时任务",
                    cronExp: item.Cron,// 每2秒执行一次
                    item
                );
                await jobManager.StartJob();//全部启动
                condict.TryAdd(item.ID, jobManager);
            }

            //定时回补
            var backManager = EasyQuartzJobManager<BackTaskService>.Create(
                                        taskId: Guid.NewGuid().ToString(),
                                        taskName: "定时回补任务",
                                        cronExp: "0 0/5 * ? * * *"
                                    );
            await backManager.StartJob();//全部启动
            await backManager.TriggerJobNow();//立即触发一次
            
            //时钟
            TimerExtentions.RegisterTimer("定时时钟", 1000, async () =>
            {
                var nextTime = await backManager.GetNextFireTime() ?? DateTime.Now;
                var spanTime = Convert.ToInt32((nextTime - DateTime.Now).TotalSeconds);
                this.UISafeInvoke(() =>
                {
                    label2.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    label3.Text = $"距离下次队列消费还有{spanTime}秒";
                });
            },true);
        }

        /// <summary>
        /// 查询
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            var queryText = textBox1.Text;
            var list = await easy.GetFreeSql()
                            .Select<PlanInfo>()
                            .WhereIf(!string.IsNullOrEmpty(queryText), x => x.Name.Contains(queryText) || x.Remark.Contains(queryText) || x.RequestPath.Contains(queryText))
                            .ToListAsync<PlanInfoDto>()
                            ;
            dataGridView1.SetCommonWithCell(new ControlExtensions.DataDisplayEntityCell<PlanInfoDto>
            {
                DataList = list,
                HeadtextList = new List<(System.Linq.Expressions.Expression<Func<PlanInfoDto, object>> Feild, string TitileName, int Width)>
                {
                    (x=>x.Name,"名称",120),
                    (x=>x.RequestMethodStr,"请求",80),
                    (x=>x.RequestPath,"请求路径",350),
                    (x=>x.Cron,"Cron表达式",150),
                    (x=>x.IsEnableStr,"是否启用",100),
                    (x=>x.StatusStr,"状态",70),
                    (x=>x.IsQueueStr,"调用模式",100),
                     (x=>x.LatestExecuteTime,"上次执行",130),
                    (x=>x.NextExecuteTime,"下次执行",130),
                    (x=>x.RetryMaxTimes,"可尝试次数",110),
                    (x=>x.RetryIntervalTime,"错误间隔时间(s)",170),
                    (x=>x.Remark,"备注",200)
                },
                ButtonList = new List<(string ButtonName, string TitileName, int Width)>
                {
                    ("立即触发","操作1",80),
                    ("启停","操作2",80),
                    ("删除","操作3",80),
                    ("详情","操作4",80),
                    ("切换模式","操作5",80),
                },
                RowAction = (entity, row) =>
                {
                    if (entity.IsEnable == 0)
                    {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                    }
                    else
                    {
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 153, 51);
                    }
                },
                CellAction = (entity, col, cell) =>
                {
                    if (entity.IsEnable == 1 && col.Name.Equals("启停"))
                    {
                        cell.Value = "停止";
                    }
                    else if (entity.IsEnable == 0 && col.Name.Equals("启停"))
                    {
                        cell.Value = "启动";
                    }
                },
                ColumnAction = (col) =>
                {
                    var canEdit = new List<string> { "Name", "RequestPath", "Cron", "RetryMaxTimes", "RetryIntervalTime", "Remark" };
                    if (canEdit.Contains(col.Name))
                    {
                        col.ReadOnly = false;
                    }
                }
            });
        }

        /// <summary>
        /// 新增
        /// </summary>
        private async void button4_Click(object sender, EventArgs e)
        {
            var serverList = easy.GetList<ServerPort>().Select(x => x.ServerUrl).ToList();
            var dict = this.SetCustomizeFormsNew(new ControlExtensions.CustomizeFormInputNew
            {
                FormTitle = "新增定时计划",
                inputs = new List<ControlExtensions.CustomizeValueInputNew>
                {
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "计划名称"
                    },
                     new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "请求方式",
                        Value = new List<string>{"Get","Post"},
                        FormControlType = ControlExtensions.FormControlTypeNew.DropDown
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "请求服务器",
                        Value = serverList,
                        FormControlType = ControlExtensions.FormControlTypeNew.DropDown
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "请求路径",
                        DefaultValue="api/sys/GetData"
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "Cron表达式"
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "错误最大尝试次数",
                        FormControlType = ControlExtensions.FormControlTypeNew.NumberBox,
                        DefaultValue = "3"
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "错误尝试间隔（s）",
                        FormControlType = ControlExtensions.FormControlTypeNew.NumberBox,
                        DefaultValue = "10"
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "是否启用",
                        FormControlType = ControlExtensions.FormControlTypeNew.RadioButton,
                        Value = new List<string> { "是", "否" },
                        DefaultValue = "是"
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "调用模式",
                        FormControlType = ControlExtensions.FormControlTypeNew.RadioButton,
                        Value = new List<string> { "排队调用", "并发调用","跳过调用" },
                        DefaultValue = "排队调用"
                    },
                    new ControlExtensions.CustomizeValueInputNew
                    {
                        Label = "备注"
                    },
                }
            });
            try
            {
                if (dict.Any())
                {
                    var inputCron = dict.GetValueOrDefault("Cron表达式");
                    if (!EasyQuartz.CheckCron(inputCron))
                    {
                       this.PopUpTips("Cron表达式不合法", "提示", MessageBoxIcon.Error);
                        return;
                    }

                    var insert = new PlanInfo
                    {
                        Name = dict.GetValueOrDefault("计划名称"),
                        RequestMethod = dict.GetValueOrDefault("请求方式").Equals("Get") ? EnumHTTPMethod.GET.GetHashCode() : EnumHTTPMethod.POST.GetHashCode(),
                        RequestPath = dict.GetValueOrDefault("请求服务器")?.Trim('/') + "/" + dict.GetValueOrDefault("请求路径")?.Trim('/'),
                        Cron = inputCron,
                        RetryMaxTimes = int.TryParse(dict.GetValueOrDefault("错误最大尝试次数"), out var outmaxtimes) ? outmaxtimes : 0,
                        RetryIntervalTime = int.TryParse(dict.GetValueOrDefault("错误最大尝试次数"), out var outmaxinter) ? outmaxinter : 0,
                        IsEnable = dict.GetValueOrDefault("是否启用").Equals("是") ? 1 : 0,
                        IsQueue = GetQueueType(dict.GetValueOrDefault("调用模式")),
                        Remark = dict.GetValueOrDefault("备注")
                    };
                    var newid = Convert.ToInt32(await easy.GetFreeSql()
                                                            .Insert<PlanInfo>(insert)
                                                            .ExecuteIdentityAsync()) ;
                    // 新增后刷新
                    var item = insert;
                    item.ID = newid;
                    if (item.IsEnable == 1)
                    {
                        var jobManager = EasyQuartzJobManager<TimerTaskService>.Create(
                                                        taskId: newid.ToString(),
                                                        taskName: "定时任务",
                                                        cronExp: item.Cron,// 每2秒执行一次
                                                        item
                                                    );
                        await jobManager.StartJob();//全部启动
                        condict.TryAdd(newid, jobManager);
                    }
                    button1_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        /// <summary>
        /// 获取模式
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int GetQueueType(string data)
        {
            if (data.Contains("排队"))
                return EnumMode.Queue.GetHashCode();            
            if (data.Contains("并发"))
                return EnumMode.Concurrency.GetHashCode();
            if (data.Contains("跳过"))
                return EnumMode.Skip.GetHashCode();
            return EnumMode.Queue.GetHashCode();//默认是排队
        }

        /// <summary>
        /// 点击了DataGridView按钮的内容
        /// </summary>
        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var p1 = dataGridView1.GetCommonByButton<PlanInfo>("立即触发", e);
            var p2 = dataGridView1.GetCommonByButton<PlanInfo>("启停", e);
            var p3 = dataGridView1.GetCommonByButton<PlanInfo>("删除", e);
            var p4 = dataGridView1.GetCommonByButton<PlanInfo>("详情", e);
            var p5 = dataGridView1.GetCommonByButton<PlanInfo>("切换模式", e);
            if (p1 != null)
            {
                // 立即触发（不考虑重试）
                var entity = p1;
                await easy.UpdateSetWhereAsync<PlanInfo>(x => x.Status, 1, x => x.ID == entity.ID);//更新为正在调用状态
                var result = await EasyHttp.SendRequest(entity.RequestPath, (EnumHTTPMethod)entity.RequestMethod);
                //成功调用了接口
                var insert = new PlanRecord()
                {
                    Name = entity.Name,
                    PlanId = entity.ID,
                    StartTime = result.StartTime,
                    EndTime = result.EndTime,
                    Msg = result.IsSuccess ? "手动触发成功" : $"手动触发失败:{result.Msg}",
                    Issucess = result.IsSuccess ? 1 : 0,
                    Result = result.ReturnJson,
                    CostTime = result.SpendTime,
                    Method = ((EnumHTTPMethod)entity.RequestMethod).ToString(),
                    HTTPCode = result.StatusCode,
                    Type = EnumTimerType.Manual.GetHashCode()
                };
                //插入记录
                await DB.easy.InsertAsync(insert);
                // 更新计划
                entity.LatestExecuteTime = DateTime.Now;
                entity.Status = 0;
                await easy.UpdateAsync(entity);
                //提示
                this.PopUpTips(insert.Msg, "提示", result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            else if (p2 != null)
            {
                condict.TryGetValue(p2.ID, out var jobManager);
                if (jobManager == null)
                {
                    var item = p2;
                    jobManager = EasyQuartzJobManager<TimerTaskService>.Create(
                                                    taskId: item.ID.ToString(),
                                                    taskName: "定时任务",
                                                    cronExp: item.Cron,
                                                    item
                                                );
                    await jobManager.StartJob();//全部启动
                    condict.TryAdd(item.ID, jobManager);
                }
                if (p2.IsEnable == 1)
                {
                    p2.IsEnable = 0;
                    p2.Status = EnumHTTPStatus.NotRunning.GetHashCode();
                    var res = await jobManager.PauseJob();
                    if (res.Success)
                    {
                        this.PopUpTips("任务暂停成功！");
                    }
                }
                else
                {
                    p2.IsEnable = 1;
                    var res = await jobManager.ResumeJob();
                    if (res.Success)
                    {
                        this.PopUpTips("任务恢复成功！");
                    }
                }
                await easy.UpdateAsync(p2);
                button1_Click(sender, e);
            }
            else if (p3 != null)
            {
                condict.TryGetValue(p3.ID, out var jobManager);
                await easy.DeleteByExpAsync<PlanInfo>(x => x.ID == p3.ID);
                button1_Click(sender, e);
                if (jobManager != null)
                {
                    await jobManager.DeleteJob();
                }
            }
            else if (p4 != null)
            {

                FormExtentions.BindForm(new FormExtentions.BindFormInput<MainForm, PlanDetailsForm>
                {
                    Form1 = this,
                    Form2 = new PlanDetailsForm(),
                    Funs1 = null,
                    Funs2 = PlanDetailsForm.SetPlanId
                });
                this.SendMessage<PlanDetailsForm>(p4.ID.ToString());
                var open = this.ShowOnlyOne<PlanDetailsForm>(false);
                open.BackgroundImage = this.BackgroundImage;
                open.BackgroundImageLayout = this.BackgroundImageLayout;
                open.MaximizeBox = false;
                open.FormBorderStyle = this.FormBorderStyle;
                open.Icon = this.Icon;
                open.Font = this.Font;
            }
            else if (p5 != null)
            {
                var dict = this.SetCustomizeForms(new CustomizeFormsExtentions.CustomizeFormInput
                {
                    FormTitle = "请选择调用模式",
                    inputs = new List<CustomizeFormsExtentions.CustomizeValueInput>
                    {
                        new CustomizeFormsExtentions.CustomizeValueInput
                        {
                            Label="调用模式",
                            FormControlType = CustomizeFormsExtentions.FormControlType.DropDown,
                            Value = new List<string>{ "排队调用", "并发调用","跳过调用" }
                        }
                    }
                });
                if (dict.Any())//如果选择了模式
                {
                    int InputIsQueue = GetQueueType(dict.GetValueOrDefault("调用模式"));
                    p5.IsQueue = InputIsQueue;
                    await easy.UpdateSetWhereAsync<PlanInfo>(x => x.IsQueue, InputIsQueue, x => x.ID == p5.ID);
                    button1_Click(sender, e);
                }

            }
            else
            {

            }

        }

        /// <summary>
        /// 保存
        /// </summary>
        private async void button5_Click(object sender, EventArgs e)
        {
            var saveList = dataGridView1.GetCommon<PlanInfoDto>(new List<(System.Linq.Expressions.Expression<Func<PlanInfoDto, object>> fields, string name)>
            {
                    (x=>x.Name,"名称"),
                    (x=>x.RequestMethodStr,"方式"),
                    (x=>x.RequestPath,"请求路径"),
                    (x=>x.Cron,"Cron表达式"),
                    (x=>x.StatusStr,"状态"),
                    (x=>x.IsEnableStr,"是否启用"),
                    (x=>x.IsQueueStr,"调用模式"),
                    (x=>x.LatestExecuteTime,"上次执行"),
                    (x=>x.NextExecuteTime,"下次执行"),
                    (x=>x.RetryMaxTimes,"可尝试次数"),
                    (x=>x.RetryIntervalTime,"错误间隔时间(s)"),
                    (x=>x.Remark,"备注")
            }).MapToList<PlanInfo>();
            foreach (var item in saveList)
            {
                var entity = easy.FirstOrDefault<PlanInfo>(x => x.ID == item.ID);
                easy.UpdateSetMoreWhere<PlanInfo>(new List<(System.Linq.Expressions.Expression<Func<PlanInfo, object>> exp, object value)>()
                {
                    (x=>x.Name,item.Name),
                    (x=>x.RetryMaxTimes,item.RetryMaxTimes),
                    (x=>x.RetryIntervalTime,item.RetryIntervalTime),
                    (x=>x.RequestPath,item.RequestPath),
                    (x=>x.Remark,item.Remark),
                    (x=>x.Cron,item.Cron),
                }, x => x.ID == item.ID);
                if (!item.Cron.Equals(entity.Cron))
                {
                    condict.TryGetValue(item.ID, out var jobManager);
                    if (jobManager == null)
                    {
                        return;
                    }
                    var res = await jobManager.UpdateCronExpression(item.Cron);
                    if (res.Success)
                    {
                        this.PopUpTips("Cron表达式更新成功");
                    }
                }
            }

            button1_Click(sender, e);
        }
        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            var path = FileExtentions.PopUpFolder();
            if (string.IsNullOrEmpty(path))//如果没选择任何文件夹
            {
                return;
            }
            //下载到目标目录
            Task.Run(() =>
            {
                try
                {
                    var name = $"CronAPI_PlanExport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.xlsx";
                    var allpath = Path.Combine(path, name);
                    var dataList = dataGridView1.GetCommon<PlanInfoDto>()
                                                .Select(x => new ExportDto
                                                {
                                                    Name = x.Name,
                                                    RequestPath = x.RequestPath,
                                                    Cron = x.Cron,
                                                    RequestMethodStr = x.RequestMethodStr,
                                                    IsQueueStr = x.IsQueueStr,
                                                    IsEnableStr = x.IsEnable == 1 ? "是" : "否",
                                                    Remark = x.Remark
                                                })
                                                .ToList();

                    EasyExcel.WritFitstExcel(allpath, new EasyExcel.MulData<ExportDto>
                    {
                        Data = dataList,
                        Titlelist = new List<string>
                        {
                                "名称",
                                "定时表达式",
                                "请求路径",
                                "请求方法",
                                "调用模式",
                                "是否启用",
                                "备注"
                        }
                    });
                    FileExtentions.OpenFile(allpath);
                    dataList.Clear();
                    dataList = null;
                    // 1. 触发回收
                    GC.Collect();
                    // 2. 等待所有终结器（Finalize）执行完毕
                    GC.WaitForPendingFinalizers();
                    // 3. 再次回收（清理终结器执行后可释放的对象）
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            );

        }

        /// <summary>
        /// 导入
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            this.ShowOnlyOne<ImportForm>();
        }
    }

}

