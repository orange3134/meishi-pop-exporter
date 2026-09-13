using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarNamecard.Exporter.Tests
{
    public class ExpressionExportTests
    {
        private GameObject avatar;
        private Mesh mesh;
        private AnimationClip clip;
        [SetUp] public void SetUp()
        {
            avatar = new GameObject("Avatar");
            var face = new GameObject("Face"); face.transform.SetParent(avatar.transform);
            mesh = new Mesh { vertices = new[] { Vector3.zero,Vector3.right,Vector3.up }, triangles = new[] {0,1,2} };
            mesh.AddBlendShapeFrame("Eyes",100,new Vector3[3],new Vector3[3],new Vector3[3]);
            mesh.AddBlendShapeFrame("Mouth",100,new Vector3[3],new Vector3[3],new Vector3[3]);
            face.AddComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
            clip = new AnimationClip { frameRate=60 };
            Set("Eyes",AnimationCurve.Linear(0,0,1,80)); Set("Mouth",AnimationCurve.Linear(0,0,1,40));
        }
        private void Set(string shape, AnimationCurve curve) => AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Face",typeof(SkinnedMeshRenderer),"blendShape."+shape),curve);
        [TearDown] public void TearDown() { Object.DestroyImmediate(avatar); Object.DestroyImmediate(mesh); Object.DestroyImmediate(clip); }
        [Test] public void AutomaticTimeUsesLastFaceKeyAndCombinesMorphs()
        {
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Face",typeof(Transform),"m_LocalPosition.x"),AnimationCurve.Linear(0,0,10,1));
            var request = new ExportExpression { name="Smile",clip=clip };
            Assert.That(request.SampleTime,Is.EqualTo(59f/60).Within(.0001));
            var warnings = new List<string>();
            var result=AvatarExpressionExport.Sample(avatar,clip,request.SampleTime,request.name,warnings);
            Assert.That(result.morphs.Length,Is.EqualTo(2));
            Assert.That(result.morphs[0].weight+result.morphs[1].weight,Is.EqualTo(118).Within(.001));
            Assert.That(warnings.Count,Is.EqualTo(1));
            Assert.That(avatar.transform.Find("Face").GetComponent<SkinnedMeshRenderer>().GetBlendShapeWeight(0),Is.Zero);
        }
        [Test] public void ManualTimeCanSelectAnEarlierFaceAndSwitchBackToAutomatic()
        {
            var request = new ExportExpression { name="Smile",clip=clip,manualTime=true,time=.5f };
            Assert.That(AvatarExpressionExport.Sample(avatar,clip,request.SampleTime,"Smile",new List<string>()).morphs[0].weight,Is.EqualTo(40).Within(.001));
            request.manualTime=false;
            Assert.That(request.SampleTime,Is.GreaterThan(.98f));
        }
        [Test] public void VeryShortClipDoesNotSelectTheStartingNeutralFrame()
        {
            Set("Eyes",AnimationCurve.Linear(0,0,1f/60,100)); Set("Mouth",AnimationCurve.Linear(0,0,1f/60,100));
            var time=AvatarExpressionExport.AutomaticTime(clip);
            Assert.That(AvatarExpressionExport.Sample(avatar,clip,time,"Short",new List<string>()).morphs[0].weight,Is.GreaterThan(90));
        }
        [Test] public void ZeroDurationClipSamplesItsOnlyFrame()
        {
            Set("Eyes",new AnimationCurve(new Keyframe(0,70))); Set("Mouth",new AnimationCurve(new Keyframe(0,30)));
            Assert.That(AvatarExpressionExport.AutomaticTime(clip),Is.Zero);
            Assert.That(AvatarExpressionExport.Sample(avatar,clip,0,"Static",new List<string>()).morphs[0].weight,Is.EqualTo(70));
        }
        [Test] public void MissingMorphAndEmptyClipFailExplicitly()
        {
            Set("Missing",AnimationCurve.Constant(0,1,10));
            Assert.Throws<InvalidOperationException>(()=>AvatarExpressionExport.Sample(avatar,clip,0,"Broken",new List<string>()));
            clip.ClearCurves();
            Assert.Throws<InvalidOperationException>(()=>AvatarExpressionExport.Sample(avatar,clip,0,"Empty",new List<string>()));
        }
        [Test] public void DuplicateNamesFailBeforeTouchingAvatar()
        {
            using var export = new AvatarExpressionExport(new[]{new ExportExpression{name="Smile",clip=clip},new ExportExpression{name=" Smile ",clip=clip}});
            Assert.Throws<InvalidOperationException>(()=>export.Register(avatar));
            Assert.That(avatar.GetComponents<Component>().Length,Is.EqualTo(1));
        }

        private void ConfigureDescriptor()
        {
            var descriptorType = TypeCache.GetTypesDerivedFrom<Component>().First(t => t.Name == "VRCAvatarDescriptor");
            var descriptor = new SerializedObject(avatar.AddComponent(descriptorType));
            var face = avatar.transform.Find("Face");
            var renderer = face.GetComponent<SkinnedMeshRenderer>();
            descriptor.FindProperty("VisemeSkinnedMesh").objectReferenceValue = renderer;
            var visemes = descriptor.FindProperty("VisemeBlendShapes");
            visemes.arraySize = 15;
            for (var i = 0; i < visemes.arraySize; i++) visemes.GetArrayElementAtIndex(i).stringValue = "Mouth";
            descriptor.FindProperty("customEyeLookSettings.eyelidsSkinnedMesh").objectReferenceValue = renderer;
            var blink = descriptor.FindProperty("customEyeLookSettings.eyelidsBlendshapes");
            blink.arraySize = 1;
            blink.GetArrayElementAtIndex(0).intValue = 0;
            descriptor.FindProperty("customEyeLookSettings.leftEye").objectReferenceValue = face;
            descriptor.FindProperty("customEyeLookSettings.rightEye").objectReferenceValue = face;
            var layers = descriptor.FindProperty("baseAnimationLayers");
            layers.arraySize = 1;
            var type = layers.GetArrayElementAtIndex(0).FindPropertyRelative("type");
            type.enumValueIndex = Array.IndexOf(type.enumNames, "FX");
            layers.GetArrayElementAtIndex(0).FindPropertyRelative("isDefault").boolValue = true;
            descriptor.ApplyModifiedPropertiesWithoutUndo();
            mesh.AddBlendShapeFrame("Happy", 100, new Vector3[3], new Vector3[3], new Vector3[3]);
        }

        [Test] public void AutomaticExtractionRemainsEnabledByDefault()
        {
            ConfigureDescriptor();
            var definition = AvatarConversion.Convert(avatar, new List<string>());
            Assert.That(definition.expressions.Select(e => e.name), Is.EquivalentTo(new[] { "aa", "ih", "ou", "ee", "oh", "blink", "Face/Happy" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AutomaticExtractionTogglePreservesEyesAndExplicitClip(bool automatic)
        {
            ConfigureDescriptor();
            using var export = new AvatarExpressionExport(new[] { new ExportExpression { name = "Custom", clip = clip } });
            export.Register(avatar);
            var warnings = new List<string>();
            var definition = AvatarConversion.Convert(avatar, warnings, automatic);
            Assert.That(definition.expressions.Length, Is.EqualTo(automatic ? 7 : 0));
            Assert.That(definition.leftEye, Is.EqualTo("Face"));
            Assert.That(definition.rightEye, Is.EqualTo("Face"));
            export.Extract(avatar, definition, warnings);
            Assert.That(definition.expressions.Length, Is.EqualTo(automatic ? 8 : 1));
            var custom = definition.expressions.Single(e => e.name == "Custom");
            Assert.That(custom.morphs.Length, Is.EqualTo(2));
            Assert.That(custom.morphs.Sum(m => m.weight), Is.EqualTo(118).Within(.001));
        }
    }
}
