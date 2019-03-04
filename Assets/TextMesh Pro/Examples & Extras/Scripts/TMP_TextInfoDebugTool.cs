using TMPro;
using UnityEngine;

namespace TextMesh_Pro.Scripts
{

    [ExecuteInEditMode]
    public class TMP_TextInfoDebugTool : MonoBehaviour
    {
        public bool ShowCharacters;
        public bool ShowWords;
        public bool ShowLinks;
        public bool ShowLines;
        public bool ShowMeshBounds;
        public bool ShowTextBounds;
        [Space(10)]
        [TextArea(2, 2)]
        public string ObjectStats;

        private TMP_Text m_TextComponent;

        private Transform m_Transform;

// Since this script is used for visual debugging, we exclude most of it in builds.
#if UNITY_EDITOR

        void OnEnable()
        {
            this.m_TextComponent = this.gameObject.GetComponent<TMP_Text>();

            if (this.m_Transform == null) this.m_Transform = this.gameObject.GetComponent<Transform>();
        }


        void OnDrawGizmos()
        {
            // Update Text Statistics
            var textInfo = this.m_TextComponent.textInfo;

            this.ObjectStats = "Characters: " + textInfo.characterCount + "   Words: " + textInfo.wordCount + "   Spaces: " + textInfo.spaceCount + "   Sprites: " + textInfo.spriteCount + "   Links: " + textInfo.linkCount
                      + "\nLines: " + textInfo.lineCount + "   Pages: " + textInfo.pageCount;


            // Draw Quads around each of the Characters
            #region Draw Characters
            if (this.ShowCharacters) this.DrawCharactersBounds();
            #endregion


            // Draw Quads around each of the words
            #region Draw Words
            if (this.ShowWords) this.DrawWordBounds();
            #endregion


            // Draw Quads around each of the words
            #region Draw Links
            if (this.ShowLinks) this.DrawLinkBounds();
            #endregion


            // Draw Quads around each line
            #region Draw Lines
            if (this.ShowLines) this.DrawLineBounds();
            #endregion


            // Draw Quad around the bounds of the text
            #region Draw Bounds
            if (this.ShowMeshBounds) this.DrawBounds();
            #endregion

            // Draw Quad around the rendered region of the text.
            #region Draw Text Bounds
            if (this.ShowTextBounds) this.DrawTextBounds();
            #endregion
        }


        /// <summary>
        /// Method to draw a rectangle around each character.
        /// </summary>
        /// <param name="text"></param>
        void DrawCharactersBounds()
        {
            var textInfo = this.m_TextComponent.textInfo;

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                // Draw visible as well as invisible characters
                var cInfo = textInfo.characterInfo[i];

                var isCharacterVisible = i >= this.m_TextComponent.maxVisibleCharacters ||
                                          cInfo.lineNumber >= this.m_TextComponent.maxVisibleLines ||
                                          (this.m_TextComponent.overflowMode == TextOverflowModes.Page && cInfo.pageNumber + 1 != this.m_TextComponent.pageToDisplay) ? false : true;

                if (!isCharacterVisible) continue;

                // Get Bottom Left and Top Right position of the current character
                var bottomLeft = this.m_Transform.TransformPoint(cInfo.bottomLeft);
                var topLeft = this.m_Transform.TransformPoint(new Vector3(cInfo.topLeft.x, cInfo.topLeft.y, 0));
                var topRight = this.m_Transform.TransformPoint(cInfo.topRight);
                var bottomRight = this.m_Transform.TransformPoint(new Vector3(cInfo.bottomRight.x, cInfo.bottomRight.y, 0));

                var color = cInfo.isVisible ? Color.yellow : Color.grey;
                this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, color);

                // Baseline
                var baselineStart = new Vector3(topLeft.x, this.m_Transform.TransformPoint(new Vector3(0, cInfo.baseLine, 0)).y, 0);
                var baselineEnd = new Vector3(topRight.x, this.m_Transform.TransformPoint(new Vector3(0, cInfo.baseLine, 0)).y, 0);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(baselineStart, baselineEnd);


