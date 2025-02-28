using Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(CharacterController))]
    public class Player : EventHandler
    {
        private Camera m_camera;
        private CharacterController m_controller;
        List<Weapon> m_weapons = new List<Weapon>();
        Weapon m_currentWeapon;
        Weapon m_nextWeapon;
        private DodgeAction dodgeAction;

        private static Player sm_instance = null;

        #region Properties

        public Camera Camera => m_camera;

        public CharacterController Controller => m_controller;

        public Weapon CurrentWeapon
        {
            get => m_currentWeapon;
            set
            {
                if (m_currentWeapon == value)
                {
                    return;
                }

                // hide old weapon
                if (m_currentWeapon != null)
                {
                    m_currentWeapon.Active = false;
                    RemoveEvent(m_currentWeapon);
                }

                // set weapon
                m_currentWeapon = value;

                if (m_currentWeapon != null)
                {
                    m_currentWeapon.Active = true;
                    PushEvent(m_currentWeapon);
                }
            }
        }

        public static Player Instance => sm_instance;

        #endregion

        private void OnEnable()
        {
            sm_instance = this;
            m_camera = GetComponentInChildren<Camera>();
            m_controller = GetComponentInChildren<CharacterController>();
        }

        private void Start()
        {
            DoomLevel.Instance?.UpdateLevel(this);
            m_weapons = new List<Weapon>(GetComponentsInChildren<Weapon>());
            CurrentWeapon = m_weapons.Count > 0 ? m_weapons[0] : null;
            m_nextWeapon = CurrentWeapon;
        }

        private void OnDisable()
        {
            sm_instance = (sm_instance == this ? null : sm_instance);
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();

            if (Popup.IsPaused) return;

            DefaultMovement();
            SwitchWeaponsÍnut();
        }

        private void TogglePause()
        {
            if (!Popup.IsPaused)
                Popup.Create<PauseMenu>();
            else
            {
                Popup popup = Main.CurrentEvent as Popup;
                popup.OnCancel();
            }
        }

        public void DefaultMovement()
        {
            // get player input
            int iForwardMovement = 0;
            int iRotation = 0;
            if (Input.GetKey(KeyCode.W)) iForwardMovement++;
            if (Input.GetKey(KeyCode.S)) iForwardMovement--;
            if (Input.GetKey(KeyCode.A)) iRotation--;
            if (Input.GetKey(KeyCode.D)) iRotation++;
            if (Input.GetKey(KeyCode.W) &&
                Input.GetKey(KeyCode.LeftShift)) iForwardMovement += 3;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Get horizontal and vertical input (typically WASD or arrow keys)
                float horizontalInput = Input.GetAxisRaw("Horizontal");
                float verticalInput = Input.GetAxisRaw("Vertical");

                // Calculate the dodge direction by combining forward and right based on input
                // horizontalInput affects the right direction (left/right), and verticalInput affects the forward direction (up/down)
                Vector3 dodgeDirection = transform.forward * verticalInput + transform.right * horizontalInput;

                // If there is any input (i.e., the dodge direction is not zero)
                if (dodgeDirection != Vector3.zero)
                {
                    dodgeDirection.Normalize(); // Ensure it's a unit vector to avoid scaling issues

                    // Trigger the dodge action with the new dodge direction
                    PushEvent(new DodgeAction(this, dodgeDirection));
                }
            }



            if (CurrentEvent is DodgeAction)
                return;

            // move forward?
            if (iForwardMovement != 0)
            {
                Vector3 vMove = transform.forward * Time.deltaTime * 2.0f * iForwardMovement;
                m_controller.Move(vMove);
            }

            // Add some gravity to the situation
            if (!m_controller.isGrounded)
            {
                m_controller.Move(Vector3.down * Time.deltaTime * 2.0f);
            }

            // rotate?
            if (iRotation != 0)
            {
                Quaternion qRotation = Quaternion.Euler(0.0f, iRotation * 90.0f * Time.deltaTime, 0.0f);
                transform.rotation *= qRotation;
            }

            // update level?
            if (iForwardMovement != 0 || iRotation != 0)
            {
                DoomLevel.Instance?.UpdateLevel(this);
            }
        }

        public void SwitchWeaponsÍnut()
        {
            // switch weapons
            if (Input.GetKeyDown(KeyCode.Alpha1) && m_weapons.Count > 0) m_nextWeapon = m_weapons[0];
            if (Input.GetKeyDown(KeyCode.Alpha2) && m_weapons.Count > 1) m_nextWeapon = m_weapons[1];
        }

        public void PerformWeaponSwitch()
        {
            if (m_nextWeapon != CurrentWeapon)
            {
                CurrentWeapon = m_nextWeapon;
            }
        }


    }
}
