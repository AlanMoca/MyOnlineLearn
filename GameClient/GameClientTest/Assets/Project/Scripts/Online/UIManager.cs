using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance {
        get
        {
            if ( _instance == null )
            {
                _instance = FindObjectOfType<UIManager>();
                if ( _instance == null )
                {
                    GameObject go = new GameObject();
                    go.name = typeof( UIManager ).Name;
                    _instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad( go );
                }
            }
            return _instance;
        }
        private set
        {
            value = _instance;
        }
    }

    public GameObject startMenu;
    public InputField userNameField;

    private void Awake()
    {
        SetClientSingleton();
    }

    private void SetClientSingleton()
    {
        DontDestroyOnLoad( this.gameObject );
        if ( Instance != null && Instance != this )
        {
            Destroy( Instance.gameObject );
        }
        else if ( Instance == null )
        {
            Instance = this;
            DontDestroyOnLoad( this.gameObject );
        }
    }

    public void ConnectToServer()
    {
        startMenu.SetActive( false );
        userNameField.interactable = false;
        Client.Instance.ConnectToServer();
    }

}
