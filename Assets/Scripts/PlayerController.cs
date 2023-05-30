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
    [SerializeField] private GameObject _camera;


    private int ammoMax;
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

    public GunInfo gunInfo;
    [SerializeField] private List<int> magazines;

    private bool _shotCool;

    private float curtime = 0;
    private bool _isreload = true;
    [SerializeField] private List<SpriteRenderer> sprites;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();

    }

    void Start()
    {
        _shotCool = true;
        asSound = GetComponent<AudioSource>();
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
        if (itemIndex == 0)
            magazines[0] = 30;
        else if (itemIndex == 1)
            magazines[1] = 10;
        else if (itemIndex == 2)
            magazines[2] = 5;
        _isreload = true;
    }
    void Update()
    {
        if (!PV.IsMine)
            return;

        Look();
        Move();
        Jump();
        if (itemIndex == 2)
            Zoom();
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

        if (Input.GetKeyDown(KeyCode.R) && _isreload)
        {
            Invoke("reload", 2f);
            _isreload = false;
        }

        if (itemIndex <= 1 && Input.GetMouseButtonDown(0) && ammo > 0 && _isreload)
        {
            items[itemIndex].Use();
            ammo--;
            magazines[itemIndex] = ammo;
        }

        if (itemIndex == 2 && Input.GetMouseButtonDown(0) && ammo > 0 && _shotCool && _isreload)
        {
            items[itemIndex].Use();
            ammo--;
            magazines[itemIndex] = ammo;
            _shotCool = false;
        }

        if (!_shotCool)
        {
            curtime += Time.deltaTime;
            if (curtime > 1)
            {
                _shotCool = true;
                curtime = 0;
            }
        }

        if (items[0] == items[itemIndex] && Input.GetKeyDown(KeyCode.LeftControl))
        {
            isBurst = !isBurst;
        }

        if (items[0] == items[itemIndex] && isBurst && Input.GetMouseButton(0) && Time.time > nextFire && ammo > 0 && _isreload)
        {
            items[itemIndex].Use();
            ammo--;
            magazines[itemIndex] = ammo;
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

    //public AudioSource foot;
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
            _camera.GetComponent<Camera>().fieldOfView = 30;
        }
        if (Input.GetMouseButtonUp(1))
        {
            _camera.GetComponent<Camera>().fieldOfView = 60;
        }
    }

    void EquipItem(int _index)
    {
        if (_index == previousItemIndex)
            return;
        foreach (var item in sprites)
        {
            Color col = item.color;
            col.a = 0.2f;
            item.color = col;
        }
        Color col1 = sprites[_index].color;
        col1.a = 1f;
        sprites[_index].color = col1;
        itemIndex = _index;

        items[itemIndex].itemGameObject.SetActive(true);
        // 탄창수
        ammoMax = ((GunInfo)items[itemIndex].GetComponent<SingleShotGun>().itemInfo).magazine;
        ammo = magazines[itemIndex];

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