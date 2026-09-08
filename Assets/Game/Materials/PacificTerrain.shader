Shader "PacificCombat/Terrain"
{
 Properties { _BaseColor("Forest",Color)=(.10,.19,.065,1) }
 SubShader
 {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A { float4 p:POSITION; float3 n:NORMAL; };
   struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; float fog:TEXCOORD2; float3 local:TEXCOORD3; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p) { float2 q=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y); }
   V vert(A a) { V o; o.world=TransformObjectToWorld(a.p.xyz); o.p=TransformWorldToHClip(o.world); o.n=TransformObjectToWorldNormal(a.n); o.fog=ComputeFogFactor(o.p.z); o.local=mul((float3x3)unity_ObjectToWorld,a.p.xyz); return o; }
   half4 frag(V i):SV_Target
   {
    float3 n=normalize(i.n);
    float slope=1-saturate(n.y);
    // Object-space noise stays attached to terrain across a floating-origin shift.
    float2 p=i.local.xz; float broad=noise(p*.006), detail=noise(p*.045);
    half3 forest=lerp(_BaseColor.rgb,half3(.24,.30,.10),broad*.65+detail*.2);
    float rock=smoothstep(.2,.55,slope)+smoothstep(600,1100,i.world.y)*.35;
    half3 basalt=lerp(half3(.14,.135,.12),half3(.34,.31,.25),broad*.8);
    half3 color=lerp(forest,basalt,saturate(rock));
    float beach=1-smoothstep(5,24+detail*13,i.world.y);
    color=lerp(color,lerp(half3(.43,.40,.27),half3(.73,.69,.48),smoothstep(0,9,i.world.y)),beach);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    float diffuse=saturate(dot(n,sun.direction));
    color*=half3(.34,.42,.43)+sun.color*(diffuse*.83*sun.shadowAttenuation);
    return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
