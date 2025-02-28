using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    SpriteRenderer sprite;
    Vector3 position;
    public Collider2D maskCollider;
    public Rigidbody2D maskRigidbody;
    public Sprite pointer, hover, grab;
    [HideInInspector] public bool hovering = false, grabbed = false;

    Vector2 pMousePos = Vector2.zero;

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        position = Camera.main.ScreenToWorldPoint(mousePos());
        position.z = 0;

        transform.position = position;
        if(grabbed){
            Vector2 distance = (Vector2)position - maskRigidbody.position;
            print(distance.magnitude);
            if(distance.magnitude > 0.08f)
                maskRigidbody.velocity = distance * 40; // change value based on how big distance is
            else
                maskRigidbody.velocity = Vector2.zero;
            // maskRigidbody.position = position;
        }

        if(Input.GetMouseButtonDown(0) && hovering) {
            grabbed = true;
            sprite.sprite = grab;
            maskRigidbody.velocity = Vector3.zero;
        }
        else if(Input.GetMouseButtonUp(0) && grabbed) {
            grabbed = false;
            sprite.sprite = hover;
            Vector2 cMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 diff = cMousePos - pMousePos;
            maskRigidbody.velocity = diff * 100;
            
        }
        pMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("grabInner") && !grabbed) {
            hovering = true;
            sprite.sprite = hover;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("grabOuter")){
            hovering = false;
            sprite.sprite = pointer;
            grabbed = false;
        }
    }

    Vector3 mousePos(){
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        return mousePos;
    }

    void OnApplicationFocus(){
        Cursor.visible = false;
    }
}
