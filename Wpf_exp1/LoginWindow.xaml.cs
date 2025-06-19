using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wpf_exp1
{
    /// <summary>
    /// LoginWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LoginWindow : Window
    {
        private DataTable DataTable { get; set; } = new DataTable(); // 修正: プロパティを初期化
        private string UserName { get; set; } = string.Empty; // 修正: プロパティを初期化
        private string Password { get; set; } = string.Empty; // 修正: プロパティを初期化

        public LoginWindow()
        {
            InitializeComponent();

            // ユーザーをデータベースに挿入
            //SqlServerDataAccess dataAccess = new SqlServerDataAccess();
            //dataAccess.InsertUser("user2", "ilverde00");
        }
        /// <summary>
        /// ログインボタンをクリックしたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            this.UserName = this.txtbox_UserName.Text.Trim(); // ユーザー名を取得
            this.Password = this.txtbox_PassWord.Text.Trim(); // パスワードを取得
            SqlServerDataAccess dataAccess = new SqlServerDataAccess();
            DataTable = dataAccess.GetUserID(this.UserName, this.Password);
            if (DataTable.Rows.Count <= 0)
            {
                MessageBox.Show("ユーザー名またはパスワードが正しくありません。",
                    "ログイン失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MainWindow mainWindow = new MainWindow(this.UserName);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// プログラムを終了するボタンをクリックしたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
