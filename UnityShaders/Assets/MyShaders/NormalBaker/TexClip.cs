using UnityEditor;
using UnityEngine;

namespace TextureUtility
{
    // 名前よろしくない
    // レンダーテクスチャをテクスチャに変換して保存するヘルパークラス
    public class TexClip : MonoBehaviour
    {
        public string fileName = "bakeNormal.png";

        public static Texture2D RT2Tex(RenderTexture rt)
        {
            Texture2D tex = new Texture2D(rt.width, rt.width, TextureFormat.RGBA32, false);
         
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        public static void SaveTexture(Texture2D tex, string fileName)
        {
            if (!fileName.EndsWith(".png"))
            {
                fileName += ".png";
            }
            
            string projectPath = Application.dataPath;
            string fullPath = System.IO.Path.Combine(projectPath, fileName);
            byte[] png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, png);
        }

        public void Bake()
        {
            var tex = RT2Tex(RenderTexture.active);
            SaveTexture(tex, fileName);
        }
}

}
