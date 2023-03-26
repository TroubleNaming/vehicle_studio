using System.Collections;
using System.Collections.Generic;
using Ubiq.Extensions;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;
using UnityEngine;

public class Body : MonoBehaviour, IGraspable, IComponent, IUseable
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
    private GameObject Owner;
    private bool IsOwn;

    public void Grasp(Hand controller)
    {
        follow.Grasp(controller);
    }

    public void Release(Hand controller)
    {
        follow.Release(controller);
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

    void Update()
    {
        if (IsOwn)
        {
            Owner.transform.position = transform.position;
        }
        if (follow.Update())
        {
            SendUpdate();
        }
    }

    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public void SendUpdate()
    {
        context.SendJson(new Message()
        {
            position = manager.GetLocalPosition(transform),
            rotation = manager.GetLocalRotation(transform),
        });

    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        var message = m.FromJson<Message>();
        transform.position = manager.GetWorldPosition(message.position);
        transform.rotation = manager.GetWorldRotation(message.rotation);
    }

    public void Use(Hand controller)
    {
        if (!IsOwn)
        {
            Owner = controller.transform.parent.gameObject;
            IsOwn = true;
        }
        else
        {
            var buffpos = Owner.transform.position;
            buffpos.y = 0;
            Owner.transform.position = buffpos;
            IsOwn = false;
        }

    }

    public void UnUse(Hand controller)
    {
        print("do nothing");
    }
}
