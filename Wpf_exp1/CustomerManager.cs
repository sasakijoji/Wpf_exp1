using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Wpf_exp1
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        public ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
    /// <summary>
    /// BLL (Business Logic Layer) のクラス
    /// </summary>
    internal class CustomerManager
    {
        /// <summary>
        /// 年齢のバリデーションチェック
        /// </summary>
        /// <param name="age"></param>
        /// <returns></returns>
        public static ValidationResult ValidateAge(int age)
        {
            if (age < 0 || age > 120)
                return new ValidationResult(false, "年齢は0〜120の間で指定してください。");
            return new ValidationResult(true);
        }
        ///// <summary>
        ///// 年齢の妥当性を検証します。
        ///// </summary>
        ///// <param name="age"></param>
        ///// <returns></returns>
        //public static bool ValidateAge(int age)
        //{
        //    if (age < 0 || age > 120)
        //    {
        //        MessageBox.Show(
        //        "年齢は 0～120 の範囲で入力してください。",
        //        "入力エラー",
        //        MessageBoxButton.OK,
        //        MessageBoxImage.Warning);
        //        return false;
        //    }
        //    return true;
        //}
          
    }
}
