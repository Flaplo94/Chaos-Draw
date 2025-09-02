using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float cameraZ = -10f;

    private bool isOverriding = false;
    private Vector2 returnPosition;

    public void FocusTemporarily(Vector2 focusPos, float duration)
    {
        StartCoroutine(FocusRoutine(focusPos, duration));
    }

    private IEnumerator FocusRoutine(Vector2 focusPos, float duration)
    {
        isOverriding = true;
        returnPosition = transform.position;

        float halfDuration = duration / 2f;

        // pan to boss
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / halfDuration;
            Vector2 pos = Vector2.Lerp(returnPosition, focusPos, t);
            transform.position = new Vector3(pos.x, pos.y, cameraZ);
            yield return null;
        }

        // stay focused for 1 second (real time)
        yield return new WaitForSecondsRealtime(1f);

        // pan back to player
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / halfDuration;
            Vector2 pos = Vector2.Lerp(focusPos, returnPosition, t);
            transform.position = new Vector3(pos.x, pos.y, cameraZ);
            yield return null;
        }

        isOverriding = false;
    }

    private void LateUpdate()
    {
        if (isOverriding || target == null) return;

        Vector2 desiredPos = target.position;
        Vector2 smoothedPos = Vector2.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(smoothedPos.x, smoothedPos.y, cameraZ);
    }
}
