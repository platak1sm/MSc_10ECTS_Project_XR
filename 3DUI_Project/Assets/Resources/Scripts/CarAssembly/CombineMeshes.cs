using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombineMeshes : MonoBehaviour
{
    [ContextMenu("Combine Car Meshes")]
    public void CombineCarMeshes()
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            Debug.LogWarning("This object must be a child of the car parent!");
            return;
        }

        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == GetComponent<MeshFilter>())
                continue; // skip self
            if (mf.sharedMesh == null)
                continue;

            combine[i].mesh = mf.sharedMesh;
            combine[i].transform = parent.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            i++;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combine, true, true);

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = combinedMesh;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (meshFilters.Length > 0 && meshFilters[0].GetComponent<MeshRenderer>() != null)
            renderer.sharedMaterial = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;

        Debug.Log("Combined mesh created with " + i + " parts.");
    }
}