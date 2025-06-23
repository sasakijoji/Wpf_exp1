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
        private int currentId = 0; // 現在の行データのIDを保持する変数
        private bool beginEdit = false; // 編集開始フラグ
        private readonly string userName;

        /// <summary>
        /// メイン画面
        /// </summary>
        /// <param name="userName">ログインユーザーの名前</param>
        public MainWindow(string userName)
        {
            this.userName = userName;
            InitializeComponent();
            this.InitializeButtons(); // ボタンの初期化
            this.InitializeTextBoxes(); // テキストボックスの初期化

            ///UIにてファイルの内容を表示
            DisplayDebug();// デバッグ用の表示メソッドを呼び出す
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
        private void DisplayDebug() {
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();

            // GetData メソッドを呼び出し、結果を取得
            DataTable data = dataAccess.GetClientsData();
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
        /// データの画面表示
        /// </summary>
        private void DisplayData()
        {
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();

            // GetData メソッドを呼び出し、結果を取得
            DataTable data = dataAccess.GetClientsData();
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
                        case "IsEditing":
                            column.Caption = "編集中";
                            continue; // "IsEditing" 列は表示しない
                        case "EditingBy":
                            column.Caption = "ユーザー名";
                            continue; // "EditingBy" 列は表示しない

                        case "StartedAt":
                            column.Caption = "時間";
                            continue; // "StartedAt" 列は表示しない

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
            else 
            {
                EditClientData(); // 編集処理を呼び出す  
            }
        }
        /// <summary>
        /// sqlServerへのデータ新規登録処理
        /// </summary>
        private void InsertClientData()
        {
            this.CheckTextBoxInput(); // テキストボックスの入力内容を検証
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();
            if (dataAccess.InsertClientData(this.txtbox_Name.Text.Trim(),
                this.txtbox_Age.Text.Trim(),this.txtbox_Address.Text.Trim()))
            {
                ///UIにてファイルの内容を表示
                ClientDataGrid.Columns.Clear(); 
                DisplayDebug();
                //textboxコンストラクタ
                this.txtbox_Name.Text = "";
                this.txtbox_Age.Text = "";
                this.txtbox_Address.Text = "";
                this.txtbox_Name.IsEnabled = true; // 名前のテキストボックス
                this.txtbox_Age.IsEnabled = true; // 年齢のテキストボックス
                this.txtbox_Address.IsEnabled = true; // 住所のテキストボックス
                this.ClientDataGrid.IsEnabled = true; // DataGridの有効化
                MessageBox.Show("データの登録が完了しました。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);
                this.btnDelete.IsEnabled = true;
                this.btnEdit.IsEnabled = true;
                this.btnNewData.IsEnabled = true;
                this.btnRegistration.IsEnabled = false; // 登録ボタンの無効か
                this.btnCancel.IsEnabled = false; // 編集ボタンの無効か
            }
            else
            {
                MessageBox.Show("データの登録に失敗しました。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// レコードをロックするメソッド
        /// </summary>
        private bool LockClientRecord()
        {
            // SqlServerDataAccess クラスのインスタンスを生成
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();
            if (this.currentId == 0) // 現在のIDが0の場合は何もロックしない
            {
                MessageBox.Show("データが選択されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!dataAccess.SetClientRecordLock(this.currentId.ToString(), this.userName))
            {
                // GetData メソッドを呼び出し、結果を取得
                DataTable data = dataAccess.GetClientsData();
                var match = data.AsEnumerable().FirstOrDefault(r => r.Field<int>("client_id") == this.currentId);
                string byUser = ""; // 編集を試みたユーザー名
                if (match != null)
                {
                    byUser = match.Field<string>("editingby");
                }
                MessageBox.Show("データのロックに失敗しました。編集者は:" + byUser + "です。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true; // ロック成功
        }
        /// <summary>
        /// 削除ボタンクリックイベント
        /// </summary>
        private void DeleteClientData()
        {
            // SqlServerDataAccess クラスのインスタンスを生成
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();
            if(false == this.LockClientRecord()) // レコードをロックできなかった場合は削除しない
            {
                return; // ロックに失敗した場合は処理を中断
            }
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
                DisplayDebug();

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
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();
            if (dataAccess.EditClientData(this.txtbox_Name.Text.Trim(),
                this.txtbox_Age.Text.Trim(),
                this.txtbox_Address.Text.Trim()
                ,this.currentId))
            {
                // テキストボックスをクリア
                //this.txtbox_Name.Text = "";
                //this.txtbox_Age.Text = "";
                //this.txtbox_Address.Text = "";
                this.txtbox_Name.IsEnabled = false; // 名前のテキストボックスを無効化
                this.txtbox_Age.IsEnabled = false; // 年齢のテキストボックスを無効化
                this.txtbox_Address.IsEnabled = false; // 住所のテキストボックスを無効化

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
            dataAccess.ReleaseClientRecordLock(this.currentId.ToString()); // 編集終了時にロックを解除
  
            int rowIndex = ClientDataGrid.Items.IndexOf(ClientDataGrid.SelectedItem); // 選択された行のインデックスを取得  
            //UIの更新                                                                          
            ClientDataGrid.Columns.Clear();
            DisplayDebug();
            //カレントRowを選択状態にする
            if (0 <= rowIndex && rowIndex < ClientDataGrid.Items.Count)
            {
                ClientDataGrid.SelectedIndex = rowIndex;
                ClientDataGrid.ScrollIntoView(ClientDataGrid.Items[rowIndex]);
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
        /// データの選択状態が変わった際に発生するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            
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
            if (this.beginEdit) 
            {
                ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();
                dataAccess.ReleaseClientRecordLock(this.currentId.ToString()); // 編集終了時にロックを解除
            }
                this.btnNewData.IsEnabled = true; // 新規データ登録ボタンの有効化
                this.btnRegistration.IsEnabled = false; // 登録ボタンの無効化
                this.btnCancel.IsEnabled = false; // キャンセルボタンの無効化
                this.ClientDataGrid.IsEnabled = true; // DataGridの無効化
                this.InitializeTextBoxes(); // テキストボックスの初期化
                int rowIndex = ClientDataGrid.Items.IndexOf(ClientDataGrid.SelectedItem); // 選択された行のインデックスを取得  
                //UIの更新                                                                          
                if (0 <= rowIndex && rowIndex < ClientDataGrid.Items.Count)
                {
                    ClientDataGrid.SelectedIndex = rowIndex;
                    ClientDataGrid.ScrollIntoView(ClientDataGrid.Items[rowIndex]);
                }

        }
        /// <summary>
        /// 編集ボタンのクリックイベント  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            ClientDataBaseAccess dataAccess = new ClientDataBaseAccess();
            // 編集モードに入る前に、現在のレコードをロックする
            if (false == this.LockClientRecord()) // レコードをロックできなかった場合は削除しない
            {
                return; // ロックに失敗した場合は処理を中断
            }
            this.editMode =  MainWindow.EDIT_MODE; // 編集モードに設定
            this.beginEdit = true; // 編集開始フラグを立てる
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