                // Draw Ascender & Descender for each character.
                var ascenderStart = new Vector3(topLeft.x, this.m_Transform.TransformPoint(new Vector3(0, cInfo.ascender, 0)).y, 0);
                var ascenderEnd = new Vector3(topRight.x, this.m_Transform.TransformPoint(new Vector3(0, cInfo.ascender, 0)).y, 0);
                var descenderStart = new Vector3(bottomLeft.x, this.m_Transform.TransformPoint(new Vector3(0, cInfo.descender, 0)).y, 0);
                var descenderEnd = new Vector3(bottomRight.x, this.m_Transform.TransformPoint(new Vector3(0, cInfo.descender, 0)).y, 0);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(ascenderStart, ascenderEnd);
                Gizmos.DrawLine(descenderStart, descenderEnd);

                // Draw Cap Height
                var capHeight = cInfo.baseLine + cInfo.fontAsset.fontInfo.CapHeight * cInfo.scale;
                var capHeightStart = new Vector3(topLeft.x, this.m_Transform.TransformPoint(new Vector3(0, capHeight, 0)).y, 0);
                var capHeightEnd = new Vector3(topRight.x, this.m_Transform.TransformPoint(new Vector3(0, capHeight, 0)).y, 0);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(capHeightStart, capHeightEnd);

