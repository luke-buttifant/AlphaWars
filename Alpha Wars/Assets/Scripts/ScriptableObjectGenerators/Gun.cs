using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.AlphaWars
{
[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
    public class Gun : ScriptableObject
    {
        public string gunName;
        public int damage;
        public int maxAmmo;
        public int clipSize;
        public float fireRate;
        public float bloom;
        public float recoil;
        public float kickback;
        public float aimSpeed;
        public float reloadTime;
        public GameObject prefab;

        public AudioClip gunshotSound;
        public float pitchRandomisation;

        private int stash; // Current ammo
        private int clip; // current bullets in clip

        public void initialise()
        {
            stash = maxAmmo;
            clip = clipSize;
        }

        public bool fireBullet()
        {
            if (clip > 0)
            {
                clip -= 1;
                return true;
            }
            else return false;
        }

        public void Reload()
        {
            stash += clip;
            clip = Mathf.Min(clipSize, stash);
            stash -= clip;
        }

        public int getStash()
        {
            return stash;
        }

        public int getClip()
        {
            return clip;
        }
    }
}