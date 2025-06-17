using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Wpf_exp1
{
    public class SqlServerDataAccess
    {
        private readonly string _connectionString;

        public SqlServerDataAccess()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ClientDataConnection"].ConnectionString;
        }

        public DataTable GetClientsData()
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
        /// ユーザー名とパスワードを使用してユーザーIDを取得します。
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public DataTable GetUserID(string userName, string password)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Users WHERE UserName = @UserName AND PasswordHash = @Password"; // ユーザー名とパスワードでフィルタリング
                //string query = "SELECT * FROM Users"; // ユーザー名とパスワードでフィルタリング
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Password", password);
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

}
