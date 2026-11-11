using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AvatarNamecard.Exporter.Tests
{
    public class ExpressionPreviewTests
    {
        private GameObject avatar;
        private Mesh mesh;
        private AnimationClip clip;
        private AvatarExpressionPreview preview;
        private ExportExpression expression;
        private static readonly FieldInfo CloneField = typeof(AvatarExpressionPreview).GetField("clone", BindingFlags.Instance | BindingFlags.NonPublic);
        private GameObject Clone => (GameObject)CloneField.GetValue(preview);
        private SkinnedMeshRenderer Face(GameObject root) => root.transform.Find("Face").GetComponent<SkinnedMeshRenderer>();
        private void Set(string name, AnimationCurve curve) => AnimationUtility.SetEditorCurve(clip,
            EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape." + name), curve);

        [SetUp]
        public void SetUp()
        {
            avatar = new GameObject("Avatar");
            var face = new GameObject("Face");
            face.transform.SetParent(avatar.transform);
            mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
            mesh.AddBlendShapeFrame("Eyes", 100, new Vector3[3], new Vector3[3], new Vector3[3]);
            mesh.AddBlendShapeFrame("Mouth", 100, new Vector3[3], new Vector3[3], new Vector3[3]);
            face.AddComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
            Face(avatar).SetBlendShapeWeight(0, 7);
            clip = new AnimationClip { frameRate = 60 };
            Set("Eyes", AnimationCurve.Linear(0, 0, 1, 80));
            Set("Mouth", AnimationCurve.Linear(0, 0, 1, 40));
            expression = new ExportExpression { clip = clip };
            preview = new AvatarExpressionPreview();
        }

        [TearDown]
        public void TearDown()
        {
            preview.Dispose();
            Object.DestroyImmediate(avatar);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void UnchangedAutomaticPoseIsReusedWithoutTouchingSource()
        {
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            var clone = Clone;
            for (var i = 0; i < 100; i++) Assert.That(preview.UpdatePose(avatar, expression), Is.False);
            Assert.That(Clone, Is.SameAs(clone));
            Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(80 * expression.SampleTime).Within(.001));
            Assert.That(Face(avatar).GetBlendShapeWeight(0), Is.EqualTo(7));
        }

        [Test]
        public void ManualChangesAndReturnToAutomaticUpdateTheSameClone()
        {
            preview.UpdatePose(avatar, expression);
            var clone = Clone;
            expression.manualTime = true; expression.time = .25f;
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(20).Within(.001));
            expression.time = .75f;
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(60).Within(.001));
            expression.manualTime = false;
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(80 * expression.SampleTime).Within(.001));
            Assert.That(Clone, Is.SameAs(clone));
        }

        [Test]
        public void CurveEditsRefreshAutomaticTimeAndRemovedCurvesRestoreOriginalWeights()
        {
            preview.UpdatePose(avatar, expression);
            var invalidations = 0;
            preview.Invalidated += () => invalidations++;
            Set("Eyes", null);
            Set("Mouth", AnimationCurve.Linear(0, 0, 2, 100));
            Assert.That(invalidations, Is.GreaterThan(0));
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(7));
            Assert.That(Face(Clone).GetBlendShapeWeight(1), Is.EqualTo(100 * expression.SampleTime / 2).Within(.001));
            Assert.That(preview.UpdatePose(avatar, expression), Is.False);
        }

        [Test]
        public void ClipSwitchRestoresMorphsThatTheNewClipDoesNotAnimate()
        {
            preview.UpdatePose(avatar, expression);
            var next = new AnimationClip();
            try
            {
                AnimationUtility.SetEditorCurve(next, EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Mouth"), AnimationCurve.Constant(0, 1, 25));
                expression.clip = next;
                Assert.That(preview.UpdatePose(avatar, expression), Is.True);
                Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(7));
                Assert.That(Face(Clone).GetBlendShapeWeight(1), Is.EqualTo(25));
            }
            finally { Object.DestroyImmediate(next); }
        }

        [Test]
        public void RefreshAndDisposeReleaseOldClonesAndCurveSubscriptions()
        {
            preview.UpdatePose(avatar, expression);
            var originalClone = Clone;
            Face(avatar).SetBlendShapeWeight(0, 12);
            preview.Invalidate();
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            Assert.That(originalClone == null, Is.True);
            var invalidations = 0;
            preview.Invalidated += () => invalidations++;
            var lastClone = Clone;
            preview.Dispose(); preview.Dispose();
            Assert.That(lastClone == null, Is.True);
            Set("Eyes", AnimationCurve.Constant(0, 1, 30));
            Assert.That(invalidations, Is.Zero);
            Assert.That(preview.UpdatePose(avatar, expression), Is.True);
            Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(30));
        }

        [UnityTest]
        public IEnumerator RepaintsReuseImageAndTimeSizeCurveChangesRefreshIt()
        {
            var window = ScriptableObject.CreateInstance<ExpressionPreviewTestWindow>();
            var renders = 0;
            Camera.CameraCallback onRender = camera => { if (Clone != null && camera.scene == Clone.scene) renders++; };
            Camera.onPreRender += onRender;
            try
            {
                window.draw = rect => preview.Draw(rect, avatar, expression);
                window.Show();
                yield return RepaintFrames(window);
                Assert.That(renders, Is.EqualTo(1), "Unchanged repaint events must reuse the image");
                expression.manualTime = true; expression.time = .25f;
                yield return RepaintFrames(window);
                Assert.That(renders, Is.EqualTo(2));
                Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(20).Within(.001));
                window.previewWidth = 150;
                yield return RepaintFrames(window);
                Assert.That(renders, Is.EqualTo(3), "Resize must render at the new resolution");
                Set("Eyes", AnimationCurve.Constant(0, 1, 35));
                yield return RepaintFrames(window);
                Assert.That(renders, Is.EqualTo(4));
                Assert.That(Face(Clone).GetBlendShapeWeight(0), Is.EqualTo(35));
                var imageField = typeof(AvatarExpressionPreview).GetField("image", BindingFlags.Instance | BindingFlags.NonPublic);
                var image = (RenderTexture)imageField.GetValue(preview);
                image.Release();
                yield return RepaintFrames(window);
                Assert.That(renders, Is.EqualTo(5), "Lost render textures must be regenerated");
                preview.Dispose();
                Assert.That(image == null, Is.True, "Cleanup must release the cached image");
            }
            finally
            {
                Camera.onPreRender -= onRender;
                window.draw = null;
                window.Close();
            }
        }

        private static IEnumerator RepaintFrames(ExpressionPreviewTestWindow window)
        {
            var target = window.repaintCount + 3;
            for (var i = 0; i < 120 && window.repaintCount < target; i++)
            {
                window.Repaint();
                yield return null;
            }
            Assert.That(window.repaintCount, Is.GreaterThanOrEqualTo(target), "Test window did not repaint");
        }
    }

    internal sealed class ExpressionPreviewTestWindow : EditorWindow
    {
        public Action<Rect> draw;
        public int repaintCount;
        public float previewWidth = 100;
        private void OnGUI()
        {
            draw?.Invoke(new Rect(0, 0, previewWidth, 100));
            if (Event.current.type == EventType.Repaint) repaintCount++;
        }
    }
}
