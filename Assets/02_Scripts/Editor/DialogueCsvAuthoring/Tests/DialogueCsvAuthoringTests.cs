using System.Collections.Generic;
using NUnit.Framework;

namespace VirtualRescue.EditorTools.DialogueCsvAuthoring.Tests
{
    public sealed class DialogueCsvAuthoringTests
    {
        [Test]
        public void ParserKeepsQuotedCommasInOneCell()
        {
            List<string[]> rows = DialogueCsvParser.Parse(
                "id,language,text\n1,kr,\"address, people, injuries\"");

            Assert.That(rows[1][2], Is.EqualTo("address, people, injuries"));
        }

        [Test]
        public void ParserEscapesQuotesAndCommas()
        {
            string row = DialogueCsvParser.CreateRow(new[]
            {
                "id",
                "kr",
                "say \"hello\", then wait"
            });

            Assert.That(row, Is.EqualTo("id,kr,\"say \"\"hello\"\", then wait\""));
        }

        [Test]
        public void GroupRowsUseBaseIdSuffixAndSequentialOrder()
        {
            DialogueCsvSaveRequest request = new()
            {
                Mode = DialogueCsvAuthoringMode.Group,
                Id = "Intro",
                Group = "intro",
                Language = "kr"
            };
            request.Lines.Add(new DialogueCsvLine("first", "", "", "", ""));
            request.Lines.Add(new DialogueCsvLine("second", "", "", "", ""));

            bool built = DialogueCsvAuthoringService.TryBuildRows(
                request,
                HeaderMap("id", "group", "order", "speaker", "audioPath", "callbackKey", "delayAfterAudio"),
                HeaderMap("id", "language", "text"),
                new HashSet<string>(),
                new HashSet<string>(),
                out List<string> dialogueRows,
                out List<string> textRows,
                out string error);

            Assert.That(built, Is.True, error);
            Assert.That(dialogueRows[0], Is.EqualTo("Intro_001,intro,1,,,,"));
            Assert.That(dialogueRows[1], Is.EqualTo("Intro_002,intro,2,,,,"));
            Assert.That(textRows[0], Is.EqualTo("Intro_001,kr,first"));
            Assert.That(textRows[1], Is.EqualTo("Intro_002,kr,second"));
        }

        [Test]
        public void SingleRowRejectsDuplicateIdFromEitherCsv()
        {
            DialogueCsvSaveRequest request = new()
            {
                Mode = DialogueCsvAuthoringMode.Single,
                Id = "Existing",
                Language = "kr"
            };
            request.Lines.Add(new DialogueCsvLine("text", "", "", "", ""));

            bool built = DialogueCsvAuthoringService.TryBuildRows(
                request,
                HeaderMap("id", "group", "order"),
                HeaderMap("id", "language", "text"),
                new HashSet<string>(),
                new HashSet<string> { "Existing" },
                out _,
                out _,
                out string error);

            Assert.That(built, Is.False);
            Assert.That(error, Does.Contain("Existing"));
        }

        private static Dictionary<string, int> HeaderMap(params string[] headers)
        {
            Dictionary<string, int> map = new();
            for (int index = 0; index < headers.Length; index++)
            {
                map.Add(headers[index], index);
            }

            return map;
        }
    }
}
