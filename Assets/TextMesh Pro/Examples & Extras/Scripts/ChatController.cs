using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TextMesh_Pro.Scripts
{
    public class ChatController : MonoBehaviour {


        public TMP_InputField TMP_ChatInput;

        public TMP_Text TMP_ChatOutput;

        public Scrollbar ChatScrollbar;

        void OnEnable()
        {
            this.TMP_ChatInput.onSubmit.AddListener(this.AddToChatOutput);

        }

        void OnDisable()
        {
            this.TMP_ChatInput.onSubmit.RemoveListener(this.AddToChatOutput);

        }


        void AddToChatOutput(string newText)
        {
            // Clear Input Field
            this.TMP_ChatInput.text = string.Empty;

            var timeNow = System.DateTime.Now;

            this.TMP_ChatOutput.text += "[<#FFFF80>" + timeNow.Hour.ToString("d2") + ":" + timeNow.Minute.ToString("d2") + ":" + timeNow.Second.ToString("d2") + "</color>] " + newText + "\n";

            this.TMP_ChatInput.ActivateInputField();

            // Set the scrollbar to the bottom when next text is submitted.
            this.ChatScrollbar.value = 0;

        }

    }
}
