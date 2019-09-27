using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror
{
    /// <summary>
    /// This component works in conjunction with the NetworkRoomManager to make up the multiplayer room system.
    /// <para>The RoomPrefab object of the NetworkRoomManager must have this component on it. This component holds basic room player data required for the room to function. Game specific data for room players can be put in other components on the RoomPrefab or in scripts derived from NetworkRoomPlayer.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mirror/NetworkRoomPlayer")]
    [HelpURL("https://mirror-networking.com/xmldocs/Components/NetworkRoomPlayer.html")]
    public class NetworkRoomPlayer : NetworkBehaviour
    {
        public NetworkRoomManager networkRoomManager;

        /// <summary>
        /// This flag controls whether the default UI is shown for the room player.
        /// <para>As this UI is rendered using the old GUI system, it is only recommended for testing purposes.</para>
        /// </summary>
        public bool showRoomGUI = true;

        /// <summary>
//        /// This is a flag that control whether this player is ready for the game to begin.
        /// This is a flag that control whether this player including all local players are ready for the game to begin.
        /// <para>When all players are ready to begin, the game will start. This should not be set directly, the SendReadyToBeginMessage function should be called on the client to set it on the server.</para>
        /// </summary>
        [SyncVar(hook = nameof(ReadyStateChanged))]
        public bool readyToBegin;

        /// <summary>
        /// Current index of the player, e.g. Player1, Player2, etc.
        /// </summary>
        [SyncVar]
        //public int index;
        public int clientIndex;

        /// <summary>
        /// Current summary of the all local player on this client ( 1 ~ NetworkRoomManager.maxLocalPlayer )
        /// </summary>
        [SyncVar]
        public int localPlayerCount;

        #region Unity Callbacks

        /// <summary>
        /// Do not use Start - Override OnStartrHost / OnStartClient instead!
        /// </summary>
        public void Start()
        {
            if (NetworkManager.singleton is NetworkRoomManager room)
            {
                // NetworkRoomPlayer object must be set to DontDestroyOnLoad along with NetworkRoomManager
                // in server and all clients, otherwise it will be respawned in the game scene which would 
                // have undesireable effects.
                if (room.dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);

                OnClientEnterRoom();
            }
            else
                Debug.LogError("RoomPlayer could not find a NetworkRoomManager. The RoomPlayer requires a NetworkRoomManager object to function. Make sure that there is one in the scene.");


        }

        public override void OnStartClient()
        {
            if (LogFilter.Debug) Debug.LogFormat("OnStartClient {0}", SceneManager.GetActiveScene().name);

            base.OnStartClient();

            networkRoomManager = NetworkManager.singleton as NetworkRoomManager;

            gameObject.tag = "NetworkRoomPlayer";

            gameObject.name = "RoomPlayer_Client" + clientIndex;

            // UpdateLocalPlayerCount();

            Debug.Log("clientIndex: " + clientIndex + " ,localPlayerCount: " + localPlayerCount, gameObject);
            
            // load additional local players from non-local clients

            /*
            if (isLocalPlayer && isServer)
            {
                Debug.Log("isLocalPlayer && isServer");
                AddLocalPlayer();
            }
            else if (isLocalPlayer && !isServer)
            {
                Debug.Log("isLocalPlayer && !isServer");
                CmdAddLocalPlayer();
            }
            Debug.Log("asd && !zxc");
            */

            if (!isServer)
            {
                CmdAddLocalPlayer();
            }
            else
            {
                AddLocalPlayer();
            }

        }

        #endregion

        #region Commands

        [Command]
        public void CmdChangeReadyState(bool readyState)
        {
            ChangeReadyState(readyState);
        }

        void ChangeReadyState(bool readyState)
        {
            readyToBegin = readyState;
            if (networkRoomManager != null)
            {
                networkRoomManager.ReadyStatusChanged();
            }
        }

        #endregion

        #region SyncVar Hooks

        void ReadyStateChanged(bool newReadyState)
        {
            OnClientReady(newReadyState);
        }

        #endregion

        #region Room Client Virtuals

        /// <summary>
        /// This is a hook that is invoked on all player objects when entering the room.
        /// <para>Note: isLocalPlayer is not guaranteed to be set until OnStartLocalPlayer is called.</para>
        /// </summary>
        public virtual void OnClientEnterRoom()
        {
            if (LogFilter.Debug) Debug.LogFormat("OnClientEnterRoom {0}", SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// This is a hook that is invoked on all player objects when exiting the room.
        /// </summary>
        public virtual void OnClientExitRoom()
        {
            if (LogFilter.Debug) Debug.LogFormat("OnClientExitRoom {0}", SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// This is a hook that is invoked on clients when a RoomPlayer switches between ready or not ready.
        /// <para>This function is called when the a client player calls SendReadyToBeginMessage() or SendNotReadyToBeginMessage().</para>
        /// </summary>
        /// <param name="readyState">Whether the player is ready or not.</param>
        public virtual void OnClientReady(bool readyState) { }

        #endregion

        [Header("Local player param.")]
        [SerializeField]
        public SyncListString listLocalPlayerName = new SyncListString();
        

        /// <summary>
        /// temporary parameter used for updating localPlayerName
        /// </summary>
        private string tempPlayerName;

        [SerializeField]
        public SyncListBool listLocalPlayerReadyToBegin = new SyncListBool();


        #region Optional UI


        /// <summary>
        /// Calculate Rect for UI elemets
        /// </summary>
        /// <param name="clientIndex"></param>
        /// <param name="localPlayerIndex"></param>
        /// <returns></returns>
        private Rect UpdateGUIArea(int clientIndex, int localPlayerIndex)
        {
            Rect rect = new Rect(10f + 150f * (localPlayerIndex), 170f + clientIndex * 140f, 140f, 130f);

            return rect;
        }

        private int localPlayerIncrement;

        /// <summary>
        /// Render a UI for the room.   Override to provide your on UI
        /// </summary>
        public virtual void OnGUI()
        {
            if (!showRoomGUI)
                return;

            if (networkRoomManager)
            {
                if (!networkRoomManager.showRoomGUI)
                    return;

                if (SceneManager.GetActiveScene().name != networkRoomManager.RoomScene)
                    return;

                if (localPlayerCount == 0)
                {
                    // do not renderGUI if there is no local players 
                    return;
                }

                for (localPlayerIncrement = 0; localPlayerIncrement < localPlayerCount; localPlayerIncrement++)
                {
                    // do only for client player, additional local players will have their own GUI scripts

                    GUI.Box(UpdateGUIArea(clientIndex,localPlayerIncrement), "");
                    GUILayout.BeginArea(UpdateGUIArea(clientIndex, localPlayerIncrement));

                    // display some matching index
                    GUILayout.Label("\t" + clientIndex + " / " + localPlayerIncrement);

                    // PLAYER name
                    if (isLocalPlayer)
                    {
                        tempPlayerName = GUILayout.TextField(listLocalPlayerName[localPlayerIncrement], 19);
                        if (tempPlayerName != listLocalPlayerName[localPlayerIncrement])
                        {
                            // CmdChangeClientPlayerName(tempPlayerName);
                            CmdChangePlayerName(localPlayerIncrement,tempPlayerName);
                        }
                    }
                    else
                    {
                        GUILayout.Label(listLocalPlayerName[localPlayerIncrement]);
                    }


                    // READY / NOT READY
                    if (listLocalPlayerReadyToBegin[localPlayerIncrement])
                        GUILayout.Label("Ready");
                    else
                        GUILayout.Label("Not Ready");

                    // READY / CANCEL BUTTON
                    if (NetworkClient.active && isLocalPlayer)
                    {
                        if (listLocalPlayerReadyToBegin[localPlayerIncrement])
                        {
                            if (GUILayout.Button("Cancel"))
                            {
                                Debug.Log("localPlayerIndex::" + localPlayerIncrement + " , status::" + false, gameObject);
                                CmdLocalPlayerReadyToBegin(localPlayerIncrement, false);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Ready"))
                            {
                                Debug.Log("localPlayerIndex::" + localPlayerIncrement + " , status::" + true, gameObject);
                                CmdLocalPlayerReadyToBegin(localPlayerIncrement,true);

                            }
                        }

                        // REMOVE ONLINE PLAYER
                        if (((isServer && clientIndex > 0) || isServerOnly) && GUILayout.Button("KICK OUT"))
                        {
                            // This button only shows on the Host for all players other than the Host
                            // Host and Players can't remove themselves (stop the client instead)
                            // Host can kick a Player this way.
                            GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
                        }

                        // ADD LOCAL PLAYER
                        if (isLocalPlayer &&
                            localPlayerIncrement == 0 &&    // display only on first local player GUI
                            networkRoomManager.allowLocalPlayers && // more than 1 local player is allowed
                            localPlayerCount < networkRoomManager.maxLocalPlayers &&    // localPlayerCount isnt greater than limit
                            GUILayout.Button("Add local player"))
                        {
                            // create local player
                            Debug.Log("Add local player");
                            // Cmd !!!
                            CmdAddLocalPlayer();
                        }

                        // ADD LOCAL PLAYER
                        if (isLocalPlayer && localPlayerIncrement == 1 && GUILayout.Button("Remove local player"))
                        {
                            // create local player
                            Debug.Log("Add local player");
                            // Cmd !!!
                            CmdRemoveLastLocalPlayer();
                        }
                        
                    }
                    GUILayout.EndArea();
                    
                }

            }
        }

        #endregion

        // this will be populated when START GAME button is pressed (OnRoomServerSceneLoadedForPlayer())
        
        // public GameObject gamePlayerGO;

        // [SerializeField]
        // public GamePlayer gamePlayer;
        
        #region AddLocalPlayer , source: this.OnStartClient(); RoomLocalPlayer.OnGUI;

        // this will create new local player instats

        [ContextMenu("CmdAddLocalPlayer")]
        [Command]
        void CmdAddLocalPlayer()
        {
            AddLocalPlayer();
            //RpcAddLocalPlayer();
        }

        [ClientRpc]
        void RpcAddLocalPlayer()
        {
            if (isServer)
            {
                return;
            }
            AddLocalPlayer();
        }

        void AddLocalPlayer()
        {
            Debug.Log("AddLocalPlayer");
            listLocalPlayerName.Add(clientIndex + " / local player " +localPlayerCount);
            listLocalPlayerReadyToBegin.Add(false);

            UpdateLocalPlayerCount();

        }

        #endregion

        #region RemoveLocalPlayer , source: this.OnGUI

        [Command]
        public void CmdRemoveLastLocalPlayer()
        {
            RemoveLastLocalPlayer();
            //RpcRemoveLastLocalPlayer();
        }

        [ClientRpc]
        void RpcRemoveLastLocalPlayer()
        {
            if (isServer)
            {
                return;
            }
            RemoveLastLocalPlayer();
        }

        void RemoveLastLocalPlayer()
        {
            // remove last local player
            listLocalPlayerName.RemoveAt(localPlayerCount-1);
            listLocalPlayerReadyToBegin.RemoveAt(localPlayerCount-1);
            UpdateLocalPlayerCount();

        }

        #endregion

        #region ChangePlayerName

        [Command]
        void CmdChangePlayerName(int localPlayerIndex, string newName)
        {
            ChangePlayerName(localPlayerIndex,newName);
        }

        void ChangePlayerName(int localPlayerIndex, string newName)
        {
            listLocalPlayerName[localPlayerIndex] = newName;
        }
        
        #endregion
        
        #region LocalPlayerReadyToBegin

        [Command]
        void CmdLocalPlayerReadyToBegin(int localPlayerIndex, bool status)
        {
            LocalPlayerReadyToBegin(localPlayerIndex, status);
        }

        void LocalPlayerReadyToBegin(int localPlayerIndex , bool status)
        {
            Debug.Log("LocalPlayerReadyToBegin() localPlayerIndex::" + localPlayerIndex + " , status::" + status,gameObject);
            listLocalPlayerReadyToBegin[localPlayerIndex] = status;
            RecalculateLocalPlayersReadyState();
        }

        #endregion

        [Server]
        public void RecalculateLocalPlayersReadyState()
        {
            
            bool ready = true;

            foreach (bool boo in listLocalPlayerReadyToBegin)
            {
                ready = boo && ready;
            }
            
            if (ready)
            {
                // all are ready
                //CmdChangeReadyState(true);
                ChangeReadyState(true);
            }
            else
            {
                Debug.Log("Not all local players are ready");
                //CmdChangeReadyState(false);
                ChangeReadyState(false);
            }
    
        }
        
        // check if localplayercoutn is correct
        [Server]
        private void UpdateLocalPlayerCount()
        {
            if (listLocalPlayerName.Count != listLocalPlayerReadyToBegin.Count)
            {
                Debug.LogError("Error::localPlayerCount");
            }
            else
            {
                localPlayerCount = listLocalPlayerName.Count;
                Debug.Log(" localPlayerCount (" + localPlayerCount + ") updated",gameObject);
            }
        }
        
    }
}
