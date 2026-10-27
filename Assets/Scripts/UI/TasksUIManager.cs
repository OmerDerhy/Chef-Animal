using UnityEngine;
using UnityEngine.UI;
using MergesChef.Feel;

namespace MergesChef.UI
{
    public class TasksUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject tasksPanel;

        [Header("UI Elements")]
        public Button openTasksButton;
        public Button closeButton;

        private void Start()
        {
            if (tasksPanel != null) tasksPanel.SetActive(false);
            if (openTasksButton != null) openTasksButton.onClick.AddListener(OpenPanel);
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            JuicyButton.Attach(openTasksButton, closeButton);
        }

        public void OpenPanel()
        {
            if (tasksPanel != null) UIPopAnimator.Show(tasksPanel);
        }

        public void ClosePanel()
        {
            if (tasksPanel != null) UIPopAnimator.Hide(tasksPanel);
        }
    }
}
