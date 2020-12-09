using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class traslationChecker : MonoBehaviour
{
    // Start is called before the first frame update
    public Quaternion oldOrientation;
    public Vector3 oldForwardDir;
    public Vector3 oldUpDir;
    public Vector3 oldRightDir;
    void Start()
    {
        oldForwardDir = transform.forward;
        oldUpDir = transform.up;
        oldRightDir = transform.right;
        oldOrientation = transform.rotation;
        

    }

    // Update is called once per frame
   
    void Update()
    {
        
        
        //Debug.LogFormat("Direction x is ({0}), Direction y is ({1}), Direction z is ({2})",transform.right,transform.up,transform.forward);
        if (transform.rotation != oldOrientation)
        {
            Debug.Log((Quaternion.FromToRotation(oldForwardDir, transform.forward).eulerAngles));
            oldForwardDir = transform.forward;

        }
    }

    private void OnDrawGizmos()
    {
        
    }
    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }
}
