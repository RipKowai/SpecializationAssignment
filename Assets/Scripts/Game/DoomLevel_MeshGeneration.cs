using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Game
{
    public partial class DoomLevel
    {
        [SerializeField]
        public Material[]       m_materials;

        [SerializeField]
        public Material         m_floor;

        [SerializeField]
        public Material         m_ceiling;

        private HashSet<Node>   m_lastDrawNodes = null;
        private Transform       m_levelGeometry;
        private Mesh            m_mesh;
        private RectInt         m_bounds;

        private const float     CEILING_HEIGHT = 3.0f;

        #region Properties

        public HashSet<Node> VisibleNodes => m_lastDrawNodes;

        public Transform LevelGeometry
        {
            get
            {
                if (m_levelGeometry == null)
                {
                    m_levelGeometry = transform.Find("LevelGeometry");
                    if (m_levelGeometry == null)
                    {
                        List<Material> materials = new List<Material>(m_materials);
                        materials.Add(m_floor);
                        materials.Add(m_ceiling);

                        GameObject go = new GameObject("LevelGeometry");
                        go.transform.parent = transform;
                        go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                        go.AddComponent<MeshFilter>();
                        go.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                        go.AddComponent<MeshCollider>();
                        m_levelGeometry = go.transform;
                    }
                }

                return m_levelGeometry;
            }
        }

        #endregion

        public void UpdateLevel(Player player)
        {
            float fFOV = player.Camera.fieldOfView;

            // draw out player position
            Vector2 vPlayerPos = new Vector2(player.transform.position.x, player.transform.position.z);
            Debug.DrawLine(vPlayerPos + new Vector2(-1.0f, -1.0f), vPlayerPos + new Vector2(1.0f, 1.0f), Color.red);
            Debug.DrawLine(vPlayerPos + new Vector2(1.0f, -1.0f), vPlayerPos + new Vector2(-1.0f, 1.0f), Color.red);

            // calculate frustum planes
            Vector2 vPlayerRight = new Vector2(player.transform.right.x, player.transform.right.z).normalized;
            Vector2 vMinDir = Quaternion.Euler(0.0f, 0.0f, -fFOV) * -vPlayerRight;
            Vector2 vMaxDir = Quaternion.Euler(0.0f, 0.0f, fFOV) * vPlayerRight;

            Plane[] frustum = new Plane[] 
            {
                new Plane(vMinDir, vPlayerPos),
                new Plane(vMaxDir, vPlayerPos),
            };

            // gather visible nodes
            HashSet<Node> visibleNodes = new HashSet<Node>();
            GetVisibleSegments(m_root, vPlayerPos, frustum, visibleNodes);

            // got a change?
            if (m_lastDrawNodes != null && m_lastDrawNodes.SetEquals(visibleNodes))
            {
                return;
            }

            m_lastDrawNodes = visibleNodes;

            // gather mesh data
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int>[] triangles = System.Array.ConvertAll(m_materials, m => new List<int>());
            foreach (Node node in visibleNodes)
            {
                AddNodeQuad(node, vertices, uv, triangles);   
            }

            if (m_mesh == null)
            {
                m_mesh = new Mesh();
                m_mesh.name = "DoomLevel";
                m_mesh.hideFlags = HideFlags.DontSave;
                m_mesh.MarkDynamic();
            }

            // add floor & ceiling
            Vector3[] floorVerts = new Vector3[]
            {
                    new Vector3(m_bounds.min.x, 0.0f, m_bounds.min.y),
                    new Vector3(m_bounds.min.x, 0.0f, m_bounds.max.y),
                    new Vector3(m_bounds.max.x, 0.0f, m_bounds.max.y),
                    new Vector3(m_bounds.max.x, 0.0f, m_bounds.min.y)
            };
            int iFloorStart = vertices.Count;
            vertices.AddRange(floorVerts);
            int iCeilingStart = vertices.Count;
            vertices.AddRange(System.Array.ConvertAll(floorVerts, v => v + Vector3.up * CEILING_HEIGHT));
            uv.AddRange(System.Array.ConvertAll(floorVerts, v => new Vector2(v.x, v.z)));
            uv.AddRange(System.Array.ConvertAll(floorVerts, v => new Vector2(v.x, v.z)));

            m_mesh.Clear();
            m_mesh.subMeshCount = m_materials.Length + 2;
            m_mesh.vertices = vertices.ToArray();
            m_mesh.uv = uv.ToArray();

            for (int i = 0; i < triangles.Length; ++i)
            {
                m_mesh.SetTriangles(triangles[i].ToArray(), i);
            }
            m_mesh.SetTriangles(new int[] { iFloorStart + 0, iFloorStart + 1, iFloorStart + 2, iFloorStart + 0, iFloorStart + 2, iFloorStart + 3 }, m_materials.Length);
            m_mesh.SetTriangles(new int[] { iCeilingStart + 0, iCeilingStart + 2, iCeilingStart + 1, iCeilingStart + 0, iCeilingStart + 3, iCeilingStart + 2 }, m_materials.Length + 1);

            m_mesh.RecalculateBounds();
            m_mesh.RecalculateNormals();

            // assign mesh
            LevelGeometry.GetComponent<MeshFilter>().mesh = m_mesh;
            LevelGeometry.GetComponent<MeshCollider>().sharedMesh = m_mesh;
        }

        protected void GetVisibleSegments(Node node, Vector3 vCameraPos, Plane[] frustum, HashSet<Node> visibleNodes)
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
                }
            }

            GetVisibleSegments(node.m_left, vCameraPos, frustum, visibleNodes);
            GetVisibleSegments(node.m_right, vCameraPos, frustum, visibleNodes);
        }

        protected void AddNodeQuad(Node node, List<Vector3> vertices, List<Vector2> uv, List<int>[] triangles)
        {
            int iStart = vertices.Count;
            Vector3 vA = new Vector3(node.A.x, 0.0f, node.A.y);
            Vector3 vB = new Vector3(node.B.x, 0.0f, node.B.y);
            Vector3 vUp = Vector3.up * CEILING_HEIGHT;
            Vector3 vRight = Vector3.Normalize(vB - vA);

            // calculate segment material
            Vector2Int v = new Vector2Int(Mathf.RoundToInt(node.Center.x),
                                          Mathf.RoundToInt(node.Center.y));
            int iMaterial = Mathf.Abs(v.GetHashCode()) % triangles.Length;

            // add verts & triangles
            Vector3[] verts = new Vector3[] { vA, vA + vUp, vB + vUp, vB };
            vertices.AddRange(verts);
            uv.AddRange(System.Array.ConvertAll(verts, v => new Vector2(Vector3.Dot(v, vRight), Vector3.Dot(v, Vector3.up))));
            triangles[iMaterial].AddRange(new int[] { iStart + 0, iStart + 2, iStart + 1, iStart + 0, iStart + 3, iStart + 2 });
        }

        protected void CalculateBounds()
        {
            Vector2Int vMin = new Vector2Int(-1, -1);
            Vector2Int vMax = new Vector2Int(1, 1);
            foreach (Segment s in m_segments)
            {
                vMin.x = Mathf.Min(Mathf.FloorToInt(s.A.x), vMin.x);
                vMin.x = Mathf.Min(Mathf.FloorToInt(s.B.x), vMin.x);
                vMin.y = Mathf.Min(Mathf.FloorToInt(s.A.y), vMin.y);
                vMin.y = Mathf.Min(Mathf.FloorToInt(s.B.y), vMin.y);

                vMax.x = Mathf.Max(Mathf.CeilToInt(s.A.x), vMax.x);
                vMax.x = Mathf.Max(Mathf.CeilToInt(s.B.x), vMax.x);
                vMax.y = Mathf.Max(Mathf.CeilToInt(s.A.y), vMax.y);
                vMax.y = Mathf.Max(Mathf.CeilToInt(s.B.y), vMax.y);
            }

            m_bounds = new RectInt(vMin.x, vMin.y, vMax.x - vMin.x, vMax.y - vMin.y);
        }
    }
}