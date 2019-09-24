using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror
{
    public class RoomLocalPlayer : NetworkBehaviour
    {
        // param of local player
        public NetworkRoomPlayer networkRoomPlayer;

        [SerializeField]
        [SyncVar]
        public int localPlayerIndex;

        [SerializeField]
        [SyncVar]
        public string localPlayerName;

        [SerializeField]
        [SyncVar]
        public bool localPlayerReadyToBegin;

        // Start is called before the first frame update
        void Start()
        {

        }


        #region Optional UI

        /// <summary>
        /// temporary parameter used for updating localPlayerName
        /// </summary>
        private string tempPlayerName;

        /// <summary>
        /// Calculate Rect for UI elemets
        /// </summary>
        /// <param name="clientIndex"></param>
        /// <param name="localPlayerIndex"></param>
        /// <returns></returns>
        private Rect UpdateGUIArea(int clientIndex, int localPlayerIndex)
        {
            if (localPlayerIndex < 1)
                Debug.LogError("Incorrect localPlayerIndex",gameObject);

            Rect rect = new Rect(10f + 150 * (localPlayerIndex - 1), 170f + clientIndex * 140f, 140f, 130f);

            return rect;
        }


        /// <summary>
        /// Render a UI for the room.   Override to provide your on UI
        /// </summary>
        public virtual void OnGUI()
        {
            if (networkRoomPlayer.networkRoomManager)
            {
                if (!networkRoomPlayer.networkRoomManager.showRoomGUI)
                    return;

                if (SceneManager.GetActiveScene().name != networkRoomPlayer.networkRoomManager.RoomScene)
                    return;

                // do stuff from here
                
                GUI.Box(UpdateGUIArea(networkRoomPlayer.clientIndex,localPlayerIndex), "");
                GUILayout.BeginArea(UpdateGUIArea(networkRoomPlayer.clientIndex, localPlayerIndex));
                
                // display some matching index
                GUILayout.Label("\t" + networkRoomPlayer.clientIndex + " / " + localPlayerIndex);

                // PLAYER name
                if (isLocalPlayer)
                {
                    tempPlayerName = GUILayout.TextField(localPlayerName, 24);
                    //CmdChangePlayerName(PlayerName);
                }
                else
                {
                    GUILayout.TextField(localPlayerName);
                }


                // READY / NOT READY
                if (localPlayerReadyToBegin)
                    GUILayout.Label("Ready");
                else
                    GUILayout.Label("Not Ready");

                // READY / CANCEL BUTTON
                if (NetworkClient.active && isLocalPlayer)
                {
                    if (localPlayerReadyToBegin)
                    {
                        if (GUILayout.Button("Cancel"))
                        {
                            networkRoomPlayer.CmdChangeLocalReadyToBegin(localPlayerIndex, false);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Ready"))
                        {
                            networkRoomPlayer.CmdChangeLocalReadyToBegin(localPlayerIndex, true);

                            // recalculate ready status on client

                        }
                    }

                    // REMOVE ONLINE PLAYER
                    if (((isServer && networkRoomPlayer.clientIndex > 0) || isServerOnly) && GUILayout.Button("KICK OUT"))
                    {
                        // This button only shows on the Host for all players other than the Host
                        // Host and Players can't remove themselves (stop the client instead)
                        // Host can kick a Player this way.
                        GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
                    }

                    // ADD LOCAL PLAYER
                    if (isLocalPlayer && localPlayerIndex == 1 && networkRoomPlayer.listRoomLocalPlayer.Count < networkRoomPlayer.networkRoomManager.maxLocalPlayers && GUILayout.Button("Add local player"))
                    {
                        // create local player
                        Debug.Log("Add local player");
                        // Cmd !!!
                        networkRoomPlayer.CmdAddLocalPlayer();
                    }

                    // REMOVE LOCAL PLAYER
                    if (isLocalPlayer && localPlayerIndex == 2 && GUILayout.Button("Remove local player"))
                    {
                        // create local player
                        Debug.Log("Remove local player");
                        // Cmd !!!
                        networkRoomPlayer.CmdRemoveLastLocalPlayer();
                    }
                    
                }

                GUILayout.EndArea();
            }

        }

        #endregion

        public void RemoveThisRoomLocalPlayer()         // todo
        {
            // Debug.Log("Destroy this: " + this, this);
            // remove from list
            networkRoomPlayer.listRoomLocalPlayer.Remove(this);
            // update list
            networkRoomPlayer.localPlayerCount = networkRoomPlayer.listRoomLocalPlayer.Count;
            // destroy
            Destroy(this);
        }
    }
}