                // Draw xAdvance for each character.
                var xAdvance = this.m_Transform.TransformPoint(cInfo.xAdvance, 0, 0).x;
                var topAdvance = new Vector3(xAdvance, topLeft.y, 0);
                var bottomAdvance = new Vector3(xAdvance, bottomLeft.y, 0);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(topAdvance, bottomAdvance);


            }
        }


        /// <summary>
        /// Method to draw rectangles around each word of the text.
        /// </summary>
        /// <param name="text"></param>
        void DrawWordBounds()
        {
            var textInfo = this.m_TextComponent.textInfo;

            for (var i = 0; i < textInfo.wordCount; i++)
            {
                var wInfo = textInfo.wordInfo[i];

                var isBeginRegion = false;

                var bottomLeft = Vector3.zero;
                var topLeft = Vector3.zero;
                var bottomRight = Vector3.zero;
                var topRight = Vector3.zero;

                var maxAscender = -Mathf.Infinity;
                var minDescender = Mathf.Infinity;

                var wordColor = Color.green;

                // Iterate through each character of the word
                for (var j = 0; j < wInfo.characterCount; j++)
                {
                    var characterIndex = wInfo.firstCharacterIndex + j;
                    var currentCharInfo = textInfo.characterInfo[characterIndex];
                    var currentLine = currentCharInfo.lineNumber;

                    var isCharacterVisible = characterIndex > this.m_TextComponent.maxVisibleCharacters ||
                                              currentCharInfo.lineNumber > this.m_TextComponent.maxVisibleLines ||
                                             (this.m_TextComponent.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 != this.m_TextComponent.pageToDisplay) ? false : true;

                    // Track Max Ascender and Min Descender
                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    if (isBeginRegion == false && isCharacterVisible)
                    {
                        isBeginRegion = true;

                        bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Word is one character
                        if (wInfo.characterCount == 1)
                        {
                            isBeginRegion = false;

                            topLeft = this.m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                            bottomLeft = this.m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                            bottomRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                            topRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                            // Draw Region
                            this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Word
                    if (isBeginRegion && j == wInfo.characterCount - 1)
                    {
                        isBeginRegion = false;

                        topLeft = this.m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = this.m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Word is split on more than one line.
                    else if (isBeginRegion && currentLine != textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        topLeft = this.m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = this.m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);
                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;

                    }
                }

                //Debug.Log(wInfo.GetWord(m_TextMeshPro.textInfo.characterInfo));
            }


        }


        /// <summary>
        /// Draw rectangle around each of the links contained in the text.
        /// </summary>
        /// <param name="text"></param>
        void DrawLinkBounds()
        {
            var textInfo = this.m_TextComponent.textInfo;

            for (var i = 0; i < textInfo.linkCount; i++)
            {
                var linkInfo = textInfo.linkInfo[i];

                var isBeginRegion = false;

                var bottomLeft = Vector3.zero;
                var topLeft = Vector3.zero;
                var bottomRight = Vector3.zero;
                var topRight = Vector3.zero;

                var maxAscender = -Mathf.Infinity;
                var minDescender = Mathf.Infinity;

                Color32 linkColor = Color.cyan;

                // Iterate through each character of the link text
                for (var j = 0; j < linkInfo.linkTextLength; j++)
                {
                    var characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    var currentCharInfo = textInfo.characterInfo[characterIndex];
                    var currentLine = currentCharInfo.lineNumber;

                    var isCharacterVisible = characterIndex > this.m_TextComponent.maxVisibleCharacters ||
                                              currentCharInfo.lineNumber > this.m_TextComponent.maxVisibleLines ||
                                             (this.m_TextComponent.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 != this.m_TextComponent.pageToDisplay) ? false : true;

                    // Track Max Ascender and Min Descender
                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    if (isBeginRegion == false && isCharacterVisible)
                    {
                        isBeginRegion = true;

                        bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Link is one character
                        if (linkInfo.linkTextLength == 1)
                        {
                            isBeginRegion = false;

                            topLeft = this.m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                            bottomLeft = this.m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                            bottomRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                            topRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                            // Draw Region
                            this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Link
                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        isBeginRegion = false;

                        topLeft = this.m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = this.m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Link is split on more than one line.
                    else if (isBeginRegion && currentLine != textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        isBeginRegion = false;

                        topLeft = this.m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        bottomLeft = this.m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        bottomRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        topRight = this.m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                        maxAscender = -Mathf.Infinity;
                        minDescender = Mathf.Infinity;
                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                }

                //Debug.Log(wInfo.GetWord(m_TextMeshPro.textInfo.characterInfo));
            }
        }


        /// <summary>
        /// Draw Rectangles around each lines of the text.
        /// </summary>
        /// <param name="text"></param>
        void DrawLineBounds()
        {
            var textInfo = this.m_TextComponent.textInfo;

            for (var i = 0; i < textInfo.lineCount; i++)
            {
                var lineInfo = textInfo.lineInfo[i];

                var isLineVisible = (lineInfo.characterCount == 1 && textInfo.characterInfo[lineInfo.firstCharacterIndex].character == 10) ||
                                      i > this.m_TextComponent.maxVisibleLines ||
                                     (this.m_TextComponent.overflowMode == TextOverflowModes.Page && textInfo.characterInfo[lineInfo.firstCharacterIndex].pageNumber + 1 != this.m_TextComponent.pageToDisplay) ? false : true;

                if (!isLineVisible) continue;

                //if (!ShowLinesOnlyVisibleCharacters)
                //{
                // Get Bottom Left and Top Right position of each line
                var ascender = lineInfo.ascender;
                var descender = lineInfo.descender;
                var baseline = lineInfo.baseline;
                var maxAdvance = lineInfo.maxAdvance;
                var bottomLeft = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].bottomLeft.x, descender, 0));
                var topLeft = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].bottomLeft.x, ascender, 0));
                var topRight = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.lastCharacterIndex].topRight.x, ascender, 0));
                var bottomRight = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.lastCharacterIndex].topRight.x, descender, 0));

                this.DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, Color.green);

                var bottomOrigin = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].origin, descender, 0));
                var topOrigin = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].origin, ascender, 0));
                var bottomAdvance = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].origin + maxAdvance, descender, 0));
                var topAdvance = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].origin + maxAdvance, ascender, 0));

                this.DrawDottedRectangle(bottomOrigin, topOrigin, topAdvance, bottomAdvance, Color.green);

                var baselineStart = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstCharacterIndex].bottomLeft.x, baseline, 0));
                var baselineEnd = this.m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.lastCharacterIndex].topRight.x, baseline, 0));

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(baselineStart, baselineEnd);

                // Draw LineExtents
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(this.m_Transform.TransformPoint(lineInfo.lineExtents.min), this.m_Transform.TransformPoint(lineInfo.lineExtents.max));

                //}
                //else
                //{
                //// Get Bottom Left and Top Right position of each line
                //float ascender = lineInfo.ascender;
                //float descender = lineInfo.descender;
                //Vector3 bottomLeft = m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstVisibleCharacterIndex].bottomLeft.x, descender, 0));
                //Vector3 topLeft = m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstVisibleCharacterIndex].bottomLeft.x, ascender, 0));
                //Vector3 topRight = m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.lastVisibleCharacterIndex].topRight.x, ascender, 0));
                //Vector3 bottomRight = m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.lastVisibleCharacterIndex].topRight.x, descender, 0));

                //DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, Color.green);

                //Vector3 baselineStart = m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.firstVisibleCharacterIndex].bottomLeft.x, textInfo.characterInfo[lineInfo.firstVisibleCharacterIndex].baseLine, 0));
                //Vector3 baselineEnd = m_Transform.TransformPoint(new Vector3(textInfo.characterInfo[lineInfo.lastVisibleCharacterIndex].topRight.x, textInfo.characterInfo[lineInfo.lastVisibleCharacterIndex].baseLine, 0));

                //Gizmos.color = Color.cyan;
                //Gizmos.DrawLine(baselineStart, baselineEnd);
                //}
            }
        }

        /// <summary>
        /// Draw Rectangle around the bounds of the text object.
        /// </summary>
        void DrawBounds()
        {
            var meshBounds = this.m_TextComponent.bounds;

            // Get Bottom Left and Top Right position of each word
            var bottomLeft = this.m_TextComponent.transform.position + (meshBounds.center - meshBounds.extents);
            var topRight = this.m_TextComponent.transform.position + (meshBounds.center + meshBounds.extents);

            this.DrawRectangle(bottomLeft, topRight, new Color(1, 0.5f, 0));
        }


        void DrawTextBounds()
        {
            var textBounds = this.m_TextComponent.textBounds;

            var bottomLeft = this.m_TextComponent.transform.position + (textBounds.center - textBounds.extents);
            var topRight = this.m_TextComponent.transform.position + (textBounds.center + textBounds.extents);

            this.DrawRectangle(bottomLeft, topRight, new Color(0f, 0.5f, 0.5f));
        }


        // Draw Rectangles
        void DrawRectangle(Vector3 BL, Vector3 TR, Color color)
        {
            Gizmos.color = color;

            Gizmos.DrawLine(new Vector3(BL.x, BL.y, 0), new Vector3(BL.x, TR.y, 0));
            Gizmos.DrawLine(new Vector3(BL.x, TR.y, 0), new Vector3(TR.x, TR.y, 0));
            Gizmos.DrawLine(new Vector3(TR.x, TR.y, 0), new Vector3(TR.x, BL.y, 0));
            Gizmos.DrawLine(new Vector3(TR.x, BL.y, 0), new Vector3(BL.x, BL.y, 0));
        }


        // Draw Rectangles
        void DrawRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
        {
            Gizmos.color = color;

            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
        }


        // Draw Rectangles
        void DrawDottedRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
        {
            var cam = Camera.current;
            var dotSpacing = (cam.WorldToScreenPoint(br).x - cam.WorldToScreenPoint(bl).x) / 75f;
            UnityEditor.Handles.color = color;

            UnityEditor.Handles.DrawDottedLine(bl, tl, dotSpacing);
            UnityEditor.Handles.DrawDottedLine(tl, tr, dotSpacing);
            UnityEditor.Handles.DrawDottedLine(tr, br, dotSpacing);
            UnityEditor.Handles.DrawDottedLine(br, bl, dotSpacing);
        }

#endif
    }
}

