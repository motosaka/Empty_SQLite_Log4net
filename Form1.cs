using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Diagnostics;

namespace Empty_SQLite_Log4net
{
    public partial class Form1 : Form
    {
	public static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
		    Program.logger.DebugFormat("button1_Clicked.");
            if (Properties.Settings.Default.DebugFlag != 1) return;

            try
            {
                using (var conn = new SQLiteConnection("Data Source=Test.db"))
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    // ----------------
                    cmd.CommandText = "CREATE TABLE FOO (ID INTEGER PRIMARY KEY, MyValue NVARCHAR(256))";
                    try
                    {
                        cmd.ExecuteNonQuery(); // Create the table, don't expect returned data
                    }
                    catch 
                    {
                        Program.logger.Warn("ÉeÅ[ÉuÉãÇÕä˘Ç…çÏÇÁÇÍÇƒÇ¢Ç‹Ç∑");
                    }

                    cmd.CommandText = "INSERT INTO FOO (MyValue) VALUES('Hello World')";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT * FROM FOO";
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string row = String.Format("ID = {0}, MyValue = {1}", reader[0], reader[1]);
                            //                             Console.WriteLine(row);
                            Program.logger.Debug(row);
                        }
                    }
                    // ----------------
                    conn.Close();
                }

            }
            catch (SQLiteException exp)
            {
                exp.ToString();
            }

        }
    }
}
