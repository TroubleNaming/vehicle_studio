using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;
using UnityEngine;
using Ubiq.Extensions;
using System;
using UnityEngine.UI;

public class ControlPanel : MonoBehaviour, IGraspable, IComponent
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
        foreach (var canvas in GetComponentsInChildren<Canvas>())
        {
            canvas.worldCamera = XRPlayerController.Singleton.headCamera;
        }
        context = NetworkScene.Register(this);
        manager = context.Scene.GetClosestComponent<ContraptionManager>();
    }

    void Update()
    {
        if (follow.Update())
        {
            SendUpdate();
        }
    }

    public void StartSimulation()
    {
        manager.StartSimulation();
    }

    public void StopSimulation()
    {
        manager.StopSimulation();
    }

    public void ChangeValue(Single value)
    {
        manager.SetVariable(value);
        SendUpdate(value);
    }

    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public float value;
    }

    // Only update the value when the user has actually changed it. This is 
    // because during the simulation the state will be sent each frame, but
    // both users should be able to adjust the slider all the time.

    private void SendUpdate(float value)
    {
        context.SendJson(new Message()
        {
            position = manager.GetLocalPosition(transform),
            rotation = manager.GetLocalRotation(transform),
            value = value
        });
    }

    public void SendUpdate()
    {
        context.SendJson(new Message()
        {
            position = manager.GetLocalPosition(transform),
            rotation = manager.GetLocalRotation(transform),
            value = float.NaN
        });
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        var message = m.FromJson<Message>();
        transform.position = manager.GetWorldPosition(message.position);
        transform.rotation = manager.GetWorldRotation(message.rotation);
        if (!float.IsNaN(message.value))
        {
            GetComponentInChildren<Slider>().SetValueWithoutNotify(message.value);
            manager.SetVariable(message.value);
        }
    }
}
