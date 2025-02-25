using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Events;

public class Popup : Events.EventHandler.GameEventBehaviour
{
    protected bool isDone = false;
    protected CanvasGroup group = null;

    private float fadeSpeed = 5.0f;

    private Vector3 originalGravity;

    public static event Action Pause;
    public static event Action UnPause;

    public static bool IsPaused { get; private set; }

    private void OnEnable()
    {
        group = GetComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    public override void OnBegin(bool firstTime)
    {
        base.OnBegin(firstTime);

        originalGravity = Physics.gravity;

        Pause?.Invoke();
        IsPaused = true;
        Physics.gravity = new Vector3(0,0,0);

        isDone = false;
        gameObject.SetActive(true);
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;

        // Moves this popup to the bottom of the hierarchy to be in front of all other popups
        transform.SetAsLastSibling();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public virtual void OnOkay()
    {
        isDone = true;
        group.interactable = false;
    }
    public virtual void OnCancel()
    {
        isDone = true;
        group.interactable = false;
    }

    public override void OnEnd()
    {
        base.OnEnd();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0.0f;

        Physics.gravity = originalGravity;
        UnPause?.Invoke();
        IsPaused = false;
    }

    public override bool IsDone()
    {
        return isDone;
    }

    /* This function is used to check if the popup doesnt exist in the scene already,
     * if it does, it simply activates it. But if it doesn't, it creates a new instance of the object */
    public static T Create<T>() where T : Popup, Events.EventHandler.IEvent
    {
        // Look for an existing object of type T in the scene
        T existingObject = FindFirstObjectByType<T>();

        if (existingObject != null)
        {
            // If the object exists, activate its GameObject
            existingObject.gameObject.SetActive(true);

            // Push the event to the EventHandler if it's not already in the stack
            if (!Events.EventHandler.Main.EventStack.Contains(existingObject))
            {
                Events.EventHandler.Main.PushEvent(existingObject);
            }

            return existingObject; // Return the found object
        }
        else
        {
            // If no existing object, load the prefab
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/" + typeof(T).Name);
            if (prefab == null)
            {
                Debug.LogError($"Prefab not found: Prefabs/UI/{typeof(T).Name}");
                return null;
            }

            // Instantiate the prefab
            GameObject go = Instantiate(prefab);

            // Get the T component
            T newInstance = go.GetComponent<T>();
            if (newInstance == null)
            {
                Debug.LogError($"Component of type {typeof(T).Name} not found on prefab.");
                return null;
            }

            // Push the new event to the EventHandler
            Events.EventHandler.Main.PushEvent(newInstance);

            return newInstance;
        }
    }

}
