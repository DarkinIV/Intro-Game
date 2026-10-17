using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaffController : MonoBehaviour
{
    public GameObject orbPrefab;
    public Transform spawnPoint;
    public float speed = 10f;
    public float fireRate = 2;
    private float nextFireTime = 0;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime) {
            GameObject orb = Instantiate(orbPrefab, spawnPoint.position, spawnPoint.rotation);
            Rigidbody orbBody = orb.GetComponent<Rigidbody>();
            orbBody.velocity = transform.forward * speed;
            nextFireTime = Time.time + 1f / fireRate;
            Destroy(orb, 3f);
        }
        
    }
}
