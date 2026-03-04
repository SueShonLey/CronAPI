using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CronAPI.Infrastructure.Method.Static
{
    public class EasyQuartz
    {
        #region 共有类
        #region 通用操作结果
        public class OperateResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }

            public static OperateResult Ok(string message = "操作成功") => new() { Success = true, Message = message };
            public static OperateResult Fail(string message, Exception ex = null) => new() { Success = false, Message = message, Exception = ex };
        }
        #endregion

        #region 任务状态枚举
        [Description("任务状态")]
        public enum EnumQuartzTaskStatus
        {
            [Description("运行中")]
            Running = 0,
            [Description("已停止/已删除")]
            Stopped = 1
        }
        #endregion

        #region Quartz调度器全局单例（关键优化：避免重复创建调度器）
        /// <summary>
        /// Quartz全局单例调度器（所有任务共享，符合Quartz最佳实践）
        /// </summary>
        public static class QuartzSchedulerSingleton
        {
            // 懒加载+线程安全，确保全局唯一实例
            private static readonly Lazy<Task<IScheduler>> _lazyScheduler = new Lazy<Task<IScheduler>>(async () =>
            {
                var scheduler = await StdSchedulerFactory.GetDefaultScheduler(CancellationToken.None);
                await scheduler.Start();
                Console.WriteLine($"【全局调度器】已启动：{scheduler.SchedulerName}（唯一实例）");
                return scheduler;
            });

            /// <summary>
            /// 获取全局唯一的调度器实例
            /// </summary>
            public static Task<IScheduler> GetSchedulerAsync() => _lazyScheduler.Value;
        }
        #endregion

        #region 判断Cron表达式是否合规
        /// <summary>
        /// 判断Cron表达式是否合规，若合规返回True
        /// </summary>
        public static bool CheckCron(string cronExpression) => CronExpression.IsValidExpression(cronExpression);
        #endregion
        #endregion

        #region 泛型类
        /// <summary>
        /// Quartz任务管理器（内聚所有任务属性+实例方法+异步+异常处理+功能完善）
        /// </summary>
        /// <typeparam name="T">额外数据类型</typeparam>
        /// <typeparam name="V">Job类型（必须实现IJob，无参构造）</typeparam>
        public class EasyQuartzJobManager<V>
            where V : IJob, new()
        {
            #region 原EasyQuartzInputDto的所有属性（直接内聚，公开可配置）
            /// <summary>
            /// 任务唯一Id（主键）
            /// </summary>
            public string TaskId { get; set; }

            /// <summary>
            /// 任务名称/描述
            /// </summary>
            public string TaskName { get; set; }

            /// <summary>
            /// Cron表达式
            /// </summary>
            public string CronExp { get; set; }

            /// <summary>
            /// 任务当前状态
            /// </summary>
            public EnumQuartzTaskStatus Status { get; set; } = EnumQuartzTaskStatus.Stopped;

            /// <summary>
            /// 额外的数据（支持匿名对象、类）
            /// </summary>
            public object ExtData { get; set; } = new object();

            /// <summary>
            /// Job类型（由泛型V自动推导，无需手动设置）
            /// </summary>
            public Type JobType => typeof(V);
            #endregion

            #region 私有字段
            // 全局共享的调度器实例（不再每个管理器创建一个）
            private readonly IScheduler _scheduler;
            #endregion

            #region 构造函数相关
            /// <summary>
            /// 基础构造函数（手动设置属性）
            /// </summary>
            public EasyQuartzJobManager()
            {
                // 获取全局单例调度器，避免Result死锁
                _scheduler = QuartzSchedulerSingleton.GetSchedulerAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// 重载构造函数（一次性设置核心属性，推荐）
            /// </summary>
            /// <param name="taskId">任务唯一Id</param>
            /// <param name="taskName">任务名称</param>
            /// <param name="cronExp">Cron表达式</param>
            /// <param name="status">初始状态（默认停止）</param>
            public EasyQuartzJobManager(string taskId, string taskName, string cronExp, object extData)
                : this()
            {
                TaskId = taskId;
                TaskName = taskName;
                CronExp = cronExp;
                Status = EnumQuartzTaskStatus.Stopped;
                ExtData = extData;
            }

            /// <summary>
            /// 静态创建方法（重载版，一次性配置核心属性，推荐）
            /// </summary>
            /// <param name="taskId">任务唯一Id</param>
            /// <param name="taskName">任务名称</param>
            /// <param name="cronExp">Cron表达式</param>
            /// <param name="status">初始状态</param>
            public static EasyQuartzJobManager<V> Create(string taskId, string taskName, string cronExp, object ExtData = null)
            {
                return new EasyQuartzJobManager<V>(taskId, taskName, cronExp, ExtData);
            }
            #endregion

            #region 核心任务操作方法（实例方法，无需传JobId，直接使用自身属性）
            /// <summary>
            /// 启动/创建任务（自动校验Cron+防重复创建，支持传递ExtData）
            /// </summary>
            public async Task<OperateResult> StartJob()
            {
                try
                {
                    // 1. 基础校验
                    if (string.IsNullOrWhiteSpace(TaskId))
                        return OperateResult.Fail("任务Id不能为空");
                    if (!CronExpression.IsValidExpression(CronExp))
                        return OperateResult.Fail($"Cron表达式非法：{CronExp}");

                    // 2. 检查任务是否已存在
                    var jobKey = new JobKey(TaskId);
                    if (await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务已存在，无法重复创建：{TaskId}");

                    // 3. 创建JobDetail（自动传递ExtData，序列化到JobDataMap）
                    var jobBuilder = JobBuilder.Create<V>()
                                                .WithIdentity(jobKey)
                                                .WithDescription(TaskName);
                    // 序列化额外数据（非空则传递）
                    if (ExtData != null)
                    {
                        jobBuilder.UsingJobData("ExtData", JsonConvert.SerializeObject(ExtData));
                    }
                    var job = jobBuilder.Build();

                    // 4. 创建Trigger（设置错失触发策略，避免任务堆积）
                    var trigger = TriggerBuilder.Create()
                                                .WithIdentity(GetTriggerKey())
                                                .WithCronSchedule(CronExp, x => x.WithMisfireHandlingInstructionDoNothing())
                                                .WithDescription($"{TaskName}的触发器")
                                                .Build();

                    // 5. 调度任务
                    await _scheduler.ScheduleJob(job, trigger);
                    Status = EnumQuartzTaskStatus.Running;
                    return OperateResult.Ok($"任务启动成功：{TaskId}（{TaskName}）");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"启动任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 暂停任务（操作自身绑定的任务，无需传参）
            /// </summary>
            public async Task<OperateResult> PauseJob()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法暂停：{TaskId}");

                    await _scheduler.PauseJob(jobKey);
                    Status = EnumQuartzTaskStatus.Stopped;
                    return OperateResult.Ok($"任务已暂停：{TaskId}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"暂停任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 恢复任务（操作自身绑定的任务，无需传参）
            /// </summary>
            public async Task<OperateResult> ResumeJob()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法恢复：{TaskId}");

                    // 获取原来的触发器
                    var triggerKey = GetTriggerKey();
                    var oldTrigger = await _scheduler.GetTrigger(triggerKey);
                    if (oldTrigger == null)
                        return OperateResult.Fail($"触发器不存在，无法恢复：{TaskId}");

                    // 重新构建触发器，设置错失触发策略为DoNothing（避免补偿）
                    var newTrigger = TriggerBuilder.Create()
                                                    .WithIdentity(triggerKey)
                                                    .WithDescription(oldTrigger.Description ?? $"{TaskName}的触发器")
                                                    .WithCronSchedule(CronExp, x => x.WithMisfireHandlingInstructionDoNothing())
                                                    .ForJob(jobKey)
                                                    .Build();

                    // 重新安排触发器（替换旧的触发器）
                    await _scheduler.RescheduleJob(triggerKey, newTrigger);

                    Status = EnumQuartzTaskStatus.Running;
                    return OperateResult.Ok($"任务已恢复：{TaskId}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"恢复任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 修改当前任务的Cron表达式（自动校验新Cron合法性）
            /// </summary>
            /// <param name="newCronExp">新的Cron表达式</param>
            public async Task<OperateResult> UpdateCronExpression(string newCronExp)
            {
                try
                {
                    // 1. 校验新Cron
                    if (!CronExpression.IsValidExpression(newCronExp))
                        return OperateResult.Fail($"新Cron表达式非法：{newCronExp}");

                    var jobKey = new JobKey(TaskId);
                    var triggerKey = GetTriggerKey();

                    // 2. 检查任务和触发器是否存在
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法更新Cron：{TaskId}");
                    var oldTrigger = await _scheduler.GetTrigger(triggerKey);
                    if (oldTrigger == null)
                        return OperateResult.Fail($"触发器不存在，无法更新Cron：{triggerKey}");

                    // 3. 重新构建触发器（保留原有描述，更新Cron）
                    var newTrigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .WithDescription(oldTrigger.Description ?? $"{TaskName}的触发器")
                        .WithCronSchedule(newCronExp, x => x.WithMisfireHandlingInstructionDoNothing())
                        .ForJob(jobKey)
                        .Build();

                    // 4. 重新调度并更新自身Cron属性
                    await _scheduler.RescheduleJob(triggerKey, newTrigger);
                    CronExp = newCronExp;
                    return OperateResult.Ok($"Cron表达式更新成功：{TaskId} -> {newCronExp}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"更新Cron表达式失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 立即触发当前任务（无视Cron调度，立即执行一次）
            /// </summary>
            public async Task<OperateResult> TriggerJobNow()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法立即执行,请检查是否执行了Create注册方法及StartJob启动方法：{TaskId}");

                    await _scheduler.TriggerJob(jobKey);
                    return OperateResult.Ok($"任务已立即执行：{TaskId}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"立即执行任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 删除当前任务（同时删除关联的触发器，彻底清理）
            /// </summary>
            public async Task<OperateResult> DeleteJob()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法删除：{TaskId}");

                    var isDeleted = await _scheduler.DeleteJob(jobKey);
                    if (isDeleted)
                    {
                        Status = EnumQuartzTaskStatus.Stopped;
                        return OperateResult.Ok($"任务已删除：{TaskId}");
                    }
                    return OperateResult.Fail($"删除任务失败：{TaskId}（调度器执行删除操作返回false）");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"删除任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 获取当前任务的实际运行状态（从调度器实时获取，非本地属性）
            /// </summary>
            public async Task<EnumQuartzTaskStatus> GetJobStatus()
            {
                var jobKey = new JobKey(TaskId);
                // 任务不存在则视为已停止
                if (!await _scheduler.CheckExists(jobKey))
                    return EnumQuartzTaskStatus.Stopped;

                var triggers = await _scheduler.GetTriggersOfJob(jobKey);
                // 无触发器则视为已停止
                if (!triggers.Any())
                {
                    Status = EnumQuartzTaskStatus.Stopped;
                    return Status;
                }

                // 遍历触发器，判断实际状态
                foreach (var trigger in triggers)
                {
                    var triggerState = await _scheduler.GetTriggerState(trigger.Key);
                    switch (triggerState)
                    {
                        case TriggerState.Paused:
                            Status = EnumQuartzTaskStatus.Stopped;
                            return Status;
                        case TriggerState.Normal:
                        case TriggerState.Blocked:
                        case TriggerState.Complete:
                            Status = EnumQuartzTaskStatus.Running;
                            return Status;
                    }
                }

                // 其他未知状态均视为已停止
                Status = EnumQuartzTaskStatus.Stopped;
                return Status;
            }

            /// <summary>
            /// 获取当前任务的下一次执行时间（转换为本地时间，null表示无后续执行/任务暂停）
            /// </summary>
            /// <returns>下一次执行本地时间（null=无后续执行）</returns>
            public async Task<DateTime?> GetNextFireTime()
            {
                try
                {
                    var triggerKey = GetTriggerKey();
                    // 从调度器获取当前任务的触发器（实时状态）
                    var trigger = await _scheduler.GetTrigger(triggerKey);
                    if (trigger == null) return null;

                    // 获取UTC下一次执行时间，转换为本地时间并返回
                    var nextFireTimeUtc = trigger.GetNextFireTimeUtc();
                    return nextFireTimeUtc?.LocalDateTime;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            #endregion

            #region 调度器操作（全局调度器，建议程序退出时统一调用）
            /// <summary>
            /// 关闭全局调度器（所有任务都会停止，程序退出时调用）
            /// </summary>
            public async Task ShutdownGlobalScheduler()
            {
                var scheduler = await QuartzSchedulerSingleton.GetSchedulerAsync();
                if (!scheduler.IsShutdown)
                {
                    await scheduler.Shutdown();
                    Console.WriteLine($"【全局调度器】已关闭：{scheduler.SchedulerName}");
                }
            }
            #endregion

            #region 私有方法
            /// <summary>
            /// 获取当前任务的触发器Key（与任务强绑定，自动生成）
            /// </summary>
            private TriggerKey GetTriggerKey()
            {
                return new TriggerKey($"{TaskId}_trigger", "Quartz_Trigger_Group");
            }
            #endregion
        }
        #endregion

        #region 命名空间类
        /// <summary>
        /// Quartz任务管理器（动态版本，通过命名空间+类名字符串指定Job类型）
        /// </summary>
        public class EasyQuartzJobManagerDynamic
        {
            #region 属性（与泛型版本保持一致）
            /// <summary>
            /// 任务唯一Id（主键）
            /// </summary>
            public string TaskId { get; set; }

            /// <summary>
            /// 任务名称/描述
            /// </summary>
            public string TaskName { get; set; }

            /// <summary>
            /// Cron表达式
            /// </summary>
            public string CronExp { get; set; }

            /// <summary>
            /// 任务当前状态
            /// </summary>
            public EnumQuartzTaskStatus Status { get; set; } = EnumQuartzTaskStatus.Stopped;

            /// <summary>
            /// 额外的数据（支持匿名对象、类）
            /// </summary>
            public object ExtData { get; set; } = new object();

            /// <summary>
            /// Job类型全名（格式："命名空间.类名, 程序集名" 或 "命名空间.类名"）
            /// 例如："ConsoleApp1.JobsPrint, ConsoleApp1" 或 "ConsoleApp1.JobsPrint"
            /// </summary>
            public string JobTypeFullName { get; set; }

            /// <summary>
            /// Job类型（从JobTypeFullName解析得到）
            /// </summary>
            public Type JobType => Type.GetType(JobTypeFullName);
            #endregion

            #region 私有字段
            private readonly IScheduler _scheduler;
            #endregion

            #region 构造函数
            /// <summary>
            /// 基础构造函数
            /// </summary>
            public EasyQuartzJobManagerDynamic()
            {
                _scheduler = QuartzSchedulerSingleton.GetSchedulerAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// 重载构造函数（推荐使用）
            /// </summary>
            public EasyQuartzJobManagerDynamic(string taskId, string taskName, string cronExp,
                                              string jobTypeFullName, object extData = null)
                : this()
            {
                TaskId = taskId;
                TaskName = taskName;
                CronExp = cronExp;
                JobTypeFullName = jobTypeFullName;
                ExtData = extData;
                Status = EnumQuartzTaskStatus.Stopped;
            }

            /// <summary>
            /// 静态创建方法（推荐）
            /// </summary>
            public static EasyQuartzJobManagerDynamic Create(string taskId, string taskName, string cronExp,
                                                            string jobTypeFullName, object extData = null)
            {
                return new EasyQuartzJobManagerDynamic(taskId, taskName, cronExp, jobTypeFullName, extData);
            }
            #endregion

            #region 核心任务操作方法（与泛型版本一致，但Job类型动态解析）
            /// <summary>
            /// 启动/创建任务
            /// </summary>
            public async Task<OperateResult> StartJob()
            {
                try
                {
                    // 1. 基础校验
                    if (string.IsNullOrWhiteSpace(TaskId))
                        return OperateResult.Fail("任务Id不能为空");
                    if (!CronExpression.IsValidExpression(CronExp))
                        return OperateResult.Fail($"Cron表达式非法：{CronExp}");

                    // 2. 校验Job类型
                    var jobType = JobType;
                    if (jobType == null)
                        return OperateResult.Fail($"无法找到Job类型：{JobTypeFullName}");
                    if (!typeof(IJob).IsAssignableFrom(jobType))
                        return OperateResult.Fail($"Job类型必须实现IJob接口：{jobType.FullName}");

                    // 3. 检查任务是否已存在
                    var jobKey = new JobKey(TaskId);
                    if (await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务已存在，无法重复创建：{TaskId}");

                    // 4. 创建JobDetail（动态类型创建）
                    var jobBuilder = JobBuilder.Create(jobType)
                                                .WithIdentity(jobKey)
                                                .WithDescription(TaskName);

                    if (ExtData != null)
                    {
                        jobBuilder.UsingJobData("ExtData", JsonConvert.SerializeObject(ExtData));
                    }
                    var job = jobBuilder.Build();

                    // 5. 创建Trigger
                    var trigger = TriggerBuilder.Create()
                                                .WithIdentity(GetTriggerKey())
                                                .WithCronSchedule(CronExp, x => x.WithMisfireHandlingInstructionDoNothing())
                                                .WithDescription($"{TaskName}的触发器")
                                                .Build();

                    // 6. 调度任务
                    await _scheduler.ScheduleJob(job, trigger);
                    Status = EnumQuartzTaskStatus.Running;
                    return OperateResult.Ok($"任务启动成功：{TaskId}（{TaskName}）");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"启动任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 暂停任务
            /// </summary>
            public async Task<OperateResult> PauseJob()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法暂停：{TaskId}");

                    await _scheduler.PauseJob(jobKey);
                    Status = EnumQuartzTaskStatus.Stopped;
                    return OperateResult.Ok($"任务已暂停：{TaskId}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"暂停任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 恢复任务
            /// </summary>
            public async Task<OperateResult> ResumeJob()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法恢复：{TaskId}");

                    // 获取原来的触发器
                    var triggerKey = GetTriggerKey();
                    var oldTrigger = await _scheduler.GetTrigger(triggerKey);
                    if (oldTrigger == null)
                        return OperateResult.Fail($"触发器不存在，无法恢复：{TaskId}");

                    // 重新构建触发器，设置错失触发策略为DoNothing（避免补偿）
                    var newTrigger = TriggerBuilder.Create()
                                                    .WithIdentity(triggerKey)
                                                    .WithDescription(oldTrigger.Description ?? $"{TaskName}的触发器")
                                                    .WithCronSchedule(CronExp, x => x.WithMisfireHandlingInstructionDoNothing())
                                                    .ForJob(jobKey)
                                                    .Build();

                    // 重新安排触发器（替换旧的触发器）
                    await _scheduler.RescheduleJob(triggerKey, newTrigger);

                    Status = EnumQuartzTaskStatus.Running;
                    return OperateResult.Ok($"任务已恢复：{TaskId}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"恢复任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 修改当前任务的Cron表达式
            /// </summary>
            public async Task<OperateResult> UpdateCronExpression(string newCronExp)
            {
                try
                {
                    // 1. 校验新Cron
                    if (!CronExpression.IsValidExpression(newCronExp))
                        return OperateResult.Fail($"新Cron表达式非法：{newCronExp}");

                    var jobKey = new JobKey(TaskId);
                    var triggerKey = GetTriggerKey();

                    // 2. 检查任务和触发器是否存在
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法更新Cron：{TaskId}");
                    var oldTrigger = await _scheduler.GetTrigger(triggerKey);
                    if (oldTrigger == null)
                        return OperateResult.Fail($"触发器不存在，无法更新Cron：{triggerKey}");

                    // 3. 重新构建触发器（保留原有描述，更新Cron）
                    var newTrigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .WithDescription(oldTrigger.Description ?? $"{TaskName}的触发器")
                        .WithCronSchedule(newCronExp, x => x.WithMisfireHandlingInstructionDoNothing())
                        .ForJob(jobKey)
                        .Build();

                    // 4. 重新调度并更新自身Cron属性
                    await _scheduler.RescheduleJob(triggerKey, newTrigger);
                    CronExp = newCronExp;
                    return OperateResult.Ok($"Cron表达式更新成功：{TaskId} -> {newCronExp}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"更新Cron表达式失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 立即触发当前任务
            /// </summary>
            public async Task<OperateResult> TriggerJobNow()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法立即执行,请检查是否执行了Create注册方法及StartJob启动方法：{TaskId}");

                    await _scheduler.TriggerJob(jobKey);
                    return OperateResult.Ok($"任务已立即执行：{TaskId}");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"立即执行任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 删除当前任务
            /// </summary>
            public async Task<OperateResult> DeleteJob()
            {
                try
                {
                    var jobKey = new JobKey(TaskId);
                    if (!await _scheduler.CheckExists(jobKey))
                        return OperateResult.Fail($"任务不存在，无法删除：{TaskId}");

                    var isDeleted = await _scheduler.DeleteJob(jobKey);
                    if (isDeleted)
                    {
                        Status = EnumQuartzTaskStatus.Stopped;
                        return OperateResult.Ok($"任务已删除：{TaskId}");
                    }
                    return OperateResult.Fail($"删除任务失败：{TaskId}（调度器执行删除操作返回false）");
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"删除任务失败：{ex.Message}", ex);
                }
            }

            /// <summary>
            /// 获取当前任务的实际运行状态
            /// </summary>
            public async Task<EnumQuartzTaskStatus> GetJobStatus()
            {
                var jobKey = new JobKey(TaskId);
                // 任务不存在则视为已停止
                if (!await _scheduler.CheckExists(jobKey))
                    return EnumQuartzTaskStatus.Stopped;

                var triggers = await _scheduler.GetTriggersOfJob(jobKey);
                // 无触发器则视为已停止
                if (!triggers.Any())
                {
                    Status = EnumQuartzTaskStatus.Stopped;
                    return Status;
                }

                // 遍历触发器，判断实际状态
                foreach (var trigger in triggers)
                {
                    var triggerState = await _scheduler.GetTriggerState(trigger.Key);
                    switch (triggerState)
                    {
                        case TriggerState.Paused:
                            Status = EnumQuartzTaskStatus.Stopped;
                            return Status;
                        case TriggerState.Normal:
                        case TriggerState.Blocked:
                        case TriggerState.Complete:
                            Status = EnumQuartzTaskStatus.Running;
                            return Status;
                    }
                }

                // 其他未知状态均视为已停止
                Status = EnumQuartzTaskStatus.Stopped;
                return Status;
            }

            /// <summary>
            /// 获取当前任务的下一次执行时间
            /// </summary>
            public async Task<DateTime?> GetNextFireTime()
            {
                try
                {
                    var triggerKey = GetTriggerKey();
                    // 从调度器获取当前任务的触发器（实时状态）
                    var trigger = await _scheduler.GetTrigger(triggerKey);
                    if (trigger == null) return null;

                    // 获取UTC下一次执行时间，转换为本地时间并返回
                    var nextFireTimeUtc = trigger.GetNextFireTimeUtc();
                    return nextFireTimeUtc?.LocalDateTime;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            /// <summary>
            /// 关闭全局调度器（所有任务都会停止，程序退出时调用）
            /// </summary>
            public async Task ShutdownGlobalScheduler()
            {
                var scheduler = await QuartzSchedulerSingleton.GetSchedulerAsync();
                if (!scheduler.IsShutdown)
                {
                    await scheduler.Shutdown();
                    Console.WriteLine($"【全局调度器】已关闭：{scheduler.SchedulerName}");
                }
            }
            #endregion

            #region 私有方法
            /// <summary>
            /// 获取当前任务的触发器Key
            /// </summary>
            private TriggerKey GetTriggerKey()
            {
                return new TriggerKey($"{TaskId}_trigger", "Quartz_Trigger_Group");
            }
            #endregion
        }
        #endregion

        #region 测试
        // 【提示】异步方法记得加await
        #region 泛型类测试
        ///// <summary>
        ///// 任务：每2秒打印一次
        ///// </summary>
        //public class JobsPrint : IJob
        //{
        //    public async Task Execute(IJobExecutionContext context)
        //    {
        //        await Task.CompletedTask;
        //        // 可选：获取传递的ExtData
        //        var extDataStr = context.JobDetail.JobDataMap.GetString("ExtData");
        //        Console.WriteLine($"【2秒任务】执行成功 - 时间：{DateTime.Now:HH:mm:ss} | 任务ID：{context.JobDetail.Key.Name} | 扩展数据：{extDataStr ?? "无"}");
        //    }
        //}
        //public class Program
        //{
        //    
        //    static async Task Main(string[] args)
        //    {
        //        Console.WriteLine("===== Quartz任务调度开始 =====");
        //        Console.WriteLine("按任意键退出程序...\n");

        //        #region 测试
        //        // 01 启动
        //        Console.WriteLine("启动任务");
        //        var jobManager = EasyQuartzJobManager<JobsPrint>.Create(
        //            taskId: "Jobs_001",
        //            taskName: "2秒定时打印任务",
        //            cronExp: "0/2 * * * * ?",// 每2秒执行一次
        //            new { DataName = "2秒任务数据", DataValue = 200 }
        //        );
        //        var jobsResult = await jobManager.StartJob();
        //        await Task.Delay(8000);

        //        // 02 暂停5s
        //        Console.WriteLine("暂停5s");
        //        var pauseResult = await jobManager.PauseJob();
        //        // 获取状态
        //        var result1 = await jobManager.GetJobStatus();
        //        Console.WriteLine("状态" + result1);
        //        await Task.Delay(5000);

        //        // 03 立即触发
        //        Console.WriteLine("立即触发");
        //        var triggerImmediately = await jobManager.TriggerJobNow();

        //        // 04 恢复任务
        //        Console.WriteLine("恢复任务");
        //        var resume = await jobManager.ResumeJob();
        //        await Task.Delay(8000);

        //        // 05 获取任务状态
        //        Console.WriteLine("获取任务状态");
        //        var result2 = await jobManager.GetJobStatus();
        //        Console.WriteLine("状态" + result2);

        //        // 06 修改cron表达式
        //        Console.WriteLine("修改cron表达式");
        //        var updateresult = await jobManager.UpdateCronExpression("0 0 8 * * ?");//每天早上八点触发一次

        //        // 07 获取下一次执行时间
        //        Console.WriteLine("获取下一次执行时间");
        //        var nextTime = await jobManager.GetNextFireTime();
        //        Console.WriteLine($"下一次执行:{nextTime}");

        //        // 08 删除任务
        //        Console.WriteLine("删除任务");
        //        var deleteresult = await jobManager.DeleteJob();
        //        var result3 = await jobManager.GetJobStatus();
        //        Console.WriteLine("状态" + result3);

        //        #endregion

        //        // 阻塞控制台，防止程序退出
        //        Console.ReadKey();

        //        // 程序退出时，关闭全局调度器（所有任务停止）
        //        await jobManager.ShutdownGlobalScheduler();
        //        Console.WriteLine("\n===== 任务调度结束 =====");
        //    }
        //}
        #endregion

        #region 命名空间类测试
        ///// <summary>
        ///// 任务：每5秒打印一次
        ///// </summary>
        //public class JobsPrint : IJob
        //{
        //    public async Task Execute(IJobExecutionContext context)
        //    {
        //        await Task.CompletedTask;
        //        // 可选：获取传递的ExtData
        //        var extDataStr = context.JobDetail.JobDataMap.GetString("ExtData");
        //        Console.WriteLine($"【5秒任务】执行成功 - 时间：{DateTime.Now:HH:mm:ss} | 任务ID：{context.JobDetail.Key.Name} | 扩展数据：{extDataStr ?? "无"}");
        //    }
        //}

        //public class Program
        //{
        //   
        //    static async Task Main(string[] args)
        //    {
        //        Console.WriteLine("===== Quartz任务调度开始 =====");
        //        Console.WriteLine("按任意键退出程序...\n");

        //        #region 测试
        //        // 使用动态版本
        //        var dynamicManager = EasyQuartzJobManagerDynamic.Create(
        //            taskId: "Dynamic_Job_001",
        //            taskName: "动态Job测试",
        //            cronExp: "0/5 * * * * ?",  // 每5秒执行一次
        //            jobTypeFullName: "ConsoleApp1.JobsPrint",  // 命名空间.类名, 程序集名
        //            extData: new { DataName = "动态任务数据", DataValue = 300 }
        //        );

        //        var result = await dynamicManager.StartJob();

        //        #endregion

        //        // 阻塞控制台，防止程序退出
        //        Console.ReadKey();

        //        // 程序退出时，关闭全局调度器（所有任务停止）
        //        await dynamicManager.ShutdownGlobalScheduler();
        //        Console.WriteLine("\n===== 任务调度结束 =====");
        //    }
        //}
        #endregion
        #endregion
    }
}
