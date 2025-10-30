using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


public class  tester :MonoBehaviour
{
    InputActionAsset asset;
    public UnityEventFromInputAction action;
    private void Start()
    {
        action.OnStart(asset.FindAction("test1"));
    }
}


/// <summary>
/// <para>A MonoBehaviour component that acts as a relay, converting a specific
/// <see cref="InputAction"/>'s 'performed' event into a standard <see cref="UnityEvent"/>.</para>
///
/// <para>This allows other scripts or Inspector-assigned methods to subscribe to input actions
/// without needing direct knowledge of the Unity Input System package.</para>
/// </summary>
/// <remarks>
/// Assign the desired <see cref="InputActionReference"/> in the Inspector.
/// Methods can then be assigned to the <see cref="OnActionPerformed"/> UnityEvent in the Inspector,
/// or subscribed to programmatically.
/// </remarks>
[DefaultExecutionOrder(-100), Serializable] // Ensures this script runs before most others to capture input early.
public class UnityEventFromInputAction
{
    /// <summary>
    /// A reference to the <see cref="InputAction"/> that this component will listen to.
    /// Drag the specific Input Action (e.g., 'Player/Jump') from your
    /// </summary>
    [Tooltip("Drag the specific Input Action (e.g., 'Player/Jump') from your Input Actions here.")]
    [SerializeField]
    private  InputAction inputActionRef;

    /// <summary>
    /// This <see cref="UnityEvent"/> is invoked when the assigned <see cref="InputAction"/>
    /// transitions to its 'performed' phase.
    /// Methods can be assigned to this event in the Unity Inspector or subscribed to programmatically.
    /// </summary>
    [Tooltip("This event is invoked when the assigned Input Action is 'performed'.")]
    [SerializeField]
    public UnityEvent OnActionPerformed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityEventFromInputAction"/> class.
    /// Sets the <see cref="inputActionRef"/> to null and initializes <see cref="OnActionPerformed"/>
    /// as a new <see cref="UnityEvent"/>.
    /// </summary>
    public UnityEventFromInputAction()
    {
        inputActionRef = null;
        OnActionPerformed = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityEventFromInputAction"/> class,
    /// linking it to an existing <see cref="UnityEvent"/>.
    /// </summary>
    /// <param name="event"> The <see cref="UnityEvent"/> instance to link to <see cref="OnActionPerformed"/>.</param>
    public UnityEventFromInputAction(UnityEvent @event)
    {
        inputActionRef = null;
        OnActionPerformed = @event;
    }

    public void OnStart(InputAction actionLink)
    {
        // Ensure the Input Action Reference and its underlying Action are valid
        if (inputActionRef != null && inputActionRef != null)
        {
            // Subscribe to the 'performed' phase of the Input Action.
            inputActionRef.performed += HandleInputActionPerformed;
            inputActionRef.Enable();
        }
        else
        {
            Debug.LogWarning($"Input Action Reference or its Action is not assigned. Please assign an Input Action in the Inspector.");
        }
    }

    public void OnDisable()
    {
        // Unsubscribe from the 'performed' event and disable the action
        if (inputActionRef != null && inputActionRef != null)
        {
            inputActionRef.performed -= HandleInputActionPerformed;
            inputActionRef.Disable();
        }
    }

    /// <summary>
    /// Callback method for when the Input Action is performed.
    /// It reads the value (if any) and invokes the appropriate UnityEvent.
    /// </summary>
    /// <param name="context">The context of the performed input action.</param>
    private void HandleInputActionPerformed(InputAction.CallbackContext _)
    {
        // Invoke the basic UnityEvent
        OnActionPerformed?.Invoke();
    }
}

