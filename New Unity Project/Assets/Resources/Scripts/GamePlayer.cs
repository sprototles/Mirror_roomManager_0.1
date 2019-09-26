﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{

    public class GamePlayer : NetworkBehaviour
    {
        public Spawn spawn;

        [SyncVar]
        public GameObject networkRoomPlayerGO;

        public NetworkRoomPlayer networkRoomPlayer;

        public List<GameObject> playerObject = new List<GameObject>();

        [SerializeField]
        private readonly KeyCode[,] keyCodes = new KeyCode[4,4];
        // {KeyCode.W,KeyCode.S,KeyCode.D,KeyCode.A};

        private void Awake()
        {
            // set KeyCodes !!!
            keyCodes[0,0] = KeyCode.W;
            keyCodes[0,1] = KeyCode.S;
            keyCodes[0,2] = KeyCode.D;
            keyCodes[0,3] = KeyCode.A;

            keyCodes[1,0] = KeyCode.UpArrow;
            keyCodes[1,1] = KeyCode.DownArrow;
            keyCodes[1,2] = KeyCode.RightArrow;
            keyCodes[1,3] = KeyCode.LeftArrow;

            keyCodes[2,0] = KeyCode.Keypad8;
            keyCodes[2,1] = KeyCode.Keypad5;
            keyCodes[2,2] = KeyCode.Keypad6;
            keyCodes[2,3] = KeyCode.Keypad4;

            keyCodes[3,0] = KeyCode.I;
            keyCodes[3,1] = KeyCode.K;
            keyCodes[3,2] = KeyCode.L;
            keyCodes[3,3] = KeyCode.J;

        }


        // Start is called before the first frame update
        void Start()
        {

            networkRoomPlayer = networkRoomPlayerGO.GetComponent<NetworkRoomPlayer>();

            gameObject.name = "GamePlayer " + networkRoomPlayer.clientIndex;

            Debug.Log("keyCodes.Length = " + keyCodes.Length, gameObject);


            spawn = GameObject.Find("Spawn").GetComponent<Spawn>();
            
            if (isLocalPlayer)
            {
                for (int i = 0; i < networkRoomPlayer.localPlayerCount; i++)
                {
                    CmdSpawnPlayerGameObject(i);
                }
            }
        }
        
        [Command]
        void CmdSpawnPlayerGameObject(int playerIncrement)
        {
            SpawnPlayerGameObject(playerIncrement);
            RpcSpawnPlayerGameObject(playerIncrement);

        }

        [ClientRpc]
        void RpcSpawnPlayerGameObject(int playerIncrement)
        {
            if (isServer) return;
            SpawnPlayerGameObject(playerIncrement);
        }
        
        void SpawnPlayerGameObject(int playerIncrement)
        {

            spawn.SpawnPlayer(playerIncrement,networkRoomPlayer.clientIndex,gameObject);
            /*
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(0 + playerIncrement * 5f, 0, networkRoomPlayer.clientIndex * 5f);
            go.transform.SetParent(gameObject.transform);
            go.name = "Cube" + playerIncrement;

            go.AddComponent<NetworkIdentity>();
            go.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            go.AddComponent<NetworkTransform>();

            playerObject.Add(go);

            NetworkServer.Spawn(go);
            */
        }
        
        private void Update()
        {
            if (!isLocalPlayer) return;


            // for-cycle for key per player     OR keyCodes.GetLength(0) instead of localPlayerCount
            for (int i = 0; i < networkRoomPlayer.localPlayerCount; i++)
            {
                // KEY UP
                if (Input.GetKeyDown(keyCodes[i, 0]))
                {
                    Debug.Log("Key " + keyCodes[i, 0].ToString() + " is pressed down");
                    playerObject[i].transform.position += new Vector3(1, 0, 0); 
                }

                // KEY DOWN
                if (Input.GetKeyDown(keyCodes[i, 1]))
                {
                    Debug.Log("Key " + keyCodes[i, 1].ToString() + " is pressed down");
                    playerObject[i].transform.position += new Vector3(-1, 0, 0);
                }

                // KEY RIGHT
                if (Input.GetKeyDown(keyCodes[i, 2]))
                {
                    Debug.Log("Key " + keyCodes[i, 2].ToString() + " is pressed down");
                    playerObject[i].transform.position += new Vector3(0, 0, -1);
                }

                // KEY LEFT
                if (Input.GetKeyDown(keyCodes[i, 3]))
                {
                    Debug.Log("Key " + keyCodes[i, 3].ToString() + " is pressed down");
                    playerObject[i].transform.position += new Vector3(0, 0, 1);
                }
            }
        }
    }
}
