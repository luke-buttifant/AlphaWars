using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

namespace Com.AlphaWars
{

    public class Weapon : MonoBehaviourPunCallbacks
    {
        #region Variables

        public Gun[] loadout;
        public Transform weaponParent;
        public GameObject bulletHolePrefab;
        public LayerMask canBeShot;

        public AudioSource sfx;
        public AudioClip hitmarkerSound;


        private float currentCoolDown;
        private int currentIndex;
        private GameObject currentWeapon;

        private Image hitmarkerImage;
        private float hitmarkerWait;

        private bool isReloading;

        private Color clearWhite = new Color(1, 1, 1, 0);

        #endregion

        #region MonoBehaviourCallbacks

        private void Start()
        {
            foreach(Gun a in loadout)
            {
                a.initialise();
                photonView.RPC("Equip", RpcTarget.All, 0);
            }

            hitmarkerImage = GameObject.Find("HUD/HitMarker/Image").GetComponent<Image>();
            hitmarkerImage.color = clearWhite;
        }
        // Update is called once per frame
        void Update()
        {
            if(Pause.paused && photonView.IsMine)
            {
                return;
            }

            //networking
            if (!photonView.IsMine)
            {
                return;
            }

            if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
            {
                photonView.RPC("Equip", RpcTarget.All, 0);
            }

            if (currentWeapon != null)
            {
                if (photonView.IsMine)
                {

                    Aim(Input.GetMouseButton(1));

                    if (Input.GetMouseButtonDown(0) && currentCoolDown <= 0 )
                    {
                        if (loadout[currentIndex].fireBullet())
                        {
                            photonView.RPC("Shoot", RpcTarget.All);
                            gunFX();
                        }
                        else
                        {
                            StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.R)) 
                    {
                        photonView.RPC("reloadRPC", RpcTarget.All);
                    }

                    //cooldown
                    if (currentCoolDown > 0)
                    {
                        currentCoolDown -= Time.deltaTime;
                    }
                }


                // Weapon Position Elasticity
                currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);

                if (photonView.IsMine)
                {
                    if(hitmarkerWait > 0)
                    {
                        hitmarkerWait -= Time.deltaTime;
                    }
                    else if (hitmarkerImage.color.a > 0)
                    {
                        hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, clearWhite, Time.deltaTime * 5f);
                    }
                }
            }


        }

        #endregion

        #region PrivateMethods

        [PunRPC]
        void Equip(int p_ind)
        {
            if (currentWeapon != null)
            {
                if (isReloading)
                {
                    StopCoroutine("Reload");
                }
                Destroy(currentWeapon);
            }

            currentIndex = p_ind;

            GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
            t_newWeapon.transform.localPosition = Vector3.zero;
            t_newWeapon.transform.localEulerAngles = Vector3.zero;
            t_newWeapon.GetComponent<Sway>().enabled = photonView.IsMine;

            t_newWeapon.GetComponent<Animator>().Play("equip", 0, 0);

            currentWeapon = t_newWeapon;

        }

        void Aim(bool p_isAiming)
        {
            Transform t_anchor = currentWeapon.transform.Find("Anchor");
            Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
            Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

            if (p_isAiming && !Input.GetKey(KeyCode.LeftShift))
            {
                //Aim
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
            else
            {
                //hip
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
        }

        [PunRPC]
        void Shoot()
        {
            Transform t_spawn = transform.Find("Cameras/NormalCamera");

            currentWeapon.GetComponent<Animator>().Play("ShootNew", 0, 0);

            //bloom
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();

            //raycast
            RaycastHit t_hit = new RaycastHit();

            if (!Input.GetMouseButton(1))
            {
                if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
                {
                    GameObject t_newHole = Instantiate(bulletHolePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                    t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                    Destroy(t_newHole, 5f);

                    //Shooting other players
                    if (photonView.IsMine)
                    {
                        if (t_hit.collider.gameObject.layer == 10)
                        {
                            bool applyDamage = false;

                            if(GameSettings.GameMode == GameMode.FFA)
                            {
                                applyDamage = true;
                            }
                            if(GameSettings.GameMode == GameMode.TDM)
                            {
                                if(t_hit.collider.transform.root.gameObject.GetComponent<Player>().awayTeam != GameSettings.IsAwayTeam)
                                {
                                    applyDamage = true;
                                }
                            }

                            if (applyDamage)
                            {
                                //RPC call to damage player
                                t_hit.collider.gameObject.GetPhotonView().RPC("takeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);

                                //Show hitmarker
                                hitmarkerImage.color = Color.white;
                                sfx.PlayOneShot(hitmarkerSound);
                                hitmarkerWait = 0.5f;
                            }

                        }
                    }
                }
            }
            else
            {
                if (Physics.Raycast(t_spawn.position, t_spawn.forward, out t_hit, 1000f, canBeShot))
                {
                    GameObject t_newHole = Instantiate(bulletHolePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                    t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                    Destroy(t_newHole, 5f);

                    //Shooting other players
                    if (photonView.IsMine)
                    {
                        if (t_hit.collider.gameObject.layer == 10)
                        {
                            bool applyDamage = false;

                            if (GameSettings.GameMode == GameMode.FFA)
                            {
                                applyDamage = true;
                            }
                            if (GameSettings.GameMode == GameMode.TDM)
                            {
                                if (t_hit.collider.transform.root.gameObject.GetComponent<Player>().awayTeam != GameSettings.IsAwayTeam)
                                {
                                    applyDamage = true;
                                }
                            }

                            if (applyDamage)
                            {
                                //RPC call to damage player
                                t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("takeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);

                                //Show hitmarker
                                hitmarkerImage.color = Color.white;
                                sfx.PlayOneShot(hitmarkerSound);
                                hitmarkerWait = 0.5f;
                            }
                        }
                    }
                }

            }

            //sound FX
            sfx.Stop();
            sfx.clip = loadout[currentIndex].gunshotSound;
            sfx.pitch = 1 - loadout[currentIndex].pitchRandomisation + Random.Range(-loadout[currentIndex].pitchRandomisation, loadout[currentIndex].pitchRandomisation);
            sfx.Play();
        }

        [PunRPC]
        private void reloadRPC()
        {
            StartCoroutine(Reload(loadout[currentIndex].reloadTime));
        }
        IEnumerator Reload(float p_wait)
        {
            isReloading = true;

            if (currentWeapon.GetComponent<Animator>())
            {
                currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
            }
            else
            {
                currentWeapon.SetActive(false);
            }

            yield return new WaitForSeconds(p_wait);

            loadout[currentIndex].Reload();
            currentWeapon.SetActive(true);
            isReloading = false;
        }

        void gunFX()
        {
            //Gun FX
            currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
            currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;

            //cooldown
            currentCoolDown = loadout[currentIndex].fireRate;
        }

        [PunRPC]
        private void takeDamage(int p_damage, int p_actor)
        {
            GetComponent<Player>().takeDamage(p_damage, p_actor);
        }
        #endregion
        
        #region Public Methods

        public void refreshAmmo(Text p_text)
        {
            int t_clip = loadout[currentIndex].getClip();
            int t_stache = loadout[currentIndex].getStash();

            p_text.text = t_clip.ToString("D2") + " / " + t_stache.ToString("D2");
        }

        #endregion
    }
}