using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestQuaternion : MonoBehaviour
{
    float angle;
    [SerializeField] private GameObject[] testOBJ;
    [SerializeField] private bool isMirror = false;
    private Quaternion adj_rotation = Quaternion.AngleAxis(90, Vector3.left);

    Vector3 start = new Vector3(0-2, 0, 0);
    Vector3 end = new Vector3(0-2, -3, 0);
    // (-0.59, -0.32, 0.15) to (-0.58, -0.43, 0.12)
    // (-0.58, -0.43, 0.12) to (-0.59, -0.53, 0.18)
    // Start is called before the first frame update
    void Start()
    {
        // 將物體從正前方轉向左側
        //transform.rotation = Quaternion.AngleAxis(-90, Vector3.up) * transform.rotation;
        // 將物體從正前方轉向下方
        //transform.rotation = Quaternion.AngleAxis(-90, Vector3.right) * transform.rotation;
        Debug.Log("_Test四元數" + transform.rotation);
        //transform.rotation = adj_rotation * adj_rotation;//Quaternion.LookRotation(Vector3.forward);
        Debug.Log("_Test四元數" + transform.rotation);

        Debug.Log("_Test四元數" + adj_rotation);
        Debug.Log("_Test四元數" + adj_rotation * adj_rotation);


        //Vector3 upwards = Vector3.up;
        //Vector3 _connDir = isMirror ? (start - end).normalized : (end - start).normalized;
        //Quaternion desiredRotation = Quaternion.LookRotation(_connDir, upwards); // 會將物體的Z軸對應第一個參數
        //Debug.Log("_Test四元數_右下" + desiredRotation);
        //transform.rotation = transform.rotation * Quaternion.AngleAxis(90, Vector3.right);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 _connDir = isMirror ? (start - end).normalized : (end - start).normalized;
        Quaternion desiredRotation = Quaternion.LookRotation(_connDir, Vector3.up);
        testOBJ[0].transform.rotation = desiredRotation * adj_rotation;

        desiredRotation = Quaternion.LookRotation(_connDir, Vector3.right);
        testOBJ[1].transform.rotation = desiredRotation;

        desiredRotation = Quaternion.LookRotation(_connDir, Vector3.forward);
        testOBJ[2].transform.rotation = desiredRotation;
    }
}
