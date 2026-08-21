using UnityEngine;

public class ShootAt : MonoBehaviour
{
    public ParticleSystem particleSystem;

    public Vector3 point;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {


        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            point = hit.point;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            Vector3 dir= (point - particleSystem.transform.position ).normalized;
            particleSystem.transform.forward = new Vector3(dir.x, 0, dir.z);
                
            
            particleSystem.Emit(1);

        }


    }
}
