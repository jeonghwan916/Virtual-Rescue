using NUnit.Framework;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring.Tests
{
    public sealed class SituationAuthoringTests
    {
        [Test]
        public void RequestBuildsPathsFromLocationAndLevel()
        {
            SituationCreationRequest request = CreateValidRequest();

            Assert.That(
                request.ScenePath,
                Is.EqualTo(
                    "Assets/01_Scenes/Situation/Kitchen/Level1/" +
                    "Scenario_Kitchen_Test.unity"));
            Assert.That(
                request.ControllerScriptPath,
                Is.EqualTo(
                    "Assets/02_Scripts/10_Situations/Kitchen/" +
                    "KitchenTestSituationController.cs"));
        }

        [Test]
        public void BasicValidationRejectsInvalidControllerClassName()
        {
            SituationCreationRequest request = CreateValidRequest();
            request.controllerClassName = "Invalid Class";

            bool valid = SituationControllerScriptGenerator.ValidateRequest(
                request,
                out string error,
                false);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("valid C# identifier"));
        }

        [Test]
        public void Level2RequiresAllowedExit()
        {
            SituationCreationRequest request = CreateValidRequest();
            request.level = (int)SituationLevel.Level2;
            request.allowedExits = System.Array.Empty<int>();

            bool valid = SituationControllerScriptGenerator.ValidateRequest(
                request,
                out string error,
                false);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("at least one allowed exit"));
        }

        [Test]
        public void GeneratedControllerInheritsSituationController()
        {
            string script = SituationControllerScriptGenerator.CreateScriptText(
                CreateValidRequest());

            Assert.That(
                script,
                Does.Contain(
                    "KitchenTestSituationController : SituationController"));
            Assert.That(script, Does.Contain("protected override void OnActivated()"));
            Assert.That(script, Does.Not.Contain("ResolveSituation();"));
        }

        private static SituationCreationRequest CreateValidRequest()
        {
            return new SituationCreationRequest
            {
                displayName = "Kitchen Test",
                situationId = "kitchen.test",
                location = (int)SituationLocation.Kitchen,
                level = (int)SituationLevel.Level1,
                sceneName = "Scenario_Kitchen_Test",
                controllerClassName = "KitchenTestSituationController",
                controllerNamespace = "VirtualRescue.Situations.KitchenTest",
                controllerScriptFolder =
                    "Assets/02_Scripts/10_Situations/Kitchen",
                weight = 1,
                minimumDay = 1
            };
        }
    }
}
