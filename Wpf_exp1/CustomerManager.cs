using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Wpf_exp1
{
    public class ClientDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
        public int Age { get; init; }
        public string Address { get; init; } = default!;
    }
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        /// <summary>
        /// ValidationResult クラスのコンストラクタ
        /// </summary>
        /// <param name="isValid"></param>
        /// <param name="errorMessage"></param>
        public ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
    /// <summary>
    /// BLL (Business Logic Layer) のクラス
    /// </summary>
    public class CustomerManager
    {
        /// <summary>
        /// 
        /// </summary>
        private int _currentId;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CustomerManager()
        {
            CurrentId = 0;
        }

        /// <summary>
        /// カレントIDを取得する変数
        /// </summary>
        public int CurrentId
        {
            get => _currentId;
            private set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "IDは0以上である必要があります。");
                _currentId = value;
            }
        }

        /// <summary>
        /// SetCurrentId メソッドをインスタンスメソッドに変更
        /// </summary>
        /// <param name="id"></param>
        public void SetCurrentId(int id)
        {
            // 必要ならバリデーション
            CurrentId = id;
        }


        public void SelectClient(int clientId)
        {
            CurrentId = clientId;
            // 選択に関連するビジネスロジックの実行
        }

        public void UpdateClientData(ClientDto dto)
        {
            if (dto.Id != CurrentId)
                throw new InvalidOperationException("更新対象Clientが一致しません。");
            // データ更新処理
        }

        /// <summary>
        /// 年齢の範囲を定義する列挙型
        /// </summary>
        public enum AgeRange
        {
            Minimum = 0, // 最小の年齢
            Maximum = 120 // 最高年齢
        }


        /// <summary>
        /// 年齢のバリデーションチェック
        /// </summary>
        /// <param name="age"></param>
        /// <returns></returns>
        public static ValidationResult ValidateAge(AgeRange age)
        {
            if (age < AgeRange.Minimum || age > AgeRange.Maximum)
            {
                return new ValidationResult(false, "年齢は0〜120の間で指定してください。");
            }
            return new ValidationResult(true);
        }
    }
}
