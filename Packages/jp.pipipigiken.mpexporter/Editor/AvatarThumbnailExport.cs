using System;
using AvatarNamecard.AvatarPackage;
using UnityEngine;

namespace AvatarNamecard.Exporter
{
    internal static class AvatarThumbnailExport
    {
        public static string Encode(Texture2D source)
        {
            if (source == null) return null;
            var scale = Mathf.Min(1f, (float)AvatarPackageThumbnail.MaxSide / Mathf.Max(source.width, source.height));
            var width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            var previousSrgbWrite = GL.sRGBWrite;
            Texture2D readable = null;
            try
            {
                // GPU copy supports compressed and non-readable imported images without changing their import settings.
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                readable.Apply(false, false);
                var png = readable.EncodeToPNG();
                AvatarPackageThumbnail.ValidatePng(png);
                return Convert.ToBase64String(png);
            }
            finally
            {
                RenderTexture.active = previous;
                GL.sRGBWrite = previousSrgbWrite;
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }
}
