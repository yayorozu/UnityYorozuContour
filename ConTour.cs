using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yorozu
{
	public class ConTour
	{
		private Color[,] _cacheColors;
		private List<List<Vector2Int>> _positionList;

		private readonly int _width;
		private readonly int _height;

		/// <summary>
		/// 左下から反時計回り
		/// </summary>
		private static readonly Vector2Int[] indexes = {
			new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
			new Vector2Int(1, 0),
			new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
			new Vector2Int(-1, 0),
		};

		public ConTour(Texture2D texture)
		{
			_width = texture.width;
			_height = texture.height;
			_positionList = new List<List<Vector2Int>>();
			_cacheColors = new Color[_width, _height];
			for (var x = 0; x < _width; x++)
			{
				for (var y = 0; y < _height; y++)
				{
					_cacheColors[x, y] = texture.GetPixel(x, y);
				}
			}
		}

		private Color GetPixel(int x, int y)
		{
			return _cacheColors[x, y];
		}

		public Texture2D Search(Color findColor, Color conTourColor)
		{
			var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
			for (var x = 0; x < _width; x++)
				for (var y = 0; y < _height; y++)
					texture.SetPixel(x, y, Color.white);

			if (!Search(findColor))
				return null;

			foreach (var list in _positionList)
			{
				foreach (var position in list)
				{
					texture.SetPixel(position.x, position.y, conTourColor);
				}
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
			EditorUtility.DisplayProgressBar("Search Texture", $"{0}/{_height}", 0f);
#endif
			for (var y = _height - 1; y >= 0; y--)
			{
#if UNITY_EDITOR
				EditorUtility.DisplayProgressBar("Search Texture", $"{_height - y}/{_height}", Mathf.Lerp(_height, 0, y));
#endif
				for (var x = _width - 1; x >= 0; x--)
				{
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
						Debug.LogError("Fail Search ConTour.");
#if UNITY_EDITOR
						EditorUtility.ClearProgressBar();
#endif
						return false;
					}

					_positionList.Add(findContours);
					var nextX = group.Min(p => p.x);
					x = Mathf.Min(x, nextX);
				}
			}
#if UNITY_EDITOR
			EditorUtility.ClearProgressBar();
#endif

			return true;
		}

		private bool IsSkip(int x, int y, out int next)
		{
			// 探索済みの範囲が含まれていたら最後まですっ飛ばす
			foreach (var list in _positionList)
			{
				if (!list.Contains(new Vector2Int(x, y)))
				{
					continue;
				}

				next = list
					.Where(p => p.y == y)
					.Min(p => p.x);

				return true;
			}

			next = x;
			return false;
		}

		/// <summary>
		/// 輪郭をトレース
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
				i = 0;

				// 探索終了
				if (target.x == startX && target.y == startY)
					break;
			}

			return positions;
		}
	}
}
