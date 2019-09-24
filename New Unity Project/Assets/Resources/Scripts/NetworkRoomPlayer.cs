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

            // client itself is count as first local player
            if (isLocalPlayer)
            {
                CmdAddLocalPlayer();
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

            }
        }

        #endregion

        [SerializeField]
        public List<RoomLocalPlayer> listRoomLocalPlayer = new List<RoomLocalPlayer>();

        #region AddLocalPlayer , source: this.OnStartClient(); RoomLocalPlayer.OnGUI;

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
            
        bool ready = true;

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

    }
}
