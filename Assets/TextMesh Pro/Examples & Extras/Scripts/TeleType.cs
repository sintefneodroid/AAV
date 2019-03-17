using System.Collections;
using TMPro;
using UnityEngine;

namespace TextMesh_Pro.Scripts
{
    
    public class TeleType : MonoBehaviour
    {


        //[Range(0, 100)]
        //public int RevealSpeed = 50;

        string label01 = "Example <sprite=2> of using <sprite=7> <#ffa000>Graphics Inline</color> <sprite=5> with Text in <font=\"Bangers SDF\" material=\"Bangers SDF - Drop Shadow\">TextMesh<#40a0ff>Pro</color></font><sprite=0> and Unity<sprite=1>";
        string label02 = "Example <sprite=2> of using <sprite=7> <#ffa000>Graphics Inline</color> <sprite=5> with Text in <font=\"Bangers SDF\" material=\"Bangers SDF - Drop Shadow\">TextMesh<#40a0ff>Pro</color></font><sprite=0> and Unity<sprite=2>";

        TMP_Text m_textMeshPro;


        void Awake()
        {
            // Get Reference to TextMeshPro Component
            this.m_textMeshPro = this.GetComponent<TMP_Text>();
            this.m_textMeshPro.text = this.label01;
            this.m_textMeshPro.enableWordWrapping = true;
            this.m_textMeshPro.alignment = TextAlignmentOptions.Top;



            //if (GetComponentInParent(typeof(Canvas)) as Canvas == null)
            //{
            //    GameObject canvas = new GameObject("Canvas", typeof(Canvas));
            //    gameObject.transform.SetParent(canvas.transform);
            //    canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            //    // Set RectTransform Size
            //    gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 300);
            //    m_textMeshPro.fontSize = 48;
            //}


        }


        IEnumerator Start()
        {

            // Force and update of the mesh to get valid information.
            this.m_textMeshPro.ForceMeshUpdate();


            var totalVisibleCharacters = this.m_textMeshPro.textInfo.characterCount; // Get # of Visible Character in text object
            var counter = 0;
            var visibleCount = 0;

            while (true)
            {
                visibleCount = counter % (totalVisibleCharacters + 1);

                this.m_textMeshPro.maxVisibleCharacters = visibleCount; // How many characters should TextMeshPro display?

                // Once the last character has been revealed, wait 1.0 second and start over.
                if (visibleCount >= totalVisibleCharacters)
                {
                    yield return new WaitForSeconds(1.0f);
                    this.m_textMeshPro.text = this.label02;
                    yield return new WaitForSeconds(1.0f);
                    this.m_textMeshPro.text = this.label01;
                    yield return new WaitForSeconds(1.0f);
                }

                counter += 1;

                yield return new WaitForSeconds(0.05f);
            }

            //Debug.Log("Done revealing the text.");
        }

    }
}