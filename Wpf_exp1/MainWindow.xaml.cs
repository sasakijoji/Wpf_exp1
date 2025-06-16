using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Wpf_exp1
{
    public class SqlServerDataAccess
    {
        private readonly string _connectionString;

        public SqlServerDataAccess()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ClientDataConnection"].ConnectionString;
        }

        public DataTable GetData()
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM baseData"; 
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            return dataTable;
        }
        /// <summary>
        /// データベースにデータを書き込みます（INSERT, UPDATE, DELETEなどのSQLコマンドを実行）。
        /// </summary>
        /// <param name="commandText">実行するSQLコマンド文字列。</param>
        /// <param name="parameters">SQLコマンドに渡すパラメーターの配列。</param>
        /// <returns>影響を受けた行数。</returns>
        public int SetData(string commandText, params SqlParameter[] parameters)
        {
            //TODO: 例外処理を入れる
            int rowsAffected = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(commandText, connection))
                {
                    // パラメーターを追加
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    // SQLコマンドを実行し、影響を受けた行数を取得
                    rowsAffected = command.ExecuteNonQuery();
                }
            }
            return rowsAffected;
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string userName = "";// 名前
        private string age = "";//年齢
        private string address = "";//住所
        public MainWindow()
        {
            InitializeComponent();
            this.btnRegistration.IsEnabled = false;//登録ボタンの無効化
            txtbox_Name.IsEnabled = false;
            txtbox_Address.IsEnabled = false;
            txtbox_Age.IsEnabled = false;
            ///UIにてファイルの内容を表示
            DisplayData();
        }
        /// <summary>
        /// データの画面表示
        /// </summary>
        private void DisplayData() {
            SqlServerDataAccess dataAccess = new SqlServerDataAccess();

            // GetData メソッドを呼び出し、結果を取得
            DataTable data = dataAccess.GetData();
            Dispatcher.Invoke(() =>
            {
                foreach (DataColumn column in data.Columns)
                {
                    switch (column.ColumnName)
                    {
                        case "client_name":
                            column.Caption = "氏名";
                            break;
                        case "client_age":
                            column.Caption = "年齢";
                            break;
                        case "client_address":
                            column.Caption = "住所";
                            break;
                        case "client_id":
                            column.Caption = "ID";
                            break;

                        // 他の列についても同様に設定
                        default:
                            break;
                    }
                    var newColumn = new DataGridTextColumn();
                    newColumn.Binding = new Binding(column.ColumnName); // データのバインド
                    newColumn.Header = column.Caption; // ここでCaptionを設定

                    // DataGridに列を追加
                    ClientDataGrid.Columns.Add(newColumn);
                }
                // UI 要素の操作
                ClientDataGrid.ItemsSource = data.DefaultView;
            });

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }
        /// <summary>
        /// ファイル名の取得
        /// </summary>
        /// <returns>ファイル名を戻り値として返す</returns>
        private static string GetFileName() 
        {
            //カレントディレクトリを取得
            string currentDirectory = Directory.GetCurrentDirectory();
            //ファイル名生成
            string fileName = currentDirectory + "\\UserDataOriginal.csv";
            return fileName; 
        }

        /// <summary>
        /// 年齢が数値かどうかのチェック
        /// </summary>
        /// <param name="input">年齢テキストボックスから入力された文字列</param>
        /// <returns>false=数値ではない true=数値</returns>
        private bool CheckAge(string input) 
        {
            bool isInteger = int.TryParse(input, out int result);
            if (isInteger)
            {
                //年齢チェック承認
                return true;
                //Console.WriteLine($"'{input}' は小数点なしの整数です。");
            }
            else
            {
                //年齢チェック却下
                MessageBox.Show("年齢が正しくありません", "注意", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
                //Console.WriteLine($"'{input}' は小数点なしの整数ではありません。");
            }
        }
        /// <summary>
        /// 登録ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRegistration_Click(object sender, RoutedEventArgs e)
        {
            this.userName = this.txtbox_Name.Text;
            this.age = this.txtbox_Age.Text;
            //TODO: 名前のNUllチェックも入れる

            //年齢の型チェック
            if (!this.CheckAge(this.age))
            {
                return;//年齢認証却下でであれば処理を抜ける
            }
            this.address = this.txtbox_Address.Text;

            // SqlServerDataAccess クラスのインスタンスを生成
            SqlServerDataAccess dataAccess = new SqlServerDataAccess();

            // 挿入するSQLコマンドを定義
            string insertQuery = "INSERT INTO baseData (client_name, client_age, client_address) VALUES (@Name, @Age, @Address)";
            // パラメーターを作成
            SqlParameter[] insertParams = new SqlParameter[]
            {
                new SqlParameter("@Name", this.txtbox_Name.Text),//名前
                new SqlParameter("@Age", this.txtbox_Age.Text), // 例: 年齢
                new SqlParameter("@Address", this.txtbox_Address.Text) // 住所
            };

            // SetData関数を呼び出して挿入を実行
            int affectedRows = dataAccess.SetData(insertQuery, insertParams);

            if (affectedRows > 0)
            {
                Console.WriteLine($"{affectedRows} 行が挿入されました。");
                ///UIにてファイルの内容を表示
                ClientDataGrid.Columns.Clear(); 
                DisplayData();
            }
            else
            {
                Console.WriteLine("データの挿入に失敗しました。");
            }
            //textboxコンストラクタ
            this.txtbox_Name.Text = "";
            this.txtbox_Age.Text = "";
            this.txtbox_Address.Text = "";
            
        }
        /// <summary>
        /// 編集ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            this.btnRegistration.IsEnabled = true;//登録ボタンの有効化
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //FIXME: 型変換処理してないのでエラーが起きる
            if (ClientDataGrid.SelectedItem != null)
            {
                var selectedRow = (DataRowView)ClientDataGrid.SelectedItem;
                txtbox_Name.Text = selectedRow.Row["client_name"].ToString(); // "ColumnName" を表示したい列名に置き換えてください
            }
        }
    }
}