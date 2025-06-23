using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Security.Cryptography; // ハッシュ化のために追加
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;


namespace Wpf_exp1
{
    /// <summary>
    /// サーバーへのデータアクセスを提供するクラスです。
    /// </summary>
    public class ClientDataBaseAccess
    {
        /// <summary>
        /// 接続文字列を格納するフィールド
        /// </summary>
        private readonly string _connectionString;

        public ClientDataBaseAccess()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ClientDataConnection"].ConnectionString;
        }
        /// <summary>
        /// クライアントのデータを取得します。
        /// </summary>
        /// <returns></returns>
        public DataTable GetClientsData()
        {
            var dataTable = new DataTable();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand("SELECT * FROM baseData", connection);
                using var adapter = new SqlDataAdapter(command);

                adapter.Fill(dataTable);
            }
            catch (SqlException sqlEx)
            {
                // DB接続・クエリ実行時のエラー処理
                Console.Error.WriteLine($"SQLエラー (GetClientsData): {sqlEx.Message}");
                // 必要に応じて独自例外や再スローも可能
                throw;
            }
            catch (Exception ex)
            {
                // その他のエラー全般（例えばDataTable生成や環境異常など）
                Console.Error.WriteLine($"予期せぬエラー (GetClientsData): {ex.Message}");
                throw;
            }
            return dataTable;
        }
        /// <summary>
        /// クライアントのレコードをIDでロックします。
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public DataTable LockClientRecordById(string clientId)
        {
            var dataTable = new DataTable();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string query =
                    "SELECT * FROM baseData WITH (UPDLOCK, ROWLOCK) WHERE client_id = @ClientId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ClientId", clientId);

                using var adapter = new SqlDataAdapter(command);
                adapter.Fill(dataTable);
            }
            catch (SqlException sqlEx)
            {
                Console.Error.WriteLine($"[SQLエラー] LockClientRecordById: {sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[予期せぬエラー] LockClientRecordById: {ex.Message}");
                throw;
            }

            return dataTable;
        }

        /// <summary>
        /// ロック取得を試み、既にロックされていれば true を返し、そうでなければロックを確保して false を返します。
        /// </summary>
        public bool CheckIfClientRecordIsLocked(string clientId, string userName)
        {
            const string sql = @"
            DECLARE @rv INT;
            EXEC @rv = sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = 0;
            SELECT @rv;
             ";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 256)
                {
                    Value = "baseData:" + clientId
                });

                var rvParam = cmd.Parameters.Add("@rv", SqlDbType.Int);
                rvParam.Direction = ParameterDirection.ReturnValue;

                cmd.ExecuteNonQuery();

                int rv = (int)rvParam.Value;
                return rv < 0;  // true = ロックされている or 取得失敗
            }
            catch (SqlException sqlEx)
            {
                Console.Error.WriteLine($"[SQLエラー] CheckIfClientRecordIsLocked: Code={sqlEx.Number}, Msg={sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[予期せぬエラー] CheckIfClientRecordIsLocked: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool SetClientRecordLock(string clientId, string userName)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                EXEC @rv = sp_getapplock
                    @Resource = @res,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = 0;

                IF @rv < 0
                    THROW 51000, 'ロック取得失敗', 1;

                UPDATE baseData
                SET IsEditing = 1, EditingBy = @UserName
                WHERE client_id = @ClientId;

                SELECT @rv;
                 ";

                cmd.Parameters.AddWithValue("@res", "baseData:" + clientId);
                cmd.Parameters.AddWithValue("@UserName", userName);
                cmd.Parameters.AddWithValue("@ClientId", clientId);

                var rvParam = cmd.Parameters.Add("@rv", SqlDbType.Int);
                rvParam.Direction = ParameterDirection.Output;

                cmd.ExecuteNonQuery();

                return (int)rvParam.Value >= 0;
            }
            catch (SqlException sqlEx)
            {
                Console.Error.WriteLine($"[SQLエラー] SetClientRecordLock: Code={sqlEx.Number}, Message={sqlEx.Message}");
                return false; 
                throw;  // スタックトレースを保持して再スロー
                
            }
            catch (InvalidOperationException invOpEx)
            {
                Console.Error.WriteLine($"[状態エラー] SetClientRecordLock: {invOpEx.Message}");
                return false;
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[予期せぬエラー] SetClientRecordLock: {ex.Message}");
                return false;
                throw;
            }
        }

        /// <summary>
        /// ロック解放処理を行うメソッドです。
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool ReleaseClientRecordLock(string clientId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            DECLARE @rv INT;

            SELECT @rv = APPLOCK_TEST('public', @res, 'Exclusive', 'Session');

            IF @rv = 0
            BEGIN
                EXEC sp_releaseapplock 
                    @Resource    = @res,
                    @LockOwner   = 'Session',
                    @DbPrincipal= 'public';
            END

            UPDATE baseData
            SET IsEditing = 0, EditingBy = NULL
            WHERE client_id = @ClientId;
";
            cmd.Parameters.AddWithValue("@res", "baseData:" + clientId);
            cmd.Parameters.AddWithValue("@ClientId", clientId);

            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"[{ex.Number}] {ex.Message}");
                return false;
            }
        }

        /// sqlServerへのデータ新規登録処理
        /// </summary>
        public bool InsertClientData(string name, string age, string Address)
        {
            // 挿入するSQLコマンドを定義
            string insertQuery = "INSERT INTO baseData (client_name, client_age, client_address) VALUES (@Name, @Age, @Address)";
            // パラメーターを作成
            SqlParameter[] insertParams = new SqlParameter[]
            {
                new SqlParameter("@Name", name),//名前
                new SqlParameter("@Age", age), // 例: 年齢
                new SqlParameter("@Address", Address) // 住所
            };
            // SetData関数を呼び出して挿入を実行
            int affectedRows = SetData(insertQuery, insertParams);
            if (affectedRows > 0)
            {
                return true;
            }
            else
            {
                return false; // 挿入が失敗した場合はfalseを返す
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="age"></param>
        /// <param name="address"></param>
        /// <param name="currentId"></param>
        /// <returns></returns>
        public bool EditClientData(string name, string age, string address,int currentId) {

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // トランザクションの開始
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 更新するSQLコマンドを定義
                        string updateQuery = @"
                    UPDATE baseData
                    SET client_name = @Name, client_age = @Age, client_address = @Address
                    WHERE client_id = @ClientId";

                        // パラメーターを作成
                        SqlParameter[] updateParams = new SqlParameter[]
                        {
                            new SqlParameter("@Name", name),
                            new SqlParameter("@Age", age),
                            new SqlParameter("@Address", address),
                            new SqlParameter("@ClientId", currentId)
                        };

                        // 更新を実行
                        int affectedRows = SetData(updateQuery, updateParams, connection, transaction);

                        if (affectedRows > 0)
                        {
                            // コミットして変更を確定
                            transaction.Commit();
                            return true;
                        }
                        else
                        {
                            // 影響を受けた行がない場合はロールバック
                            transaction.Rollback();
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                        // エラー発生時はロールバック
                        transaction.Rollback();
                        throw;
                    }
                }
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private int SetData(string query, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
            catch (SqlException sqlEx)
            {
                // SQL エラー（例：制約違反、接続切れなど）
                Console.Error.WriteLine($"[SQLエラー] SetData: {sqlEx.Number} – {sqlEx.Message}");
                throw; // 必要に応じて再スロー
            }
            catch (InvalidOperationException invOpEx)
            {
                // 接続状態不正など、ADO.NET の状態異常エラー
                Console.Error.WriteLine($"[状態エラー] SetData: {invOpEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // その他の予期しない例外
                Console.Error.WriteLine($"[予期せぬエラー] SetData: {ex.Message}");
                throw;
            }
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
