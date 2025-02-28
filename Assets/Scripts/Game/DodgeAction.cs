using Game;
using UnityEngine;
using Events;

public class DodgeAction : EventHandler.GameEvent, IPause
{
    private Vector3 dodgeDirection;
    private float dodgeSpeed;
    private float dodgeDuration;
    private float startTime;
    private float pauseStartTime;
    private float pausedDuration;
    private bool isDodging;
    private Player player;

    public DodgeAction(Player playerInstance)
    {
        player = playerInstance;
        isDodging = false;
    }

    public void StartDodge(Vector3 direction, float speed, float duration)
    {
        dodgeDirection = direction.normalized; // Normalize direction to ensure consistency
        dodgeSpeed = speed; // This represents the total distance to travel during the dodge
        dodgeDuration = duration;
        startTime = Time.time;
        pausedDuration = 0f;
        isDodging = true;
    }

    public void Execute()
    {
        if (!isDodging || Popup.IsPaused) return;

        float elapsedTime = Time.time - startTime - pausedDuration;
        if (elapsedTime < dodgeDuration)
        {
            float progress = elapsedTime / dodgeDuration;
            Vector3 move = dodgeDirection * dodgeSpeed * progress;
            Debug.Log($"Dodge Movement: {move}");
            player.Controller.Move(move);
        }
        else
        {
            isDodging = false;
            player.EndDodge();
        }
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
