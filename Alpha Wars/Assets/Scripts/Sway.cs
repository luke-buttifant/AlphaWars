using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.AlphaWars
{
    public class Sway : MonoBehaviourPunCallbacks
    {
        #region Variables

        public float swayIntensity;
        public float smooth;

        private Quaternion originRotation;
        #endregion


        #region MonoBehavior CallBacks

        private void Start()
        {
            originRotation = transform.localRotation;
        }
        private void Update()
        {
            if (Pause.paused)
            {
                return;
            }

            //networking
            if (!photonView.IsMine)
            {
                return;
            }
            updateSway();
        }
        #endregion


        #region Private Methods

        private void updateSway()
        {
            //Controls
            float t_x_mouse = Input.GetAxis("Mouse X");
            float t_y_mouse = Input.GetAxis("Mouse Y");


            //calculate target rotation
            Quaternion t_x_adj = Quaternion.AngleAxis(-swayIntensity * t_x_mouse, Vector3.up);
            Quaternion t_y_adj = Quaternion.AngleAxis(swayIntensity * t_x_mouse, Vector3.right);
            Quaternion targetRotation = originRotation * t_x_adj * t_y_adj;

            //rotate towards target rotation
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
        }
        #endregion
    }
}
