using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Domain.Dto
{
    /// <summary>
    /// HTTP请求返回结果
    /// </summary>
    public class HttpClientResult
    {
        /// <summary>
        /// 返回的Json信息
        /// </summary>
        public string ReturnJson { get; set; } = string.Empty;
        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode { get; set; }
        /// <summary>
        /// 是否连接成功
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 输出“响应成功”或者try-catch到的信息
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 花费时间(ms)
        /// </summary>
        public int SpendTime { get; set; }
        /// <summary>
        /// 请求开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 请求结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
    }
}
