using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            int rowsAffected = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        // パラメータを追加
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        // SQLコマンドを実行し、影響を受けた行数を取得
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                // SQL関連のエラー処理
                //LogError(ex);
                throw new ApplicationException("データベース操作中にエラーが発生しました。", ex);
            }
            catch (InvalidOperationException ex)
            {
                // 接続状態やコマンドの状態に関連するエラー処理
                //LogError(ex);
                throw new ApplicationException("データベース接続の状態に問題があります。", ex);
            }
            catch (Exception ex)
            {
                // その他の一般的なエラー処理
                //LogError(ex);
                throw new ApplicationException("予期しないエラーが発生しました。", ex);
            }
            return rowsAffected;
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private string userName = "";// 名前
        //private string age = "";//年齢
        //private string address = "";//住所
        // 新規か登録かのモードを定義    
        private const int NEW_MODE = 0; // 新規登録モード
        private const int EDIT_MODE = 1; // 編集モード
        private int editMode = NEW_MODE; // 編集モードのフラグ（0: 新規登録, 1: 編集）
      　//カレントIDを保持する変数
        private int currentId = 0; // 現在のIDを保持する変数

        /// <summary>
        /// 
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.InitializeButtons(); // ボタンの初期化
            this.InitializeTextBoxes(); // テキストボックスの初期化

            ///UIにてファイルの内容を表示
            DisplayData();
            this.ClientDataGrid.SelectionMode = DataGridSelectionMode.Single; // 単一選択モードに設定
     
        }
        /// <summary>
        /// ボタンの初期化
        /// </summary>
        private void InitializeButtons()
        {
            this.btnRegistration.IsEnabled = false;//登録ボタンの無効化
            this.btnCancel.IsEnabled = false;//キャンセルボタンの無効化 
            this.btnEdit.IsEnabled = false; // 編集ボタンの無効化
            this.btnDelete.IsEnabled = false; // 削除ボタンの無効化
        }
        /// <summary>
        /// テキストボックスの初期化
        /// </summary>
        private void InitializeTextBoxes()
        {
            txtbox_Name.IsEnabled = false;
            txtbox_Address.IsEnabled = false;
            txtbox_Age.IsEnabled = false;
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
            }
            else
            {
                //年齢チェック却下
                return false;
            }
        }
        /// <summary>
        /// 登録ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRegistration_Click(object sender, RoutedEventArgs e)
        {
            if (this.editMode == MainWindow.NEW_MODE)
            {
                InsertClientData(); // 新規登録処理を呼び出す
            }
            else {
                EditClientData(); // 編集処理を呼び出す  
            }
        }
        /// <summary>
        /// sqlServerへのデータ新規登録処理
        /// </summary>
        private void InsertClientData()
        {
            this.CheckTextBoxInput(); // テキストボックスの入力内容を検証
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
                //textboxコンストラクタ
                this.txtbox_Name.Text = "";
                this.txtbox_Age.Text = "";
                this.txtbox_Address.Text = "";
                MessageBox.Show("データの登録が完了しました。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);
                this.btnDelete.IsEnabled = true;
                this.btnEdit.IsEnabled = true;
                this.btnNewData.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("データの登録に失敗しました。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("データの挿入に失敗しました。");
            }
        }
        /// <summary>
        /// 削除ボタンクリックイベント
        /// </summary>
        private void DeleteClientData()
        {

            // SqlServerDataAccess クラスのインスタンスを生成
            SqlServerDataAccess dataAccess = new SqlServerDataAccess();

            // 削除する SQL コマンドを定義
            string deleteQuery = "DELETE FROM baseData WHERE client_name = @Name AND client_age = @Age AND client_address = @Address";

            // パラメーターを作成
            SqlParameter[] deleteParams = new SqlParameter[]
            {
             new SqlParameter("@Name", this.txtbox_Name.Text), // 名前
             new SqlParameter("@Age", this.txtbox_Age.Text),   // 年齢
             new SqlParameter("@Address", this.txtbox_Address.Text) // 住所
            };

            // SetData 関数を呼び出して削除を実行
            int affectedRows = dataAccess.SetData(deleteQuery, deleteParams);

            if (affectedRows > 0)
            {
                Console.WriteLine($"{affectedRows} 行が削除されました。");
                // UI にてデータを再表示
                ClientDataGrid.Columns.Clear();
                DisplayData();

                // テキストボックスをクリア
                this.txtbox_Name.Text = "";
                this.txtbox_Age.Text = "";
                this.txtbox_Address.Text = "";

                MessageBox.Show("データの削除が完了しました。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("データの削除に失敗しました。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("データの削除に失敗しました。");
            }
        }
        /// <summary>
        /// sqlServerへのデータ更新処理
        /// </summary>
        private void EditClientData()
        {
            if (this.currentId == 0) // 現在のIDが0の場合は何も更新しない
            {
                MessageBox.Show("更新するデータが選択されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.CheckTextBoxInput(); // テキストボックスの入力内容を検証

            // SqlServerDataAccess クラスのインスタンスを生成
            SqlServerDataAccess dataAccess = new SqlServerDataAccess();

            // 現在のIDを使用して更新するSQLコマンドを定義
            // 更新するSQLコマンドを定義
            string updateQuery = "UPDATE baseData SET client_name = @Name, client_age = @Age, client_address = @Address where client_id = "+ this.currentId ;

            // パラメーターを作成
            SqlParameter[] updateParams = new SqlParameter[]
            {
                new SqlParameter("@Name", this.txtbox_Name.Text), // 名前
                new SqlParameter("@Age", this.txtbox_Age.Text),   // 年齢
                new SqlParameter("@Address", this.txtbox_Address.Text), // 住所
            };

            // SetData 関数を呼び出して更新を実行
            int affectedRows = dataAccess.SetData(updateQuery, updateParams);

            if (affectedRows > 0)
            {
                Console.WriteLine($"{affectedRows} 行が更新されました。");

                // UI にてファイルの内容を表示
                ClientDataGrid.Columns.Clear();
                DisplayData();

                // テキストボックスをクリア
                this.txtbox_Name.Text = "";
                this.txtbox_Age.Text = "";
                this.txtbox_Address.Text = "";
       

                MessageBox.Show("データの更新が完了しました。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);

                // ボタンの有効化
                this.btnDelete.IsEnabled = true;
                this.btnEdit.IsEnabled = true;
                this.btnNewData.IsEnabled = true;
                this.ClientDataGrid.IsEnabled = true; // DataGridの有効化
                this.btnRegistration.IsEnabled = false; // 登録ボタンの無効化
                this.btnCancel.IsEnabled = false; // キャンセルボタンの無効化
            }
            else
            {
                MessageBox.Show("データの更新に失敗しました。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("データの更新に失敗しました。");
            }
        }

        /// <summary>
        /// checkTextBoxInputメソッドは、テキストボックスの入力内容を検証します。
        /// </summary>
        private void CheckTextBoxInput()
        {
            // 名前が未入力であれば処理を抜ける
            if (string.IsNullOrEmpty(this.txtbox_Name.Text))
            {
                MessageBox.Show("名前を入力してください", "注意", MessageBoxButton.OK, MessageBoxImage.Information);
                return; // 名前が未入力であれば処理を抜ける
            }
            //年齢の型チェック
            if (!this.CheckAge(this.txtbox_Age.Text))
            {
                MessageBox.Show("年齢が正しくありません", "注意", MessageBoxButton.OK, MessageBoxImage.Information);
                return;//年齢認証却下でであれば処理を抜ける
            }
            // 住所が未入力であれば処理を抜ける
            if (string.IsNullOrEmpty(this.txtbox_Address.Text))
            {
                MessageBox.Show("住所を入力してください", "注意", MessageBoxButton.OK, MessageBoxImage.Information);
                return; // 住所が未入力であれば処理を抜ける
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            // HACK: FIXME: 型変換処理してないのでエラーが起きる
            if (this.btnNewData.IsEnabled == true) //もし新規ボタンが有効なら
            {
                this.btnEdit.IsEnabled = true; // 編集ボタンの有効化
                this.btnDelete.IsEnabled = true; // 削除ボタンの有効化
            }
            if (ClientDataGrid.SelectedItem != null)
            {
                var selectedRow = (DataRowView)ClientDataGrid.SelectedItem;
                this.txtbox_Name.Text = selectedRow.Row["client_name"].ToString(); // 名前
                this.txtbox_Age.Text = selectedRow.Row["client_age"].ToString(); // 年齢
                this.txtbox_Address.Text = selectedRow.Row["client_address"].ToString(); // 住所
                this.currentId = Convert.ToInt32(selectedRow.Row["client_id"]); // 現在のIDを取得
            }
        }
        /// <summary>
        /// 新規データ登録ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNewData_Click(object sender, RoutedEventArgs e)
        {
            this.editMode = 0; // 新規登録モードに設定
            this.txtbox_Name.Focus(); // 名前のテキストボックスにフォーカスを当てる
            this.txtbox_Name.CaretIndex = this.txtbox_Name.Text.Length; // テキストボックスのカーソルを最後に移動
            this.btnRegistration.IsEnabled = true;//登録ボタンの有効化
            this.btnCancel.IsEnabled = true; // キャンセルボタンの有効化
            this.btnNewData.IsEnabled = false; // 新規データ登録ボタンの無効化
            this.btnEdit.IsEnabled = false; // 編集ボタンの無効化
            this.btnDelete.IsEnabled = false; // 削除ボタンの無効化
            //  textboxの有効化
            txtbox_Name.IsEnabled = true;
            txtbox_Address.IsEnabled = true;
            txtbox_Age.IsEnabled = true;
            //textboxコンストラクタ
            this.txtbox_Name.Text = "";
            this.txtbox_Age.Text = "";
            this.txtbox_Address.Text = "";
            this.ClientDataGrid.SelectedItem = null; // 選択をクリア
            this.ClientDataGrid.IsEnabled = false; // DataGridの無効化
        }
        /// <summary>
        /// 削除ボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteClientData(); // 削除処理を呼び出す
        }
        /// <summary>
        /// キャンセルボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
                this.btnNewData.IsEnabled = true; // 新規データ登録ボタンの有効化
                this.btnRegistration.IsEnabled = false; // 登録ボタンの無効化
                this.btnCancel.IsEnabled = false; // キャンセルボタンの無効化
                this.ClientDataGrid.IsEnabled = true; // DataGridの無効化
                this.InitializeTextBoxes(); // テキストボックスの初期化

        }
        /// <summary>
        /// 編集ボタンのクリックイベント  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
               this.editMode = 1; // 編集モードに設定
               this.txtbox_Name.IsEnabled = true; // 名前のテキストボックスを有効化
               this.txtbox_Age.IsEnabled = true; // 年齢のテキストボックスを有効化
               this.txtbox_Address.IsEnabled = true; // 住所のテキストボックスを有効化
               this.ClientDataGrid.IsEnabled = false; // DataGridの無効化
               this.btnCancel.IsEnabled = true; // キャンセルボタンの有効化
               this.btnRegistration.IsEnabled = true; // 登録ボタンの有効化
               this.btnNewData.IsEnabled = false; // 新規データ登録ボタンの無効化
               this.btnEdit.IsEnabled = false; // 編集ボタンの無効化
               this.btnDelete.IsEnabled = false; // 削除ボタンの無効化
        }
    }
}