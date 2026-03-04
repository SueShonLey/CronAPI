using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Infrastructure.Method.Extension
{
    public static class Ext
    {
        /// <summary>
        /// 一个实体列表映射到另一个实体列表（属性名称相同则映射）
        /// </summary>
        public static List<TTarget> MapToList<TTarget>(this IEnumerable<object> sourceList) where TTarget : new()
        {
            var targetList = new List<TTarget>();

            foreach (var source in sourceList)
            {
                var target = new TTarget();
                var sourceProperties = source.GetType().GetProperties(); // 使用实际对象的类型
                var targetProperties = typeof(TTarget).GetProperties();

                foreach (var sourceProp in sourceProperties)
                {
                    var targetProp = targetProperties.FirstOrDefault(tp => tp.Name == sourceProp.Name && tp.CanWrite);

                    if (targetProp != null && targetProp.PropertyType == sourceProp.PropertyType)
                    {
                        targetProp.SetValue(target, sourceProp.GetValue(source));
                    }
                }

                targetList.Add(target);
            }

            return targetList;
        }

        /// <summary>
        /// 序列化美化
        /// </summary>
        public static string JsonBeautify(this string json)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
        }
    }
}
