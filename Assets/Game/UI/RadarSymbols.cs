using UnityEngine;

namespace PacificCombat
{
    /// <summary>Small, antialiased geometric symbols, cached once by each HUD.</summary>
    public static class RadarSymbols
    {
        public static readonly Color Friend = new Color(.35f, .86f, 1f);
        public static readonly Color Foe = new Color(1f, .38f, .30f);
        public static readonly Color Ink = new Color(.025f, .06f, .075f);

        public static Texture2D Contact(bool friendly)
        {
            return Raster(64, (x, y) =>
            {
                float d = friendly ? Mathf.Sqrt(x*x+y*y) : (Mathf.Abs(x)+Mathf.Abs(y)) * .7071068f;
                float edge = friendly ? .67f : .64f;
                Color color = friendly ? Friend : Foe;
                float silhouette = Mathf.Clamp01((edge + .12f - d) * 32);
                float border = Mathf.Clamp01((edge - d) * 32);
                float fill = Mathf.Clamp01((edge - .17f - d) * 32);
                Color result = Color.Lerp(Ink, color, border);
                if (friendly) result = Color.Lerp(result, Ink, fill);
                if (friendly && x*x+y*y < .10f*.10f) result = Friend;
                result.a = silhouette; return result;
            });
        }

        public static Texture2D Player()
        {
            return Raster(64, (x, y) =>
            {
                // Aircraft silhouette, nose at the top; not a font-dependent glyph.
                y = -y;
                bool fuselage = Mathf.Abs(x) < .105f && y > -.87f && y < .65f;
                bool wing = y > -.14f && y < .26f && Mathf.Abs(x) < .77f && y > Mathf.Abs(x)*.45f-.16f;
                bool tail = y > .48f && y < .72f && Mathf.Abs(x) < .34f;
                return fuselage || wing || tail ? new Color(.98f, .98f, .91f) : Color.clear;
            });
        }

        public static Texture2D Grid()
        {
            return Raster(256, (x, y) =>
            {
                float r = Mathf.Sqrt(x*x+y*y);
                float rings = Mathf.Min(Mathf.Abs(r-.94f), Mathf.Abs(r-.47f));
                float cross = Mathf.Min(Mathf.Abs(x), Mathf.Abs(y));
                Color color = new Color(.045f,.105f,.11f,.95f);
                float stroke = Mathf.Clamp01((.010f - Mathf.Min(rings, cross)) * 128);
                color = Color.Lerp(color, new Color(.25f,.43f,.42f), stroke * (r < .96f ? 1 : 0));
                color.a = Mathf.Clamp01((.98f-r)*128); return color;
            });
        }

        static Texture2D Raster(int size, System.Func<float,float,Color> sample)
        {
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false) {
                name = "Radar symbol", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave };
            var pixels = new Color[size*size];
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                Color color=Color.clear;
                for (int sy=0;sy<2;sy++) for(int sx=0;sx<2;sx++)
                    color += sample((x+(sx+.5f)/2)/size*2-1,(y+(sy+.5f)/2)/size*2-1)*.25f;
                pixels[y*size+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
    }
}
