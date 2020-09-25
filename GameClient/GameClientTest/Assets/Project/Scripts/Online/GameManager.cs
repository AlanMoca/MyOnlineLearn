using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

#region Singleton

    private static GameManager _instance;
    public static GameManager Instance {
        get
        {
            if ( _instance == null )
            {
                _instance = FindObjectOfType<GameManager>();
                if ( _instance == null )
                {
                    GameObject go = new GameObject();
                    go.name = typeof( GameManager ).Name;
                    _instance = go.AddComponent<GameManager>();
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

#endregion

    public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();
    public GameObject localPlayerPrefab;
    public GameObject playerPrefab;

    public void SpawnPlayer( int _id, string _userName, Vector3 _position, Quaternion _rotation )
    {
        GameObject _player;
        //Checamos si el player que vamos a spawnear es el jugador local para instanciarle el apropiado prefab.
        if ( _id == Client.Instance.myId )
        {
            _player = Instantiate( localPlayerPrefab, _position, _rotation );
        }
        else
        {
            _player = Instantiate( playerPrefab, _position, _rotation );
        }

        _player.GetComponent<PlayerManager>().id = _id;
        _player.GetComponent<PlayerManager>().userName = _userName;


        //Finalmente agregamos el jugador al diccionario de players
        players.Add( _id, _player.GetComponent<PlayerManager>() );
    }

}
