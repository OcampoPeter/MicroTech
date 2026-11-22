using UnityEngine;

// Small helpers shared by the Bent, Missing and Fried tasks:
//   - SetColor: tint a renderer without making a new material copy
//   - GetBounds / FitFactor: size a model so it fits a platform
public static class TaskVisuals
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); // URP/HDRP
    static readonly int Color_ = Shader.PropertyToID("_Color");        // Built-in
    static readonly int Emission = Shader.PropertyToID("_EmissionColor");

    // Color a renderer. emission > 0 makes it glow.
    public static void SetColor(Renderer r, Color color, float emission = 0f)
    {
        if (r == null) return;
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(BaseColor, color);
        block.SetColor(Color_, color);
        if (emission > 0f)
        {
            block.SetColor(Emission, color * emission);
            r.sharedMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            block.SetColor(Emission, Color.black);
        }
        r.SetPropertyBlock(block);
    }

    // The size and position of a platform in the world (from its renderers, or its colliders).
    public static Bounds GetBounds(Renderer[] renderers, Collider[] colliders)
    {
        bool has = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.one);

        foreach (var r in renderers)
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }

        // No renderers: use the colliders instead.
        if (!has)
        {
            foreach (var c in colliders)
            {
                if (!has) { b = c.bounds; has = true; }
                else b.Encapsulate(c.bounds);
            }
        }
        return b;
    }

    // Works out how much a model must be scaled so it covers the same width and depth
    // as the platform. It scales evenly (never stretched or squashed), so the height
    // follows the same scale. 1 = the model is already the right size.
    public static float FitFactor(GameObject modelPrefab, Bounds platformBounds)
    {
        // Measure a temporary copy at its own size, away from everything else.
        GameObject temp = Object.Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();

        float factor = 1f;
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float fx = b.size.x > 0.0001f ? platformBounds.size.x / b.size.x : 1f;
            float fz = b.size.z > 0.0001f ? platformBounds.size.z / b.size.z : 1f;
            factor = Mathf.Min(fx, fz); // the smaller one, so the model never sticks out
        }

        // Switch the copy off right away and let Unity remove it at the end of the frame.
        // (DestroyImmediate is not allowed while the player is touching something.)
        temp.SetActive(false);
        Object.Destroy(temp);
        return factor;
    }
}