using CronAPI.Domain.Enumeration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CronAPI.Domain.Entity.Model;

namespace CronAPI.Domain.Dto
{
    public class PlanRecordDto : PlanRecord
    {
        public string isSucessStr => Issucess ==1 ? "成功" : "失败";
        public string TypeStr => GetType(Type);

        private string GetType(int type)
        {
            var enumType = (EnumTimerType)type;
            switch (enumType)
            {
                case EnumTimerType.Timing:
                    return "定时触发";
                case EnumTimerType.Manual:
                    return "手动触发";
                case EnumTimerType.ErrorRetry:
                    return "错误重试";
                case EnumTimerType.Queue:
                    return "排队触发";
                default:
                    return "未知类型";
            }
        }
    }
}
