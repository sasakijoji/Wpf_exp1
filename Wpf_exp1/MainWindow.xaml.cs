using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
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
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private ControlFile cFile;///ControlFileクラスインスタンス
        private List<string> lines;///個人データ群
        private string userName = "";// 名前
        private string age = "";//年齢
        private string address = "";//住所
        public MainWindow()
        {

            InitializeComponent();
            // SqlServerDataAccess クラスのインスタンスを生成
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

            // ControlFileのコンストラクタ
            this.cFile = new ControlFile();
            lines = new List<string>();
            ///起動したらCSVファイルの読み込みをする
            this.cFile.ReadFile(GetFileName(), lines);
            ///UIにてファイルの内容を表示
            DisplayData();
        }
        /// <summary>
        /// データの画面表示
        /// </summary>
        private void DisplayData() {
            /// ヘッダーデータ文字列
            string identifier = "名前, 年齢, 住所";
            ///データの数の量だけループしデータをリストビューに埋め込む
            for (int i = 0; i < lines.Count; i++)
            {
                if (identifier == lines[i]) /// iがヘッダーだったとき、この反復をスキップして次の反復に進む
                {
                    continue;
                }
                ListViewItem itemName = new ListViewItem();
                ListViewItem itemAge = new ListViewItem();
                ListViewItem itemAddress = new ListViewItem();
                string line = lines[i];///リスト型文字列に変換
                string[] data = line.Split(',');///カンマで区切って文字列を分ける
                itemName.Content = data[0];///名前
                itemAge.Content = data[1];///年齢
                itemAddress.Content = data[2];//住所
                //listView_name.Items.Add(itemName);
                //listView_age.Items.Add(itemAge);
                //listView_address.Items.Add(itemAddress);
            }
        }
        /// <summary>
        /// ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.userName = this.txtbox_Name.Text;
            this.age = this.txtbox_Age.Text;
            //年齢の型チェック
            if (!this.CheckAge(this.age))
            {
                return;//年齢認証却下でであれば処理を抜ける
            }
            this.address = this.txtbox_Address.Text;
            ///ユーザーから入力されたデータをリストに格納
            List<string> newItems = new List<string> { this.userName, this.age, this.address};
            //CSVファイルに書き込み
            string currentFileName = GetFileName();
            cFile.WriteFile(GetFileName(),newItems, lines);
            ///::::::::::::::::::::///
            ///リストビューに表示
            ///::::::::::::::::::::///
            ListViewItem itemName = new ListViewItem();
            ListViewItem itemAge = new ListViewItem();
            ListViewItem itemAddress = new ListViewItem();
            itemName.Content = this.userName;
            itemAge.Content = this.age;
            itemAddress.Content = this.address;
            //listView_name.Items.Add(itemName);
            //listView_age.Items.Add(itemAge);
            //listView_address.Items.Add(itemAddress);
            //textboxコンストラクタ
            this.txtbox_Name.Text = "";
            this.txtbox_Age.Text = "";
            this.txtbox_Address.Text = "";
        }
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

    }
}