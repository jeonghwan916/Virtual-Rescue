using System;
using UnityEngine;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal enum SituationValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    internal sealed class SituationValidationResult
    {
        public SituationValidationResult(
            SituationValidationSeverity severity,
            string message,
            UnityEngine.Object context = null,
            Action fix = null,
            string fixLabel = "Fix")
        {
            Severity = severity;
            Message = message;
            Context = context;
            Fix = fix;
            FixLabel = fixLabel;
        }

        public SituationValidationSeverity Severity { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
        public Action Fix { get; }
        public string FixLabel { get; }
    }
}
