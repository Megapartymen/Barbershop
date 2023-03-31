using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtScript : MonoBehaviour
{
    public Transform Target;
    public bool IsOnlyY;

    private void Update()
    {
        LookAt(Target);
    }

    private void LookAt(Transform target)
    {
        if (target == null)
            transform.LookAt(Camera.main.transform.GetChild(0));
        else
            transform.LookAt(target);

        if (IsOnlyY)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0));
        }
    }
}
