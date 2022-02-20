using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using TMPro;

namespace Com.AlphaWars
{

    public class Player : MonoBehaviourPunCallbacks
    {

        private const int LAYER_PLAYER = 10;
        private const int LAYER_GUN = 8;

        #region Variables

        public float speed;
        public float jumpForce;
        public float sprintModifier;
        public float maxHealth;

        private Animator anim;

        public GameObject PlayerMesh;

        public Camera normalCam;
        public LayerMask ground;
        public Transform groundDetector;
        public Transform weaponParent;
        public GameObject cameraParent;

        [Tooltip("Objects you want to be affected whenever there's a layer change.")]
        public GameObject[] modifiableLayers;

        [HideInInspector]public ProfileData playerProfile;
        [HideInInspector] public bool awayTeam;
        public TextMeshPro playerUsernameText;

        private Vector3 targetWeaponBobPosition;
        private Rigidbody rig;
        private Vector3 weaponParentOrigin;

        public Renderer[] teamIndicators;

        private Text ui_team;

        private float movementCounter;
        private float idleCounter;

        private float baseFOV;
        private float sprintFOVmodifier = 1.25f;
        private float currentHealth;

        private Manager manager;
        private Weapon weapon;
        private Transform ui_healthbar;
        private Image crosshair;
        private Text ui_Ammo;
        private Text ui_username;

        //Animation
        public float animSpeed = 10.0f;
        public float rotationSpeed = 100.0f;


        private void Start()
        {
            

            crosshair = GameObject.Find("HUD/Crosshair/Image").GetComponent<Image>();

            manager = GameObject.Find("Manager").GetComponent<Manager>();
            weapon = GetComponent<Weapon>();

            currentHealth = maxHealth;

            cameraParent.SetActive(photonView.IsMine);


            // THIS IS THE PROBLEM.... 
            //This is neccasary as it does change other players layers to be EnemyPlayer.
            //But for unknown reasons if the camera culling mask is set to localPlayer, EnemyPlayer dissapear as well. 
            //I would also like enemy players pistols to be hidden. 
            if (!photonView.IsMine)
            {
                gameObject.layer = LAYER_PLAYER;
                ChangeModifiableLayers(LAYER_PLAYER);
                ChangeWeaponLayer(LAYER_GUN);
            }
            //---------------------------------------------------------------


            baseFOV = normalCam.fieldOfView;
            
            rig = GetComponent<Rigidbody>();

            weaponParentOrigin = weaponParent.localPosition;

            if (photonView.IsMine)
            {
                ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
                ui_Ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
                ui_username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
                ui_team = GameObject.Find("HUD/Team/Text").GetComponent<Text>();

                //Animator
                anim = GetComponent<Animator>();

         
                refreshHealthBar();
                ui_username.text = Launcher.myProfile.username;

                photonView.RPC("syncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

                if(GameSettings.GameMode == GameMode.TDM)
                {
                    photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);

                    if (GameSettings.IsAwayTeam)
                    {
                        ui_team.text = "Blue Team";
                        ui_team.color = Color.blue;
                    }
                    else
                    {
                        ui_team.text = "Red Team";
                        ui_team.color = Color.red;
                    }
                }
            }
            else
            {
                if (ui_team.gameObject)
                {
                    ui_team.gameObject.SetActive(false);
                }
                
            }


        }

          
        private void ColourTeamIndicators(Color p_colour)
        {
            foreach (Renderer renderer in teamIndicators) renderer.material.color = p_colour;
        }


        #endregion

        #region MonoBehaviourCallbacks
        private void Update()
        {

            if (Input.GetMouseButton(1))
            {
                crosshair.color = new Color(1, 1, 1, 0);
            }
            else
            {
                crosshair.color = new Color(1, 1, 1, 1);
                
            }

            //networking
            if (!photonView.IsMine)
            {
                return;
            }
            //Axles
            float t_hmove = Input.GetAxisRaw("Horizontal");
            float t_vmove = Input.GetAxisRaw("Vertical");

            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKeyDown(KeyCode.Space);
            bool pause = Input.GetKeyDown(KeyCode.Escape);

            //States
            bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;

            //Pause
            if (pause)
            {
                GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
            }

            if (Pause.paused)
            {
                t_hmove = 0f;
                t_vmove = 0f;
                sprint = false;
                jump = false;
                pause = false;
                isGrounded = false;
                isJumping = false;
                isSprinting = false;
            }

            //Jumping
            if (isJumping)
            {
                rig.AddForce(Vector3.up * jumpForce);
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                takeDamage(400, -1);
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 0);

            }

