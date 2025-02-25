using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Web;
using DSA;
using UnityEditor.Animations;
using UnityEngine.UIElements;

namespace Game
{
    [CustomEditor(typeof(DoomLevel))]
    public class DoomLevelEditor : Editor
    {
        private Gradient                m_valueColors;
        private Pose                    m_player;
        private int                     m_passiveControl;
        private DoomLevel.Segment       m_newSegment = null;
        private DoomLevel.Node          m_selectedNode = null;
        private Vector2                 m_vTreePosition;

        private static readonly Color   COLOR_GRAY = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color   COLOR_SELECTED = new Color(0.4f, 0.4f, 1.0f);
        private static readonly Color   COLOR_LEFT = new Color(1.0f, 0.4f, 0.4f);
        private static readonly Color   COLOR_RIGHT = new Color(0.4f, 1.0f, 0.4f);

        private const float             PLAYER_CAMERA_ANGLE = 45.0f;
        private const float             PLAYER_NEAR_PLANE = 0.1f;
        private const float             PLAYER_FAR_PLANE = 30.0f;

        private void OnEnable()
        {
            m_valueColors = new Gradient();
            m_valueColors.colorKeys = new GradientColorKey[]{
                new GradientColorKey(Color.red, 0.0f),
                new GradientColorKey(new Color(1.0f, 0.5f, 0.0f), 0.2f),
                new GradientColorKey(Color.yellow, 0.4f),
                new GradientColorKey(Color.green, 0.6f),
                new GradientColorKey(Color.blue, 0.8f),
                new GradientColorKey(new Color(1.0f, 0.0f, 1.0f), 1.0f)
            };

            m_passiveControl = GUIUtility.GetControlID(FocusType.Passive);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DoomLevel lvl = target as DoomLevel;

            #if true
            GUILayout.Space(10);
            if (GUILayout.Button("Create Balanced Tree"))
            {
                lvl.CalculateBalancedTree();
            }
            #endif
        }

        private void OnSceneGUI()
        {
            Tools.current = Tool.None;
            DoomLevel lvl = target as DoomLevel;

            #if true
            // draw level segments 
            for (int i = 0; i < lvl.m_segments.Count; ++i)
            {
                DoomLevel.Segment segment = lvl.m_segments[i];
                DrawSegment(segment, -1, new Color(0.0f, 0.0f, 0.0f), 3.0f);
            }
            #endif

            #if false
            // edit player
            HashSet<DoomLevel.Node> visibleNodes = new HashSet<DoomLevel.Node>();
            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0, m_player.position, Quaternion.identity, 0.6f, EventType.Repaint);
            m_player.rotation = m_player.rotation.normalized;
            m_player.position = Handles.DoPositionHandle(m_player.position, m_player.rotation);
            m_player.rotation = Handles.DoRotationHandle(m_player.rotation, m_player.position).normalized;
            m_player.position.z = 0.0f;

            // draw frustum
            Vector3 vMinDir = Quaternion.Euler(0.0f, 0.0f, -PLAYER_CAMERA_ANGLE) * m_player.rotation * Vector3.up;
            Vector3 vMaxDir = Quaternion.Euler(0.0f, 0.0f, PLAYER_CAMERA_ANGLE) * m_player.rotation * Vector3.up;
            float fNearLength = PLAYER_NEAR_PLANE / Mathf.Cos(PLAYER_CAMERA_ANGLE * Mathf.Deg2Rad);
            float fFarLength = PLAYER_FAR_PLANE / Mathf.Cos(PLAYER_CAMERA_ANGLE * Mathf.Deg2Rad);

            Vector3[] frustumPoints = new Vector3[]
            {
                m_player.position + vMinDir * fNearLength,
                m_player.position + vMinDir * fFarLength,
                m_player.position + vMaxDir * fFarLength,
                m_player.position + vMaxDir * fNearLength
            };
            Handles.color = new Color(0.0f, 1.0f, 1.0f, 0.2f);
            Handles.DrawAAConvexPolygon(frustumPoints);

