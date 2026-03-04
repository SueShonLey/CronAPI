using CronAPI.Domain.Enumeration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CronAPI.Domain.Entity.Model;

namespace CronAPI.Domain.Dto
{
    public class PlanQueueInfoDto
    {
        public string PlanName { get; set; }
        public DateTime CreateTime { get; set; }
        public string RequestPath { get; internal set; }
        public int RequestMethod { get; internal set; }
        public int QueueId { get; internal set; }
        public string RequestMethodStr { get; internal set; }
    }

    public  class PlanInfoDto : PlanInfo
    {
        public string RequestMethodStr => RequestMethod == EnumHTTPMethod.GET.GetHashCode() ? "GET" : "POST";
        public string StatusStr => Status == EnumHTTPStatus.Running.GetHashCode() ? "调用中" : "挂起";
        public string IsEnableStr => IsEnable==1 ? "启用" : "禁用";
        public string IsQueueStr => GetDesQueue(IsQueue);

        private string GetDesQueue(int isQueue)
        {
            var enums = (EnumMode)isQueue;
            switch (enums)
            {
                case EnumMode.Concurrency:
                    return "并发模式";
                case EnumMode.Queue:
                    return "排队模式";
                case EnumMode.Skip:
                    return "跳过模式";
                default:
                    return "排队模式";
            }
        }
    }

    public class ExportDto
    {
        public string Name { get; internal set; }
        public string Cron { get; internal set; }
        public string RequestPath { get; internal set; }
        public string RequestMethodStr { get; internal set; }
        public string IsQueueStr { get; internal set; }
        public string IsEnableStr { get; internal set; }
        public string Remark { get; internal set; }

    }
}
