using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.Rendering;
using System;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    [SerializeField] Image healthbarImage;
    [SerializeField] GameObject ui;

    [SerializeField] GameObject cameraHolder;

    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

    [SerializeField] Item[] items;

    public int ammoMax = 30;
    int ammo = 0;
    int itemIndex;
    int previousItemIndex = -1;

    public GameObject deathGameObjectPrefab;
    public TMP_Text tmp;

    float verticalLookRotation;
    bool grounded;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;

    Rigidbody rb;

    PhotonView PV;

    Animator anim;
    AudioSource asSound;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;

    PlayerManager playerManager;

    public AudioClip reloadSfx, dieSfx;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();

    }

    void Start()
    {
        asSound = GetComponent<AudioSource>();
        ammo = ammoMax;
        anim = GetComponentInChildren<Animator>();
        if (PV.IsMine)
        {
            EquipItem(1);
            EquipItem(2);
            EquipItem(0);
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
        }
    }
    public void reload()
    {
        asSound.PlayOneShot(reloadSfx);
        ammo = ammoMax;
    }
    void Update()
    {
        if (!PV.IsMine)
            return;

        Look();
        Move();
        Jump();

        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            if (itemIndex >= items.Length - 1)
            {
                EquipItem(0);
            }
            else
            {
                EquipItem(itemIndex + 1);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            if (itemIndex <= 0)
            {
                EquipItem(items.Length - 1);
            }
            else
            {
                EquipItem(itemIndex - 1);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Invoke("reload", 2f);
        }


        if (Input.GetMouseButtonDown(0) && ammo > 0)
        {
            items[itemIndex].Use();
            ammo--;
        }

        if (items[0] == items[itemIndex] && Input.GetKeyDown(KeyCode.LeftControl))
        {
            isBurst = !isBurst;
        }

        if (items[0] == items[itemIndex] && isBurst && Input.GetMouseButton(0) && Time.time > nextFire && ammo > 0)
        {
            items[itemIndex].Use();
            ammo--;
            nextFire = Time.time + fireRate;
        }

        if (transform.position.y < -10f) // Die if you fall out of the world
        {
            Die();
        }
        tmp.text = ammo + "/" + ammoMax;
    }
    bool isBurst = true;
    private float fireRate = 0.1f;
    private float nextFire = 0.0f;

    void Look()
    {
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
        if (Input.GetKey(KeyCode.LeftShift))
        {
            anim.SetTrigger("run");
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpForce);
        }
    }

    void Zoom()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Camera.main.fieldOfView = 30;
            // sni.SetActive(true);

        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            Camera.main.fieldOfView = 60;
            // sni.SetActive(false);
        }
    }

    void EquipItem(int _index)
    {
        if (_index == previousItemIndex)
            return;

        itemIndex = _index;

        items[itemIndex].itemGameObject.SetActive(true);

        if (previousItemIndex != -1)
        {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;

        if (PV.IsMine)
        {
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }
    }

    public void SetGroundedState(bool _grounded)
    {
        grounded = _grounded;
    }

    void FixedUpdate()
    {
        if (!PV.IsMine)
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, string shooter)
    {
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage, shooter);

        if (PV.IsMine)
        {
            if (currentHealth < 0)
            {
                PV.RPC("Die", RpcTarget.AllViaServer, shooter);
            }
        }
    }

    [PunRPC]
    public void dieEffect()
    {
        Instantiate(deathGameObjectPrefab, transform.position, Quaternion.identity);
        asSound.PlayOneShot(dieSfx);
    }
    [PunRPC]
    void RPC_TakeDamage(float damage, string shooter, PhotonMessageInfo info)
    {
        currentHealth -= damage;

        healthbarImage.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            PV.RPC("dieEffect", RpcTarget.All);
            Die(shooter);
            PlayerManager.Find(info.Sender).GetKill();
        }
    }

    void Die(string enemyNickName)
    {
        if (PV.IsMine)
        {
            string msg = $"<color=#00ff00>[{PV.Owner.NickName}]</color> 님은 {enemyNickName}에게 살해당했습니다.";
            GameManager.Instance.SendLog(msg);
        }
        playerManager.Die();
    }

    void Die()
    {
        if (PV.IsMine)
        {
            string msg = $"<color=#00ff00>[{PV.Owner.NickName}]</color> 님은 낙사했습니다.";
            GameManager.Instance.SendLog();
        }
        playerManager.Die();
    }
}