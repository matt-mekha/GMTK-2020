using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleScript : MonoBehaviour
{
    void OnHover(GameScript gameScript) {
        gameScript.OnHover(gameObject);
    }

    void OnCollision(GameScript gameScript) {
        gameScript.OnCollision(GetComponentInChildren<Rigidbody>());
    }
}
