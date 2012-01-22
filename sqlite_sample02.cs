using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace System.Data.SQLiteManager
{
    /// <summary>
    /// SQLiteデータベースを抽象化する（Sngletonパターンを適用してしている）
    /// </summary>
    class SQLiteAdapter : IDisposable
    {
        #region フィールドとプロパティ

        /// <summary>
        /// SQLの実体
        /// </summary>
        SQLiteConnection connection;

        /// <summary>
        /// クエリの実行
        /// </summary>
        SQLiteCommand command;

        /// <summary>
        /// トランザクション
        /// </summary>
        SQLiteTransaction transaction;

        /// <summary>
        /// 自分自身の実体
        /// </summary>
        static SQLiteAdapter instance;

        /// <summary>
        /// 手動でトランザクションを行う
        /// </summary>
        bool autoTransaction;

        /// <summary>
        /// Singletonのインタンス
        /// </summary>
        /// <exception cref="NullReferenceException">データベースが開かれていないときにスローされる例外</exception>
        public static SQLiteAdapter Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new NullReferenceException("データベースが開かれていません。初期化してから利用してください。");
                }

                return instance;
            }
        }

        /// <summary>
        /// 自動でトランザクションを行う（初期値はtrue）
        /// </summary>
        public bool AutoTransaction
        {
            get { return autoTransaction; }
            set { autoTransaction = value; }
        }

        #endregion

        /// <summary>
        /// privateにすることで、外部からインスタンスを作成させない
        /// </summary>
        /// <param name="dbFilename">接続するSQLiteデータベースのファイル名</param>
        /// <exception cref="FileNotFoundException">指定されたファイルが見つからない時にスローされる</exception>
        /// <exception cref="SQLiteException">SQLiteへの接続が失敗した時にスローされる</exception>
        private SQLiteAdapter(string dbFilename)
        {
            //デフォルトだと自動でトランザクションを行う
            this.autoTransaction = true;

            //if (!System.IO.File.Exists(dbFilename))
            //{
            //    //DBファイルが見つからない時
            //    throw new System.IO.FileNotFoundException("指定されたファイルが見つかりませんでした。");
            //}

            //データベースファイルに接続
            if ((this.connection = new SQLiteConnection("Data Source=" + dbFilename)) == null)
            {
                throw new SQLiteException("DBの接続に失敗しました。");
            }

            //コマンドの作成
            if ((this.command = this.connection.CreateCommand()) == null)
            {
                throw new SQLiteException("SQLの実行ができません。");
            }

            //データベースを開く
            this.connection.Open();
        }

        /// <summary>
        /// ObjectのDbTypeを求める
        /// </summary>
        /// <param name="unknown">DbTypeが不明なObject</param>
        /// <returns>Objectに対応するDbType</returns>
        DbType getDbType(object unknown)
        {
            //DB型を判定
            if (unknown is int)
            {
                //整数
                return DbType.Int32;
            }
            else if (unknown is string)
            {
                //文字列
                return DbType.String;
            }
            else if (unknown is float || unknown is double)
            {
                //実数
                return DbType.Double;
            }
            else
            {
                throw new ArgumentException("渡された引数の型が処理できない形式です");
            }
        }

        /// <summary>
        /// パラメータをセット（プリペアステートメント）
        /// </summary>
        /// <param name="sql">実行するSQL</param>
        /// <param name="arg">パラメータ</param>
        private void setParameter(string sql, object[] arg)
        {
            //SQLセット
            this.command.CommandText = sql;
            this.command.Parameters.Clear();

            for (int i = 0; i < arg.Length; i++)
            {
                //パラメータ作成
                SQLiteParameter parameter = this.command.CreateParameter();
                parameter.DbType = getDbType(arg[i]);
                parameter.Value = arg[i];

                //パラメータを追加
                this.command.Parameters.Add(parameter);
            }

            //パラメータをセット
            this.command.Prepare();
        }

        /// <summary>
        /// 戻りのないSQL（INSERT,DELETE,UPDATEなど）を実行する
        /// </summary>
        /// <param name="sql">実行するSQL（'?'でのprepare statementに対応）</param>
        /// <param name="arg">パラメータ</param>
        /// <exception cref="SQLiteException">SQLの実行時エラーが発生した時</exception>
        /// <example>ExecuteNonQuery("INSERT INTO TEST_TABLE VALUES(?,?,?)", data1, data2, data3);</example>
        public void ExecuteNonQuery(string sql, params object[] arg)
        {
            if (autoTransaction)
            {
                //自動でトランザクションが行われる設定になっていれば

                //トランザクションの開始
                this.transactionStart();
            }

            try
            {
                //パラメータをセット
                setParameter(sql,arg);

                //SQLを実行
                this.command.ExecuteNonQuery();

                if (autoTransaction)
                {
                    //自動でトランザクションが行われる設定になっていれば

                    //トランザクションのコミット
                    this.transactionCommit();
                }
            }
            catch (Exception ex)
            {
                //トランザクションのロールバック
                this.transactionRollBack();

                //例外を投げる
                throw new SQLiteException(ex.ToString());
            }

        }

        /// <summary>
        /// INSERTの実行でオートインクリメントされる場合、自動で割り振られた番号を取得
        /// </summary>
        /// <param name="sql">実行するSQL（'?'でのprepare statementに対応）</param>
        /// <param name="arg">パラメータ</param>
        /// <returns>自動で割り振られた番号</returns>
        /// <exception cref="SQLiteException">SQLの実行時エラーが発生した時</exception>
        /// <exexample>int insertId = ExecuteInsert("INSERT INTO TEST_TABLE VALUES(?,?)", data1, data2);</exexample>
        public int ExecuteInsert(string sql, params object[] arg)
        {
            //まずは、INSERTを実行
            ExecuteNonQuery(sql, arg);

            try
            {
                //AUTOINCREMENTされた値を取得
                int num;
                command.CommandText = "SELECT LAST_INSERT_ROWID()";
                using (SQLiteDataReader sdr = command.ExecuteReader())
                {
                    num = int.Parse(sdr[0].ToString());
                    //ここは、sdr.GetInt32()でなぜか上手く行かないので注意
                }

                //AUTOINCREMENTされた値
                return num;
            }
            catch (Exception ex)
            {
                //例外を投げる
                throw new SQLiteException(ex.ToString());
            }
        }

        /// <summary>
        /// SELECTの実行
        /// </summary>
        /// <param name="sql">実行するSQL（'?'でのprepare statementに対応）</param>
        /// <param name="arg">パラメータ</param>
        /// <returns>得られる結果</returns>
        /// <exception cref="SQLiteException">SQLの実行時エラーが発生した時</exception>
        /// <example>ExecuteReader("SELECT TEST_NAME FROM TEST_TABLE WHERE TEST_ID=?", testId);</example>
        public string[][] ExecuteReader(string sql, params object[] arg)
        {
            try
            {
                //パラメータをセット
                setParameter(sql, arg);

                //DBからSELECT結果を読み込み
                using (SQLiteDataReader sdr = this.command.ExecuteReader())
                {
                    List<string[]> tuples = new List<string[]>();
                    while (sdr.Read())
                    {
                        //SELECT結果を１行ずつ読み込み

                        string[] column = new string[sdr.FieldCount];
                        for (int i = 0; i < sdr.FieldCount; i++)
                        {
                            //SELECT結果を１セルずつ読み込み
                            column[i] = sdr[i].ToString();
                        }

                        //１行追加
                        tuples.Add(column);
                    }

                    //リストを配列に変換して返す
                    return tuples.ToArray();
                }

            }
            catch (Exception ex)
            {
                //例外を投げる
                throw new SQLiteException(ex.ToString());
            }
        }

        /// <summary>
        /// 指定されたテーブルの列名を格納した配列を得る
        /// </summary>
        /// <param name="table">テーブル名</param>
        /// <returns>テーブルが持つ列名</returns>
        public string[] GetRowsName(string table)
        {
            try
            {
                //クエリの実行
                List<string> rows = new List<string>();
                command.CommandText = string.Format("SELECT {0}.* FROM {1}", table, table);
                using (SQLiteDataReader sdr = command.ExecuteReader())
                {
                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        //各列名を取得
                        rows.Add(sdr.GetName(i));
                    }
                }

                //リストを配列に変換して返す
                return rows.ToArray();
            }
            catch (Exception ex)
            {
                //DBが例外を吐いた場合
                throw new SQLiteException(ex.ToString());
            }
        }

        #region 静的なメソッド

        /// <summary>
        /// DBに接続する
        /// </summary>
        /// <param name="dbFilename">接続するSQLiteのDBファイル名</param>
        public static void Connect(string dbFilename)
        {
            instance = new SQLiteAdapter(dbFilename);
        }

        /// <summary>
        /// DBから切断する
        /// </summary>
        public static void Disconnect()
        {
            instance.Dispose();
            instance = null;
        }

        /// <summary>
        /// DBファイルを新規作成する
        /// </summary>
        /// <param name="dbFilename"></param>
        public static void CreateDB(string dbFilename)
        {
            System.IO.File.Create(dbFilename);
        }

        #endregion

        #region トランザクション

        /// <summary>
        /// トランザクションの開始
        /// </summary>
        private void transactionStart()
        {
            //トランザクションの開始
            this.transaction = this.connection.BeginTransaction();
        }

        /// <summary>
        /// トランザクションのコミット
        /// </summary>
        private void transactionCommit()
        {
            //トランザクションのコミット
            this.transaction.Commit();

            //トランザクションを開放
            this.transaction.Dispose();

            this.transaction = null;
        }

        /// <summary>
        /// トランザクションのロールバック
        /// </summary>
        private void transactionRollBack()
        {
            //トランザクションのロールバック
            this.transaction.Rollback();

            //トランザクションの開放
            this.transaction.Dispose();

            this.transaction = null;
        }

        /// <summary>
        /// トランザクションの開始
        /// </summary>
        public void TransactionStart()
        {
            if (autoTransaction)
            {
                throw new SQLiteException("自動でトランザクションを開始するように設定されています。");
            }

            this.transactionStart();
        }

        /// <summary>
        /// トランザクションのコミット
        /// </summary>
        public void TransactionCommit()
        {
            if (autoTransaction)
            {
                throw new SQLiteException("自動でトランザクションを開始するように設定されています。");
            }

            this.transactionCommit();
        }

        #endregion

        #region IDisposable メンバ

        /// <summary>
        /// 資源を解放する
        /// </summary>
        public void Dispose()
        {
            //コマンドを閉じる
            this.command.Dispose();

            //接続を切る
            this.connection.Close();
        }

        #endregion
    }
}

