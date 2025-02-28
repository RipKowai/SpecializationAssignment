using Game;
using UnityEngine;
using Events;

public class DodgeAction : EventHandler.GameEvent, IPause
{
    private Vector3 dodgeDirection;
    private float dodgeSpeed = 10f;
    private float dodgeDuration = 0.2f;
    private float startTime;
    private float pauseStartTime;
    private float pausedDuration;
    private bool isDodging;
    private Player player;

    public DodgeAction(Player playerInstance, Vector3 dodgeDirection)
    {
        player = playerInstance;
        isDodging = false;
        this.dodgeDirection = dodgeDirection;
        Debug.Log(dodgeDirection);
    }

    public override void OnBegin(bool bFirstTime)
    {
        base.OnBegin(bFirstTime);
        startTime = Time.time;
        pausedDuration = 0f;
        isDodging = true;
    }

    public override void OnUpdate()
    {
        if (!isDodging || Popup.IsPaused) return;

        float elapsedTime = Time.time - startTime - pausedDuration;
        if (elapsedTime < dodgeDuration)
        {
            float progress = elapsedTime / dodgeDuration;
            Vector3 move = dodgeDirection; // Scale the movement by dodge speed and delta time
            Debug.Log($"Dodge Movement: {move}");
            //player.Controller.Move(move);

            player.transform.position += move * dodgeSpeed * Time.deltaTime;
        }
        else
        {
            isDodging = false;
        }
    }

    public override void OnEnd()
    {
        base.OnEnd();
        isDodging = false;
    }

    public override bool IsDone()
    {
        return !isDodging;
    }

    public virtual void Pause()
    {
        if (!isDodging) return;

        pauseStartTime = Time.time;
    }

    public virtual void Unpause()
    {
        if (!isDodging) return;

        pausedDuration += Time.time - pauseStartTime;
    }
}
