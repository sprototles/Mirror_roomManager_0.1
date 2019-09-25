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

            clientPlayerName = "Client player " + clientIndex;

            // client itself is count as first local player

            Debug.Log("clientIndex: " + clientIndex + " ,localPlayerCount: " + localPlayerCount, gameObject);
            
            // load additional local players from non-local clients
            if (!isLocalPlayer && localPlayerCount > 1)
            {
                Debug.Log("Load: " + localPlayerCount + " players", gameObject);
                for (int i = 1; i <= localPlayerCount; i++)
                {
                    Debug.Log("Load player" + i, gameObject);
                    LoadLocalPlayer(i);
                }

                if (localPlayerCount != listRoomLocalPlayer.Count)
                {
                    Debug.LogError("One or more local players not loaded correctly");
                }
            }

        }

        #endregion

        #region Commands

        [Command]
        public void CmdChangeReadyState(bool readyState)
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

        #region Optional UI

        [Header("Local player param.")]

        [SerializeField]
        [SyncVar]
        public string clientPlayerName;

        /// <summary>
        /// temporary parameter used for updating localPlayerName
        /// </summary>
        private string tempPlayerName;

        [SerializeField]
        [SyncVar]
        bool clientReadyToBegin;

        /// <summary>
        /// Calculate Rect for UI elemets
        /// </summary>
        /// <param name="clientIndex"></param>
        /// <param name="localPlayerIndex"></param>
        /// <returns></returns>
        private Rect UpdateGUIArea(int clientIndex)
        {
            Rect rect = new Rect(10f , 170f + clientIndex * 140f, 140f, 130f);

            return rect;
        }


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

                // do only for client player, additional local players will have their own GUI scripts
                
                GUI.Box(UpdateGUIArea(clientIndex), "");
                GUILayout.BeginArea(UpdateGUIArea(clientIndex));

                // display some matching index
                GUILayout.Label("\t" + clientIndex + " / 0");

                // PLAYER name
                if (isLocalPlayer)
                {
                    tempPlayerName = GUILayout.TextField(clientPlayerName, 24);
                    if (tempPlayerName != clientPlayerName)
                    {
                        CmdChangeClientPlayerName(tempPlayerName);
                    }
                }
                else
                {
                    GUILayout.Label(clientPlayerName);
                }


                // READY / NOT READY
                if (clientReadyToBegin)
                    GUILayout.Label("Ready");
                else
                    GUILayout.Label("Not Ready");

                // READY / CANCEL BUTTON
                if (NetworkClient.active && isLocalPlayer)
                {
                    if (clientReadyToBegin)
                    {
                        if (GUILayout.Button("Cancel"))
                        {
                            CmdChangeClientReadyToBegin(false);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Ready"))
                        {
                            CmdChangeClientReadyToBegin(true);

                            // recalculate ready status on client

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
                    if (isLocalPlayer && listRoomLocalPlayer.Count < networkRoomManager.maxLocalPlayers && GUILayout.Button("Add local player"))
                    {
                        // create local player
                        Debug.Log("Add local player");
                        // Cmd !!!
                        CmdAddLocalPlayer();
                    }

                }
                GUILayout.EndArea();
            }
        }

        #endregion

        [SerializeField]
        public List<RoomLocalPlayer> listRoomLocalPlayer = new List<RoomLocalPlayer>();

        #region AddLocalPlayer , source: this.OnStartClient(); RoomLocalPlayer.OnGUI;

        // this will create new local player instats

        [ContextMenu("CmdAddLocalPlayer")]
        [Command]
        public void CmdAddLocalPlayer()
        {
            AddLocalPlayer();
            RpcAddLocalPlayer();
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
            listRoomLocalPlayer.Add(gameObject.AddComponent<RoomLocalPlayer>());

            localPlayerCount = listRoomLocalPlayer.Count;

            listRoomLocalPlayer[localPlayerCount - 1].networkRoomPlayer = this;

            // sync values with server or add default

            listRoomLocalPlayer[localPlayerCount - 1].localPlayerReadyToBegin = false;
            listRoomLocalPlayer[localPlayerCount - 1].localPlayerIndex = localPlayerCount;
            listRoomLocalPlayer[localPlayerCount - 1].localPlayerName = "Local player " + localPlayerCount;

        }

        #endregion

        #region LoadLocalPlayer ; this is called when client will join to server, where are already some players with some parameters
        // this will load local player already created on server
        // this will be executed only on local machine, since server and original client have that info

        void LoadLocalPlayer(int _localPlayerIndex)
        {
            Debug.Log("LoadLocalPlayer(" + _localPlayerIndex + ")",gameObject);

            listRoomLocalPlayer.Add(gameObject.AddComponent<RoomLocalPlayer>());
            Debug.Log("LoadLocalPlayer() :: AddComponent", gameObject);

            listRoomLocalPlayer[_localPlayerIndex - 1].networkRoomPlayer = this;
            Debug.Log("LoadLocalPlayer() :: networkRoomPlayer = this", gameObject);


            // get status from server
            CmdGetLocalPlayerReadyToBeginStatus(_localPlayerIndex);

            Debug.Log("CmdGetLocalPlayerReadyToBeginStatus = " + listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerReadyToBegin, gameObject);

            listRoomLocalPlayer[localPlayerCount - 1].localPlayerIndex = _localPlayerIndex;

            Debug.Log("localPlayerIndex = " + listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerIndex, gameObject);

            // get name from server
            CmdGetLocalPlayerName(_localPlayerIndex);
            Debug.Log("CmdGetLocalPlayerName = " + listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerName, gameObject);

        }

        
        [Command]
        void CmdGetLocalPlayerReadyToBeginStatus(int _localPlayerIndex)
        {
            TargetGetLocalPlayerReadyToBeginStatus(_localPlayerIndex, listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerReadyToBegin);
        }

        [TargetRpc]
        void TargetGetLocalPlayerReadyToBeginStatus(int _localPlayerIndex , bool loadedLocalPlayerReadyToBegin)
        {
            listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerReadyToBegin = loadedLocalPlayerReadyToBegin;
        }


        [Command]
        void CmdGetLocalPlayerName(int _localPlayerIndex)
        {
            TargetGetLocalPlayerName(_localPlayerIndex, listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerName);
        }

        [TargetRpc]
        void TargetGetLocalPlayerName(int _localPlayerIndex, string loadedLocalPlayerName)
        {
            listRoomLocalPlayer[_localPlayerIndex - 1].localPlayerName = loadedLocalPlayerName;
        }


        #endregion

        #region RemoveLocalPlayer , source: this.OnGUI

        [Command]
        public void CmdRemoveLastLocalPlayer()
        {
            RemoveLastLocalPlayer();
            RpcRemoveLastLocalPlayer();
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
            listRoomLocalPlayer[listRoomLocalPlayer.Count - 1].RemoveThisRoomLocalPlayer();
        }

        #endregion
        
        #region ChangeClientReadyToBegin , source: 

        [Command]
        void CmdChangeClientReadyToBegin(bool status)
        {
            ChangeClientReadyToBegin(status);
            RecalculateLocalPlayersReadyState();
            RpcChangeClientReadyToBegin(status);
        }

        [ClientRpc]
        void RpcChangeClientReadyToBegin(bool status)
        {
            if (isServer) return;
            ChangeClientReadyToBegin(status);
        }

        void ChangeClientReadyToBegin(bool status)
        {
            clientReadyToBegin = status;
        }

        #endregion

        #region ChangeLocalReadyToBegin , source: LocalPlayer.OnGUI

        [Command]
        public void CmdChangeLocalReadyToBegin(int localPlayerIndex, bool status)
        {
            ChangeLocalReadyToBegin(localPlayerIndex,status);
            RecalculateLocalPlayersReadyState();
            RpcChangeLocalReadyToBegin(localPlayerIndex,status);
        }

        [ClientRpc]
        void RpcChangeLocalReadyToBegin(int localPlayerIndex, bool status)
        {
            if (isServer) return;

            ChangeLocalReadyToBegin(localPlayerIndex,status);
        }

        void ChangeLocalReadyToBegin(int localPlayerIndex, bool status)
        {
            listRoomLocalPlayer[localPlayerIndex - 1].localPlayerReadyToBegin = status;
        }

        #endregion

        public void RecalculateLocalPlayersReadyState()
        {
            
        bool ready = clientReadyToBegin;

        // check every local player is ready ?
        foreach (RoomLocalPlayer roomPlayer in listRoomLocalPlayer)
        {
            ready = roomPlayer.localPlayerReadyToBegin && ready;   // if not ready, result will be false
        }

            if (ready)
            {
                // all are ready
                CmdChangeReadyState(true);
            }
            else
            {
                Debug.Log("Not all local players are ready");
                CmdChangeReadyState(false);
            }
    
        }
        
        #region ChangeLocalPlayerName , source: LocalPlayer.OnGUI
        
        [Command]
        public void CmdChangeLocalPlayerName(int playerIndex, string newName)
        {
            ChangeLocalPlayerName(playerIndex, newName);
            RpcChangeLocalPlayerName(playerIndex, newName);
        }

        [ClientRpc]
        void RpcChangeLocalPlayerName(int playerIndex, string newName)
        {
            if (isServer)
            {
                return;
            }
            ChangeLocalPlayerName(playerIndex, newName);
        }

        void ChangeLocalPlayerName(int playerIndex, string newName)
        {
            listRoomLocalPlayer[playerIndex - 1].localPlayerName = newName;
        }

        #endregion

        #region ChangeClientPlayerName , source: ClientPlayer.OnGUI

        [Command]
        public void CmdChangeClientPlayerName( string newName)
        {
            ChangeClientPlayerName( newName);
            RpcChangeClientPlayerName( newName);
        }

        [ClientRpc]
        void RpcChangeClientPlayerName(string newName)
        {
            if (isServer)
            {
                return;
            }
            ChangeClientPlayerName(newName);
        }

        void ChangeClientPlayerName(string newName)
        {
            clientPlayerName = newName;
        }

        #endregion



    }
}
