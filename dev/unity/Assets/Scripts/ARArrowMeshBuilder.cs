// Assets/Scripts/ARArrowMeshBuilder.cs
using UnityEngine;

/// <summary>
/// Procedurally builds a chevron/arrow mesh on the ARArrow prefab at runtime.
/// Attach this to the ARArrow prefab root. It will create a MeshFilter
/// and MeshRenderer automatically if not present.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ARArrowMeshBuilder : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] float width      = 1.0f;
    [SerializeField] float depth      = 0.8f;  // arrow length in Z
    [SerializeField] float thickness  = 0.25f; // stem width
    [SerializeField] float height     = 0.04f; // Y extrusion

    [Header("Material")]
    [SerializeField] Material arrowMaterial; // Assign URP/Lit or unlit material

    void Awake()
    {
        BuildMesh();
    }

    void BuildMesh()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();

        if (arrowMaterial != null)
            mr.material = arrowMaterial;

        mf.mesh = CreateChevronMesh(width, depth, thickness, height);
    }

    /// <summary>
    /// Creates a flat chevron (V-shape) lying on the XZ plane, extruded by `h` on Y.
    /// The tip points in the +Z direction.
    /// </summary>
    public static Mesh CreateChevronMesh(float w, float d, float t, float h)
    {
        // Top-face vertices (Y = h/2), mirrored to bottom (Y = -h/2)
        // The chevron is built as two quads (left arm + right arm)

        float hw = w * 0.5f;
        float ht = t * 0.5f;

        // Left arm: from (-hw, ?, 0) to (-ht, ?, d)
        // Right arm: mirror on X

        Vector3[] top = new Vector3[]
        {
            // Left arm (4 corners)
            new Vector3(-hw,  h, 0),
            new Vector3(-hw + t, h, 0),
            new Vector3(-ht,  h, d),
            new Vector3(-ht + t, h, d),

            // Right arm (mirrored)
            new Vector3( hw - t, h, 0),
            new Vector3( hw,     h, 0),
            new Vector3( ht - t, h, d),
            new Vector3( ht,     h, d),
        };

        Vector3[] bot = new Vector3[top.Length];
        for (int i = 0; i < top.Length; i++)
            bot[i] = new Vector3(top[i].x, 0, top[i].z);

        Vector3[] verts = new Vector3[top.Length + bot.Length];
        top.CopyTo(verts, 0);
        bot.CopyTo(verts, top.Length);

        // Triangles: each quad = 2 tris (top face only for flat ground arrow)
        int[] tris = new int[]
        {
            // Left arm top
            0,2,1,  1,2,3,
            // Right arm top
            4,6,5,  5,6,7,
            // Left arm bottom (flipped winding)
            8+0, 8+1, 8+2,  8+1, 8+3, 8+2,
            // Right arm bottom
            8+4, 8+5, 8+6,  8+5, 8+7, 8+6,
        };

        var mesh = new Mesh { name = "ArrowChevron" };
        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
