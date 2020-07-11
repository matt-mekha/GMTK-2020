using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckMovement : MonoBehaviour
{

    private const float wheelRotationSpeed = -500f;

    private const float noiseFrequencyX = 2f;
    private const float noiseFrequencyY = 4f;
    private const float noiseAmplitudeX = 0.5f;
    private const float noiseAmplitudeY = 0.5f;

    private float x = 0;
    private float y = 0;
    private Vector3 startPos;

    public GameObject[] wheels;

    void Start() {
        startPos = transform.localPosition;
    }

    void Update()
    {
        if(!GameScript.alive) return;

        x += Time.deltaTime * noiseFrequencyX;
        y += Time.deltaTime * noiseFrequencyY;

        transform.localPosition = startPos + new Vector3(Mathf.PerlinNoise(x, 0) * noiseAmplitudeX, Mathf.PerlinNoise(0, y) * noiseAmplitudeY, 0);

        float wheelDelta = Time.deltaTime * wheelRotationSpeed;
        foreach (GameObject wheel in wheels)
        {
            wheel.transform.Rotate(0, 0, wheelDelta);
        }
    }
}
