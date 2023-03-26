using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Extensions;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;
using UnityEngine;

// the alignment functionality modified FollowHelper and this file. 

public class Beam : MonoBehaviour, IGraspable, IComponent
{
    /// <summary>
    /// // This property fulfils INetworkSpawnable. Spawnable objects need to 
    /// have their Ids set by the Object Spawner before they are registered, so
    /// all spawned objects can communicate with eachother.
    /// </summary>
    public NetworkId NetworkId { get; set; } 

    private FollowHelper follow;
    private NetworkContext context;
    private ContraptionManager manager;
    Collider collider_istrigger;

    public void Grasp(Hand controller)
    {
        follow.Grasp(controller);
    }

    public void Release(Hand controller)
    {
        follow.Release(controller);
        Attach();
    }

    private void Awake()
    {
        follow = new FollowHelper(transform);
    }

    void Start()
    {
        context = NetworkScene.Register(this);
        manager = context.Scene.GetClosestComponent<ContraptionManager>();
    }

    public void Attach()
    {
        Attach(manager.GetTouchingRigidBodies(GetComponent<Collider>()).FirstOrDefault());
        SendUpdate();
    }

    private void Attach(Rigidbody parent)
    {
        if (parent)
        {
            transform.parent = parent.transform;
        }
        else
        {
            transform.parent = null;
        }
    }

    void Update()
    {
        if (follow.Update())
        {
            SendUpdate();
        }
    }

    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public NetworkId attachedId;
    }

    public void SendUpdate()
    {
        
        context.SendJson(new Message()
        {
            position = manager.GetLocalPosition(transform),
            rotation = manager.GetLocalRotation(transform),
            attachedId = manager.GetNetworkId(transform.parent)
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        Debug.Log("process message");
        var message = m.FromJson<Message>();
        transform.position = manager.GetWorldPosition(message.position);
        transform.rotation = manager.GetWorldRotation(message.rotation);
        Attach(manager.GetComponentRigidBody(message.attachedId));
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.name != "Manipulator")
        {
            follow.attchment_transform = other.transform;
            follow.contacting = true;
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.name != "Manipulator")
        {
            follow.attchment_transform = other.transform;
            follow.contacting = true;
        }
    }
    public void OnTriggerExit(Collider other)
    {
        follow.contacting = false;
    }

}