            // draw non-culled segments
            Plane min = new Plane(Quaternion.Euler(0.0f, 0.0f, -PLAYER_CAMERA_ANGLE) * m_player.rotation * -Vector3.right, m_player.position);
            Plane max = new Plane(Quaternion.Euler(0.0f, 0.0f, PLAYER_CAMERA_ANGLE) * m_player.rotation * Vector3.right, m_player.position);
            Plane near = new Plane(m_player.up, m_player.position + m_player.up * PLAYER_NEAR_PLANE);
            Plane far = new Plane(-m_player.up, m_player.position + m_player.up * PLAYER_FAR_PLANE);
            Plane[] frustum = new Plane[] { min, max, near, far };
            DrawPlayerNodeSegments(lvl.Root, m_player.position, frustum, visibleNodes);

            // update level
            Vector3 vCameraPos = new Vector3(m_player.position.x, 1.6f, m_player.position.y);
            Quaternion qCameraRot = Quaternion.LookRotation(new Vector3(m_player.up.x, 0.0f, m_player.up.y)).normalized;
            lvl.UpdateLevel(visibleNodes, vCameraPos, qCameraRot);

            #else
            // draw BSP segments
            if (Application.isPlaying && lvl.VisibleNodes != null)
            {
                DrawNodeSegments(lvl.Root, lvl.Root == m_selectedNode ? COLOR_SELECTED : m_selectedNode == null ? new Color(1.0f, 0.5f, 0.0f) : COLOR_GRAY);
            }
            #endif

            // draw BSP tree
            #if false
            if (lvl.Root != null)
            {
                if (m_selectedNode == lvl.Root)
                {
                    m_vTreePosition = Handles.DoPositionHandle(m_vTreePosition, Quaternion.identity);
                }

                Handles.matrix = Matrix4x4.Translate(m_vTreePosition);
                int iMaxDepth = lvl.Root.Depth;
                float fSize = Mathf.Pow(iMaxDepth, 2) * 0.5f;
                DrawTree(lvl.Root, new Vector2(-fSize, fSize), Vector2.zero, 0, lvl.Root == m_selectedNode ? COLOR_SELECTED : COLOR_GRAY);
                Handles.matrix = Matrix4x4.identity;
            }
            #endif

            #if true
            // ray cast against ground plane
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Plane ground = new Plane(Vector3.forward, Vector3.zero);
            float fEnter;
            if (ground.Raycast(ray, out fEnter))
            {
                // draw mouse coord
                Vector3 vMousePos = ray.origin + ray.direction * fEnter;
                Vector2Int mouseCoord = new Vector2Int(Mathf.RoundToInt(vMousePos.x), Mathf.RoundToInt(vMousePos.y));
                Handles.color = Color.red;
                Handles.SphereHandleCap(0, (Vector2)mouseCoord, Quaternion.identity, 0.2f, EventType.Repaint);

                // add/remove segment?
                if (Event.current.type == EventType.MouseDown && 
                    !Event.current.alt && 
                    Event.current.button != 2)
                {                    
                    Event.current.Use();
                    GUIUtility.hotControl = m_passiveControl;

                    if (Event.current.button == 0)
                    {
                        // add segments
                        if (Event.current.control)
                        {
                            if (m_newSegment == null)
                            {
                                m_newSegment = new DoomLevel.Segment { A = mouseCoord };
                            }
                            else if (mouseCoord != m_newSegment.A)
                            {
                                m_newSegment.B = mouseCoord;
                                lvl.m_segments.Add(m_newSegment);
                                m_newSegment = null;
                                EditorUtility.SetDirty(lvl);
                            }
                        }
                    }
                    else if (Event.current.button == 1)
                    {
                        // remove segments
                        int iCount = lvl.m_segments.Count;
                        lvl.m_segments.RemoveAll(s => s.Contains(mouseCoord));
                        if (iCount != lvl.m_segments.Count)
                        {
                            EditorUtility.SetDirty(lvl);
                        }
                    }

                }

                // draw new segement?
                if(!Event.current.control)
                {
                    m_newSegment = null;
                }
                else if (m_newSegment != null)
                {
                    m_newSegment.B = mouseCoord;
                    DrawSegment(m_newSegment, -1, new Color(1.0f, 0.5f, 0.0f), 5.0f);
                }
            }
            #endif

