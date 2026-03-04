using CronAPI.Domain.Dto;
using CronAPI.Domain.Enumeration;
using CronAPI.Infrastructure.Method.Static;
using CronAPI.Infrastructure.ORM;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CronAPI.Domain.Entity.Model;

namespace CronAPI.Applications.Services
{
    /// <summary>
    /// 定时调度
    /// </summary>
    public class TimerTaskService : IJob
    {
        EasyCrud easy = DB.easy;
        public async Task Execute(IJobExecutionContext context)
        {
            var ext = JsonConvert.DeserializeObject<PlanInfo>(context.JobDetail.JobDataMap.GetString("ExtData"));
            var entity = await easy.FirstOrDefaultAsync<PlanInfo>(x => x.ID == ext.ID);
            await easy.UpdateSetWhereAsync<PlanInfo>(x => x.Status, 1, x => x.ID == ext.ID);//更新为正在调用状态
            var result = new HttpClientResult();
            Debug.WriteLine($"执行定时任务:{DateTime.Now}");
            if (entity != null)
            {
                //重试
                var times = entity.RetryMaxTimes;

                //判断需不需要排队
                if (entity.IsQueue == EnumMode.Queue.GetHashCode() && entity.Status == 1) //如果需要排队且状态是调用中
                {
                    //插入队列表
                    var queue = new PlanQueue()
                    {
                        PlanId = entity.ID,
                        CreateTime = DateTime.Now,
                        Remark = $"【任务】{entity.Name}正在调用中，又设置了排队模式，故排队调用。"
                    };
                    await DB.easy.InsertAsync(queue);
                    return;
                }

                //判断需不需要跳过
                if(entity.IsQueue == EnumMode.Skip.GetHashCode() && entity.Status == 1)
                {
                    return;
                }

                for (var i = 0; i <= times; i++)
                {
                    result = await EasyHttp.SendRequest(entity.RequestPath, (EnumHTTPMethod)entity.RequestMethod);
                    if (result.IsSuccess)
                    {
                        var type = EnumTimerType.Timing.GetHashCode();
                        var msg = "定时调用成功";
                        if (i != 0)
                        {
                            type = EnumTimerType.ErrorRetry.GetHashCode();
                            msg = $"错误重试第{i}次调用成功";
                        }
                        //成功调用了接口
                        var insert = new PlanRecord()
                        {
                            Name = entity.Name,
                            PlanId = entity.ID,
                            StartTime = result.StartTime,
                            EndTime = result.EndTime,
                            Msg = msg,
                            Issucess = 1,
                            Result = result.ReturnJson,
                            CostTime = result.SpendTime,
                            Method = ((EnumHTTPMethod)entity.RequestMethod).ToString(),
                            HTTPCode = result.StatusCode,
                            Type = type
                        };
                        await DB.easy.InsertAsync(insert);
                        await CompleteUpdate(context, easy, entity);
                        return;//退出循环
                    }
                    Thread.Sleep(entity.RetryIntervalTime * 1000);//睡眠多少秒
                }
                //来到这里说明重试都失败了
                string tips = times != 0 ? $"定时调用失败，经过{times}次重试后仍然失败!" : "定时调用失败！";
                var inserterr = new PlanRecord()
                {
                    Name = entity.Name,
                    PlanId = entity.ID,
                    StartTime = result.StartTime,
                    EndTime = result.EndTime,
                    Msg = tips + result.Msg,
                    Issucess = 0,
                    Result = result.ReturnJson,
                    CostTime = result.SpendTime,
                    Method = ((EnumHTTPMethod)entity.RequestMethod).ToString(),
                    HTTPCode = result.StatusCode,
                    Type = EnumTimerType.ErrorRetry.GetHashCode()
                };
                await DB.easy.InsertAsync(inserterr);
                await CompleteUpdate(context, easy, entity);
            }
        }

        /// <summary>
        /// 维护计划详细表：已完成任务
        /// </summary>
        /// <param name="context"></param>
        /// <param name="easy"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static async Task CompleteUpdate(IJobExecutionContext context, EasyCrud easy, PlanInfo entity)
        {
            //更新计划状态
            var nextTime = context.Trigger.GetNextFireTimeUtc();
            entity.LatestExecuteTime = DateTime.Now;
            entity.Status = 0;
            entity.NextExecuteTime = nextTime.HasValue ? nextTime.Value.LocalDateTime : DateTime.MinValue;
            await easy.GetFreeSql().Update<PlanInfo>()
                                    .SetSource(entity)
                                    .UpdateColumns(x => new { x.LatestExecuteTime, x.NextExecuteTime, x.Status })
                                    .ExecuteAffrowsAsync();
        }
    }
}
