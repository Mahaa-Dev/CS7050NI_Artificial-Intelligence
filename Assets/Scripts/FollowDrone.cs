using UnityEngine;

public class FollowDrone : MonoBehaviour
{
    public Transform droneTransform;
    public Vector3 offset = new Vector3(-20f, 25f, 20f);
    public float smoothSpeed = 1f;

    void LateUpdate()
    {
        if (droneTransform != null)
        {
            // Calculate the desired position based on the drone's position and offset
            Vector3 desiredPosition = droneTransform.position + offset;

            // Smoothly move the camera towards the desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Look at the drone
            transform.LookAt(droneTransform);
        }
    }
}
