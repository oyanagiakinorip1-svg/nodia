using UnityEngine;

namespace Nodia.Nodes
{
    // Keeps a world-space label (the node title) readable from any angle by
    // always turning to face the camera - needed since the player can
    // approach a node from any direction in free-fly 3D space.
    public class Billboard : MonoBehaviour
    {
        private Transform cam;

        private void LateUpdate()
        {
            if (cam == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera == null) return;
                cam = mainCamera.transform;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
        }
    }
}
