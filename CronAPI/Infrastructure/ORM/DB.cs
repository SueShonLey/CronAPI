using CronAPI.Domain.Constant;
using CronAPI.Infrastructure.Method.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Infrastructure.ORM
{
    public static class DB
    {
        // 懒加载实例：线程安全，仅第一次访问时初始化
        private static readonly Lazy<EasyCrud> _lazyEasy = new Lazy<EasyCrud>(() =>
            new EasyCrud(FreeSql.DataType.Sqlite, "", Const.initsql));

        // 公共属性：返回懒加载的唯一实例
        public static EasyCrud easy => _lazyEasy.Value;
    }
}
