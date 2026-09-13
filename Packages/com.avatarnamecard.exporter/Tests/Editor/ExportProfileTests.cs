using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AvatarNamecard.Exporter.Tests
{
    public class ExportProfileTests
    {
        private string folder;
        private string previousProfile;
        private AvatarExporterWindow window;
        private AnimationClip clip;

        [SetUp] public void SetUp()
        {
            previousProfile = EditorPrefs.GetString(AvatarExporterWindow.LastProfileKey, "");
            EditorPrefs.DeleteKey(AvatarExporterWindow.LastProfileKey);
            folder = "Assets/MeishiPopProfileTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, folder + "/Face.anim");
            window = ScriptableObject.CreateInstance<AvatarExporterWindow>();
        }

        [TearDown] public void TearDown()
        {
            if (window != null) Object.DestroyImmediate(window);
            foreach (var guid in AssetDatabase.FindAssets("t:AvatarExportProfile", new[] { folder }))
                Undo.ClearUndo(AssetDatabase.LoadAssetAtPath<AvatarExportProfile>(AssetDatabase.GUIDToAssetPath(guid)));
            AssetDatabase.DeleteAsset(folder);
            if (string.IsNullOrEmpty(previousProfile)) EditorPrefs.DeleteKey(AvatarExporterWindow.LastProfileKey);
            else EditorPrefs.SetString(AvatarExporterWindow.LastProfileKey, previousProfile);
        }

        private void SetWindowSettings()
        {
            var settings = new SerializedObject(window);
            settings.FindProperty("autoExtractExpressions").boolValue = false;
            settings.FindProperty("target").intValue = (int)BuildTarget.StandaloneOSX;
            var list = settings.FindProperty("expressions");
            list.arraySize = 2;
            for (var i = 0; i < 2; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("name").stringValue = "Face " + i;
                item.FindPropertyRelative("clip").objectReferenceValue = clip;
                item.FindPropertyRelative("manualTime").boolValue = i == 1;
                item.FindPropertyRelative("time").floatValue = .4f;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test] public void ProfileSurvivesReloadAndClipRenameWithAllSettings()
        {
            SetWindowSettings();
            var saved = window.SaveNewProfile(folder + "/Profile.asset");
            Assert.That(AssetDatabase.MoveAsset(folder + "/Face.anim", folder + "/Renamed.anim"), Is.Empty);
            AssetDatabase.ImportAsset(folder + "/Profile.asset", ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<AvatarExportProfile>(folder + "/Profile.asset");
            Assert.That(loaded.autoExtractExpressions, Is.False);
            Assert.That(loaded.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(loaded.expressions.ConvertAll(e => e.name), Is.EqualTo(new[] { "Face 0", "Face 1" }));
            Assert.That(AssetDatabase.GetAssetPath(loaded.expressions[0].clip), Is.EqualTo(folder + "/Renamed.anim"));
            Assert.That(loaded.expressions[0].manualTime, Is.False);
            Assert.That(loaded.expressions[1].manualTime, Is.True);
            Assert.That(loaded.expressions[1].time, Is.EqualTo(.4f));
        }

        [Test] public void DuplicateDoesNotOverwriteOrShareExpressionEntries()
        {
            SetWindowSettings();
            var original = window.SaveNewProfile(folder + "/Profile.asset");
            var copy = window.SaveNewProfile(folder + "/Profile.asset");
            Assert.That(AssetDatabase.GetAssetPath(copy), Is.Not.EqualTo(AssetDatabase.GetAssetPath(original)));
            Assert.That(copy.expressions[0], Is.Not.SameAs(original.expressions[0]));
            copy.expressions[0].name = "Changed";
            Assert.That(original.expressions[0].name, Is.EqualTo("Face 0"));
            Assert.That(copy.expressions[0].clip, Is.SameAs(original.expressions[0].clip));
        }

        [Test] public void LastProfileRestoresAfterWindowRecreationAndProfileMove()
        {
            SetWindowSettings();
            window.SaveNewProfile(folder + "/Profile.asset");
            Object.DestroyImmediate(window);
            Assert.That(AssetDatabase.MoveAsset(folder + "/Profile.asset", folder + "/Moved.asset"), Is.Empty);
            window = ScriptableObject.CreateInstance<AvatarExporterWindow>();
            var settings = new SerializedObject(window);
            Assert.That(AssetDatabase.GetAssetPath(settings.FindProperty("profile").objectReferenceValue), Is.EqualTo(folder + "/Moved.asset"));
            Assert.That(settings.FindProperty("autoExtractExpressions").boolValue, Is.False);
            Assert.That(settings.FindProperty("expressions").arraySize, Is.EqualTo(2));
            window.SelectProfile(null);
            Object.DestroyImmediate(window);
            window = ScriptableObject.CreateInstance<AvatarExporterWindow>();
            Assert.That(new SerializedObject(window).FindProperty("profile").objectReferenceValue, Is.Null);
        }

        [UnityTest] public IEnumerator EditsAndUndoRedoAreAutomaticallySaved()
        {
            var profile = window.SaveNewProfile(folder + "/Profile.asset");
            SetWindowSettings();
            window.CommitProfileSettings();
            Undo.FlushUndoRecordObjects();
            yield return null;
            yield return null;
            Assert.That(EditorUtility.IsDirty(profile), Is.False);
            Assert.That(File.ReadAllText(folder + "/Profile.asset"), Does.Contain("Face 1"));
            Undo.PerformUndo();
            yield return null;
            yield return null;
            Assert.That(profile.expressions.Count, Is.Zero);
            Assert.That(new SerializedObject(window).FindProperty("expressions").arraySize, Is.Zero);
            Assert.That(File.ReadAllText(folder + "/Profile.asset"), Does.Not.Contain("Face 1"));
            Undo.PerformRedo();
            yield return null;
            yield return null;
            Assert.That(profile.expressions.Count, Is.EqualTo(2));
            Assert.That(File.ReadAllText(folder + "/Profile.asset"), Does.Contain("Face 1"));
        }

        [Test] public void UndoAfterSwitchingProfilesAlsoSavesTheEditedProfile()
        {
            var first = window.SaveNewProfile(folder + "/First.asset");
            var second = window.SaveNewProfile(folder + "/Second.asset");
            window.SelectProfile(first);
            SetWindowSettings();
            window.CommitProfileSettings();
            window.SelectProfile(second);
            Undo.PerformUndo();
            Assert.That(first.expressions.Count, Is.Zero);
            Assert.That(File.ReadAllText(folder + "/First.asset"), Does.Not.Contain("Face 1"));
            Assert.That(new SerializedObject(window).FindProperty("profile").objectReferenceValue, Is.SameAs(second));
        }

        [Test] public void SwitchingProfilesKeepsSettingsSeparateAndDeletedProfileRestoresSafely()
        {
            var empty = window.SaveNewProfile(folder + "/Empty.asset");
            SetWindowSettings();
            var filled = window.SaveNewProfile(folder + "/Filled.asset");
            window.SelectProfile(empty);
            Assert.That(new SerializedObject(window).FindProperty("expressions").arraySize, Is.Zero);
            window.SelectProfile(filled);
            Assert.That(new SerializedObject(window).FindProperty("expressions").arraySize, Is.EqualTo(2));
            Object.DestroyImmediate(window);
            AssetDatabase.DeleteAsset(folder + "/Filled.asset");
            window = ScriptableObject.CreateInstance<AvatarExporterWindow>();
            Assert.That(new SerializedObject(window).FindProperty("profile").objectReferenceValue, Is.Null);
        }
    }
}
