using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_exp1
{
    internal class UserDataBaseAccess
    {
        private readonly string _connectionString;
        //private readonly string _databaseName;
        //private readonly string _databasePassword;
        //private readonly string _databaseUserName;

        public UserDataBaseAccess()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ClientDataConnection"].ConnectionString;
        }
        /// <summary>
        /// ユーザーをデータベースに挿入します。(ユーザー登録画面はいまのところないので直に値を引数にいれて使う)
        /// </summary>
        /// <param name="userName">ユーザー名</param>
        /// <param name="password">パスワード</param>
        public void InsertUser(string userName, string password)
        {
            try
            {
                // ソルトを生成
                Guid salt = PasswordUtility.GenerateSalt();

                // パスワードをハッシュ化
                byte[] passwordHash = PasswordUtility.HashPassword(password, salt);

                // 現在の日付と時刻を設定
                DateTime createdAt = DateTime.Now;

                // SQLクエリを構築
                string query = @"
                INSERT INTO [dbo].[Users] ([UserName], [PasswordHash], [CreatedAt], [Salt])
                VALUES (@UserName, @PasswordHash, @CreatedAt, @Salt) ";

                // SQL接続とコマンドを設定
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // パラメータの設定
                        command.Parameters.AddWithValue("@UserName", userName);
                        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        command.Parameters.AddWithValue("@CreatedAt", createdAt);
                        command.Parameters.AddWithValue("@Salt", salt);

                        // SQL接続を開いて実行
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"SQLエラー: {ex.Message}");
                throw; // 呼び出し元で処理する場合は再スロー
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"一般的なエラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ユーザー名とパスワードを照合し、ユーザーIDを取得します。
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public DataTable GetUserID(string userName, string password)
        {
            var dataTable = new DataTable();

            try
            {
                var (storedHash, storedSalt) = GetStoredCredentials(userName);
                if (storedHash == null || storedSalt == null)
                    return dataTable; // ユーザー不存在／不完全なレコード

                if (!ValidatePassword(password, storedHash, storedSalt))
                    return dataTable; // パスワード不一致

                dataTable = GetUserRecord(userName);
                return dataTable;
            }
            catch (SqlException sqlEx)
            {
                // SQL関連の例外ログ
                LogError($"SQL error in GetUserID: {sqlEx.Number} – {sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // その他の例外
                LogError($"Unexpected error in GetUserID: {ex.Message}");
                throw;
            }
        }

        private (byte[] hash, byte[] salt) GetStoredCredentials(string userName)
        {
            const string query = "SELECT PasswordHash, Salt FROM Users WHERE UserName = @UserName";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserName", userName);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return (null, null);

            var hash = reader["PasswordHash"] as byte[];
            byte[] salt = reader["Salt"] is Guid g ? g.ToByteArray() : null;
            return (hash, salt);
        }

        private bool ValidatePassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            var inputHash = HashPassword(password, storedSalt);
            return inputHash.SequenceEqual(storedHash);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private DataTable GetUserRecord(string userName)
        {
            const string query = "SELECT * FROM Users WHERE UserName = @UserName";
            var dataTable = new DataTable();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserName", userName);

            conn.Open();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dataTable);

            return dataTable;
        }
        /// <summary>
        /// エラーログを出力するメソッド
        /// </summary>
        /// <param name="message"></param>
        private void LogError(string message)
        {
            // ログフレームワークに出力する場所（例：Serilog, NLog など）
            Console.Error.WriteLine(message);
        }

        // パスワードをハッシュ化する関数 (byte[]を返すように変更)
        // **重要: 本番環境ではPBKDF2, bcrypt, scrypt, Argon2などを利用してください**
        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256ビットのハッシュ
            }
        }
    }
}
