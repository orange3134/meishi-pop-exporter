using System.Linq;
using AvatarNamecard.AvatarPackage;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarNamecard.Exporter.Tests
{
    public class ThumbnailExportTests
    {
        [Test] public void UnspecifiedThumbnailDoesNotProduceMetadata()
        {
            Assert.That(AvatarThumbnailExport.Encode(null), Is.Null);
        }

        [TestCase(8, 4, 8, 4)]
        [TestCase(1200, 600, 512, 256)]
        [TestCase(600, 1200, 256, 512)]
        public void NonReadableImagePreservesColorAlphaAndAspectRatio(int width, int height, int expectedWidth, int expectedHeight)
        {
            var source = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            var active = RenderTexture.active;
            var srgb = GL.sRGBWrite;
            try
            {
                source.SetPixels(Enumerable.Repeat(new Color(.25f, .5f, .75f, .4f), width * height).ToArray());
                source.Apply(false, true);
                var data = AvatarPackageThumbnail.Decode(AvatarThumbnailExport.Encode(source));
                Assert.That(decoded.LoadImage(data), Is.True);
                Assert.That(decoded.width, Is.EqualTo(expectedWidth));
                Assert.That(decoded.height, Is.EqualTo(expectedHeight));
                var color = decoded.GetPixel(0, 0);
                Assert.That(color.r, Is.EqualTo(.25f).Within(.02f));
                Assert.That(color.g, Is.EqualTo(.5f).Within(.02f));
                Assert.That(color.b, Is.EqualTo(.75f).Within(.02f));
                Assert.That(color.a, Is.EqualTo(.4f).Within(.02f));
                Assert.That(source.isReadable, Is.False);
                Assert.That(RenderTexture.active, Is.SameAs(active));
                Assert.That(GL.sRGBWrite, Is.EqualTo(srgb));
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(decoded); }
        }
    }
}
