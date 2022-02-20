using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.AlphaWars
{


    public class MainMenu : MonoBehaviour
    {
        public Launcher launcher;

        public void Start()
        {
            Pause.paused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void JoinMatch()
        {
            launcher.Join();
        }

        public void CraeteMatch()
        {
            launcher.Create();
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
