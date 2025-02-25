using Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Animator))]
    public abstract class Weapon : EventHandler.GameEventBehaviour, IPause
    {
        private Animator    m_animator;

        #region Properties

        public Animator Animator => m_animator;

        public bool Active
        {
            get => m_animator.GetBool("Active");
            set => m_animator.SetBool("Active", value);
        }

        public Player Player => GetComponentInParent<Player>();

        #endregion

        protected virtual void OnEnable()
        {
            m_animator = GetComponent<Animator>();
            Active = false;

            Popup.Pause += Pause;
            Popup.UnPause += Unpause;
        }

        private void OnDisable()
        {
            Popup.Pause -= Pause;
            Popup.UnPause -= Unpause;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Popup.IsPaused) return;

            // allow players to switch weapons
            Player.PerformWeaponSwitch();
        }

        public override bool IsDone()
        {
            return !Active;
        }

        public virtual void Pause()
        {
            m_animator.speed = 0;
        }

        public virtual void Unpause()
        {
            m_animator.speed = 1;
        }
    }
}