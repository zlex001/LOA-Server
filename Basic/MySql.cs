using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Basic
{
    public class MySql : Manager
    {
        public interface IId
        {
            string Id { get; }
        }
        public class Data : Basic.Data, IId
        {
            public string Id { get; set; }

        }

        #region Singleton
        private static MySql instance;
        public static MySql Instance { get { if (instance == null) { instance = new MySql(); } return instance; } }
        #endregion

        #region Connection Management
        protected MySqlConnection Connection(string str)
        {
            return new MySqlConnection(str);
        }
        #endregion

        #region Connection Methods
        // 连接数据库
        public bool Connect(string str, string database, string ip, int port, string user, string pw)
        {
            try
            {
                using (var mysql = Connection(str))
                {
                    mysql.Open();
                    return true;
                }
            }
            catch (MySqlException)
            {
                return false;
            }
        }


        #endregion

        #region Data Operations
        // 通用查询表原始数据
        public List<Dictionary<string, object>> Query(string connection, string table)
        {
            using (var mysql = Connection(connection))
            {
                mysql.Open();
                return ExecuteQuery(mysql, $"SELECT * FROM {table}", reader =>
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add(reader.GetName(i), reader.GetValue(i));
                    }
                    return row;
                });
            }
        }
        
        // 加载数据
        public void Load<T>(string str) where T : Element
        {
            var rawData = Query(str, typeof(T).Name);
            foreach (var row in rawData)
            {
                Create<T>(row);
            }
        }

        // 插入数据
        public void Insert(string str, Basic.Data data)
        {
            using (var mysql = Connection(str))
            {
                mysql.Open();
                string tableName = data.GetType().Name;
                var columnNames = data.ToDictionary.Keys.ToList();
                string columns = string.Join(",", columnNames);
                string values = string.Join(",", columnNames.Select(x => "@" + x));
                string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
                ExecuteNonQuery(mysql, query, data);
            }
        }

        // 更新数据
        public void Update(string str, Basic.Data data)
        {
            using (var mysql = Connection(str))
            {
                mysql.Open();
                string table = data.GetType().Name;
                var dict = data.ToDictionary;
                var columns = dict.Keys.ToList();
                string setClause = string.Join(",", columns.Select(x => $"{x}=@{x}"));
                string query = $"UPDATE {table} SET {setClause} WHERE Id=@Id";
                ExecuteNonQuery(mysql, query, data);
            }
        }

        // 保存数据
        public void Save(string str, Data data)
        {
            using (var mysql = Connection(str))
            {
                mysql.Open();
                string tableName = data.GetType().Name;
                using (var cmd = new MySqlCommand($"SELECT * FROM {tableName} WHERE Id=@Id", mysql))
                {
                    cmd.Parameters.AddWithValue("@Id", data.ToDictionary.GetValueOrDefault("Id"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        bool exists = reader.Read();
                        if (exists)
                        {
                            reader.Close();
                            Update(str, data);
                        }
                        else
                        {
                            reader.Close();
                            Insert(str, data);
                        }
                    }
                }
            }
        }


        // 保存所有数据
        public void Save<T>(string str) where T : Data
        {
            foreach (T data in Content.Gets<T>())
            {
                Save(str, data);
            }
        }

        // 删除数据
        public void Delete(string str, Basic.Data data)
        {
            using (var mysql = Connection(str))
            {
                mysql.Open();
                string tableName = data.GetType().Name;
                string query = $"DELETE FROM {tableName} WHERE Id=@Id";
                ExecuteNonQuery(mysql, query, data);
            }
        }
        #endregion

        #region Query Execution
        // 执行非查询操作
        private void ExecuteNonQuery(MySqlConnection mysql, string query, Basic.Data data)
        {
            using (var cmd = new MySqlCommand(query, mysql))
            {
                var dict = data.ToDictionary;
                var parameters = dict.Select(p => new MySqlParameter("@" + p.Key, p.Value)).ToArray();
                cmd.Parameters.Clear();
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        // 执行非查询操作（通用版本）
        protected void ExecuteNonQuery(MySqlConnection mysql, string query, params MySqlParameter[] parameters)
        {
            try
            {
                using (var cmd = new MySqlCommand(query, mysql))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException)
            {
            }
        }

        // 执行查询操作并返回结果列表
        protected List<Dictionary<string, object>> ExecuteQuery(MySqlConnection mysql, string query, Func<MySqlDataReader, Dictionary<string, object>> rowMapper)
        {
            var result = new List<Dictionary<string, object>>();
            using (var cmd = new MySqlCommand(query, mysql))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(rowMapper(reader));
                    }
                }
            }
            return result;
        }

        // 执行查询操作并返回单个结果
        protected void ExecuteQuery(MySqlConnection mysql, string query, Action<MySqlDataReader> rowMapper)
        {
            using (var cmd = new MySqlCommand(query, mysql))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rowMapper(reader);
                    }
                }
            }
        }
        #endregion

        #region Data Updates
        // 更新 ID 字段
        public void UpdateFieldById(string str, string table, string id, string field, string value)
        {
            using (var mysql = Connection(str))
            {
                mysql.Open();
                string query = $"UPDATE {table} SET {field} = @Value WHERE Id = @Id";
                using (var cmd = new MySqlCommand(query, mysql))
                {
                    cmd.Parameters.AddWithValue("@Value", value);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion




    }
}

