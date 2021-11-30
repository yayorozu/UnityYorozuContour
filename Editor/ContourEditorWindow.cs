using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool
{
	public class ContourEditorWindow : EditorWindow
	{
		[MenuItem("Tools/Contour")]
		private static void ShowWindow()
		{
			var window = GetWindow<ContourEditorWindow>();
			window.titleContent = new GUIContent("Contour Search");
			window.Show();
		}

		[SerializeField]
		private Texture2D _texture2D;
		private Color _contourColor = Color.black;
		private bool _isBinarization;
		private float _threshold = 0.5f;

		private void OnGUI()
		{
			_texture2D = (Texture2D) EditorGUILayout.ObjectField(_texture2D, typeof(Texture2D), false);
			_contourColor = EditorGUILayout.ColorField("Contour Color", _contourColor);
			_isBinarization = EditorGUILayout.ToggleLeft("to Binarization", _isBinarization);
			if (_isBinarization)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					_threshold = EditorGUILayout.Slider("Threshold", _threshold, 0, 1);
				}
			}

			using (new EditorGUI.DisabledScope(_texture2D == null))
			{
				if (GUILayout.Button("Search Contour & Generate Texture"))
				{
					TraceConTour(_texture2D, _contourColor, _threshold);
				}

				if (_isBinarization)
				{
					if (GUILayout.Button("Generate Binarization Texture"))
					{
						SaveBinarizationTexture(_texture2D, _threshold, false);
					}

					if (GUILayout.Button("Search Binarization Contour & Blend Texture"))
					{
						SaveBinarizationTexture(_texture2D, _threshold, true);
					}

				}
			}
		}

		/// <summary>
		/// 輪郭をトレース
		/// </summary>
		private void TraceConTour(Texture2D src, Color color, float threshold)
		{
			var contour = new Contour(src);
			if (_isBinarization)
			{
				contour.ToBinarization(threshold);
			}

			if (!contour.Search(color))
			{
				return;
			}

			var texture = contour.GetContourTexture(Color.black);
			var savePath = EditorUtility.SaveFilePanelInProject("Select Save Path", "Contour", "png", "");
			if (texture != null && !string.IsNullOrEmpty(savePath))
			{
				System.IO.File.WriteAllBytes(savePath, texture.EncodeToPNG());
				AssetDatabase.Refresh();
			}
		}

		private void SaveBinarizationTexture(Texture2D src, float threshold, bool isBlend)
		{
			var contour = new Contour(src);
			contour.ToBinarization(threshold);
			if (isBlend)
			{
				contour.Search(Color.black);
			}

			Texture2D texture;
			texture = isBlend ?
				contour.BlendContourTexture(src, Color.black) :
				contour.GetBinarizationTexture();

			var fileName = isBlend ? "Blend" : "Binarization";
			var savePath = EditorUtility.SaveFilePanelInProject("Select Save Path", fileName, "png", "");
			if (texture != null && !string.IsNullOrEmpty(savePath))
			{
				System.IO.File.WriteAllBytes(savePath, texture.EncodeToPNG());
				AssetDatabase.Refresh();
			}
		}
	}
}