            //headbob
            if(t_hmove == 0 && t_vmove == 0)
            {
                if (Input.GetMouseButton(1))
                {
                    weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 2f);
                }
                else
                {
                    headbob(idleCounter, 0.005f, 0.005f);
                    idleCounter += Time.deltaTime;
                    weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
                }
                
            }
            else if (!isSprinting && isGrounded)
            {
                if (Input.GetMouseButton(1))
                {
                    weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, weaponParentOrigin, Time.deltaTime * 6f);
                }
                else
                {
                    headbob(movementCounter, 0.015f, 0.015f);
                    movementCounter += Time.deltaTime * 3f;
                    weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
                }
                
            }
            else if (!isGrounded)
            {
                headbob(idleCounter, 0.005f, 0.005f);
                idleCounter += 0;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else
            {
                if (isGrounded)
                {
                    headbob(movementCounter, 0.025f, 0.03f);
                    movementCounter += Time.deltaTime * 4f;
                    weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
                }
            }

            //Animation
            photonView.RPC("jumpingAnimation", RpcTarget.All, isJumping);

            // UI Refreshes
            refreshHealthBar();
            weapon.refreshAmmo(ui_Ammo);
                
        }

        // Update is called once per frame
        void FixedUpdate()
        {

            //networking
            if (!photonView.IsMine)
            {
                return;
            }

            //Axles
            float t_hmove = Input.GetAxisRaw("Horizontal");
            float t_vmove = Input.GetAxisRaw("Vertical");

            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKeyDown(KeyCode.Space);

            //States
            bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;

            if (Pause.paused)
            {
                t_hmove = 0f;
                t_vmove = 0f;
                sprint = false;
                jump = false;
                isGrounded = false;
                isJumping = false;
                isSprinting = false;
            }

            //Movement
            Vector3 t_direction = new Vector3(t_hmove,0, t_vmove);
            t_direction.Normalize();

            float t_adjustedSpeed = speed;

            if (isSprinting)
            {
                t_adjustedSpeed *= sprintModifier;
            }
            Vector3 t_targetVelocity = transform.TransformDirection(t_direction) * t_adjustedSpeed * Time.deltaTime;
            t_targetVelocity.y = rig.velocity.y;
            rig.velocity = t_targetVelocity;


            //Movement
            if (isSprinting)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVmodifier, Time.deltaTime * 8);
            }
            else
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8);
            }

            //Animations
            float t_anim_horizontal = 0f;
            float t_anim_vertical = 0f;

            if (isGrounded)
            {
                t_anim_horizontal = t_direction.x;
                t_anim_vertical = t_direction.z;
            }

            anim.SetFloat("Horizontal", t_anim_horizontal);
            anim.SetFloat("Vertical", t_anim_vertical);

            //Animation
            photonView.RPC("jumpingAnimation", RpcTarget.All, isJumping);
        }
        #endregion

        #region Private Methods

        private void Animations()
        {
            if (photonView.IsMine)
            {
                float translation = Input.GetAxisRaw("Vertical") * animSpeed;
                float rotation = Input.GetAxisRaw("Horizontal") * rotationSpeed;

            }

        }
        // I added this method so that not all objects under Player will be affected by the layer change.
        private void ChangeModifiableLayers(int p_layer)
        {
            foreach(var g in modifiableLayers) {
                ChangeLayerRecursively(g.transform, p_layer);
            }
        }

        private void ChangeLayerRecursively(Transform p_transform, int p_layer)
        {
            var children = p_transform.GetComponentsInChildren<Transform>();
            foreach(Transform t in children)
            {
                p_transform.gameObject.layer = p_layer;
                if (t != p_transform)
                {
                    ChangeLayerRecursively(t, p_layer);
                }
            }
        }

        private void ChangeWeaponLayer(int p_layer) {
            ChangeLayerRecursively(weaponParent.transform, p_layer);
        }

        void headbob(float p_z, float p_x_intensity, float p_y_intensity)
        {
            targetWeaponBobPosition = weaponParentOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity, Mathf.Sin(p_z * 2) * p_y_intensity, 0);
        }

        void refreshHealthBar()
        {
            float t_health_ratio = (float)currentHealth / (float)maxHealth;
            ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);
        }

        public void TrySync()
        {
            if (!photonView.IsMine) return;

            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

            if(GameSettings.GameMode == GameMode.TDM)
            {
                photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);
            }
        }

        [PunRPC]
        private void syncProfile(string p_username, int p_level, int p_xp)
        {
            playerProfile = new ProfileData(p_username, p_level, p_xp);
            playerUsernameText.text = playerProfile.username;

        }

        [PunRPC]
        private void SyncTeam(bool p_awayTeam)
        {
            awayTeam = p_awayTeam;

            if (awayTeam)
            {
                ColourTeamIndicators(Color.blue);
            }
            else
            {
                ColourTeamIndicators(Color.red);
            }
        }

        #endregion

        #region Public Methods

        public void takeDamage(int p_damage, int p_actor)
        {
            if (photonView.IsMine)
            {
                currentHealth -= p_damage;
                refreshHealthBar();

                Debug.Log(currentHealth);

                if(currentHealth <= 0)
                {
                    manager.Spawn();
                    manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                    if(p_actor >= 0)
                    {
                        manager.ChangeStat_S(p_actor, 0, 1);
                    }

                    PhotonNetwork.Destroy(gameObject);
                }
            }

        }
        #endregion

        #region RPC's

        [PunRPC]
        private void jumpingAnimation(bool isJumping)
        {
            if (isJumping)
            {
               anim.SetTrigger("isJumping");
            }
        }
        


        #endregion
    }
}