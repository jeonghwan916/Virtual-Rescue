using UnityEditor;
using UnityEngine;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal sealed class DoorIdReferenceWindow : EditorWindow
    {
        private const string ImagePath =
            "Assets/02_Scripts/Editor/SituationAuthoring/doorIDs.png";

        private Texture2D _image;

        public static void Open()
        {
            DoorIdReferenceWindow window =
                GetWindow<DoorIdReferenceWindow>(true, "Door ID 배치도");
            window.minSize = new Vector2(640f, 320f);
            window.position = new Rect(120f, 120f, 1200f, 560f);
            window.LoadImage();
            window.Show();
        }

        private void OnEnable()
        {
            LoadImage();
        }

        private void OnGUI()
        {
            if (_image == null)
            {
                EditorGUILayout.HelpBox(
                    $"Door ID 배치도 이미지를 찾을 수 없습니다.\n{ImagePath}",
                    MessageType.Error);
                return;
            }

            Rect imageArea = new(
                8f,
                8f,
                position.width - 16f,
                position.height - 16f);
            GUI.DrawTexture(
                imageArea,
                _image,
                ScaleMode.ScaleToFit,
                false);
        }

        private void LoadImage()
        {
            _image = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            Repaint();
        }
    }
}
