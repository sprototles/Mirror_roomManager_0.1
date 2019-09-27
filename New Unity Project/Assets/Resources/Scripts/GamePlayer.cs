using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{

    public class GamePlayer : NetworkBehaviour
    {
        [SyncVar]
        public int clientIndex;

        [SyncVar]
        public int localPlayerClientCount;

        public List<GameObject> playerControlledObject = new List<GameObject>();
              
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
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            if (hasAuthority && isLocalPlayer)
            {
                for(int i = 0; i < localPlayerClientCount; i++)
                {
                    CmdSpawnPlayerGameObject(i);
                }

            }
        }

        public NetworkIdentity spawnPrefab;
        GameObject instantiateGameObject;


        [Command]
        void CmdSpawnPlayerGameObject(int objectIndex)
        {
            Debug.Log("CmdSpawnPlayerGameObject");
            instantiateGameObject = Instantiate(spawnPrefab.gameObject, new Vector3(1f * objectIndex, 0f, clientIndex * 1f), Quaternion.identity);
            
            playerControlledObject.Add(instantiateGameObject);
            NetworkServer.Spawn(instantiateGameObject);
        }


        #region Update()
        
        private void Update()
        {
            if (!isLocalPlayer) return;


            // for-cycle for key per player     OR keyCodes.GetLength(0) instead of localPlayerCount
            for (int i = 0; i < localPlayerClientCount; i++)
            {
                Vector3 playerPosIncrement = Vector3.zero;

                // KEY UP
                if (Input.GetKeyDown(keyCodes[i, 0]))
                {
                    Debug.Log("Key " + keyCodes[i, 0].ToString() + " is pressed down");
                    playerPosIncrement += new Vector3(1, 0, 0);
                }

                // KEY DOWN
                if (Input.GetKeyDown(keyCodes[i, 1]))
                {
                    Debug.Log("Key " + keyCodes[i, 1].ToString() + " is pressed down");
                    playerPosIncrement += new Vector3(-1, 0, 0);
                }

                // KEY RIGHT
                if (Input.GetKeyDown(keyCodes[i, 2]))
                {
                    Debug.Log("Key " + keyCodes[i, 2].ToString() + " is pressed down");
                    playerPosIncrement += new Vector3(0, 0, -1);
                }

                // KEY LEFT
                if (Input.GetKeyDown(keyCodes[i, 3]))
                {
                    Debug.Log("Key " + keyCodes[i, 3].ToString() + " is pressed down");
                    playerPosIncrement += new Vector3(0, 0, 1);
                }
                CmdUpdateControlledObjectPosition(i, playerPosIncrement);
            }
        }
        
        #endregion

        [Command]
        void CmdUpdateControlledObjectPosition(int playerIncrement, Vector3 pos)
        {
            UpdateControlledObjectPosition(playerIncrement, pos);
            //RpcUpdateControlledObjectPosition(playerIncrement, pos);
        }

        [ClientRpc]
        void RpcUpdateControlledObjectPosition(int playerIncrement, Vector3 pos)
        {
            if (isServer) return;
            UpdateControlledObjectPosition(playerIncrement, pos);
        }
        
        void UpdateControlledObjectPosition(int playerIncrement, Vector3 pos)
        {
            playerControlledObject[playerIncrement].transform.position += pos;
        }

    }
}
