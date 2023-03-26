using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;
using System.Linq;
using System;


public class FollowHelper
{
    
    private Vector3 localGrabPoint;
    private Quaternion localGrabRotation;
    private Transform transform;
    private Transform follow;
    public Transform attchment_transform;
    public bool contacting;

    public bool IsGrasped => follow != null;

    public FollowHelper(Transform transform)
    {
        this.transform = transform;
    }

    public void Grasp(Hand controller)
    {
        var handTransform = controller.transform;
        localGrabPoint = handTransform.InverseTransformPoint(transform.position);
        localGrabRotation = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
        follow = handTransform;
    }

    public void Release(Hand controller)
    {
        follow = null;
    }

    public bool Update()
    {
        if (follow)
        {
            if (contacting)
            {
                Quaternion buffer = attchment_transform.rotation;
                List<Quaternion> rotation_list = new List<Quaternion>();

                int[] arr = { 0, 90,  180, 270  };
                var combinations = arr.SelectMany((_, i) => arr.SelectMany((_, j) => arr.Select((_, k) => new int[] { arr[i], arr[j], arr[k] })));

                foreach (var combination in combinations)
                {
                    rotation_list.Add(new Quaternion() {eulerAngles = buffer.eulerAngles + new Vector3(combination[0], combination[1], combination[2]) });
                }

                List<float> rotation_diviation = new List<float>();
                foreach (var item in rotation_list)
                {
                    rotation_diviation.Add((transform.rotation.eulerAngles - item.eulerAngles).magnitude);
                }

                int min_idx = 0;
                float minval = (transform.rotation.eulerAngles - rotation_list[min_idx].eulerAngles).magnitude;
                
                for (int i = 1; i < rotation_list.Count; i++)
                {
                    if ((transform.rotation.eulerAngles - rotation_list[i].eulerAngles).magnitude < minval)
                    {
                        minval = (transform.rotation.eulerAngles - rotation_list[i].eulerAngles).magnitude;
                        min_idx = i;
                    }
                }
                transform.rotation = rotation_list[min_idx];
            }
            else
            {
                transform.rotation = follow.rotation * localGrabRotation;
            }
            
            transform.position = follow.TransformPoint(localGrabPoint);
            return true;
        }
        return false;
    }
}
