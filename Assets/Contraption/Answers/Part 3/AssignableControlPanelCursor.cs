using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;

public class AssignableControlPanelCursor : MonoBehaviour, IGraspable
{
    private FollowHelper follow;

    private void Awake()
    {
        follow = new FollowHelper(transform);
    }

    public void Grasp(Hand controller)
    {
        follow.Grasp(controller);
    }

    public void Release(Hand controller)
    {
        follow.Release(controller);
        Drop();
    }

    void Update()
    {
        follow.Update();
    }

    void Drop()
    {
        GetComponentInParent<AssignableControlPanel>().Assign(GetComponent<Collider>());
        transform.localPosition = Vector3.zero;
    }
}
