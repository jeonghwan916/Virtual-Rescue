namespace VirtualRescue.QuestGuide
{
    public interface IQuestGuideActionHandler
    {
        bool CanHandle(string actionId);
        void Handle(string actionId);
    }
}
