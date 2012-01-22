using System;
using System.Data.SQLite;

namespace SQLiteTest
{
    class Program
    {
        static SQLiteConnection _conn = null;
        static void Main(string[] args)
        {
            ConnectionOpen();

            CreateTable();

            InsertRecord();

            SelectRecord();

            ConnectionClose();
        }

        /// <summary>
        /// データベースに接続
        /// </summary>
        private static void ConnectionOpen()
        {
            _conn = new SQLiteConnection();
            _conn.ConnectionString = "Data Source=testdb.db;Version=3;";
            _conn.Open();
        }

        /// <summary>
        /// テーブルの作成
        /// </summary>
        private static void CreateTable()
        {
            SQLiteCommand command = _conn.CreateCommand();
            command.CommandText = "CREATE TABLE Test (id integer primary key AUTOINCREMENT, text varchar(100))";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// レコードを挿入
        /// </summary>
        private static void InsertRecord()
        {
            for (int i = 0; i < 10; i++)
            {
                SQLiteCommand command = _conn.CreateCommand();
                command.CommandText = "INSERT INTO Test (text) VALUES (@1)";
                SQLiteParameter parameter = command.CreateParameter();
                parameter.ParameterName = "@1";
                parameter.Value = "this is " + i.ToString() + " text";
                command.Parameters.Add(parameter);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// レコードを取得
        /// </summary>
        private static void SelectRecord()
        {
            // 全データの取得
            SQLiteCommand command = _conn.CreateCommand();
            command.CommandText = "SELECT * FROM Test";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(string.Format("ID = {0}, Name = {1}",
                    reader.GetInt32(0),
                    reader.GetString(1)
                ));
            }
        }

        /// <summary>
        /// データベース接続を閉じる
        /// </summary>
        private static void ConnectionClose()
        {
            _conn.Close();
        }
    }
}
