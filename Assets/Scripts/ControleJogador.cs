using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControleJogador : MonoBehaviour
{
    private Rigidbody rb;
    public float velocidade;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();   
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float pulo = 0.0f;
        if (Input.GetKey(KeyCode.Space)) {
            pulo = 5;
        }

        Vector3 mov = new Vector3(h, pulo, v);
        rb.AddForce(mov*velocidade);
    }
}
