using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Domain.Entity
{
    public class Model
    {
        [Table("PlanInfo")]
        public class PlanInfo
        {

            [FreeSql.DataAnnotations.Column(IsPrimary = true, IsIdentity = true, Name = "ID")]
            public int ID { get; set; }

            [Column("Name")]
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 【EnumHTTPMethod】0:GET 1:POST
            /// </summary>
            [Column("RequestMethod")]
            public int RequestMethod { get; set; }

            [Column("RequestPath")]
            public string RequestPath { get; set; } = string.Empty;

            [Column("Cron")]
            public string Cron { get; set; } = string.Empty;

            [Column("LatestExecuteTime")]
            public DateTime LatestExecuteTime { get; set; } 

            [Column("NextExecuteTime")]
            public DateTime NextExecuteTime { get; set; } 
            /// <summary>
            /// 0 挂起 1调用中
            /// </summary>
            [Column("Status")]
            public int Status { get; set; }

            [Column("RetryMaxTimes")]
            public int RetryMaxTimes { get; set; }

            [Column("RetryIntervalTime")]
            public int RetryIntervalTime { get; set; }

            [Column("IsEnable")]
            public int IsEnable { get; set; }

            [Column("IsQueue")]
            public int IsQueue { get; set; }

            [Column("Remark")]
            public string Remark { get; set; } = string.Empty;

        }
        [Table("PlanRecord")]
        public class PlanRecord
        {

            [FreeSql.DataAnnotations.Column(IsPrimary = true, IsIdentity = true, Name = "ID")]
            public int ID { get; set; }


            [Column("Name")]
            public string Name { get; set; } = string.Empty;

            [Column("PlanId")]
            public int PlanId { get; set; }

            [Column("StartTime")]
            public DateTime StartTime { get; set; }

            [Column("EndTime")]
            public DateTime EndTime { get; set; }

            [Column("Msg")]
            public string Msg { get; set; } = string.Empty;

            [Column("Result")]
            public string Result { get; set; } = string.Empty;
            /// <summary>
            /// 花费时间（ms）
            /// </summary>
            [Column("CostTime")]
            public int CostTime { get; set; }

            [Column("Method")]
            public string Method { get; set; } = string.Empty;

            [Column("Type")]
            public int Type { get; set; } 

            [Column("Issucess")]
            public int Issucess { get; set; }

            [Column("HTTPCode")]
            public int HTTPCode { get; set; }

        }
        [Table("ServerPort")]
        public class ServerPort
        {

            [FreeSql.DataAnnotations.Column(IsPrimary = true, IsIdentity = true, Name = "Id")]
            public int Id { get; set; }

            [Column("Name")]
            public string Name { get; set; } = string.Empty;

            [Column("ServerUrl")]
            public string ServerUrl { get; set; } = string.Empty;


            [Column("Remark")]
            public string Remark { get; set; } = string.Empty;

        }

        [Table("PlanQueue")]
        public class PlanQueue
        {

            [FreeSql.DataAnnotations.Column(IsPrimary = true, IsIdentity = true, Name = "Id")]
            public int Id { get; set; }

            [Column("PlanId")]
            public int PlanId { get; set; }

            [FreeSql.DataAnnotations.Column(ServerTime = DateTimeKind.Local, CanUpdate = false, Name = "CreateTime")]
            public DateTime CreateTime { get; set; }

            [Column("Remark")]
            public string Remark { get; set; } = string.Empty;

        }

    }
}
