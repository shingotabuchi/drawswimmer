using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ye : MonoBehaviour
{
    void Update()
    {
        float moveTheta = Random.Range(0, 2f * Mathf.PI);
        transform.position += 0.05f * new Vector3(Mathf.Cos(moveTheta), Mathf.Sin(moveTheta), 0);
    }
}
