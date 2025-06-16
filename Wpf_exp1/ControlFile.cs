using System;
using System.IO;
using System.Windows;

public class ControlFile

{
  /// <summary>
  /// ファイルの書き込み
  /// </summary>
  /// <param name="filePath">ファイルパス</param>
  /// <param name="newItems">ユーザーから入力された情報</param>
  public void WriteFile(string filePath, List<string> newItems, List<string> listOfLists)
	{
        // リストのリストを定義
        //List<List<string>> listOfLists = new List<List<string>>();
        string identifier = "名前, 年齢, 住所";  //Csvファイルの一行目のヘッダーを追加;
        //リストが空かどうかのチェック
        if (listOfLists.Count != 0)
        {
            if (identifier != listOfLists[0])
            {
                //ヘッダーをリストに追加
                listOfLists.Add(identifier);
            }
        }
        else {
            //ヘッダーをリストに追加
            listOfLists.Add(identifier);
        }
        ///CSVファイルへの書き込み
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                //ユーザーから入力された情報をリストに追加
                listOfLists.Add(newItems[0] + "," + newItems[1] + ","+ newItems[2]);
                //リストの中身をデータ分だけ書き込む
                foreach (var row in listOfLists)
                {
                    // 各行をカンマ区切りで書き込む
                    writer.WriteLine(string.Join(",", row));
                }
            }
            MessageBox.Show("CSV書き込み完了");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラーが発生しました: {ex.Message}");
        }
    }
    /// <summary>
    /// CSVファイルの読み込み
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="lines">データを行ごとにわけて格納するためのリスト</param>
    public void ReadFile(string filePath, List<string> lines) {
        //List<string> lines = new List<string>();
        // ファイルの存在チェック
        if (!File.Exists(filePath))
        {
            return;
        }
        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                // ファイルを1行ずつ読み込み
                while ((line = reader.ReadLine()) != null)
                {
                    // 各行をリストに追加
                    lines.Add(line);
                }
            }
            // リストの内容を表示（例：リストボックスに追加）
            foreach (var line in lines)
            {
                Console.WriteLine(lines);  // またはListBoxなどに表示
                //lists.Add(lines);
            }

           // MessageBox.Show("ファイルの読み込みが完了しました!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エラー: {ex.Message}");
        }
    }
    /// <summary>
    /// ヘッダーチェック用の関数
    /// </summary>
    /// <param name="headerLine"></param>
    /// <returns></returns>
    private bool ChaeckHeader(string headerLine) {
        return  true;
    } 
}
