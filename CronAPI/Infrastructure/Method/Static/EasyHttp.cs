using CronAPI.Domain.Dto;
using CronAPI.Domain.Enumeration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Infrastructure.Method.Static
{
    public class EasyHttp
    {
        private static HttpClient client;

        static EasyHttp()
        {
            // 配置ServicePointManager，支持低版本协议
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            // 初始化HttpClient
            client = new HttpClient();
        }

        public static async Task<HttpClientResult> SendRequest(string url,EnumHTTPMethod method)
        {
            if(method == EnumHTTPMethod.GET)
            {
                return await GetAsync(url);
            }
            else
            {
                return await PostAsync(url,"");
            }
        }

        /// <summary>
        /// GET 请求
        /// </summary>
        public static async Task<HttpClientResult> GetAsync(string url, Dictionary<string, string> headers = null)
        {
            var result = new HttpClientResult();
            result.StartTime = DateTime.Now;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // 开始计时
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                // 添加请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // 发送GET请求
                response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // 如果响应状态码不成功，将抛出异常

                // 读取返回内容并返回
                result.IsSuccess = true;
                result.Msg = "响应成功";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Msg = $"请求错误: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.ReturnJson = await response.Content.ReadAsStringAsync();
                result.StatusCode = (int)response.StatusCode;
                result.EndTime = DateTime.Now;
                result.SpendTime = (int)stopwatch.ElapsedMilliseconds; // 记录花费时间
            }

            return result;
        }

        /// <summary>
        /// POST 请求
        /// </summary>
        public static async Task<HttpClientResult> PostAsync(string url, string jsonData, Dictionary<string, string> headers = null)
        {
            var result = new HttpClientResult();
            result.StartTime = DateTime.Now;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // 开始计时
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                // 设置请求内容为JSON
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // 添加请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // 发送POST请求
                response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode(); // 如果响应状态码不成功，将抛出异常

                // 读取返回内容并返回
                result.IsSuccess = true;
                result.Msg = "响应成功";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Msg = $"请求错误: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.ReturnJson = await response.Content.ReadAsStringAsync();
                result.StatusCode = (int)response.StatusCode;
                result.EndTime = DateTime.Now;
                result.SpendTime = (int)stopwatch.ElapsedMilliseconds; // 记录花费时间
            }

            return result;
        }
    }
}
