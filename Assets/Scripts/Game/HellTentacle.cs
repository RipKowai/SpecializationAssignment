using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class HellTentacle : ProceduralMesh
    {
        [SerializeField, Range(2, 20)]
        public int                  m_iNumBones = 4;

        [SerializeField, Range(1.0f, 10.0f)]
        public float                m_fTentacleLength = 3.0f;

        [SerializeField, Range(1, 5)]
        public int                  m_iNumRowsPerBone = 2;

        [SerializeField, Range(3, 32)]
        public int                  m_iNumSegments = 6;

        [SerializeField]
        public AnimationCurve       m_radiusCurve = new AnimationCurve();

        [SerializeField]
        public Color                m_color = Color.red;

        private Transform           m_root;
        private List<Transform>     m_bones = new List<Transform>();
        private List<Transform>     m_tentacleBones = new List<Transform>();

        protected override void Start()
        {
            base.Start();

            if (Application.isPlaying)
            {
                foreach (Transform tb in m_tentacleBones)
                {
                    StartCoroutine(TentacleChainAnimation(tb));
                }
            }
        }

        protected override Mesh CreateMesh()
        {
            // add rows
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<BoneWeight> weights = new List<BoneWeight>();
            List<int> triangles = new List<int>();

            List<Vector3> points = GenerateUniformPoints(9, 1.0f);
            for (int i = 0; i < points.Count; ++i)
            {
                AddTentacle("Tentacle" + (i + 1).ToString("00"), vertices, colors, weights, triangles);
            }

            // create mesh
            Mesh mesh = new Mesh();
            mesh.name = "TentacleMesh";
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = vertices.ToArray();
            mesh.colors = colors.ToArray();
            mesh.boneWeights = weights.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.bindposes = m_bones.ConvertAll(b => b.worldToLocalMatrix).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // place tentacles
            for(int i=0; i<m_tentacleBones.Count && i<points.Count; ++i)
            {
                m_tentacleBones[i].localPosition = points[i];
                m_tentacleBones[i].rotation = Quaternion.LookRotation(points[i]);
            }

            return mesh;
        }

        public override void UpdateMesh()
        {
            // destroy old root
            Transform root = transform.Find("Root");
            if (root != null)
            {
                root.transform.parent = null;
                if (Application.isPlaying)
                {
                    Destroy(root.gameObject);
                }
                else
                {
                    DestroyImmediate(root.gameObject);
                }
            }

            // create root bone
            m_tentacleBones.Clear();
            m_bones.Clear();
            GameObject go = new GameObject("Root");
            go.transform.parent = transform;
            m_root = go.transform;
            m_bones.Add(m_root);

            // create mesh
            base.UpdateMesh();

            // assign bones
            SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
            smr.bones = m_bones.ToArray();
            smr.rootBone = m_root;
        }

        private void OnDrawGizmos()
        {
            if (m_bones != null && m_bones.Count > 0)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < m_bones.Count; ++i)
                {
                    Gizmos.matrix = m_bones[i].localToWorldMatrix;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one * 0.1f);
                    Gizmos.matrix = Matrix4x4.identity;

                    if (i > 0)
                    {
                        Gizmos.DrawLine(m_bones[i].position, m_bones[i - 1].position);
                    }
                }
            }
        }

        public static List<Vector3> GenerateUniformPoints(int pointCount, float radius = 1.0f)
        {
            List<Vector3> points = new List<Vector3>();

            float goldenRatio = (1 + Mathf.Sqrt(5)) / 2; // Golden ratio
            float angleIncrement = 2 * Mathf.PI / goldenRatio;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / pointCount; // Normalized index (0 to 1)
                float inclination = Mathf.Acos(1 - 2 * t); // Polar angle (0 to π)
                float azimuth = angleIncrement * i;       // Azimuthal angle (0 to 2π)

                // Convert spherical coordinates to Cartesian coordinates
                float x = radius * Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = radius * Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = radius * Mathf.Cos(inclination);

                points.Add(new Vector3(x, y, z));
            }

            return points;
        }

        #region Skeleton Creation

        protected List<Transform> CreateTentacleBoneChain(string name)
        {
            // create bone chain
            Transform parent = m_root;
            float fBoneStep = m_fTentacleLength / (m_iNumBones - 1);
            List<Transform> boneChain = new List<Transform>();
            for (int i = 0; i < m_iNumBones; i++)
            {
                GameObject go = new GameObject(i == 0 ? name : "Bone" + i.ToString("00"));
                go.transform.parent = parent;
                parent = go.transform;
                go.transform.position = i * fBoneStep * Vector3.up;
                m_bones.Add(go.transform);
                boneChain.Add(go.transform);
            }

            return boneChain;
        }

        #endregion

        #region Mesh Creation

        protected BoneWeight GetWeightAt(List<Transform> boneChain, Vector3 vCenter)
        {
            float fBoneStep = m_fTentacleLength / (m_iNumBones - 1);
            float fInfluence = fBoneStep * 1.2f;
            List<Transform> bonesInRange = boneChain.FindAll(b => Vector3.Distance(b.position, vCenter) < fInfluence);

            if (bonesInRange.Count > 4)
            {
                throw new System.Exception("Auch");
            }

            List<float> boneInfluences = bonesInRange.ConvertAll(b => 1.0f - Mathf.Clamp01(Vector3.Distance(vCenter, b.position) / fInfluence));
            float fTotalInfluence = 0.0f;
            foreach (float f in boneInfluences)
            {
                fTotalInfluence += f;
            }
            boneInfluences = boneInfluences.ConvertAll(f => f / fTotalInfluence);

            return new BoneWeight
            {
                boneIndex0 = bonesInRange.Count > 0 ? m_bones.IndexOf(bonesInRange[0]) : 0,
                weight0 = boneInfluences.Count > 0 ? boneInfluences[0] : 0.0f,

                boneIndex1 = bonesInRange.Count > 1 ? m_bones.IndexOf(bonesInRange[1]) : 0,
                weight1 = boneInfluences.Count > 1 ? boneInfluences[1] : 0.0f,

                boneIndex2 = bonesInRange.Count > 2 ? m_bones.IndexOf(bonesInRange[2]) : 0,
                weight2 = boneInfluences.Count > 2 ? boneInfluences[2] : 0.0f,

                boneIndex3 = bonesInRange.Count > 3 ? m_bones.IndexOf(bonesInRange[3]) : 0,
                weight3 = boneInfluences.Count > 3 ? boneInfluences[3] : 0.0f
            };
        }

        protected Transform AddTentacle(string name, List<Vector3> vertices, List<Color> colors, List<BoneWeight> weights, List<int> triangles)
        {
            List<Transform> boneChain = CreateTentacleBoneChain(name);

            int iStart = vertices.Count;
            int iNumRows = (m_iNumBones - 1) * m_iNumRowsPerBone + 1;
            for (int i = 0; i < boneChain.Count - 1; ++i)
            {
                Transform b1 = boneChain[i];
                Transform b2 = boneChain[i + 1];

                for (int iRow = 0; iRow < m_iNumRowsPerBone; ++iRow)
                {
                    float f = iRow / (float)m_iNumRowsPerBone;
                    float fRadius = m_radiusCurve.Evaluate((i * m_iNumRowsPerBone + iRow) / (float)iNumRows);
                    Vector3 vCenter = Vector3.Lerp(b1.position, b2.position, f);
                    BoneWeight bw = GetWeightAt(boneChain, vCenter);
                    AddRow(vCenter, fRadius, bw, vertices, colors, weights);
                }
            }
            AddRow(m_bones[m_bones.Count - 1].position, 0.0f, GetWeightAt(boneChain, Vector3.up * m_fTentacleLength), vertices, colors, weights);

            // create triangles
            for (int i = 0; i < iNumRows - 1; ++i)
            {
                for (int j = 0; j < m_iNumSegments; ++j)
                {
                    triangles.AddRange(new int[]
                    {
                        iStart + (i + 0) * m_iNumSegments + (j + 0) % m_iNumSegments,
                        iStart + (i + 1) * m_iNumSegments + (j + 0) % m_iNumSegments,
                        iStart + (i + 1) * m_iNumSegments + (j + 1) % m_iNumSegments,
                        iStart + (i + 0) * m_iNumSegments + (j + 0) % m_iNumSegments,
                        iStart + (i + 1) * m_iNumSegments + (j + 1) % m_iNumSegments,
                        iStart + (i + 0) * m_iNumSegments + (j + 1) % m_iNumSegments,
                    });
                }
            }

            m_tentacleBones.Add(boneChain[0]);
            return boneChain[0];
        }

        protected void AddRow(Vector3 vCenter, float fRadius, BoneWeight bw, List<Vector3> vertices, List<Color> colors, List<BoneWeight> weights)
        {
            Color insideColor = Color.Lerp(m_color, Color.white, 0.65f);

            for (int i = 0; i < m_iNumSegments; ++i)
            {
                float fAngle = (i / (float)m_iNumSegments) * Mathf.PI * 2.0f;
                float fFlattenAmount = Mathf.Clamp01(Mathf.Abs(fAngle - Mathf.PI) / Mathf.PI);
                float fFlattenedRadius = fRadius * Mathf.Clamp01(fFlattenAmount + 0.5f);

                vertices.Add(vCenter + new Vector3(Mathf.Cos(fAngle), 0.0f, Mathf.Sin(fAngle)) * fFlattenedRadius); ;
                colors.Add(Color.Lerp(insideColor, m_color, Mathf.Clamp01(fFlattenAmount * 2.0f)));
                weights.Add(bw);
            }
        }

        protected void CreateSphere()
        {
            Mesh sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        }

        #endregion


        #region Animation

        protected IEnumerator TentacleChainAnimation(Transform firstBone)
        {
            // get bone chain
            List<Transform> bones = new List<Transform>();
            Transform b = firstBone;
            while (b != null)
            {
                bones.Add(b);
                b = b.childCount > 0 ? b.GetChild(0) : null;
            }

            while (true)
            {
                for (int i = 0; i < bones.Count; i++)
                {
                    float fAngleX = GetAngleAtTime(i * 0.9f, 2.2f, 7.0f) + GetAngleAtTime(i * 1.0f, 3.5f, 4.0f);
                    float fAngleZ = GetAngleAtTime(-i * 1.2f, 2.0f, 8.0f) + GetAngleAtTime(i * 1.77f, 3.2f, 3.0f);
                    bones[i].localEulerAngles = new Vector3(fAngleX, 0.0f, fAngleZ);
                }

                yield return null;
            }
        }

        float GetAngleAtTime(float fTimeOffset, float fSpeed, float fAmplitude)
        {
            return Mathf.Sin(Time.time * fSpeed + fTimeOffset) * fAmplitude;
        }

        #endregion
    }
}