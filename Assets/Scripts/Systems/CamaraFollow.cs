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

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (duration / 2f);
            Vector2 pos = Vector2.Lerp(returnPosition, focusPos, t);
            transform.position = new Vector3(pos.x, pos.y, cameraZ);
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (duration / 2f);
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
