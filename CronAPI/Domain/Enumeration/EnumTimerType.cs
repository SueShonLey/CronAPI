using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Domain.Enumeration
{
    /// <summary>
    /// 枚举-调用类型(0:定时触发,1:手动触发,2:错误重试,3:排队触发)
    /// </summary>
    [Description("调用类型")]
    public enum EnumTimerType
    {
        /// <summary>
        ///定时触发
        /// </summary>
        [Description("定时触发")]
        Timing = 0,
        /// <summary>
        ///手动触发
        /// </summary>
        [Description("手动触发")]
        Manual = 1,
        /// <summary>
        ///错误重试
        /// </summary>
        [Description("错误重试")]
        ErrorRetry = 2,
        /// <summary>
        /// 排队触发
        /// </summary>
        [Description("排队触发")]
        Queue = 3,
    }

    /// <summary>
    /// 枚举-调用方式(0:GET,1:POST)
    /// </summary>
    [Description("调用方式")]
    public enum EnumHTTPMethod
    {
        /// <summary>
        ///GET
        /// </summary>
        [Description("GET")]
        GET = 0,
        /// <summary>
        ///POST
        /// </summary>
        [Description("POST")]
        POST = 1,
    }


    /// <summary>
    /// 枚举-调用状态(0:挂起,1:运行中)
    /// </summary>
    [Description("调用状态")]
    public enum EnumHTTPStatus
    {
        /// <summary>
        ///未运行
        /// </summary>
        [Description("挂起")]
        NotRunning = 0,
        /// <summary>
        ///运行中
        /// </summary>
        [Description("运行中")]
        Running = 1,
    }

    /// <summary>
    /// 枚举-模式选择(0:并发,1:排队,2:跳过)
    /// </summary>
    [Description("模式选择")]
    public enum EnumMode
    {
        /// <summary>
        ///并发
        /// </summary>
        [Description("并发")]
        Concurrency = 0,
        /// <summary>
        ///排队
        /// </summary>
        [Description("排队")]
        Queue = 1,
        /// <summary>
        ///跳过
        /// </summary>
        [Description("跳过")]
        Skip = 2,
    }

}
