using Game;
using UnityEngine;

public class DodgeAction
{
    public bool IsPaused { get; private set; }
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
        IsPaused = false;
        isDodging = false;
    }

    public void StartDodge(Vector3 direction, float speed, float duration)
    {
        dodgeDirection = direction.normalized; // Normalize direction to ensure consistency
        dodgeSpeed = speed; // This represents the total distance to travel during the dodge
        dodgeDuration = duration;
        startTime = Time.time;
        pausedDuration = 0f;
        IsPaused = false;
        isDodging = true;
    }

    public void Execute()
    {
        if (!isDodging || IsPaused || Popup.IsPaused) return;

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

    public void Pause()
    {
        if (!isDodging) return;

        IsPaused = true;
        pauseStartTime = Time.time;
    }

    public void UnPause()
    {
        if (!isDodging) return;

        IsPaused = false;
        pausedDuration += Time.time - pauseStartTime;
    }
}
