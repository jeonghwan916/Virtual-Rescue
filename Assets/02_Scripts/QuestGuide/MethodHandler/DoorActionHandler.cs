using UnityEngine;
using VirtualRescue.QuestGuide;
namespace VirtualRescue.QuestGuide
{
    [DisallowMultipleComponent]
    public class DoorActionHandler : MonoBehaviour, IQuestGuideActionHandler
    {
        private const string DoorPrefix = "door:";

        //[SerializeField] private DoorController _doorController;
        
        public bool CanHandle(string actionId)
        {
            return !string.IsNullOrWhiteSpace(actionId) && actionId.StartsWith(DoorPrefix);
        }

        public void Handle(string actionId)
        {
            /*
            if (_doorController == null)
            {
                Debug.LogWarning("DoorController is not assigned.", this);
                return;
            }
            */

            string command = actionId.Substring(DoorPrefix.Length);

            switch (command)
            {
                case "open_main":
                    Debug.Log("open_main");
                    //_doorController.OpenMainDoor();
                    break;

                case "close_main":
                    Debug.Log("close_main");
                    //_doorController.CloseMainDoor();
                    break;

                case "lock_main":
                    Debug.Log("lock_main");
                    //_doorController.LockMainDoor();
                    break;

                case "unlock_main":
                    Debug.Log("unlock_main");
                    //_doorController.UnlockMainDoor();
                    break;

                default:
                    Debug.LogWarning($"Unhandled door guide command: {command}", this);
                    break;
            }

        }
    }
}
