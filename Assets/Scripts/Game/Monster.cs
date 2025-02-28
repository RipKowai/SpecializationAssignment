using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game
{
    public class Monster : MonoBehaviour, IPause
    {
        private Animator animator;


        private class MeshData
        {
            public List<Vector3>            m_vertices;
            public List<Vector3>            m_normals;
            public List<Vector2>            m_uv;
            public List<Vector4>            m_tangents;
            public List<BoneWeight>         m_weights;
            public List<int>                m_triangles;
            public List<int[]>              m_borderEdges;
            public Dictionary<int, int>     m_vertexLookup;

            public MeshData(int iVertexCount, int iTriangleCount)
            {
                m_vertices = new List<Vector3>(iVertexCount);
                m_normals = new List<Vector3>(iVertexCount);
                m_uv = new List<Vector2>(iVertexCount);
                m_tangents = new List<Vector4>(iVertexCount);
                m_weights = new List<BoneWeight>(iVertexCount);
                m_vertexLookup = new Dictionary<int, int>(iVertexCount);
                m_borderEdges = new List<int[]>(100);
                m_triangles = new List<int>(iTriangleCount * 3);
            }

            public void AddCap()
            {
                // calculate center
                Vector3 vCenter = Vector3.zero;
                Vector2 vCenterUV = Vector2.zero;
                foreach (int[] edge in m_borderEdges)
                {
                    vCenter += m_vertices[edge[0]];
                    vCenter += m_vertices[edge[1]];
                    vCenterUV += m_uv[edge[0]];
                    vCenterUV += m_uv[edge[1]];
                }
                vCenter /= m_borderEdges.Count * 2;
                vCenterUV /= m_borderEdges.Count * 2;

                // calculate cap normal
                Vector3 vCapUp = Vector3.zero;
                foreach (int[] edge in m_borderEdges)
                {
                    Plane p = new Plane(vCenter, m_vertices[edge[0]], m_vertices[edge[1]]);
                    vCapUp += p.normal;
                }
                vCapUp = Vector3.Normalize(vCapUp);

                // add center vertex
                int iCenterIndex = m_vertices.Count;
                m_vertices.Add(vCenter);
                m_uv.Add(vCenterUV);
                m_normals.Add(vCapUp);
                m_tangents.Add(m_tangents[m_borderEdges[0][0]]);
                m_weights.Add(m_weights[m_borderEdges[0][0]]);

                // create the cap triangles
                foreach (int[] edge in m_borderEdges)
                {
                    m_triangles.Add(iCenterIndex);
                    m_triangles.Add(edge[0]);
                    m_triangles.Add(edge[1]);
                }
            }

            public Mesh ToMesh(string name)
            {
                Mesh mesh = new Mesh();
                mesh.name = name;
                mesh.hideFlags = HideFlags.DontSave;
                mesh.vertices = m_vertices.ToArray();
                mesh.normals = m_normals.ToArray();
                mesh.uv = m_uv.ToArray();
                mesh.tangents = m_tangents.ToArray();
                mesh.boneWeights = m_weights.ToArray();
                mesh.triangles = m_triangles.ToArray();
                mesh.RecalculateBounds();
                mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size * 2.0f);
                return mesh;
            }
        }

        public static List<Monster> AllMonsters = new List<Monster>();

        static GameObject sm_bloodPrefab = null;

        #region Properties

        #endregion

        private void OnEnable()
        {
            AllMonsters.Add(this);

            animator = GetComponent<Animator>();

            Popup.Pause += Pause;
            Popup.UnPause += Unpause;
            
        }

        private void OnDisable()
        {
            AllMonsters.Remove(this);

            Popup.Pause += Pause;
            Popup.UnPause += Unpause;
        }

        protected void EnableRagdoll()
        {
            // enable rigid bodies
            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = false;
            }

            // disable animations
            Animator animator = GetComponent<Animator>();
            animator.enabled = false;
        }

        public void Dismember(Transform bone)
        {            
            SkinnedMeshRenderer smr = transform.Find("Mesh").GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = smr.sharedMesh;

            // split bones into 2 categories (Member vs Body)
            Transform[] bones = smr.bones;
            bool[] memberBones = new bool[bones.Length];
            bool[] bodyBones = new bool[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    bool bIsPartOfMember = bone == bones[i] || bones[i].IsChildOf(bone);
                    memberBones[i] = bIsPartOfMember;
                    bodyBones[i] = !bIsPartOfMember;
                }
            }

            // split mesh
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Vector2[] uv = mesh.uv;
            BoneWeight[] weights = mesh.boneWeights;
            bool[] vertexBelongsToMember = System.Array.ConvertAll(weights, bw => memberBones[bw.boneIndex0]);

            // create target data
            MeshData member = new MeshData(mesh.vertexCount / 2, triangles.Length / 6);
            MeshData body = new MeshData(mesh.vertexCount, triangles.Length / 3);

            // process all triangles
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int iMemberCount = (vertexBelongsToMember[triangles[i + 0]] ? 1 : 0) +
                                   (vertexBelongsToMember[triangles[i + 1]] ? 1 : 0) +
                                   (vertexBelongsToMember[triangles[i + 2]] ? 1 : 0);

                bool bBelongsToMember = iMemberCount >= 2;
                MeshData target = bBelongsToMember ? member : body;

                // add triangle
                int[] triangle = new int[3];
                for (int j = 0; j < 3; ++j)
                {
                    int iOldIndex = triangles[i + j];
                    int iNewIndex;

                    // add vertex?
                    if (!target.m_vertexLookup.TryGetValue(iOldIndex, out iNewIndex))
                    {
                        iNewIndex = target.m_vertices.Count;
                        target.m_vertexLookup[iOldIndex] = iNewIndex;
                        target.m_vertices.Add(vertices[iOldIndex]);
                        target.m_normals.Add(normals[iOldIndex]);
                        target.m_tangents.Add(tangents[iOldIndex]);
                        target.m_uv.Add(uv[iOldIndex]);
                        target.m_weights.Add(weights[iOldIndex]);
                    }

                    target.m_triangles.Add(iNewIndex);
                    triangle[j] = iNewIndex;
                }

                // is this a border triangle?
                if (iMemberCount != 0 && iMemberCount != 3)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        int k = (j + 1) % 3;
                        if ((bBelongsToMember && vertexBelongsToMember[triangles[i + j]] && vertexBelongsToMember[triangles[i + k]]) ||
                            (!bBelongsToMember && !vertexBelongsToMember[triangles[i + j]] && !vertexBelongsToMember[triangles[i + k]]))
                        {
                            target.m_borderEdges.Add(new int[] { triangle[j], triangle[k] });
                        }
                    }
                }
            }

            // set new body mesh
            int iReplacement = System.Array.IndexOf(smr.bones, bone.parent);
            UpdateWeights(body.m_weights, bodyBones, iReplacement);
            body.AddCap();
            Mesh bodyMesh = body.ToMesh("BodyMesh");
            bodyMesh.bindposes = mesh.bindposes;
            smr.sharedMesh = bodyMesh;

            // create renderer for member
            GameObject go = Instantiate(smr.gameObject, smr.transform.parent);
            go.name = bone.name + "_Member";
            smr = go.GetComponent<SkinnedMeshRenderer>();
            iReplacement = System.Array.IndexOf(smr.bones, bone);
            UpdateWeights(member.m_weights, memberBones, iReplacement);
            member.AddCap();
            Mesh memberMesh = member.ToMesh(go.name);
            memberMesh.bindposes = mesh.bindposes;
            smr.sharedMesh = memberMesh;

            // get joint
            CharacterJoint cc = bone.GetComponent<CharacterJoint>();
            if (cc != null)
            {
                Destroy(cc);
            }

            // simulate member bodies
            foreach (Rigidbody rb in bone.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = false;
            }

            // spawn blood
            SpawnBlood(bone, bone.position);
            SpawnBlood(bone.parent, bone.position);

            // release bone
            bone.transform.parent = null;

            // disable animations
            float fAnimatorTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            int nameHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            animator.Rebind();
            animator.Play(nameHash, 0, fAnimatorTime);

            // add some 'umph'?
            Rigidbody boneBody = bone.GetComponent<Rigidbody>();
            boneBody.AddForce(Random.insideUnitSphere * 10.0f, ForceMode.Impulse);

            // death!
            if (bone.name.StartsWith("spine") ||
                bone.name.StartsWith("head") ||
                bone.name.StartsWith("neck"))
            {
                EnableRagdoll();
                Destroy(this);
            }
        }

        protected void UpdateWeights(List<BoneWeight> weights, bool[] validBones, int iReplacementBone)
        {
            for (int i = 0; i < weights.Count; i++)
            {
                BoneWeight bw = weights[i];
                bw.boneIndex0 = validBones[bw.boneIndex0] ? bw.boneIndex0 : iReplacementBone;
                bw.boneIndex1 = validBones[bw.boneIndex1] ? bw.boneIndex1 : iReplacementBone;
                bw.boneIndex2 = validBones[bw.boneIndex2] ? bw.boneIndex2 : iReplacementBone;
                bw.boneIndex3 = validBones[bw.boneIndex3] ? bw.boneIndex3 : iReplacementBone;
                weights[i] = bw;
            }
        }

        protected void SpawnBlood(Transform bone, Vector3 vPosition)
        {
            if (sm_bloodPrefab == null)
            {
                sm_bloodPrefab = Resources.Load<GameObject>("Blood");
            }

            GameObject go = Instantiate(sm_bloodPrefab, bone);
            go.transform.position = vPosition;
            go.transform.parent = bone;

            // TODO: destroy blood effect once it finished
        }

        public void Pause()
        {
            ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Pause();
            }
            animator.speed = 0f;
            Physics.autoSimulation = false;
        }

        public void Unpause()
        {
            ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Play();
            }
            animator.speed = 1f;
            Physics.autoSimulation = true;
        }
    }
}