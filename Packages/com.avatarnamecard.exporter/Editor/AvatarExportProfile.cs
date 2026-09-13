using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AvatarNamecard.Exporter
{
    [CreateAssetMenu(menuName = "MEISHI Pop/Export Profile", fileName = "Avatar Export Profile")]
    public sealed class AvatarExportProfile : ScriptableObject
    {
        public bool autoExtractExpressions = true;
        public BuildTarget target = BuildTarget.iOS;
        public Texture2D thumbnail;
        public List<ExportExpression> expressions = new List<ExportExpression>();

        internal static List<ExportExpression> CopyExpressions(IEnumerable<ExportExpression> source)
        {
            return source.Select(e => e == null ? new ExportExpression() : new ExportExpression
            {
                name = e.name, clip = e.clip, manualTime = e.manualTime, time = e.time
            }).ToList();
        }

        internal void SetSettings(bool automatic, BuildTarget destination, IEnumerable<ExportExpression> source, Texture2D image = null)
        {
            Undo.RecordObject(this, "Edit MEISHI Pop export profile");
            autoExtractExpressions = automatic;
            target = destination;
            thumbnail = image;
            expressions = CopyExpressions(source);
            EditorUtility.SetDirty(this);
        }
    }
}
