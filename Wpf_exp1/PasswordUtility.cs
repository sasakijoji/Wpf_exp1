using System;
using System.Security.Cryptography;

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
