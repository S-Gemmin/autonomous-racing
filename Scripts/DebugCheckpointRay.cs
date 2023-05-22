using UnityEngine;

public class DebugCheckpointRay : MonoBehaviour
{
    public Collider[] checkpoints;

    private void OnDrawGizmos()
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            Gizmos.color = Color.yellow;
            Transform cpt = checkpoints[i].transform;
            Gizmos.DrawLine(cpt.position, cpt.position + 3f * cpt.forward);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cpt.position, Vector3.one);
        }
    }

    private void OnDrawGizmosSelected()
    {
        GUIStyle textStyle = new GUIStyle();
        textStyle.normal.textColor = Color.white;
        textStyle.alignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < checkpoints.Length; i++)
        {
            Transform current = checkpoints[i].transform;
            Vector3 position = current.position + current.up * checkpoints[i].bounds.size.y;
            UnityEditor.Handles.Label(position, current.gameObject.name, textStyle);
        }
    }
}
