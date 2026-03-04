using CronAPI.Domain.Dto;
using CronAPI.Domain.Enumeration;
using CronAPI.Infrastructure.Method.Static;
using CronAPI.Infrastructure.ORM;
using DocumentFormat.OpenXml.Wordprocessing;
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
    /// 定时回补（5min轮询一次）
    /// </summary>
    public class BackTaskService : IJob
    {
        EasyCrud easy = DB.easy;
        private static bool IsRun = false;
        public async Task Execute(IJobExecutionContext context)
        {
            if (IsRun)
            {
                return;
            }
            try
            {
                IsRun = true;
                var list = await easy.GetFreeSql()
                                    .Select<PlanQueue>()
                                    .OrderBy(x=>x.CreateTime)
                                    .ToListAsync();
                //默认删除 30天前创建的内容
                var deleteList = list.Where(x => x.CreateTime < DateTime.Now.AddDays(-30)).ToList();
                await easy.DeleteAsync(deleteList);

                //逐个回补
                foreach (var item in list)
                {
                    Debug.WriteLine($"执行定时回补任务:{DateTime.Now}");
                    var entity = await easy.FirstOrDefaultAsync<PlanInfo>(x => x.ID == item.PlanId);
                    var result = new HttpClientResult();
                    if (entity != null)
                    {
                        // 是否需要排队
                        if (entity.IsQueue == EnumMode.Queue.GetHashCode() && entity.Status == 1) //如果需要排队且状态是调用中
                        {
                            continue;//什么都不做
                        }

                        //判断需不需要跳过
                        if (entity.IsQueue == EnumMode.Skip.GetHashCode() && entity.Status == 1)//如果需要跳过且状态是调用中
                        {
                            await easy.DeleteAsync(item);//删掉队列
                            continue;
                        }

                        // 调用接口
                        await easy.UpdateSetWhereAsync<PlanInfo>(x => x.Status, 1, x => x.ID == entity.ID);
                        //不需要排队，或者需要排队但不是调用中的状态
                        var times = entity.RetryMaxTimes;
                        bool flag = false;
                        for (var i = 0; i <= times; i++)
                        {
                            flag = false;
                            result = await EasyHttp.SendRequest(entity.RequestPath, (EnumHTTPMethod)entity.RequestMethod);
                            if (result.IsSuccess)
                            {
                                var type = EnumTimerType.Queue.GetHashCode();
                                var msg = "排队调用成功";
                                if (i != 0)
                                {
                                    type = EnumTimerType.ErrorRetry.GetHashCode();
                                    msg = $"错误重试第{i}次后排队调用成功";
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
                                await easy.DeleteAsync(item);//删掉队列
                                flag = true;
                                break;
                            }
                            Thread.Sleep(entity.RetryIntervalTime * 1000);//睡眠多少秒
                        }
                        //来到这里说明重试都失败了
                        if (!flag)
                        {
                            string tips = times != 0 ? $"排队调用失败，经过{times}次重试后仍然失败!" : "排队调用失败！";
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
                                Type = EnumTimerType.Queue.GetHashCode()
                            };
                            await DB.easy.InsertAsync(inserterr);
                            await CompleteUpdate(context, easy, entity);//宣布调用结束
                                                                        // 执行完毕删掉队列
                            await easy.DeleteAsync(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                IsRun = false;
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
                                    .UpdateColumns(x => new { x.LatestExecuteTime, x.NextExecuteTime,x.Status })
                                    .ExecuteAffrowsAsync();
        }
    }
}
