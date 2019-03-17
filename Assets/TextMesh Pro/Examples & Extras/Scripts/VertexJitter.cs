using System.Collections;
using TMPro;
using UnityEngine;

namespace TextMesh_Pro.Scripts
{

    public class VertexJitter : MonoBehaviour
    {

        public float AngleMultiplier = 1.0f;
        public float SpeedMultiplier = 1.0f;
        public float CurveScale = 1.0f;

        TMP_Text m_TextComponent;
        bool hasTextChanged;

        /// <summary>
        /// Structure to hold pre-computed animation data.
        /// </summary>
        struct VertexAnim
        {
            public float angleRange;
            public float angle;
            public float speed;
        }

        void Awake()
        {
            this.m_TextComponent = this.GetComponent<TMP_Text>();
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


        void Start()
        {
            this.StartCoroutine(this.AnimateVertexColors());
        }


        void ON_TEXT_CHANGED(Object obj)
        {
            if (obj == this.m_TextComponent) this.hasTextChanged = true;
        }

        /// <summary>
        /// Method to animate vertex colors of a TMP Text object.
        /// </summary>
        /// <returns></returns>
        IEnumerator AnimateVertexColors()
        {

            // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
            // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
            this.m_TextComponent.ForceMeshUpdate();

            var textInfo = this.m_TextComponent.textInfo;

            Matrix4x4 matrix;

            var loopCount = 0;
            this.hasTextChanged = true;

            // Create an Array which contains pre-computed Angle Ranges and Speeds for a bunch of characters.
            var vertexAnim = new VertexAnim[1024];
            for (var i = 0; i < 1024; i++)
            {
                vertexAnim[i].angleRange = Random.Range(10f, 25f);
                vertexAnim[i].speed = Random.Range(1f, 3f);
            }

            // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
            var cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            while (true)
            {
                // Get new copy of vertex data if the text has changed.
                if (this.hasTextChanged)
                {
                    // Update the copy of the vertex data for the text object.
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                    this.hasTextChanged = false;
                }

                var characterCount = textInfo.characterCount;

                // If No Characters then just yield and wait for some text to be added
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }


                for (var i = 0; i < characterCount; i++)
                {
                    var charInfo = textInfo.characterInfo[i];

                    // Skip characters that are not visible and thus have no geometry to manipulate.
                    if (!charInfo.isVisible)
                        continue;

                    // Retrieve the pre-computed animation data for the given character.
                    var vertAnim = vertexAnim[i];

                    // Get the index of the material used by the current character.
                    var materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    var vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the cached vertices of the mesh used by this text element (character or sprite).
                    var sourceVertices = cachedMeshInfo[materialIndex].vertices;

                    // Determine the center point of each character at the baseline.
                    //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                    // Determine the center point of each character.
                    Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                    // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                    // This is needed so the matrix TRS is applied at the origin for each character.
                    Vector3 offset = charMidBasline;

                    var destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                    destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                    destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                    destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                    vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(loopCount / 25f * vertAnim.speed, 1f));
                    var jitterOffset = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.25f, .25f), 0);

                    matrix = Matrix4x4.TRS(jitterOffset * this.CurveScale, Quaternion.Euler(0, 0, Random.Range(-5f, 5f) * this.AngleMultiplier), Vector3.one);

                    destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                    destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                    destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                    destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                    destinationVertices[vertexIndex + 0] += offset;
                    destinationVertices[vertexIndex + 1] += offset;
                    destinationVertices[vertexIndex + 2] += offset;
                    destinationVertices[vertexIndex + 3] += offset;

                    vertexAnim[i] = vertAnim;
                }

                // Push changes into meshes
                for (var i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    this.m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                loopCount += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}