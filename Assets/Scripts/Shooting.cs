using UnityEngine;

public class Shooting : MonoBehaviour
{
    //Variables
    public Transform FirePoint;
    public GameObject Fire;
    public GameObject HitPoint;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        RaycastHit hit;

        if(Physics.Raycast(FirePoint.position, transform.TransformDirection(Vector3.forward), out hit, 100))
        {
            Debug.DrawRay(FirePoint.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

            GameObject a = Instantiate(Fire, FirePoint.position, Quaternion.identity);
            GameObject b = Instantiate(HitPoint, hit.point, Quaternion.identity);

            Destroy(a, 1);
            Destroy(b, 1);
        }
    }
}
