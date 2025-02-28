using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arrow : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 velocity;
    bool stuck = false;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();    
        velocity = rb.velocity;    
    }

    // Update is called once per frame
    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("wall")){
            //get stuck
            stuck = true;
            rb.velocity = Vector2.zero;
        }
        else if(collision.gameObject.layer == LayerMask.NameToLayer("player")) {
            print("OUCH");
            Destroy(gameObject);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("wall")){
            //get unstuck
            stuck = false;
            rb.velocity = velocity;
        }
    }
}
