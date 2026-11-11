using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarNamecard.Exporter
{
    internal sealed class AvatarExpressionPreview : IDisposable
    {
        private struct MorphCurve
        {
            public SkinnedMeshRenderer renderer;
            public int index;
            public AnimationCurve curve;
        }

        private PreviewRenderUtility preview;
        private GameObject clone;
        private GameObject source;
        private AnimationClip clip;
        private readonly List<MorphCurve> curves = new List<MorphCurve>();
        private float automaticTime;
        private float sampledTime = float.NaN;
        private int clipDirtyCount;
        private bool invalidated;
        // Owned by PreviewRenderUtility; Cleanup releases it along with the preview scene.
        private Texture image;
        private Vector2 imageSize;
        private float imagePixelsPerPoint;
        public event Action Invalidated;

        public void Invalidate()
        {
            invalidated = true;
            Invalidated?.Invoke();
        }

        public void Draw(Rect rect, GameObject avatar, ExportExpression expression)
        {
            // Layout, mouse movement and keyboard events must not sample or render the avatar.
            if (Event.current.type != EventType.Repaint) return;
            if (avatar == null || expression?.clip == null)
            {
                Dispose();
                return;
            }
            if (rect.width <= 0 || rect.height <= 0) return;
            var poseChanged = UpdatePose(avatar, expression);
            var pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            var textureLost = image is RenderTexture renderTexture && !renderTexture.IsCreated();
            if (poseChanged || image == null || textureLost || imageSize != rect.size || imagePixelsPerPoint != pixelsPerPoint)
            {
                preview.BeginPreview(rect, GUIStyle.none);
                try { preview.camera.Render(); }
                finally { image = preview.EndPreview(); }
                imageSize = rect.size;
                imagePixelsPerPoint = pixelsPerPoint;
            }
            GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit, false);
        }

        internal bool UpdatePose(GameObject avatar, ExportExpression expression)
        {
            if (source != avatar || clip != expression.clip || clone == null || invalidated ||
                clipDirtyCount != EditorUtility.GetDirtyCount(expression.clip))
                CreatePreview(avatar, expression.clip);

            // AutomaticTime scans every curve. Compute it once per clip revision, not per morph/event.
            var time = expression.manualTime ? Mathf.Clamp(expression.time, 0, clip.length) : automaticTime;
            if (sampledTime == time) return false;
            foreach (var morph in curves)
                morph.renderer.SetBlendShapeWeight(morph.index, morph.curve.Evaluate(time));
            sampledTime = time;
            return true;
        }

        private void CreatePreview(GameObject avatar, AnimationClip animation)
        {
            Dispose();
            source = avatar;
            clip = animation;
            preview = new PreviewRenderUtility();
            clone = Object.Instantiate(avatar);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var script in clone.GetComponentsInChildren<MonoBehaviour>(true))
                if (script != null) Object.DestroyImmediate(script);
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            preview.AddSingleGO(clone);
            var humanoid = clone.GetComponent<Animator>();
            var head = humanoid != null && humanoid.isHuman ? humanoid.GetBoneTransform(HumanBodyBones.Head) : null;
            var center = head != null ? head.position + Vector3.up * 0.06f : Vector3.up * 1.4f;
            preview.camera.transform.position = center + Vector3.forward * 0.65f;
            preview.camera.transform.LookAt(center);
            preview.camera.nearClipPlane = 0.01f; preview.camera.farClipPlane = 20;
            preview.camera.fieldOfView = 35;
            preview.lights[0].intensity = 1; preview.lights[0].transform.rotation = Quaternion.Euler(25, 180, 0);
            preview.lights[1].intensity = 0.5f;
            preview.ambientColor = new Color(.5f, .5f, .5f, 1);

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(SkinnedMeshRenderer) || !binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) continue;
                var node = string.IsNullOrEmpty(binding.path) ? clone.transform : clone.transform.Find(binding.path);
                var mesh = node != null ? node.GetComponent<SkinnedMeshRenderer>() : null;
                var index = mesh != null && mesh.sharedMesh != null ? mesh.sharedMesh.GetBlendShapeIndex(binding.propertyName.Substring(11)) : -1;
                var curve = index >= 0 ? AnimationUtility.GetEditorCurve(clip, binding) : null;
                if (curve != null) curves.Add(new MorphCurve { renderer = mesh, index = index, curve = curve });
            }
            automaticTime = AvatarExpressionExport.AutomaticTime(clip);
            clipDirtyCount = EditorUtility.GetDirtyCount(clip);
            AnimationUtility.onCurveWasModified += OnCurveModified;
        }

        private void OnCurveModified(AnimationClip changedClip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType change)
        {
            if (changedClip == clip) Invalidate();
        }

        public void Dispose()
        {
            AnimationUtility.onCurveWasModified -= OnCurveModified;
            image = null;
            preview?.Cleanup(); preview = null;
            if (clone != null) Object.DestroyImmediate(clone);
            clone = null; source = null; clip = null;
            curves.Clear();
            sampledTime = float.NaN;
            invalidated = false;
        }
    }
}
