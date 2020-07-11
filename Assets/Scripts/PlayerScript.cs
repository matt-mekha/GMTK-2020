using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    
    public GameScript gameScript;

    void OnCollisionEnter(Collision collision) {
        gameScript.OnCollision(collision);
    }
}