            SceneView.RepaintAll();
            Handles.matrix = Matrix4x4.identity;
        }

        #if true
        protected void DrawSegment(DoomLevel.Segment segment, int iIndex, Color color, float fThickness)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, segment.A, Quaternion.identity, 0.2f, EventType.Repaint);
            Handles.SphereHandleCap(0, segment.B, Quaternion.identity, 0.2f, EventType.Repaint);
            Handles.DrawLine(segment.A, segment.B, fThickness);
            DSAEditorUtils.DrawArrow(segment.Center, segment.Center + segment.Right, 0.5f, color, fThickness * 0.75f);

            if (iIndex >= 0)
            {
                DSAEditorUtils.DrawTextAt(iIndex.ToString(), segment.Center - segment.Right * 0.5f, 14.0f, Color.white, TextAnchor.MiddleCenter);
            }
        }
        #endif

        #if true
        protected void DrawNodeSegments(DoomLevel.Node node, Color color)
        {
            if (node == null)
            {
                return;
            }

            bool bSelected = node == m_selectedNode;
            //color = bSelected ? COLOR_SELECTED : color;
            color = (target as DoomLevel).VisibleNodes.Contains(node) ? COLOR_SELECTED : COLOR_GRAY;
            DrawSegment(node, -1, color, 2.0f);
            DrawNodeSegments(node.m_left, bSelected ? COLOR_LEFT : color);
            DrawNodeSegments(node.m_right, bSelected ? COLOR_RIGHT : color);
        }
        #endif

        #if false
        protected void DrawPlayerNodeSegments(DoomLevel.Node node, Vector3 vCameraPos, Plane[] frustum, HashSet<DoomLevel.Node> visibleNodes)
        {
            if (node == null)
            {
                return;
            }

            // facing camera?
            Vector3 vToCamera = Vector3.Normalize(vCameraPos - (Vector3)node.Center);
            if (Vector3.Dot(vToCamera, node.Right) > 0.0f)
            {
                // in frustum?
                bool bInFrustum = System.Array.FindIndex(frustum, f => !f.GetSide(node.A) && !f.GetSide(node.B)) < 0;
                if (bInFrustum)
                {
                    visibleNodes.Add(node);
                    DrawSegment(node, -1, Color.green, 2.0f);
                }
            }

            DrawPlayerNodeSegments(node.m_left, vCameraPos, frustum, visibleNodes);
            DrawPlayerNodeSegments(node.m_right, vCameraPos, frustum, visibleNodes);
        }
        #endif

        #if true
        void DrawTree(DoomLevel.Node node, Vector2 vRange, Vector2 vParentPos, int iDepth, Color color)
        {
            Vector2 vCenter = new Vector2((vRange.x + vRange.y) * 0.5f, iDepth * -3.0f);
            Rect rect = new Rect(vCenter.x - 0.5f, vCenter.y - 0.5f, 1.0f, 1.0f);
            Vector2 vNodePos = new Vector2(rect.center.x, rect.y);
            bool bSelected = node == m_selectedNode;
            color = bSelected ? COLOR_SELECTED : color;

            // draw parent link
            if (iDepth > 0)
            {
                Handles.color = Color.black;
                Handles.DrawLine(new Vector3(rect.center.x, rect.yMax, 0.0f), vParentPos, 3.0f);
            }

            // draw element
            DSAEditorUtils.DrawElement(0, rect, 0.0f, color, "", null, false);

            // select/deselect node?
            Handles.color = Color.clear;
            if (Handles.Button(rect.center, Quaternion.identity, 1.0f, 1.0f, Handles.CubeHandleCap))
            {
                m_selectedNode = m_selectedNode == node ? null : node;
            }

            // draw children
            if (node.m_left != null)
            {
                DrawTree(node.m_left, new Vector2(vRange.x, vCenter.x), vNodePos, iDepth + 1, bSelected ? COLOR_LEFT : color);
            }
            if (node.m_right != null)
            {
                DrawTree(node.m_right, new Vector2(vCenter.x, vRange.y), vNodePos, iDepth + 1, bSelected ? COLOR_RIGHT : color);
            }
        }
        #endif
    }
}