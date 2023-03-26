using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This is used to tell the ContraptionManager that a MonoBehaviour is a machine
/// part. All Parts are Spawnable, so for convenience we can also define 
/// INetworkSpawnable here.
/// </summary>
public interface IComponent : INetworkSpawnable
{
    /// <summary>
    /// Forces the Component to send an update even if nothing on it has changed
    /// This may be done if a new player joins a room, for example, and needs
    /// to sync.
    /// </summary>
    public void SendUpdate();
}

/// <summary>
/// This is used to tell the ContraptionManager that the part has a Control 
/// Variable that can be set
/// </summary>
public interface IVariable
{
    float Value { get; set; }
}

/// <summary>
/// The ContraptionManager is the "game manager" Networked Component. Though each
/// Component knows how to behave individually (things like Attach, or Grasp), 
/// the manager keeps track of the machine as a whole, and the simulation state.
/// </summary>
public class ContraptionManager : MonoBehaviour
{
    public NetworkContext context;
    private NetworkId simulationOwner;
    private NetworkId me;

    private class MachinePart
    {
        public MachinePart(IComponent component)
        {
            this.component = component;
            monoBehaviour = component as MonoBehaviour;
            rigidBody = monoBehaviour.GetComponent<Rigidbody>();
            transform = monoBehaviour.transform;
            variable = component as IVariable;
        }

        public MonoBehaviour monoBehaviour;
        public IComponent component;
        public Transform transform;
        public Rigidbody rigidBody;
        public Vector3 startPosition;
        public Quaternion startRotation;
        public IVariable variable;
    }

    private Dictionary<Transform, MachinePart> components = new Dictionary<Transform, MachinePart>();

    void Start()
    {
        context = NetworkScene.Register(this);
        me = context.Scene.Id;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            AddComponents(root);
        }

        NetworkSpawnManager.Find(this).OnSpawned.AddListener(OnSpawned);

        RoomClient.Find(this).OnJoinedRoom.AddListener(OnRoom);
    }

    void OnRoom(IRoom other)
    {
        RequestMachineState();
    }

    void OnSpawned(GameObject o, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        o.transform.position = transform.position + Vector3.up * 0.5f; // Put newly spawned Components in an easy to access location

        AddComponents(o);
    }

    void AddComponents(GameObject root)
    {
        foreach (var item in root.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is IComponent).Select(mb => new MachinePart(mb as IComponent)))
        {
            components.Add(item.transform, item);
        }
    }

    public void StartSimulation()
    {
        // If someone else has started the simulation, then don't do anything,
        // or our local simulation will conflict with the one being run remotely.

        if(simulationOwner.Valid)
        {
            return;
        }

        foreach (var item in components.Values)
        {
            if (!item.monoBehaviour.name.Contains("Control Panel")) 
            {
                Collider collider = item.monoBehaviour.GetComponent<Collider>();
                collider.isTrigger = false;
            } 
            
            item.startPosition = item.transform.position;
            item.startRotation = item.transform.rotation;
            if(item.rigidBody)
            {
                item.rigidBody.isKinematic = false;
            }
        }

        // Tell everyone else that we have started the simulation

        UpdateSimulationOwner(me); // Note how we are using the Scene Id, not the Manager's Id, because the Scene Id uniquely identifiers the computer
    }

    public void StopSimulation()
    {
        // If we started the simulation, then we can immediately end it.

        if (simulationOwner == me)
        {
            foreach (var item in components.Values)
            {
                if (!item.monoBehaviour.name.Contains("Control Panel"))
                {
                    // for assembly
                    Collider collider = item.monoBehaviour.GetComponent<Collider>();
                    collider.isTrigger = true;
                }
                

                if (item.rigidBody)
                {
                    item.rigidBody.isKinematic = true;
                }
                item.transform.position = item.startPosition;
                item.transform.rotation = item.startRotation;
            }

            // Send one last update as soon as the objects are back in their 
            // starting positions.

            UpdateMachineComponents();
        }

        // Setting the owner to Null requests any current owners stop their
        // simulations; if the owner was us we stopped it above, so this will
        // have no effect.

        UpdateSimulationOwner(NetworkId.Null);
    }

    public void SetVariable(float value)
    {
        foreach (var item in components.Values)
        {
            if (item.variable != null)
            {
                item.variable.Value = value;
            }
        }
    }

    private struct Message
    {
        public NetworkId SimulationOwner;
        public bool RequestState;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage m)
    {
        var message = m.FromJson<Message>();

        if (message.RequestState)
        {
            UpdateMachineComponents();
        }
        else
        {
            if (simulationOwner == me && !message.SimulationOwner.Valid) // We are running the simulation, and someone else has requested we stop it.
            {
                StopSimulation();
            }
            simulationOwner = message.SimulationOwner;
        }
    }

    private void UpdateSimulationOwner(NetworkId id)
    {
        context.SendJson(new Message()
        {
            SimulationOwner = id,
            RequestState = false
        });
        simulationOwner =  id;
    }

    private void RequestMachineState()
    {
        context.SendJson(new Message()
        {
            RequestState = true
        });
    }

    private void UpdateMachineComponents()
    {
        foreach (var item in components.Values)
        {
            item.component.SendUpdate();
        }
    }

    private void Update()
    {
        if(simulationOwner == me)
        {
            UpdateMachineComponents();
        }
    }

    /// <summary>
    /// Find all the Colliders that the Collider collider is touching.
    /// </summary>
    public IEnumerable<Collider> GetTouchingColliders(Collider collider)
    {
        foreach (var component in components.Values)
        {
            foreach (var componentCollider in component.monoBehaviour.GetComponentsInChildren<Collider>())
            {
                if (componentCollider == collider)
                {
                    continue;
                }

                Vector3 direction;
                float depth;

                if (Physics.ComputePenetration(componentCollider, component.transform.position, component.transform.rotation,
                        collider, collider.transform.position, collider.transform.rotation,
                        out direction,
                        out depth))
                {
                    yield return componentCollider;
                }
            }
        }
    }

    /// <summary>
    /// Find all the RigidBodies that the Collider collider is touching.
    /// </summary>
    public IEnumerable<Rigidbody> GetTouchingRigidBodies(Collider collider)
    {
        return GetTouchingColliders(collider).Where(c => c.attachedRigidbody != null).Select(c => c.attachedRigidbody);
    }

    public Rigidbody GetComponentRigidBody(NetworkId id)
    {
        foreach (var item in components.Values)
        {
            if(item.component.NetworkId == id)
            {
                return item.rigidBody;
            }
        }
        return null;
    }

    public IComponent GetComponent(NetworkId id)
    {
        foreach (var item in components.Values)
        {
            if (item.component.NetworkId == id)
            {
                return item.component;
            }
        }
        return null;
    }

    public NetworkId GetNetworkId(Transform transform)
    {
        if (transform && components.ContainsKey(transform))
        {
            return components[transform].component.NetworkId;
        }
        else
        {
            return NetworkId.Null;
        }
    }

    public Vector3 GetLocalPosition(Transform component)
    {
        return transform.InverseTransformPoint(component.position);
    }

    public Quaternion GetLocalRotation(Transform component)
    {
        return Quaternion.Inverse(transform.rotation) * component.rotation;
    }

    public Vector3 GetWorldPosition(Vector3 localPosition)
    {
        return transform.TransformPoint(localPosition);
    }

    public Quaternion GetWorldRotation(Quaternion localRotation)
    {
        return transform.rotation * localRotation;
    }


}
