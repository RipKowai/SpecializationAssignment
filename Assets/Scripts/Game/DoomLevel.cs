using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    [ExecuteInEditMode]
    public partial class DoomLevel : MonoBehaviour
    {
        [System.Serializable]
        public class Segment
        {
            [SerializeField]
            public Vector2      A;

            [SerializeField]
            public Vector2      B;

            #region Properties

            public Vector2 Center => (A + B) * 0.5f;

            public Vector2 Forward => (B - A).normalized;

            public Vector2 Right
            {
                get
                {
                    Vector2 vForward = Forward;
                    return new Vector2(-vForward.y, vForward.x);
                }
            }

            public float Length => Vector2.Distance(A, B);

            public Plane Plane => new Plane(Right, A);

            #endregion

            public bool Contains(Vector2 v)
            {
                return Vector2.Distance(v, A) < 0.001f ||
                       Vector2.Distance(v, B) < 0.001f;
            }

            public int GetSign(Vector2 p)
            {
                Vector2 vToP = (p - A).normalized;

                // is on plane?
                if (Vector2.Distance(p, A) < 0.001f || Mathf.Abs(Vector2.Dot(vToP, Forward)) > 0.999999999f)
                {
                    return 0;
                }

                // in front or in back?
                return Vector2.Dot(vToP, Right) > 0.0f ? 1 : -1;
            }
        }

        public class Node : Segment
        {
            public Node m_left;
            public Node m_right;

            #region Properties

            public int Depth => CalculateDepth(this);

            #endregion

            public static int CalculateDepth(Node node)
            {
                if (node == null)
                {
                    return 0;
                }

                return 1 + Mathf.Max(CalculateDepth(node.m_left), CalculateDepth(node.m_right));
            }
        }

        [SerializeField]
        public int                  m_iLevelSeed = 0;

        public List<Segment>        m_segments = new List<Segment>();

        private Node                m_root;
        private static DoomLevel    sm_instance;

        #region Properties

        public Node Root => m_root;

        public static DoomLevel Instance => sm_instance;

        #endregion

        private void OnEnable()
        {
            m_root = CreateBinarySpacePartitioningTree(m_iLevelSeed);
            sm_instance = this;
            CalculateBounds();
        }

        private void OnDisable()
        {
            sm_instance = (sm_instance == this ? null : sm_instance);
        }

        public Node CreateBinarySpacePartitioningTree(int iSeed)
        {
            Random.InitState(iSeed);
            List<Segment> remaingSegments = new List<Segment>(m_segments);
            return CreateNode(remaingSegments);
        }

        protected Node CreateNode(List<Segment> remainingSegments)
        {
            // do we have remaing segments?
            remainingSegments.RemoveAll(s => s.Length < 0.1f);      // THIS WAS FIXED
            if (remainingSegments.Count == 0)
            {
                return null;
            }

            // pick splitter segment
            //Segment segment = remainingSegments[remainingSegments.Count / 2];             // 1st attempt
            Segment segment = remainingSegments[Random.Range(0, remainingSegments.Count)];  // 2nd attempt
            remainingSegments.Remove(segment);

            // create node
            Node node = new Node { A = segment.A, B = segment.B };

            // split remaining segments into 2 groups
            List<Segment> frontSegments = new List<Segment>();
            List<Segment> backSegments = new List<Segment>();
            foreach (Segment s in remainingSegments)
            {
                SplitSegment(node, s, frontSegments, backSegments);
            }

            // create children
            node.m_left = CreateNode(backSegments);
            node.m_right = CreateNode(frontSegments);

            return node;
        }

        protected void SplitSegment(Node node, Segment segment, List<Segment> frontSegments, List<Segment> backSegments)
        {
            int iSignA = node.GetSign(segment.A);
            int iSignB = node.GetSign(segment.B);

            if ((iSignA == 1 && iSignB == 1) ||
                (iSignA == 0 && iSignB == 1) ||
                (iSignA == 1 && iSignB == 0) ||
                (iSignA == 0 && iSignB == 0))
            {
                frontSegments.Add(segment);
                return;
            }
            else if ((iSignA == -1 && iSignB == -1) ||
                     (iSignA == 0 && iSignB == -1) ||
                     (iSignA == -1 && iSignB == 0))
            {
                backSegments.Add(segment);
                return;
            }
            else if (iSignA != iSignB)
            {
                // THIS WAS FIXED: - segment.Forward * 0.001f
                Ray ray = new Ray(segment.A - segment.Forward * 0.001f, segment.Forward);
                float fEnter;

                if (node.Plane.Raycast(ray, out fEnter) ||
                    node.Plane.flipped.Raycast(ray, out fEnter))
                {
                    Vector2 vSplit = segment.A + segment.Forward * fEnter;
                    Segment splitA = new Segment { A = segment.A, B = vSplit };
                    Segment splitB = new Segment { A = vSplit, B = segment.B };
                    backSegments.Add(iSignA < 0 ? splitA : splitB);
                    frontSegments.Add(iSignA > 0 ? splitA : splitB);
                }
                else
                {
                    Debug.Log("Failed to split segment");
                }

                return;
            }

            throw new System.Exception("Should not happen");
        }

        int CalculateTreePenalty(Node node)
        {
            if (node == null)
            {
                return 0;
            }

            int iLeftDepth = Node.CalculateDepth(node.m_left);
            int iRightDepth = Node.CalculateDepth(node.m_right);
            int iDifference = Mathf.Abs(iLeftDepth - iRightDepth);

            return 1 + iDifference + CalculateTreePenalty(node.m_left) + CalculateTreePenalty(node.m_right);
        }

        public void CalculateBalancedTree()
        {
            int iBestPenalty = int.MaxValue;
            int iBestSeed = -1;
            for (int iSeed = 0; iSeed < 10000; ++iSeed)
            {
                Node tempTree = CreateBinarySpacePartitioningTree(iSeed);
                int iTreePenalty = CalculateTreePenalty(tempTree);

                if (iTreePenalty < iBestPenalty)
                {
                    iBestPenalty = iTreePenalty;
                    iBestSeed = iSeed;
                }
            }

            Debug.Log("Best Seed: " + iBestSeed);
        }
    }
}