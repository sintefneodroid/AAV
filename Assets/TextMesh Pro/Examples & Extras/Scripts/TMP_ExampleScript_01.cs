using TMPro;
using UnityEngine;

namespace TextMesh_Pro.Scripts
{

    public class TMP_ExampleScript_01 : MonoBehaviour
    {
        public enum objectType { TextMeshPro = 0, TextMeshProUGUI = 1 };

        public objectType ObjectType;
        public bool isStatic;

        private TMP_Text m_text;

        //private TMP_InputField m_inputfield;


        private const string k_label = "The count is <#0080ff>{0}</color>";
        private int count;

        void Awake()
        {
            // Get a reference to the TMP text component if one already exists otherwise add one.
            // This example show the convenience of having both TMP components derive from TMP_Text. 
            if (this.ObjectType == 0)
                this.m_text = this.GetComponent<TextMeshPro>() ?? this.gameObject.AddComponent<TextMeshPro>();
            else
                this.m_text = this.GetComponent<TextMeshProUGUI>() ?? this.gameObject.AddComponent<TextMeshProUGUI>();

            // Load a new font asset and assign it to the text object.
            this.m_text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");

            // Load a new material preset which was created with the context menu duplicate.
            this.m_text.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/Anton SDF - Drop Shadow");

            // Set the size of the font.
            this.m_text.fontSize = 120;

            // Set the text
            this.m_text.text = "A <#0080ff>simple</color> line of text.";

            // Get the preferred width and height based on the supplied width and height as opposed to the actual size of the current text container.
            var size = this.m_text.GetPreferredValues(Mathf.Infinity, Mathf.Infinity);

            // Set the size of the RectTransform based on the new calculated values.
            this.m_text.rectTransform.sizeDelta = new Vector2(size.x, size.y);
        }


        void Update()
        {
            if (!this.isStatic)
            {
                this.m_text.SetText(k_label, this.count % 1000);
                this.count += 1;
            }
        }

    }
}
