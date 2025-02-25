using Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Weapon_Shotgun : Weapon
    {
        protected class FireEvent : EventHandler.GameEvent
        {
            Weapon_Shotgun  m_shotgun;
            float           m_fTime;

            public FireEvent(Weapon_Shotgun shotgun)
            {
                m_shotgun = shotgun;
            }

            public override void OnBegin(bool bFirstTime)
            {
                base.OnBegin(bFirstTime);

                m_shotgun.Animator.SetTrigger("Fire");
                m_shotgun.m_iShellsLeft--;
                m_fTime = 0.5f;

                // do damage here (perhaps in the future delay it 0.05f - 0.1f secs
                Monster bestHit = null;
                float fBestDistance = float.MaxValue;
                Player player = m_shotgun.Player;
                Vector3 vPlayerForward = player.transform.forward;
                foreach (Monster monster in Monster.AllMonsters)
                {
                    Vector3 vToMonster = monster.transform.position - player.transform.position;
                    if (Vector3.Dot(vPlayerForward, vToMonster) < 0.95f)
                    {
                        continue;
                    }

                    float fDistance = Vector3.Distance(player.transform.position, monster.transform.position);
                    if (fDistance < fBestDistance)
                    {
                        fBestDistance = fDistance;
                        bestHit = monster;
                    }
                }

                // deal some pain!
                if (bestHit != null)
                {
                    CharacterJoint[] joints = bestHit.GetComponentsInChildren<CharacterJoint>();
                    if (joints.Length > 0)
                    {
                        CharacterJoint cj = joints[Random.Range(0, joints.Length)];
                        bestHit.Dismember(cj.transform);
                    }
                }
            }

            public override void OnUpdate()
            {
                base.OnUpdate();
                m_fTime -= Time.deltaTime;
            }

            public override bool IsDone()
            {
                // TODO: correct way would be to monitor the Animator state
                //bool bIsFiring = m_shotgun.Animator.GetCurrentAnimatorStateInfo(0).IsName("Shotgun_Fire");
                //return bIsFiring;

                return m_fTime < 0.0f;      // <--- The Hacky way
            }

        }

        protected class ReloadEvent : EventHandler.GameEvent
        {
            Weapon_Shotgun  m_shotgun;
            float           m_fTime;

            public ReloadEvent(Weapon_Shotgun shotgun)
            {
                m_shotgun = shotgun;
            }

            public override void OnBegin(bool bFirstTime)
            {
                base.OnBegin(bFirstTime);

                m_shotgun.Animator.SetTrigger("Reload");
                m_fTime = 3.0f;
            }

            public override void OnUpdate()
            {
                base.OnUpdate();
                m_fTime -= Time.deltaTime;
                //m_shotgun.Player.SwitchWeaponsLogic();        // <-- Allow weapon switching or not?
            }

            public override void OnEnd()
            {
                base.OnEnd();
                m_shotgun.m_iShellsLeft = MAX_SHELLS;
            }

            public override bool IsDone()
            {
                // TODO: correct way would be to monitor the Animator state
                //bool bIsFiring = m_shotgun.Animator.GetCurrentAnimatorStateInfo(0).IsName("XXX");
                //return bIsFiring;
                return m_fTime < 0.0f;      // <--- The Hacky way
            }
        }

        private int     m_iShellsLeft = MAX_SHELLS;

        const int       MAX_SHELLS = 6;

        #region Properties

        #endregion

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            if (m_iShellsLeft == 0)
            {
                Player.PushEvent(new ReloadEvent(this));
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // fire?
            bool bAttack = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
            if (bAttack)
            {
                Player.PushEvent(new FireEvent(this));
            }
        }
    }
}