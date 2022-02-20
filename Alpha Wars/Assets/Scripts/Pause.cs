using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace Com.AlphaWars
{

    public class Pause : MonoBehaviour
    {
        public GameObject settingsMenu;
        public GameObject ySlider;
        public GameObject xSlider;

        public static bool settingsMenuClicked = false;

        public static bool paused = false;
        private bool disconnecting = false;

        private Look look;

        public void TogglePause()
        {
            if (disconnecting) return;

            paused = !paused;

            transform.GetChild(0).gameObject.SetActive(paused);
            Cursor.lockState = (paused) ? CursorLockMode.None : CursorLockMode.Confined;
            Cursor.visible = paused;
        }
        
        public void Quit()
        {
            disconnecting = true;
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene(0);
        }

        public void SettingsPressed()
        {
            settingsMenuClicked = !settingsMenuClicked;

            settingsMenu.SetActive(settingsMenuClicked);
            transform.GetChild(0).gameObject.SetActive(!settingsMenuClicked);
        }

        public void closeSettings()
        {
            settingsMenuClicked = !settingsMenuClicked;

            settingsMenu.SetActive(settingsMenuClicked);
            transform.GetChild(0).gameObject.SetActive(!settingsMenuClicked);
        }


    }
}
