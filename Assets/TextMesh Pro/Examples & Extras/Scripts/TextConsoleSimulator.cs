using System.Collections;
using TMPro;
using UnityEngine;

namespace TextMesh_Pro.Scripts
{
    public class TextConsoleSimulator : MonoBehaviour
    {
        TMP_Text m_TextComponent;
        bool hasTextChanged;

        void Awake()
        {
            this.m_TextComponent = this.gameObject.GetComponent<TMP_Text>();
        }


        void Start()
        {
            this.StartCoroutine(this.RevealCharacters(this.m_TextComponent));
            //StartCoroutine(RevealWords(m_TextComponent));
        }


        void OnEnable()
        {
            // Subscribe to event fired when text object has been regenerated.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(this.ON_TEXT_CHANGED);
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(this.ON_TEXT_CHANGED);
        }


        // Event received when the text object has changed.
        void ON_TEXT_CHANGED(Object obj)
        {
            this.hasTextChanged = true;
        }


        /// <summary>
        /// Method revealing the text one character at a time.
        /// </summary>
        /// <returns></returns>
        IEnumerator RevealCharacters(TMP_Text textComponent)
        {
            textComponent.ForceMeshUpdate();

            var textInfo = textComponent.textInfo;

            var totalVisibleCharacters = textInfo.characterCount; // Get # of Visible Character in text object
            var visibleCount = 0;

            while (true)
            {
                if (this.hasTextChanged)
                {
                    totalVisibleCharacters = textInfo.characterCount; // Update visible character count.
                    this.hasTextChanged = false;
                }

                if (visibleCount > totalVisibleCharacters)
                {
                    yield return new WaitForSeconds(1.0f);
                    visibleCount = 0;
                }

                textComponent.maxVisibleCharacters = visibleCount; // How many characters should TextMeshPro display?

                visibleCount += 1;

                yield return null;
            }
        }


        /// <summary>
        /// Method revealing the text one word at a time.
        /// </summary>
        /// <returns></returns>
        IEnumerator RevealWords(TMP_Text textComponent)
        {
            textComponent.ForceMeshUpdate();

            var totalWordCount = textComponent.textInfo.wordCount;
            var totalVisibleCharacters = textComponent.textInfo.characterCount; // Get # of Visible Character in text object
            var counter = 0;
            var currentWord = 0;
            var visibleCount = 0;

            while (true)
            {
                currentWord = counter % (totalWordCount + 1);

                // Get last character index for the current word.
                if (currentWord == 0) // Display no words.
                    visibleCount = 0;
                else if (currentWord < totalWordCount) // Display all other words with the exception of the last one.
                    visibleCount = textComponent.textInfo.wordInfo[currentWord - 1].lastCharacterIndex + 1;
                else if (currentWord == totalWordCount) // Display last word and all remaining characters.
                    visibleCount = totalVisibleCharacters;

                textComponent.maxVisibleCharacters = visibleCount; // How many characters should TextMeshPro display?

                // Once the last character has been revealed, wait 1.0 second and start over.
                if (visibleCount >= totalVisibleCharacters)
                {
                    yield return new WaitForSeconds(1.0f);
                }

                counter += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}