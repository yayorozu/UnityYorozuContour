using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yorozu
{
	public class Contour
	{
		private readonly Color[,] _cacheColors;
		private readonly List<List<Vector2Int>> _positionList;
		private readonly int _width;
		private readonly int _height;
		private bool _binarization;

		/// <summary>
		/// 輪郭の座標データ
		/// </summary>
		public IEnumerable<List<Vector2Int>> Points => _positionList;

		/// <summary>
		/// 左下から反時計回り
		/// </summary>
		private static readonly Vector2Int[] indexes = {
			new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
			new Vector2Int(1, 0),
			new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
			new Vector2Int(-1, 0),
		};

		public Contour(Texture2D texture)
		{
			_width = texture.width;
			_height = texture.height;
			_positionList = new List<List<Vector2Int>>();
			_cacheColors = new Color[_width, _height];
			var copyTexture = new Texture2D(_width, _height, texture.format, false);

			Graphics.CopyTexture(texture, copyTexture);

			for (var x = 0; x < _width; x++)
			{
				for (var y = 0; y < _height; y++)
				{
					_cacheColors[x, y] = copyTexture.GetPixel(x, y);
				}
			}

			Object.DestroyImmediate(copyTexture);
		}

		private Color GetPixel(int x, int y)
		{
			return _cacheColors[x, y];
		}

		/// <summary>
		/// テクスチャデータを2値化する
		/// </summary>
		public void ToBinarization(float threshold = 0.5f)
		{
			_binarization = true;
			var t = Mathf.Clamp01(threshold);
			for (var x = 0; x < _width; x++)
			{
				for (var y = 0; y < _height; y++)
				{
					var color = GetPixel(x, y);
					var v = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
					var newColor = v > t ? Color.white : Color.black;
					newColor.a = color.a;
					_cacheColors[x, y] = newColor;
				}
			}
		}

		/// <summary>
		/// 2値化したテクスチャを取得
		/// </summary>
		/// <returns></returns>
		public Texture2D GetBinarizationTexture()
		{
			// 2値化してない
			if (!_binarization)
			{
				Debug.LogError("Please Call ToBinarization Method.");
				return null;
			}

			var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
			for (var x = 0; x < _width; x++)
				for (var y = 0; y < _height; y++)
					texture.SetPixel(x, y, GetPixel(x, y));

			texture.Apply();

			return texture;
		}

		/// <summary>
		/// 輪郭を塗ったテクスチャを取得
		/// </summary>
		public Texture2D GetContourTexture(Color contourColor)
		{
			var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
			for (var x = 0; x < _width; x++)
				for (var y = 0; y < _height; y++)
					texture.SetPixel(x, y, Color.white);

			if (_positionList.Count > 0)
			{
				foreach (var list in _positionList)
				{
					foreach (var position in list)
					{
						texture.SetPixel(position.x, position.y, contourColor);
					}
				}
			}
			else
			{
				// 輪郭が見つからなかった
				Debug.Log("Contour not found.");
			}

			texture.Apply();

			return texture;
		}

		/// <summary>
		/// 全部の輪郭を探す
		/// </summary>
		public bool Search(Color findColor)
		{
#if UNITY_EDITOR
			EditorUtility.DisplayCancelableProgressBar("Search Texture", $"{0}/{_height * _width}", 0f);
#endif
			for (var y = _height - 1; y >= 0; y--)
			{
				for (var x = 0; x < _width; x++)
				{
#if UNITY_EDITOR
					if (EditorUtility.DisplayCancelableProgressBar(
						"Search Texture",
						$"{(_height - y - 1) * _width + x}/{_height * _width}",
						((_height - y - 1) * _width + x) / (float)(_height * _width))
					)
					{
						Debug.Log($"{_positionList.Count}");
						EditorUtility.ClearProgressBar();
						return false;
					}
#endif
					var color = GetPixel(x, y);
					if (color != findColor)
						continue;

					if (IsSkip(x, y, out var next))
					{
						x = next;
						continue;
					}

					// 輪郭検索
					var findContours = FindConTour(findColor, x, y);
					// Xを見つけた最大まですすめる
					var group = findContours
						.Where(p => p.y == y);

					// 最後まで探索できなかった
					if (!group.Any())
					{
						// 例外
						Debug.LogError("Fail Search Contour.");
#if UNITY_EDITOR
						EditorUtility.ClearProgressBar();
#endif
						return false;
					}

					_positionList.Add(findContours);
					x = group.Max(p => p.x);
				}
			}
#if UNITY_EDITOR
			EditorUtility.ClearProgressBar();
#endif
			return true;
		}

		/// <summary>
		/// すでに輪郭を調査済みだった場合スキップする
		/// </summary>
		private bool IsSkip(int x, int y, out int next)
		{
			var search = new Vector2Int(x, y);
			// 探索済みの範囲が含まれていたら最後まですっ飛ばす
			foreach (var list in _positionList)
			{
				if (!list.Contains(search))
				{
					continue;
				}

				next = list
					.Where(p => p.y == y)
					.Max(p => p.x);

				return true;
			}

			next = x;
			return false;
		}

		/// <summary>
		/// 輪郭を探す
		/// </summary>
		private List<Vector2Int> FindConTour(Color targetColor, int startX, int startY)
		{
			var tracePosition = new Vector2Int(startX, startY);
			var positions = new List<Vector2Int>();
			var startIndex = 0;

			for (var i = 0; i < indexes.Length; i++)
			{
				var index = (startIndex + i) % indexes.Length;
				var target = tracePosition + indexes[index];

				if (target.x < 0 ||
				    target.y < 0 ||
				    target.x >= _width ||
				    target.y >= _height)
					continue;

				var color = GetPixel(target.x, target.y);
				if (color != targetColor)
					continue;

				// 2つ以上は許容しない
				if (positions.Count(p => p.x == target.x && p.y == target.y) >= 5)
					break;

				tracePosition = target;
				positions.Add(target);
				startIndex = (index + 6) % indexes.Length;
				i = -1;

				// 探索終了
				if (target.x == startX && target.y == startY)
					break;
			}

			return positions;
		}
	}
}
