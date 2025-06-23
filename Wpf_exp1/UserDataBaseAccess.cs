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
        /// ユーザーをデータベースに挿入します。
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
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
        /// ユーザーの情報の照合
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public DataTable GetUserID(string userName, string password)
        {
            DataTable dataTable = new DataTable();
            byte[] storedPasswordHashBytes = null;
            byte[] storedSalt = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT PasswordHash, Salt FROM Users WHERE UserName = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256).Value = userName;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["PasswordHash"] != DBNull.Value)
                            {
                                storedPasswordHashBytes = (byte[])reader["PasswordHash"];
                            }
                            if (reader["Salt"] != DBNull.Value)
                            {
                                storedSalt = ((Guid)reader["Salt"]).ToByteArray();
                            }
                        }
                    }
                }
            }
            if (storedPasswordHashBytes != null && storedSalt != null)
            {
                byte[] hashedPasswordFromInputBytes = HashPassword(password, storedSalt);

                if (hashedPasswordFromInputBytes.SequenceEqual(storedPasswordHashBytes))
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string finalQuery = "SELECT * FROM Users WHERE UserName = @UserName";

                        using (SqlCommand finalCommand = new SqlCommand(finalQuery, connection))
                        {
                            finalCommand.Parameters.AddWithValue("@UserName", userName);
                            using (SqlDataAdapter adapter = new SqlDataAdapter(finalCommand))
                            {
                                adapter.Fill(dataTable);
                            }
                        }
                    }
                }
            }

            return dataTable;
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
