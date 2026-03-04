using FreeSql;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Infrastructure.Method.Static
{
    public class EasyCrud
    {
        private readonly IFreeSql _freesql;
        private readonly string defualtDB_MS = "Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True";
        private readonly string defualtDB_SQLite = "Data Source={0}; Pooling=true;Min Pool Size=100";


        /// <summary>
        /// 设置FreeSql实例
        /// 示例字符串：
        /// Data Source = localhost; User Id = sa; Password= 密码;Initial Catalog = master; TrustServerCertificate=True;
        /// Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True
        /// Data Source = localhost; Port=3306;User ID = root; Password= 密码; Initial Catalog = master;
        /// </summary>
        public EasyCrud(FreeSql.DataType type = DataType.SqlServer, string conn = "", string InitializeSQL = "")
        {
            if (string.IsNullOrEmpty(conn))
            {
                if (type == DataType.SqlServer)
                {
                    conn = defualtDB_MS;
                }
                else if (type == DataType.Sqlite)
                {
                    string db = "base.db";
                    conn = string.Format(defualtDB_SQLite, db);
                    // 检查数据库文件是否存在，如果不存在，则创建一个空的数据库文件
                    if (!File.Exists(db))
                    {
                        // 创建一个空的 SQLite 数据库文件
                        SQLiteConnection.CreateFile(db);
                        SetTips($"数据库文件 '{db}' 不存在，已创建。");
                    }
                    else
                    {
                        SetTips($"数据库文件 '{db}' 已存在。");
                    }
                }
                else
                {
                    throw new Exception("请先设置连接字符串！");
                }
            }
            _freesql = new FreeSqlBuilder()
                              .UseConnectionString(type, conn)//数据库字符串设置
                              .UseNoneCommandParameter(true)//无参化
                              .UseQuoteSqlName(false)//不使用[] ``这些符号
                              .UseMonitorCommand(cmd =>
                              {
                                  SetTips($"【SQL】{cmd.CommandText.Trim()}"); // 打印 SQL
                                  cmd.CommandTimeout = 100000; // 设置命令超时时间
                              })
                              .Build();
            _freesql.Aop.CommandAfter += (s, e) =>
            {
                if (e.Exception != null)
                {
                    //做一些日志记录的操作。
                    SetTips($"【报错】:{e.Exception.Message}\r\n");
                }
                SetTips($"【时间】{DateTime.Now}\n【耗时】{e.ElapsedMilliseconds}ms\r\n");
            };
            if (!string.IsNullOrEmpty(InitializeSQL))//初始化sql
            {
                _freesql.Ado.ExecuteNonQuery(InitializeSQL);
            }
            SetTips("\n---------------------------------\n");
        }

        /// <summary>
        /// 设置输出
        /// </summary>
        private void SetTips(string data)
        {
            Console.WriteLine(data);
            Debug.WriteLine(data);
        }

        /// <summary>
        /// 获取FreeSql实例
        /// </summary>
        public IFreeSql GetFreeSql()
        {
            if (_freesql == null)
            {
                throw new InvalidOperationException("FreeSql instance has not been set.");
            }
            return _freesql;
        }

        #region 同步版
        /// <summary>
        /// 测试是否正常连接
        /// </summary>
        public bool TryConnect()
        {
            var fsql = GetFreeSql();
            return fsql.Ado.ExecuteConnectTest();
        }

        /// <summary>
        /// Ado执行指定sql
        /// (若出现Unsupported command(COM_RESET_CONNECTION)报错，连接字符串加上：Pooling=false;)
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public bool ExecuteNonQuery(string sql)
        {
            var fsql = GetFreeSql();
            return fsql.Ado.ExecuteNonQuery(sql) != 0;
        }

        /// <summary>
        /// Ado查询返回指定实体
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public List<T> AdoQuery<T>(string sql) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Ado.Query<T>(sql);
        }

        /// <summary>
        /// Ado查询返回指定实体
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public System.Data.DataTable AdoQueryDataTable<T>(string sql) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<object>().WithSql(sql).ToDataTable();
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键）
        /// </summary>
        public bool Insert<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Insert(entity).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键）
        /// </summary>
        public bool Insert<T>(List<T> enList) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Insert(enList).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键，忽略指定列）
        /// </summary>
        /*
            .IgnoreColumns(a => a.CreateTime)
            .IgnoreColumns(a => new { a.Title, a.CreateTime })
         */
        public bool InsertAndIgnore<T>(T entity, Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Insert(entity).IgnoreColumns(exp).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键，忽略指定列）
        /// </summary>
        /*
            .IgnoreColumns(a => a.CreateTime)
            .IgnoreColumns(a => new { a.Title, a.CreateTime })
         */
        public bool InsertAndIgnore<T>(List<T> enList, Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Insert(enList).IgnoreColumns(exp).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 更新数据（更新依据：实体特性标注的主键）
        /// </summary>
        public bool Update<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Update<T>().SetSource(entity).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 更新数据（更新依据：实体特性标注的主键）
        /// </summary>
        public bool Update<T>(List<T> enList) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Update<T>().SetSource(enList).ExecuteAffrows() != 0;
        }


        /// <summary>
        /// 删除数据（删除依据：实体特性标注的主键）
        /// </summary>
        public bool Delete<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Delete<T>(entity).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 删除数据（删除依据：实体特性标注的主键）
        /// </summary>
        public bool Delete<T>(List<T> enList) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Delete<T>(enList).ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 删除数据（删除依据：表达式）
        /// </summary>
        public bool DeleteByExp<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            var effCount = fsql.Delete<T>().Where(exp).ExecuteAffrows();
            return effCount != 0;
        }

        /// <summary>
        /// 查询数据（查询依据：表达式）
        /// </summary>
        public List<T> GetList<T>(Expression<Func<T, bool>> exp = null) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<T>().WhereIf(exp != null, exp).ToList();
        }

        /// <summary>
        /// 查询数据（查询依据：表达式）
        /// </summary>
        public System.Data.DataTable GetDataTable<T>(Expression<Func<T, bool>> exp = null) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<object>().WithSql(fsql.Select<T>().WhereIf(exp != null, exp).ToSql()).ToDataTable();
        }

        /// <summary>
        /// 查询分页数据（查询依据：表达式）
        /// </summary>
        public (List<T> List, int TotalCount) GetPageList<T>(EasyPageInput<T> input) where T : class
        {
            if (input.orderBy == null)
            {
                throw new Exception("分页必须排序！");
            }
            var fsql = GetFreeSql();
            var dataList = fsql.Select<T>()
                        .WhereIf(input.where != null, input.where)
                        .OrderBy(input.orderBy)
                        .Page(input.PageIndex, input.PageSize)
                        .Count(out var TotalCount)
                        .ToList();
            return (List: dataList, TotalCount: Convert.ToInt32(TotalCount));
        }

        /// <summary>
        /// 查询第一条数据（查询依据：表达式）
        /// </summary>
        public T FirstOrDefault<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<T>().Where(exp).ToOne();
        }

        /// <summary>
        /// 查询是否存在数据（查询依据：表达式）
        /// </summary>
        public bool Any<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<T>().Any(exp);
        }

        /// <summary>
        /// 查询数据条数（查询依据：表达式）
        /// </summary>
        public int Count<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            var totals = fsql.Select<T>().Where(exp).Count();
            return Convert.ToInt32(totals);
        }

        /// <summary>
        /// 查表返回单列(入参1：字段，入参2：条件)
        /// </summary>
        public List<V> GetSingleList<T, V>(Expression<Func<T, V>> method1, Expression<Func<T, bool>> method2 = null) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<T>().WhereIf(method2 != null, method2).ToList<V>(method1);
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，先删后插）
        /// </summary>
        public bool Save<T>(T entity) where T : class
        {
            Delete(entity);
            return Insert(entity);
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，先删后插）
        /// </summary>
        public bool BatchSave<T>(List<T> enlist) where T : class
        {
            Delete(enlist);
            return Insert(enlist);
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，执行Merge into的sql）
        /// </summary>
        public bool SaveMerge<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.InsertOrUpdate<T>()
                        .SetSource(entity)
                        .ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，执行Merge into的sql）
        /// </summary>
        public bool BatchSaveMerge<T>(List<T> enlist) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.InsertOrUpdate<T>()
                        .SetSource(enlist)
                        .ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 【更新字段+更新值、更新条件】单字段单条件更新
        /// （update set age = 10 where id = 1)
        /// </summary>
        public bool UpdateSetWhere<T>(Expression<Func<T, object>> exp, object value, Expression<Func<T, bool>> expWhere) where T : class
        {
            var fsql = GetFreeSql();
            var res = fsql.Update<T>()
                                .Set(exp, value)
                                .Where(expWhere)
                                .ExecuteAffrows() != 0;
            return res;
        }

        /// <summary>
        /// 【更新字段+更新值、更新条件】多字段单条件更新
        /// （update set age = 10,Name='小明' where id = 1)
        /// </summary>
        public bool UpdateSetMoreWhere<T>(List<(Expression<Func<T, object>> exp, object value)> inputList, Expression<Func<T, bool>> expWhere) where T : class
        {
            var fsql = GetFreeSql();
            var update = fsql.Update<T>();
            inputList.ToList().ForEach(item => update.Set(item.exp, item.value));//构造Set条件
            var res = update.Where(expWhere).ExecuteAffrows() != 0;
            return res;
        }

        /// <summary>
        /// 【更新列表、更新字段、更新主键（可不传）】多字段多条件更新
        /// （update set age = 10,Name='小明' where id = 1 and pid = 10;
        ///     update set age = 11,Name='小红' where id = 2 and pid = 20;)
        /// </summary>
        /// <param name="enList">数据</param>
        /// <param name="cols">字段</param>
        /// <param name="keys">指定更新的键（不传则默认根据IsPrimary特性标注的字段作更新）</param>
        /// <returns></returns>
        public bool UpdateOnlyColumns<T>(List<T> enList, Expression<Func<T, object>> cols, Expression<Func<T, object>>? keys = null) where T : class
        {
            /*
             示例：
             fsql.Update<Student>().SetSource(enList,x=>new { x.id,x.pid })
                                  .UpdateColumns(x => new { x.age ,x.pid})
                                  .ExecuteAffrows() != 0;
             */
            var fsql = GetFreeSql();
            var update = fsql.Update<T>();
            if (keys == null)
            {
                return update.SetSource(enList)
                            .UpdateColumns(cols)
                            .ExecuteAffrows() != 0;
            }
            return update.SetSource(enList, keys)
                        .UpdateColumns(cols)
                        .ExecuteAffrows() != 0;
        }

        /// <summary>
        /// 获取单字段最大值，返回object类型
        /// </summary>
        public object Max<T>(Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<T>().Max(exp);
        }

        /// <summary>
        /// 获取单字段最小值，返回object类型
        /// </summary>
        public object Min<T>(Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return fsql.Select<T>().Min(exp);
        }
        #endregion

        #region 异步版
        /// <summary>
        /// 删除全表数据
        /// </summary>
        public async Task<bool> TruncateTableAsync<T>() where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Delete<T>().Where(x => true).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 测试是否正常连接
        /// </summary>
        public async Task<bool> TryConnectAsync()
        {
            var fsql = GetFreeSql();
            return await fsql.Ado.ExecuteConnectTestAsync();
        }

        /// <summary>
        /// Ado执行指定sql - 异步版
        /// (若出现Unsupported command(COM_RESET_CONNECTION)报错，连接字符串加上：Pooling=false;)
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<bool> ExecuteNonQueryAsync(string sql)
        {
            var fsql = GetFreeSql();
            return await fsql.Ado.ExecuteNonQueryAsync(sql) != 0;
        }

        /// <summary>
        /// Ado查询返回指定实体 - 异步版
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<List<T>> AdoQueryAsync<T>(string sql) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Ado.QueryAsync<T>(sql);
        }

        /// <summary>
        /// Ado查询返回指定实体
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<System.Data.DataTable> AdoQueryDataTableAsync<T>(string sql) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<object>().WithSql(sql).ToDataTableAsync();
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键）- 异步版
        /// </summary>
        public async Task<bool> InsertAsync<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Insert(entity).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键）- 异步版
        /// </summary>
        public async Task<bool> InsertAsync<T>(List<T> enList) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Insert(enList).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键，忽略指定列）- 异步版
        /// </summary>
        /*
            .IgnoreColumns(a => a.CreateTime)
            .IgnoreColumns(a => new { a.Title, a.CreateTime })
         */
        public async Task<bool> InsertAndIgnoreAsync<T>(T entity, Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Insert(entity).IgnoreColumns(exp).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 插入新数据（插入依据：实体特性标注的主键，忽略指定列）- 异步版
        /// </summary>
        /*
            .IgnoreColumns(a => a.CreateTime)
            .IgnoreColumns(a => new { a.Title, a.CreateTime })
         */
        public async Task<bool> InsertAndIgnoreAsync<T>(List<T> enList, Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Insert(enList).IgnoreColumns(exp).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 更新数据（更新依据：实体特性标注的主键）- 异步版
        /// </summary>
        public async Task<bool> UpdateAsync<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Update<T>().SetSource(entity).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 更新数据（更新依据：实体特性标注的主键）- 异步版
        /// </summary>
        public async Task<bool> UpdateAsync<T>(List<T> enList) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Update<T>().SetSource(enList).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 删除数据（删除依据：实体特性标注的主键）- 异步版
        /// </summary>
        public async Task<bool> DeleteAsync<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Delete<T>(entity).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 删除数据（删除依据：实体特性标注的主键）- 异步版
        /// </summary>
        public async Task<bool> DeleteAsync<T>(List<T> enList) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Delete<T>(enList).ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 删除数据（删除依据：表达式）- 异步版
        /// </summary>
        public async Task<bool> DeleteByExpAsync<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            var effCount = await fsql.Delete<T>().Where(exp).ExecuteAffrowsAsync();
            return effCount != 0;
        }

        /// <summary>
        /// 查询数据（查询依据：表达式）- 异步版
        /// </summary>
        public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> exp = null) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<T>().WhereIf(exp != null, exp).ToListAsync();
        }

        /// <summary>
        /// 查询数据（查询依据：表达式）
        /// </summary>
        public async Task<System.Data.DataTable> GetDataTableAsync<T>(Expression<Func<T, bool>> exp = null) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<object>().WithSql(fsql.Select<T>().WhereIf(exp != null, exp).ToSql()).ToDataTableAsync();

        }
        /// <summary>
        /// 查询分页数据（查询依据：表达式）- 异步版
        /// </summary>
        public async Task<(List<T> List, int TotalCount)> GetPageListAsync<T>(EasyPageInput<T> input) where T : class
        {
            if (input.orderBy == null)
            {
                throw new Exception("分页必须排序！");
            }
            var fsql = GetFreeSql();
            var dataList = await fsql.Select<T>()
                            .WhereIf(input.where != null, input.where)
                            .OrderBy(input.orderBy)
                            .Page(input.PageIndex, input.PageSize)
                            .Count(out var TotalCount)
                            .ToListAsync();
            return (List: dataList, TotalCount: Convert.ToInt32(TotalCount));
        }

        /// <summary>
        /// 查询第一条数据（查询依据：表达式）- 异步版
        /// </summary>
        public async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<T>().Where(exp).ToOneAsync();
        }

        /// <summary>
        /// 查询是否存在数据（查询依据：表达式）- 异步版
        /// </summary>
        public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<T>().AnyAsync(exp);
        }

        /// <summary>
        /// 查询数据条数（查询依据：表达式）- 异步版
        /// </summary>
        public async Task<int> CountAsync<T>(Expression<Func<T, bool>> exp) where T : class
        {
            var fsql = GetFreeSql();
            var totals = await fsql.Select<T>().Where(exp).CountAsync();
            return Convert.ToInt32(totals);
        }

        /// <summary>
        /// 查表返回单列(入参1：字段，入参2：条件) - 异步版
        /// </summary>
        public async Task<List<V>> GetSingleListAsync<T, V>(Expression<Func<T, V>> method1, Expression<Func<T, bool>> method2 = null) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<T>().WhereIf(method2 != null, method2).ToListAsync(method1);
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，先删后插）- 异步版
        /// </summary>
        public async Task<bool> SaveAsync<T>(T entity) where T : class
        {
            await DeleteAsync(entity);
            return await InsertAsync(entity);
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，先删后插）- 异步版
        /// </summary>
        public async Task<bool> BatchSaveAsync<T>(List<T> enlist) where T : class
        {
            await DeleteAsync(enlist);
            return await InsertAsync(enlist);
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，执行Merge into的sql）- 异步版
        /// </summary>
        public async Task<bool> SaveMergeAsync<T>(T entity) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.InsertOrUpdate<T>()
                            .SetSource(entity)
                            .ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 保存实体（保存依据：根据实体特性配置的主键，执行Merge into的sql）- 异步版
        /// </summary>
        public async Task<bool> BatchSaveMergeAsync<T>(List<T> enlist) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.InsertOrUpdate<T>()
                            .SetSource(enlist)
                            .ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 【更新字段+更新值、更新条件】单字段单条件更新
        /// （update set age = 10 where id = 1)
        /// </summary>
        public async Task<bool> UpdateSetWhereAsync<T>(Expression<Func<T, object>> exp, object value, Expression<Func<T, bool>> expWhere) where T : class
        {
            var fsql = GetFreeSql();
            var res = await fsql.Update<T>()
                                .Set(exp, value)
                                .Where(expWhere)
                                .ExecuteAffrowsAsync() != 0;
            return res;
        }


        /// <summary>
        /// 【更新字段+更新值、更新条件】多字段单条件更新
        /// （update set age = 10,Name='小明' where id = 1)
        /// </summary>
        public async Task<bool> UpdateSetMoreWhereAsync<T>(List<(Expression<Func<T, object>> exp, object value)> inputList, Expression<Func<T, bool>> expWhere) where T : class
        {
            var fsql = GetFreeSql();
            var update = fsql.Update<T>();
            inputList.ToList().ForEach(item => update.Set(item.exp, item.value)); // 构造Set条件
            var res = await update.Where(expWhere).ExecuteAffrowsAsync() != 0;
            return res;
        }

        /// <summary>
        /// 【更新列表、更新字段、更新主键（可不传）】多字段多条件更新
        /// （update set age = 10,Name='小明' where id = 1 and pid = 10;
        ///     update set age = 11,Name='小红' where id = 2 and pid = 20;)
        /// </summary>
        /// <param name="enList">数据</param>
        /// <param name="cols">字段</param>
        /// <param name="keys">指定更新的键（不传则默认根据IsPrimary特性标注的字段作更新）</param>
        /// <returns></returns>
        public async Task<bool> UpdateOnlyColumnsAsync<T>(List<T> enList, Expression<Func<T, object>> cols, Expression<Func<T, object>>? keys = null) where T : class
        {
            /*
             示例：
             fsql.Update<Student>().SetSource(enList,x=>new { x.id,x.pid })
                                  .UpdateColumns(x => new { x.age ,x.pid})
                                  .ExecuteAffrows() != 0;
             */
            var fsql = GetFreeSql();
            var update = fsql.Update<T>();
            if (keys == null)
            {
                return await update.SetSource(enList)
                            .UpdateColumns(cols)
                            .ExecuteAffrowsAsync() != 0;
            }
            return await update.SetSource(enList, keys)
                                .UpdateColumns(cols)
                                .ExecuteAffrowsAsync() != 0;
        }

        /// <summary>
        /// 获取单字段最大值，返回object类型 - 异步版
        /// </summary>
        public async Task<object> MaxAsync<T>(Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<T>().MaxAsync(exp);
        }

        /// <summary>
        /// 获取单字段最小值，返回object类型 - 异步版
        /// </summary>
        public async Task<object> MinAsync<T>(Expression<Func<T, object>> exp) where T : class
        {
            var fsql = GetFreeSql();
            return await fsql.Select<T>().MinAsync(exp);
        }
        #endregion
    }


    public class EasyPageInput<T> where T : class
    {
        /// <summary>
        /// 分页查询条件
        /// </summary>
        public Expression<Func<T, bool>> where { get; set; } = null;
        /// <summary>
        /// 分页排序条件
        /// </summary>
        public Expression<Func<T, object>> orderBy { get; set; }
        /// <summary>
        /// 第几页
        /// </summary>
        public int PageIndex { get; set; } = 1;
        /// <summary>
        /// 一页多少条
        /// </summary>
        public int PageSize { get; set; } = 100000;

    }
}
