using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Domain.Constant
{
    public class Const
    {
        /// <summary>
        /// 初始化SQL
        /// </summary>
        public static readonly string initsql = $@"-- 1. 创建PlanInfo表（不存在则创建）
CREATE TABLE IF NOT EXISTS PlanInfo (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,  -- 主键，自增
    Name TEXT NOT NULL,                    -- 计划名称，非空
    RequestMethod INTEGER  NOT NULL,                    -- 请求方法（GET/POST等）
    RequestPath TEXT  NOT NULL,                      -- 请求路径
    Cron TEXT  NOT NULL,                             -- 定时任务表达式
    LatestExecuteTime TEXT  NOT NULL,                -- 最近执行时间（SQLite无datetime，用TEXT存ISO格式）
    NextExecuteTime TEXT  NOT NULL,                  -- 下次执行时间
    Status INTEGER  NOT NULL,                        -- 状态（0/1等，整型）
    RetryMaxTimes INTEGER  NOT NULL,                 -- 最大重试次数
    RetryIntervalTime INTEGER  NOT NULL,             -- 重试间隔时间（秒）
    IsEnable INTEGER  NOT NULL,                      -- 是否启用（0/1，整型）
    IsQueue INTEGER  NOT NULL,                       -- 调用模式（0/1/2，整型）
    Remark TEXT                            -- 备注
);

-- 2. 创建PlanRecord表（不存在则创建）
CREATE TABLE IF NOT EXISTS PlanRecord (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,  -- 主键，自增
    Name TEXT  NOT NULL,                             -- 记录名称
    PlanId INTEGER  NOT NULL,                        -- 关联PlanInfo的ID
    StartTime TEXT NOT NULL,               -- 开始时间，非空
    EndTime TEXT,                          -- 结束时间
    Msg TEXT,                              -- 执行信息/日志
    Result TEXT,                           -- 执行结果
    CostTime INTEGER  NOT NULL,                      -- 耗时（毫秒/秒，整型）
    Method TEXT,                           -- 请求方法
    Type INT NOT NULL,                           -- 任务类型EnumTimerType
    Issucess INTEGER  NOT NULL,                      -- 是否成功（0/1，整型）
    HTTPCode INTEGER  NOT NULL                       -- HTTP状态码
);

-- 3. 创建ServerPort表（不存在则创建）
CREATE TABLE IF NOT EXISTS ServerPort (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,   -- 主键，自增
    Name TEXT NOT NULL,                     -- 服务器名称，非空
    ServerUrl TEXT NOT NULL,                -- 服务器URL:Server+Port
    Remark TEXT                             -- 备注
);

-- 4.创建PlanQueue表,计划信息队列表（不存在则创建）
CREATE TABLE IF NOT EXISTS PlanQueue (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    PlanId INTEGER NOT NULL,                     
    CreateTime TEXT NOT NULL,                
    Remark TEXT                          
);
";

        public static readonly string dropsql = $@"
DROP TABLE IF EXISTS PlanInfo;
DROP TABLE IF EXISTS PlanRecord;
DROP TABLE IF EXISTS ServerPort;
DROP TABLE IF EXISTS PlanQueue;
";


        public static readonly string rule = @"
1.接口调度分 4 类触发方式：定时触发（Cron 表达式）、手动触发（人工点击）、排队触发（排队模式下，定时触发时接口若处于调用中则计划入队）、错误重试（定时 / 排队触发出错时，按重试次数触发）。

2.排队模式下，接口两次调用间隔不得小于单次调用耗时，否则会导致队列堆积失控；可通过队列管理界面查看堆积情况，调整调用频率。

3.排队模式下，禁用调度计划仍会消费队列中已有的计划，需手动清空队列方可停止。

4.队列每 5 分钟触发一次消费，当前队列未消费完则暂停；队列默认自动清空 30 天前创建的计划。
";
    }
}
