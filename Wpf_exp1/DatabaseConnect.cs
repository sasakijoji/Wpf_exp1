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
    /// <summary>
    /// サーバーへのデータアクセスを提供するクラスです。
    /// </summary>
    public class SqlServerDataAccess
    {
        /// <summary>
        /// 接続文字列を格納するフィールド
        /// </summary>
        private readonly string _connectionString;

        public SqlServerDataAccess()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ClientDataConnection"].ConnectionString;
        }
        /// <summary>
        /// クライアントのデータを取得します。
        /// </summary>
        /// <returns></returns>
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
        public DataTable LockClientRecordById(string clientId)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM baseData WITH (UPDLOCK, ROWLOCK) WHERE client_id=@ClientId";
                // パラメーターを作成
                SqlParameter[] updateParams = new SqlParameter[]
                {
                            new SqlParameter("@ClientId", clientId)
                };
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
        /// ロックを取得し、クライアントのレコードが編集中かどうかを確認し、
        /// かかってなければロックフラグを立てる
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CheckIfClientRecordisLocked(string clientId, string userName)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // セッション所有のロックを取得（タイムアウト0で即時取得or失敗）
                var cmd = new SqlCommand("DECLARE @rv INT;EXEC @rv = sp_getapplock @Resource, 'Exclusive', 'Session', 0", connection);
                cmd.Parameters.AddWithValue("@Resource", "baseData:" + clientId);
                var rvParam = cmd.Parameters.Add("@rv", SqlDbType.Int);
                rvParam.Direction = ParameterDirection.ReturnValue;
                cmd.ExecuteNonQuery();
                int rv = (int)rvParam.Value;
                if (rv < 0)
                {
                    //throw new InvalidOperationException("編集中のためロック取得失敗");
                    return true; // ロックされている場合はtrueを返す
                }
            }
            return false; // ロックされていない場合はfalseを返す
        }
        public bool SetClientRecordLock(string clientId, string userName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    //DECLARE @rv INT;
                    cmd.CommandText = @"
                        EXEC @rv = sp_getapplock
                        @Resource = @res,
                        @LockMode = 'Exclusive',
                        @LockOwner = 'Session',
                        @LockTimeout = 0;

                        IF @rv < 0
                        THROW 51000, 'ロック取得失敗', 1;

                        -- 編集フラグを立てる
                        UPDATE baseData
                        SET IsEditing = 1, EditingBy = @UserName
                        WHERE client_id = @ClientId;

                        SELECT @rv;
";
                    // パラメータをすべて正しく追加
                    cmd.Parameters.AddWithValue("@res", "baseData:" + clientId);
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    cmd.Parameters.AddWithValue("@ClientId", clientId);

                    // 戻り値取得用
                    var rvParam = cmd.Parameters.Add("@rv", SqlDbType.Int);
                    rvParam.Direction = ParameterDirection.Output;

                    // 実行
                    cmd.ExecuteNonQuery();

                    int rv = (int)rvParam.Value;
                    return rv >= 0;
                }
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
            using (var command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }
    }

}
