using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DSA
{
    /// <summary>
    /// A Small helper class to help with the visualization of Data Structures & Algorithms
    /// </summary>
    public static class DSAEditorUtils
    {
        public delegate Color ColorCallback<T>(T elem, int iIndex);
        public delegate string StringCallback<T>(T elem);
        //public delegate void NodeDrawingCallback<T>(Tree<T>.Node node, Rect rect);
        //public delegate void BinaryNodeDrawingCallback<T>(BinaryTree<T>.Node node, Rect rect);

        private static GUIStyle         sm_textStyle;

        #region Array Drawing

        public static void DrawArray<T>(T[] array, ColorCallback<T> cc, StringCallback<T> sc, bool bDrawIndices = true)
        {
            // iterate through the array and draw the elements
            Rect dest = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            for (int i = 0; i < array.Length; i++)
            {
                DrawElement(array[i], dest, 0.0f, cc(array[i], i), sc(array[i]), i.ToString(), bDrawIndices);
                dest.x += dest.width * 1.05f;
            }
        }

        #endregion

        #region Data Structures Drawing

        /*
        public static void DrawList<T>(List<T> list, ColorCallback<T> cc, StringCallback<T> sc)
        {
            // iterate through the list and draw the elements
            Rect dest = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            for (int i = 0; i < list.Count; i++)
            {
                DrawElement(list[i], dest, 0.0f, cc(list[i]), sc(list[i]), i.ToString());
                dest.x += dest.width * 1.05f;
            }

            // draw remaining capacity
            for (int i = list.Count; i < list.Capacity; ++i)
            {
                DrawElement(list[i], dest, 0.0f, new Color(1.0f, 1.0f, 1.0f, 0.3f), "", i.ToString());;
                dest.x += dest.width * 1.05f;
            }
        }

        public static void DrawQueue<T>(Queue<T> queue, ColorCallback<T> cc, StringCallback<T> sc)
        {
            // iterate through the queue and draw the elements
            Rect dest = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            for (int i = 0; i < queue.Count; i++)
            {
                DrawElement(queue[i], dest, 0.0f, cc(queue[i]), sc(queue[i]), i.ToString());
                dest.x += dest.width * 1.05f;
            }

            // draw remaining capacity
            for (int i = queue.Count; i < queue.Capacity; ++i)
            {
                DrawElement(queue[i], dest, 0.0f, new Color(1.0f, 1.0f, 1.0f, 0.3f), "", i.ToString()); ;
                dest.x += dest.width * 1.05f;
            }
        }

        public static void DrawStack<T>(Stack<T> stack, ColorCallback<T> cc, StringCallback<T> sc)
        {
            // iterate through the stack and draw the elements
            Rect dest = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            for (int i = 0; i < stack.Count; i++)
            {
                DrawElement(stack[i], dest, 0.0f, cc(stack[i]), sc(stack[i]), i.ToString());
                dest.x += dest.width * 1.05f;
            }

            // draw remaining capacity
            for (int i = stack.Count; i < stack.Capacity; ++i)
            {
                DrawElement(stack[i], dest, 0.0f, new Color(1.0f, 1.0f, 1.0f, 0.3f), "", i.ToString()); ;
                dest.x += dest.width * 1.05f;
            }
        }

        public static void DrawLinkedList<T>(LinkedList<T> list, ColorCallback<T> cc, StringCallback<T> sc)
        {
            // iterate through the stack and draw the elements
            Rect dest = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            int iCount = list.Count;
            for (int i = 0; i < iCount; i++)
            {                
                DrawElement(list[i], dest, 0.0f, cc(list[i]), sc(list[i]), i.ToString());

                Rect next = new Rect(dest.xMax, dest.y, dest.width * 0.4f, dest.height);
                DrawRect(next, 0.0f, new Color(1.0f, 1.0f, 1.0f, 0.5f), Color.black, 2.0f);
                if (i < iCount - 1)
                {
                    DrawArrow(next.center, (Vector3)next.center + Vector3.right * 0.75f, 0.15f, Color.red, 4.0f);
                }

                dest.x += dest.width * 2.0f;
            }
        }
        */
        #endregion

        #region Performance Drawing

        #endregion
        /*
        #region Trees

        public static void DrawTree<T>(Tree<T> tree, ColorCallback<T> cc, StringCallback<T> sc, NodeDrawingCallback<T> dc)
        {
            if (tree != null && tree.m_root != null)
            {
                Dictionary<Tree<T>.Node, Rect> layout = DoTreeLayout(tree);
                DrawNode(tree.m_root, cc, sc, dc, layout);
            }
        }

        static void DrawNode<T>(Tree<T>.Node node, ColorCallback<T> cc, StringCallback<T> sc, NodeDrawingCallback<T> dc, Dictionary<Tree<T>.Node, Rect> layout)
        {
            Rect nodeRect = layout[node];

            // draw children
            foreach (Tree<T>.Node child in node.Children)
            {
                // parent link
                Rect childRect = layout[child];
                Handles.color = Color.black;
                Handles.DrawLine(new Vector3(nodeRect.center.x, nodeRect.y, 0.0f), new Vector3(childRect.center.x, childRect.yMax, 0.0f), 2.0f);

                // draw child
                DrawNode<T>(child, cc, sc, dc, layout);
            }
            
            // draw element
            DrawElement<T>(node.m_value, nodeRect, 0.0f, cc(node.m_value, 0), sc(node.m_value), null, false);
            
            // drawing callback
            if (dc != null)
            {
                dc(node, nodeRect);
            }
        }

        static Dictionary<Tree<T>.Node, Rect> DoTreeLayout<T>(Tree<T> tree)
        {
            Dictionary<Tree<T>.Node, Rect> layout = new Dictionary<Tree<T>.Node, Rect>();
            Dictionary<int, List<Tree<T>.Node>> depthLookup = new Dictionary<int, List<Tree<T>.Node>>();
            CreateTreeDepthLookup(tree.m_root, 0, depthLookup);

            Vector2 vMinMax = Vector2.zero;
            Rect root = new Rect(0, 0, 1, 1);
            DoTreeLayout(tree.m_root, 0, root, ref vMinMax, depthLookup, layout);

            for (int i = 0; i < 4; ++i)
            {
                SmoothLayout(tree.m_root, 0, depthLookup, layout);
            }

            return layout;
        }

        static void DoTreeLayout<T>(Tree<T>.Node node, int iDepth, Rect targetNode, ref Vector2 vParentMinMaxRange, Dictionary<int, List<Tree<T>.Node>> depthLookup, Dictionary<Tree<T>.Node, Rect> layout)
        {
            // layout children
            Vector2 vMinMax = new Vector2(targetNode.x, targetNode.xMax);
            if (node.m_children != null)
            {
                iDepth++;
                for (int i = 0; i < node.m_children.Length; ++i)
                {
                    int iIndex = depthLookup[iDepth].IndexOf(node.m_children[i]);
                    float x = (iIndex - depthLookup[iDepth].Count * 0.5f) * 3.0f;
                    Rect childRect = new Rect(x - 0.5f, targetNode.center.y - 2.5f, 1.0f, 1.0f);
                    DoTreeLayout<T>(node.m_children[i], iDepth, childRect, ref vMinMax, depthLookup, layout);
                }
            }

            // center element
            float fCenter = (vMinMax.x + vMinMax.y) * 0.5f;
            targetNode.x = fCenter - targetNode.width * 0.5f;
            layout[node] = targetNode;

            // update parent min max range
            vParentMinMaxRange.x = Mathf.Min(vParentMinMaxRange.x, targetNode.x);
            vParentMinMaxRange.y = Mathf.Max(vParentMinMaxRange.y, targetNode.xMax);
        }

        static void CreateTreeDepthLookup<T>(Tree<T>.Node node, int iDepth, Dictionary<int, List<Tree<T>.Node>> depthLookup)
        {
            if (node == null)
            {
                return;
            }

            if (!depthLookup.ContainsKey(iDepth))
            {
                depthLookup[iDepth] = new List<Tree<T>.Node>();
            }

            depthLookup[iDepth].Add(node);

            foreach (Tree<T>.Node child in node.Children)
            {
                CreateTreeDepthLookup(child, iDepth + 1, depthLookup);
            }
        }

        static void SmoothLayout<T>(Tree<T>.Node node, int iDepth, Dictionary<int, List<Tree<T>.Node>> depthLookup, Dictionary<Tree<T>.Node, Rect> layout)
        {
            // spread away from siblings
            Rect nodeRect = layout[node];
            int iDepthIndex = depthLookup[iDepth].IndexOf(node);
            if (iDepthIndex > 0)
            {
                Tree<T>.Node prev = depthLookup[iDepth][iDepthIndex - 1];
                Rect prevRect = layout[prev];
                if (nodeRect.center.x < prevRect.center.x + 2.0f)
                {
                    nodeRect.x += 1.0f;
                    layout[node] = nodeRect;
                }
            }

            if (iDepthIndex < depthLookup[iDepth].Count - 1)
            {
                Tree<T>.Node next = depthLookup[iDepth][iDepthIndex + 1];
                Rect nextRect = layout[next];
                if (nodeRect.center.x > nextRect.center.x - 2.0f)
                {
                    nodeRect.x -= 1.0f;
                    layout[node] = nodeRect;
                }
            }

            // smooth children
            if (node.m_children != null && node.m_children.Length > 0)
            {
                Vector2 vMinMax = new Vector2(float.MaxValue, -float.MaxValue);
                foreach (Tree<T>.Node child in node.Children)
                {
                    SmoothLayout(child, iDepth + 1, depthLookup, layout);
                    Rect childRect = layout[child];
                    vMinMax.x = Mathf.Min(vMinMax.x, childRect.x);
                    vMinMax.y = Mathf.Max(vMinMax.y, childRect.xMax);
                }

                // center parent 
                nodeRect.x = (vMinMax.x + vMinMax.y) * 0.5f - 0.5f;
                layout[node] = nodeRect;
            }
        }

        #endregion

        #region Binary Trees

        public static void DrawBinaryTree<T>(BinaryTree<T> tree, ColorCallback<T> cc, StringCallback<T> sc, BinaryNodeDrawingCallback<T> dc)
        {
            if (tree != null && tree.m_root != null)
            {
                int iMaxDepth = tree.Depth;
                float fSize = Mathf.Pow(iMaxDepth, 2) * 0.5f;
                DrawBinaryNode(tree.m_root, new Vector2(-fSize, fSize), Vector2.zero, 0, cc, sc, dc);
            }
        }

        static void DrawBinaryNode<T>(BinaryTree<T>.Node node, Vector2 vRange, Vector2 vParentPos, int iDepth, ColorCallback<T> cc, StringCallback<T> sc, BinaryNodeDrawingCallback<T> dc)
        {
            Vector2 vCenter = new Vector2((vRange.x + vRange.y) * 0.5f, iDepth * -3.0f);
            Rect rect = new Rect(vCenter.x - 0.5f, vCenter.y - 0.5f, 1.0f, 1.0f);
            Vector2 vNodePos = new Vector2(rect.center.x, rect.y);

            // draw parent link
            if (iDepth > 0)
            {
                Handles.color = Color.black;
                Handles.DrawLine(new Vector3(rect.center.x, rect.yMax, 0.0f), vParentPos, 3.0f);
            }

            // draw element
            DrawElement<T>(node.m_value, rect, 0.0f, cc(node.m_value, 0), sc(node.m_value), null, false);

            // draw children
            if (node.m_left != null)
            {
                DrawBinaryNode(node.m_left, new Vector2(vRange.x, vCenter.x), vNodePos, iDepth + 1, cc, sc, dc);
            }
            if (node.m_right != null)
            {
                DrawBinaryNode(node.m_right, new Vector2(vCenter.x, vRange.y), vNodePos, iDepth + 1, cc, sc, dc);
            }

            // callback
            if (dc != null)
            {
                dc(node, rect);
            }
        }

        #endregion
        */
        #region Drawing Helpers

        public static void DrawText(string txt, Rect destRect, float fSize, Color color, TextAnchor anchor)
        {
            if (sm_textStyle == null || sm_textStyle.font == null)
            {
                sm_textStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
                sm_textStyle.richText = true;
                sm_textStyle.alignment = TextAnchor.MiddleCenter;
                sm_textStyle.font = Resources.Load<Font>("Fonts/Candara");
                sm_textStyle.normal.textColor = Color.white;
                sm_textStyle.onNormal.textColor = Color.white;
                sm_textStyle.fontStyle = FontStyle.Normal;
            }

            sm_textStyle.alignment = anchor;
            sm_textStyle.fontSize = Mathf.RoundToInt(fSize);
            sm_textStyle.normal.textColor = color;
            sm_textStyle.hover.textColor = color;
            GUI.color = color;
            GUI.Label(destRect, txt, sm_textStyle);
        }

        public static void DrawTextAt(string txt, Vector3 vWorldPosition, float fSize, Color color, TextAnchor anchor)
        {
            const float SIZE = 600.0f;

            if (!string.IsNullOrEmpty(txt))
            {
                Vector3 vSP = HandleUtility.WorldToGUIPointWithDepth(vWorldPosition);
                if (vSP.z > 0.0f)
                {
                    Rect sr = new Rect(vSP.x, vSP.y, 0.0f, 0.0f);
                    switch (anchor)
                    {
                        case TextAnchor.UpperLeft:
                            sr = new Rect(vSP.x, vSP.y, SIZE, SIZE);
                            break;
                        case TextAnchor.UpperCenter:
                            sr = new Rect(vSP.x - SIZE * 0.5f, vSP.y, SIZE, SIZE);
                            break;
                        case TextAnchor.UpperRight:
                            sr = new Rect(vSP.x - SIZE, vSP.y, SIZE, SIZE);
                            break;
                        case TextAnchor.MiddleLeft:
                            sr = new Rect(vSP.x, vSP.y - SIZE * 0.5f, SIZE, SIZE);
                            break;
                        case TextAnchor.MiddleCenter:
                            sr = new Rect(vSP.x - SIZE * 0.5f, vSP.y - SIZE * 0.5f, SIZE, SIZE);
                            break;
                        case TextAnchor.MiddleRight:
                            sr = new Rect(vSP.x - SIZE, vSP.y - SIZE * 0.5f, SIZE, SIZE);
                            break;
                        case TextAnchor.LowerLeft:
                            sr = new Rect(vSP.x, vSP.y - SIZE, SIZE, SIZE);
                            break;
                        case TextAnchor.LowerCenter:
                            sr = new Rect(vSP.x - SIZE * 0.5f, vSP.y - SIZE, SIZE, SIZE);
                            break;
                        case TextAnchor.LowerRight:
                            sr = new Rect(vSP.x - SIZE, vSP.y - SIZE, SIZE, SIZE);
                            break;
                    }

                    Handles.BeginGUI();
                    DrawText(txt, sr, fSize, color, anchor);
                    Handles.EndGUI();
                }
            }
        }

        public static void DrawArrow(Vector3 v1, Vector3 v2, float fSize, Color color, float fThickness)
        {
            Vector3 vCamForward = SceneView.currentDrawingSceneView.camera.transform.forward;

            Handles.color = color;
            Handles.DrawLine(v1, v2, fThickness);
            Vector3 vForward = (v2 - v1).normalized;
            Vector3 vUp = Vector3.Cross(vCamForward, vForward).normalized;
            Handles.DrawLine(v2, v2 - vForward * fSize + vUp * fSize, fThickness);
            Handles.DrawLine(v2, v2 - vForward * fSize - vUp * fSize, fThickness);
        }   

        public static void DrawRect(Rect dest, float fZ, Color fill, Color border, float fBorderThickness)
        {
            Vector3[] corners = new Vector3[]
            {
                new Vector3(dest.x, dest.y, fZ),
                new Vector3(dest.x, dest.yMax, fZ),
                new Vector3(dest.xMax, dest.yMax, fZ),
                new Vector3(dest.xMax, dest.y, fZ),
            };

            // fill
            Handles.color = fill;
            Handles.DrawAAConvexPolygon(corners);

            // outline
            Handles.color = border;
            for (int i = 0; i < corners.Length; ++i)
            {
                Handles.DrawLine(corners[i], corners[(i + 1) % corners.Length], fBorderThickness);
            }
        }

        public static void DrawElement<T>(T elem, Rect dest, float fZ, Color color, string txt, string index, bool bDrawIndices = true)
        {
            // draw rect
            DrawRect(dest, fZ, color, Color.black, 2.0f);

            // draw index
            if (bDrawIndices)
            {
                Vector3 vSP = HandleUtility.WorldToGUIPointWithDepth(new Vector3(dest.center.x, dest.yMax, fZ));
                if (vSP.z > 0.0f)
                {
                    Rect sr = new Rect(vSP.x - 300.0f, vSP.y + 3.0f, 600.0f, 300.0f);
                    Handles.BeginGUI();
                    DrawText(index, sr, 12.0f, Color.white, TextAnchor.UpperCenter);
                    Handles.EndGUI();
                }
            }

            // text
            if (!string.IsNullOrEmpty(txt))
            {
                Vector3 vSP = HandleUtility.WorldToGUIPointWithDepth(new Vector3(dest.center.x, dest.center.y, fZ));
                if (vSP.z > 0.0f)
                {
                    Rect sr = new Rect(vSP.x - 300.0f, vSP.y - 300.0f, 600.0f, 600.0f);
                    Handles.BeginGUI();
                    DrawText(txt, sr, 18.0f, Color.black, TextAnchor.MiddleCenter);
                    Handles.EndGUI();
                }
            }
        }

        #endregion
    }
}