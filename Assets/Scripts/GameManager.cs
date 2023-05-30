using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance = null;

    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text chatListText;
    [SerializeField] private TMP_Text killLog;
    [SerializeField] private TMP_InputField chatMsgIf;
    private PhotonView pv;

    void Awake()
    {
        Instance = this;
        Cursor.visible = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        exitButton.onClick.AddListener(() => OnExitButtonClick());
    }

    private void OnExitButtonClick()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
        }
        else
            Cursor.visible = false;
        if (Input.GetKeyDown(KeyCode.Return))
        {
            chatMsgIf.Select();
        }
    }

    public void SendMsg2(string msg)
    {
        pv.RPC("ChatMsg", RpcTarget.AllBufferedViaServer, msg);
    }

    public void SendMsg()
    {
        string msg = $"<color=#00ff00>[{PhotonNetwork.NickName}]</color> {chatMsgIf.text}";
        pv.RPC("ChatMsg", RpcTarget.AllBufferedViaServer, msg);

        chatMsgIf.text = "";
    }

    [PunRPC]
    public void ChatMsg(string msg)
    {
        chatListText.text += msg + "\n";
    }
    [PunRPC]
    public void killLogtext(string msg)
    {
        killLog.text += msg + "\n";
    }

    public void SendLog(string msg)
    {
        pv.RPC("killLogtext", RpcTarget.AllBufferedViaServer, msg);
    }
    public void SendLog()
    {
        string msg = $"<color=#00ff00>[{PhotonNetwork.NickName}]</color> ���� �����߽��ϴ�.";
        pv.RPC("killLogtext", RpcTarget.AllBufferedViaServer, msg);
    }
}
