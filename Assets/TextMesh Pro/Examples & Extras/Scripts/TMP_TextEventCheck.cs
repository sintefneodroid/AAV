using UnityEngine;

namespace TextMesh_Pro.Scripts
{
    public class TMP_TextEventCheck : MonoBehaviour
    {

        public TMP_TextEventHandler TextEventHandler;

        void OnEnable()
        {
            if (this.TextEventHandler != null)
            {
                this.TextEventHandler.onCharacterSelection.AddListener(this.OnCharacterSelection);
                this.TextEventHandler.onSpriteSelection.AddListener(this.OnSpriteSelection);
                this.TextEventHandler.onWordSelection.AddListener(this.OnWordSelection);
                this.TextEventHandler.onLineSelection.AddListener(this.OnLineSelection);
                this.TextEventHandler.onLinkSelection.AddListener(this.OnLinkSelection);
            }
        }


        void OnDisable()
        {
            if (this.TextEventHandler != null)
            {
                this.TextEventHandler.onCharacterSelection.RemoveListener(this.OnCharacterSelection);
                this.TextEventHandler.onSpriteSelection.RemoveListener(this.OnSpriteSelection);
                this.TextEventHandler.onWordSelection.RemoveListener(this.OnWordSelection);
                this.TextEventHandler.onLineSelection.RemoveListener(this.OnLineSelection);
                this.TextEventHandler.onLinkSelection.RemoveListener(this.OnLinkSelection);
            }
        }


        void OnCharacterSelection(char c, int index)
        {
            Debug.Log("Character [" + c + "] at Index: " + index + " has been selected.");
        }

        void OnSpriteSelection(char c, int index)
        {
            Debug.Log("Sprite [" + c + "] at Index: " + index + " has been selected.");
        }

        void OnWordSelection(string word, int firstCharacterIndex, int length)
        {
            Debug.Log("Word [" + word + "] with first character index of " + firstCharacterIndex + " and length of " + length + " has been selected.");
        }

        void OnLineSelection(string lineText, int firstCharacterIndex, int length)
        {
            Debug.Log("Line [" + lineText + "] with first character index of " + firstCharacterIndex + " and length of " + length + " has been selected.");
        }

        void OnLinkSelection(string linkID, string linkText, int linkIndex)
        {
            Debug.Log("Link Index: " + linkIndex + " with ID [" + linkID + "] and Text \"" + linkText + "\" has been selected.");
        }

    }
}