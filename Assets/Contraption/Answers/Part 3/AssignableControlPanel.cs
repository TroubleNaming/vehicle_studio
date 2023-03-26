using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;
using UnityEngine;
using Ubiq.Extensions;
using System;
using UnityEngine.UI;
using System.Linq;

public class AssignableControlPanel : MonoBehaviour, IGraspable, IComponent
{
    /// <summary>
    /// // This property fulfils INetworkSpawnable. Spawnable objects need to 
    /// have their Ids set by the Object Spawner before they are registered, so
    /// all spawned objects can communicate with eachother.
    /// </summary>
    public NetworkId NetworkId { get; set; }

    public LineRenderer controlLineRenderer;

    private FollowHelper follow;
    private NetworkContext context;
    private ContraptionManager manager;
    private MonoBehaviour receiver;

    public void Grasp(Hand controller)
    {
        follow.Grasp(controller);
    }

    public void Release(Hand controller)
    {
        follow.Release(controller);
    }

    public void Assign(Collider cursor)
    {
        receiver = null;
        foreach (var collider in manager.GetTouchingColliders(cursor))
        {
            var variableReceiver = collider.GetComponents<MonoBehaviour>().Where(mb => mb is IVariable).FirstOrDefault();
            if(variableReceiver)
            {
                receiver = variableReceiver;
                break;
            }
        }
        SendUpdate();
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

        if(receiver != null)
        {
            controlLineRenderer.SetPosition(0, transform.position);
            controlLineRenderer.SetPosition(1, receiver.transform.position);
            controlLineRenderer.enabled = true;
        }
        else
        {
            controlLineRenderer.enabled = false;
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
        if (receiver)
        {
            (receiver as IVariable).Value = value;
        }
        SendUpdate(value);
    }

    private struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public float value;
        public NetworkId attachedId;
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
            attachedId = receiver ? manager.GetNetworkId(receiver.transform) : NetworkId.Null,
            value = value
        });
    }

    public void SendUpdate()
    {
        context.SendJson(new Message()
        {
            position = manager.GetLocalPosition(transform),
            rotation = manager.GetLocalRotation(transform),
            attachedId = receiver ? manager.GetNetworkId(receiver.transform) : NetworkId.Null,
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
        receiver = manager.GetComponent(message.attachedId) as MonoBehaviour;
    }
}
