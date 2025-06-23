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
            this.txtbox_PassWord.Visibility = Visibility.Collapsed; // パスワードを非表示

            // ユーザーをデータベースに挿入
            //SqlServerDataAccess dataAccess = new SqlServerDataAccess();
            //dataAccess.InsertUser("user2", "ilverde00");

#if DEBUG
            //            if (true) // デバッグ用のフラグを追加
            //デバッグ用コード
            //MainWindow mainWindow = new MainWindow(this.UserName);
            //mainWindow.Show();
            //this.Close()

#endif
        }
        /// <summary>
        /// ログインボタンをクリックしたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            //チェックボックスにより判断
            if (chkBox_ShowPassword.IsChecked == true)
            {
                this.Password = txtbox_PassWord.Text;
            }
            else 
            {
                this.Password = pssBox.Password;
            }

            this.UserName = this.txtbox_UserName.Text.Trim(); // ユーザー名を取得
            UserDataBaseAccess userDataAccess = new UserDataBaseAccess();
            DataTable = userDataAccess.GetUserID(this.UserName, this.Password);

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
        /// <summary>
        /// チェックボックスをチェックしたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkBox_ShowPassword_Checked(object sender, RoutedEventArgs e)

        {
            this.pssBox.Visibility = Visibility.Collapsed; // パスワードボックスを非表示
            this.Password = this.pssBox.Password; // パスワードを取得
            this.txtbox_PassWord.Visibility = Visibility.Visible; // パスワードを表示
            this.txtbox_PassWord.Text = this.Password; // パスワードをテキストボックスに設定
        }
        /// <summary>
        /// checkboxをチェックを外したときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkBox_ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            this.txtbox_PassWord.Visibility = Visibility.Collapsed; // パスワードを非表示
            this.pssBox.Visibility = Visibility.Visible; // パスワードボックスを表示
            this.pssBox.Focus(); // パスワードボックスにフォーカスを移動
            this.Password = this.txtbox_PassWord.Text.Trim(); // テキストボックスからパスワードを取得
            this.pssBox.Password = this.Password; // パスワードを再設定

        }
    }
}
