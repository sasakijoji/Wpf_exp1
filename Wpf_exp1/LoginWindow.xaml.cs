using System;
using System.Collections.Generic;
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
        private string UserName { get; set; }
        private string Password { get; set; }
        public LoginWindow()
        {
            InitializeComponent();
            SqlServerDataAccess dataAccess = new SqlServerDataAccess();
            this.UserName = "jojisasaki";
            this.Password = "ilverde00"; //
            dataAccess.GetUserID(this.UserName,this.Password);
        }
        /// <summary>
        /// ログインボタンをクリックしたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            // todo: ログイン処理を実装する
            MainWindow mainWindow = new MainWindow();
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
        //private bool ConfirmUserAccount(string userName, string password)
        //{
        //    // ここでユーザー名とパスワードを確認する処理を実装する
        //    // 例えば、データベースやファイルからの確認など
        //    // 確認が成功した場合はtrueを返し、失敗した場合はfalseを返す
        //    // ここでは仮の実装として常にtrueを返す
        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {
        //        string query = "SELECT PasswordHash FROM Users WHERE UserName = @UserName";
        //        SqlCommand command = new SqlCommand(query, connection);
        //        command.Parameters.AddWithValue("@UserName", "user1");
        //        connection.Open();
        //        var result = command.ExecuteScalar();
        //        if (result != null)
        //        {
        //            string storedHash = result.ToString();
        //            string enteredHash = Convert.ToBase64String(HashPassword("entered_password"));
        //            if (storedHash == enteredHash)
        //            {
        //                // 認証成功
        //            }
        //            else
        //            {
        //                // 認証失敗
        //            }
        //        }
        //        else
        //        {
        //            // ユーザーが存在しない
        //        }
        //    }

        //    return true;
        //}
    }
}
