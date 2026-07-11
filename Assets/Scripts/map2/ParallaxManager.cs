using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layer;
       [Range (0 ,1) ] public float parallaxFactor;
    }

    public ParallaxLayer[] layers ; 

    public Transform camTransform ;
    private Vector3 lastCameraPosition ;





    // Start is called before the first frame update
    void Start()
    {
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        
        if (camTransform != null)
        {
            lastCameraPosition = camTransform.position;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (camTransform == null) return;

        Vector3 cameraDelta = camTransform.position - lastCameraPosition;

        foreach(ParallaxLayer layer in layers)
        {
            float moveX =cameraDelta.x * layer.parallaxFactor;
            float moveY =cameraDelta.y * layer.parallaxFactor;

            layer.layer.position += new Vector3(moveX, moveY, 0);
        }
        lastCameraPosition = camTransform.position;
    }
}
