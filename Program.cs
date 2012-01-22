using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Empty_SQLite_Log4net
{
    static class Program
    {
	public static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            logger.Info("開始");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            logger.Info("終了");

        }
    }
}
