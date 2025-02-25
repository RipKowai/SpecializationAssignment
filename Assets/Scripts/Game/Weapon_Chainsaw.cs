using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Weapon_Chainsaw : Weapon
    {
        private float       m_fChainSpeed;
        private float       m_fChainOffset;
        private Material    m_chainMaterial;
        private Transform   m_blade;

        private const float IDLE_SPEED = 1.0f;
        private const float ATTACK_SPEED = 10.0f;

        static Collider[]   sm_colliders = new Collider[20];

        #region Properties

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

            m_chainMaterial = GetComponentInChildren<MeshRenderer>().materials[2];
            m_blade = transform.Find("Chainsaw/Blade");
        }

        private void Update()
        {
            // update chain offset
            m_fChainOffset += m_fChainSpeed * Time.deltaTime;
            m_chainMaterial.SetTextureOffset("_MainTex", new Vector2(-m_fChainOffset, 0.0f));
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Popup.IsPaused)
            {
                m_fChainSpeed = 0f;
                return;
            }

            // attack?
            bool bAttack = Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.RightControl);
            Animator.SetBool("Attack", bAttack);

            // update chain speed
            m_fChainSpeed = Mathf.MoveTowards(m_fChainSpeed, bAttack ? ATTACK_SPEED : IDLE_SPEED, Time.deltaTime * ATTACK_SPEED);

            // are we attacking?
            if (bAttack && m_fChainSpeed > ATTACK_SPEED * 0.5f)
            {
                CheckForHit();
            }
        }

        protected void CheckForHit()
        {
            // look for colliders around the blade
            int iNumHits = Physics.OverlapSphereNonAlloc(m_blade.position, 0.3f, sm_colliders);

            // find closest character joint to blade
            CharacterJoint bestHit = null;
            float fBestDistance = float.MaxValue;
            for (int i = 0; i < iNumHits; ++i)
            {
                Collider collider = sm_colliders[i];
                CharacterJoint cj = collider.GetComponent<CharacterJoint>();
                if (cj != null)
                {
                    float fDistance = Vector3.Distance(m_blade.position, cj.transform.position);
                    if (fDistance < fBestDistance)
                    {
                        fBestDistance = fDistance;
                        bestHit = cj;
                    }
                }
            }

            // do some damage?            
            if (bestHit != null)
            {
                Monster monster = bestHit.GetComponentInParent<Monster>();
                monster?.Dismember(bestHit.transform);
            }
        }

        //public override void Pause()
        //{
        //    Animator.speed = 0;
        //}
        //
        //public override void Unpause()
        //{
        //    Animator.speed = 1;
        //}
    }
}