# UnityYorozuContour

テクスチャの輪郭を抽出するツール

輪郭線追跡アルゴリズムを利用してテクスチャの輪郭を検索する

※ テクスチャによってはうまく輪郭が取れない場合がある

<img src="https://cdn-ak.f.st-hatena.com/images/fotolife/h/hacchi_man/20211130/20211130231346.png" width="200"><img src="https://cdn-ak.f.st-hatena.com/images/fotolife/h/hacchi_man/20211130/20211130231400.png" width="200">

## 使い方

```cs
/// <summary>
/// 指定色で輪郭抽出
/// </summary>
private void TraceConTour(Texture2D src, Color color)
{
    // インスタンス生成
    var contour = new Contour(src);
    
    // 指定の色で輪郭を探索
		if (!contour.Search(color))
		{
  			return;
		}
    
    // 輪郭が取れた場合は輪郭の色を指定してテクスチャを生成
    var texture = contour.GetContourTexture(Color.black);
}
```

```cs
/// <summary>
/// 2値化して輪郭抽出
/// </summary>
private void TraceConTourByBinarization(Texture2D src, float threshold)
{
    // インスタンス生成
    var contour = new Contour(src);
    
    // テクスチャをしきい値指定して2値化
    contour.ToBinarization(threshold);

    // 輪郭を探索
    if (!contour.Search(Color.black))
    {
        return;
    }

    // 2値化 したテクスチャを取得
    var binTexture = contour.GetBinarizationTexture();
    // 輪郭が取れた場合は輪郭の色を指定してテクスチャを生成
    var texture = contour.GetContourTexture(Color.black);
}
```

# ライセンス

本プロジェクトは [MIT License](LICENSE) の下でライセンスされています。  
詳細については、LICENSE ファイルをご覧ください。
