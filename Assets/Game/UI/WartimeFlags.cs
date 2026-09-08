using UnityEngine;

namespace PacificCombat
{
    // Country identification for a WWII aircraft roster; not a modern flag set.
    public static class WartimeFlags
    {
        public static int Country(AircraftType type) => type == AircraftType.A6MZero ? 1 : type == AircraftType.Bf109 ? 2 : 0;
        public static string CountryName(AircraftType type) => Country(type) == 1 ? "Japan" : Country(type) == 2 ? "Germany" : "United States";
        public static Texture2D Create(int country)
        {
            const int w=190,h=100;
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false) { name=country==0?"United States 48-star flag":country==1?"Japan WWII flag":"Germany 1935-1945 flag",filterMode=FilterMode.Bilinear };
            var pixels=new Color32[w*h];
            Color white=new Color(.96f,.95f,.9f), red=new Color(.68f,.025f,.055f), blue=new Color(.035f,.07f,.20f);
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                float u=(x+.5f)/w,v=(y+.5f)/h; Color color=white;
                if(country==0)
                {
                    color=Mathf.FloorToInt((1-v)*13)%2==0?red:white;
                    if(u<.4f && v>6f/13)
                    {
                        color=blue;
                        float sx=u/.4f*8,sy=(v-6f/13)/(7f/13)*6;
                        Vector2 p=new Vector2((sx-Mathf.Floor(sx)-.5f)*2,(sy-Mathf.Floor(sy)-.5f)*2);
                        if(Star(p,.65f)) color=white;
                    }
                }
                else
                {
                    Vector2 p=new Vector2((u-.5f)*1.9f,v-.5f);
                    if(country==1) color=p.magnitude<.29f?red:white;
                    else
                    {
                        color=p.magnitude<.38f?white:red;
                        // Four hooked arms inside the historical white disc, rotated 45 degrees.
                        p=new Vector2(p.x+p.y,p.y-p.x)*.70710678f;
                        for(int arm=0;arm<4;arm++)
                        {
                            if((p.x>=-.045f && p.x<=.25f && Mathf.Abs(p.y)<.045f)
                                || (p.x>.16f && p.x<.25f && p.y>=0 && p.y<.25f)) color=Color.black;
                            p=new Vector2(-p.y,p.x);
                        }
                    }
                }
                pixels[y*w+x]=color;
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
        static bool Star(Vector2 point,float radius)
        {
            bool inside=false; Vector2 previous=Vertex(9,radius);
            for(int i=0;i<10;i++)
            {
                Vector2 next=Vertex(i,radius);
                if((next.y>point.y)!=(previous.y>point.y) && point.x<(previous.x-next.x)*(point.y-next.y)/(previous.y-next.y)+next.x) inside=!inside;
                previous=next;
            }
            return inside;
        }
        static Vector2 Vertex(int i,float r) { float a=i*Mathf.PI/5;return new Vector2(Mathf.Sin(a),Mathf.Cos(a))*(i%2==0?r:r*.4f); }
    }
}
