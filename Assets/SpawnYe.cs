using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnYe : MonoBehaviour
{
    [SerializeField] private GameObject ye;
    [SerializeField] private int spawnCount;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject newYe = Instantiate(ye);
            newYe.transform.position = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
