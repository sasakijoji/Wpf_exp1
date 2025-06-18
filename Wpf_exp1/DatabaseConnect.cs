using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography; // ハッシュ化のために追加


namespace Wpf_exp1
{
    public class PasswordUtility
    {
        // パスワードとソルトを使ってハッシュ化するメソッド
        public static byte[] HashPassword(string password, Guid salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt.ToByteArray(), 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);  // 256ビットのハッシュ
            }
        }

        // ソルトを生成するメソッド
        public static Guid GenerateSalt()
        {
            return Guid.NewGuid();
        }
    }

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
        /// ユーザーをデータベースに挿入します。
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public void InsertUser(string userName, string password)
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
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);  // ハッシュ化されたパスワードを設定
                    command.Parameters.AddWithValue("@CreatedAt", createdAt);
                    command.Parameters.AddWithValue("@Salt", salt);  // ソルトを設定

                    // SQL接続を開いて実行
                    connection.Open();
                    command.ExecuteNonQuery();
                }
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
