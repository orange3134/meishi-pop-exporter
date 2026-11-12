using System;
using System.IO;
using System.Linq;
using AvatarNamecard.AvatarPackage.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace AvatarNamecard.Exporter.Tests
{
    public class IosShaderSettingsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void DesktopBuildUsesIosTiersAndRestoresSourceEvenOnFailure(bool fail)
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").First();
            var before = EditorJsonUtility.ToJson(settings);
            var file = File.ReadAllBytes("ProjectSettings/GraphicsSettings.asset");
            var ios = IosShaderSettings.DescribeIos();
            try
            {
                using (IosShaderSettings.ForBundle(BuildTarget.StandaloneOSX))
                {
                    foreach (var tier in new[] { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3 })
                        Assert.That(JsonUtility.ToJson(EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, tier)),
                            Is.EqualTo(JsonUtility.ToJson(EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.iOS, tier))));
                    // Simulate a build persisting changes without passing a persistent GraphicsSettings
                    // object to SaveToSerializedFileAndForget (unsupported in Unity 2022.3).
                    File.AppendAllText("ProjectSettings/GraphicsSettings.asset", "\n# Simulated settings write during bundle build\n");
                    Assert.That(File.ReadAllBytes("ProjectSettings/GraphicsSettings.asset"), Is.Not.EqualTo(file));
                    if (fail) throw new InvalidOperationException("Simulated build failure");
                }
            }
            catch (InvalidOperationException) when (fail) { }
            Assert.That(EditorJsonUtility.ToJson(settings), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes("ProjectSettings/GraphicsSettings.asset"), Is.EqualTo(file));
            Assert.That(IosShaderSettings.DescribeIos(), Is.EqualTo(ios));
        }
        [Test] public void IosBundleBuildDoesNotModifyAnyTier()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").First();
            var before = EditorJsonUtility.ToJson(settings);
            using (IosShaderSettings.ForBundle(BuildTarget.iOS)) Assert.That(EditorJsonUtility.ToJson(settings), Is.EqualTo(before));
            Assert.That(EditorJsonUtility.ToJson(settings), Is.EqualTo(before));
        }
    }
}
