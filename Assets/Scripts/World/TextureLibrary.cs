using System.Collections.Generic;
using UnityEngine;

// Maps FBX material names to generated textures and swaps renderers' materials.
public static class TextureLibrary
{
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static bool loaded;

    static readonly Dictionary<string, string> Map = new Dictionary<string, string>
    {
        { "Bark", "bark" },
        { "Cut", "cutwood" },
        { "Wood", "wood" }, { "WoodL", "wood" },
        { "WoodD", "wooddark" },
        { "Leaves", "needles" }, { "Leaves2", "needles" },
        { "LeafB", "bushleaf" }, { "LeafB2", "bushleaf" },
        { "GrassA", "grassblade" }, { "GrassB", "grassblade" },
        { "Rock", "rock" },
        { "Roof", "roof" }, { "Roof2", "roof" }, { "Roof3", "roof" }, { "RoofM", "roof" },
        // houses are built from planks: walls = plank siding, columns/chimney = dark wood
        { "Wall", "floorwood" }, { "Wall2", "wood" }, { "Wall3", "wooddark" },
        { "Stone", "wooddark" },
        { "FloorW", "floorwood" },
        { "Metal", "metal" }, { "MetalD", "metal" },
    };

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        var shader = Shader.Find("Standard");
        foreach (var kv in Map)
        {
            if (cache.ContainsKey(kv.Key)) continue;
            var tex = Resources.Load<Texture2D>("Textures/" + kv.Value);
            if (tex == null) continue;
            tex.wrapMode = TextureWrapMode.Repeat;
            var m = new Material(shader);
            m.mainTexture = tex;
            m.color = Color.white;
            cache[kv.Key] = m;
        }
    }

    public static Material For(string materialName)
    {
        EnsureLoaded();
        if (materialName != null && Map.TryGetValue(materialName, out var tex) && cache.TryGetValue(materialName, out var m))
            return m;
        return null;
    }

    // Swap every renderer material whose name matches the map.
    public static void Apply(GameObject root)
    {
        if (root == null) return;
        EnsureLoaded();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var nm = mats[i] != null ? mats[i].name : "";
                var rep = For(nm);
                if (rep != null) { mats[i] = rep; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }
}
