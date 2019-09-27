using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public class Spawn : NetworkBehaviour
    {
        public NetworkIdentity spawnPrefab;
        GameObject instantiateGameObject;
        
        public void SpawnPlayer(float x, float z,GameObject gamePlayer)
        {

            instantiateGameObject = Instantiate(spawnPrefab.gameObject, new Vector3(x * 3f, 1, z * 3f), Quaternion.identity);
            instantiateGameObject.name = "Cube[" + x +","+ z + "]";
            instantiateGameObject.transform.position = new Vector3(x * 5f, 0, z * 5f);
            instantiateGameObject.transform.SetParent(gamePlayer.transform);
            gamePlayer.GetComponent<GamePlayer>().playerControlledObject.Add(instantiateGameObject);
            
            NetworkServer.SpawnWithClientAuthority(instantiateGameObject,gamePlayer);

        }
    }
}
