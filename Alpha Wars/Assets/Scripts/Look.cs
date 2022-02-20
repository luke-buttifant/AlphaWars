using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

namespace Com.AlphaWars
{
    public class Look : MonoBehaviourPunCallbacks
    {
        #region Variables

        public static bool cursorLocked = true;

        public Transform player;
        public Transform cams;
        public Transform weaponParent;

        public float newXSens = 200f;
        public float newYSens = 200f;
        public float maxAngle;

        private Quaternion camCenter;

        #endregion

        #region MonoBehaviour Callbacks

        void Start()
        {
            // Set rotation origin for cameras to camCenter
            camCenter = cams.localRotation;
        }

        // Update is called once per frame
        void Update()
        {

            //networking
            if (!photonView.IsMine)
            {
                return;
            }

            if (Pause.paused)
            {
                return;
            }

            UpdateCursorLock();
            updateXSensitivity(newXSens);
            updateYSensitivity(newYSens);

        }
        #endregion

        #region PrivateMethods
        void setY(float newYSens)
        {
            float t_input = Input.GetAxis("Mouse Y") * newYSens * Time.deltaTime;
            Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
            Quaternion t_delta = cams.localRotation * t_adj;

            if(Quaternion.Angle(camCenter, t_delta) < maxAngle)
            {
                cams.localRotation = t_delta;
            }

            weaponParent.rotation = cams.rotation;
            
        }

        void setX(float newXSens)
        {
            float t_input = Input.GetAxis("Mouse X") * newXSens * Time.deltaTime;
            Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
            Quaternion t_delta = player.localRotation * t_adj;

            player.localRotation = t_delta;

        }

        void UpdateCursorLock()
        {
            if (cursorLocked == true)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    cursorLocked = false;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    cursorLocked = true;
                }
            }
        }

        public void updateXSensitivity(float newXSens)
        {
            Debug.Log("New Sens is " + newYSens);
            setX(newXSens);
        }

        public void updateYSensitivity(float newYSens)
        {
            Debug.Log("New Y Sens is " + newYSens);
            setY(newYSens);
        }
        #endregion
    }
}