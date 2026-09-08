Shader "PacificCombat/Shallows"
{
 Properties { _BaseColor("Lagoon",Color)=(.07,.43,.39,1) }
 SubShader
 {
  Tags { "RenderType"="Transparent" "Queue"="Transparent-20" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 p:POSITION; float2 uv:TEXCOORD0; };
   struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; float fog:TEXCOORD2; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   float hash(float2 p) { float3 q=frac(float3(p.xyx)*.1031); q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z); }
   float noise(float2 p) { float2 q=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y); }
   V vert(A a) { V o; o.p=TransformObjectToHClip(a.p.xyz); o.uv=a.uv; o.local=a.p.xz; o.fog=ComputeFogFactor(o.p.z); return o; }
   half4 frag(V i):SV_Target
   {
    float fringe=smoothstep(0,.16,i.uv.x)*(1-smoothstep(.48,1,i.uv.x));
    float patches=noise(i.local*.004)*.65+noise(i.local*.013)*.35;
    half3 color=lerp(_BaseColor.rgb,half3(.16,.50,.44),patches*.45);
    float surf=smoothstep(.63,.9,noise(i.local*.018+_Time.y*.012))*(1-smoothstep(.08,.32,i.uv.x))*.16;
    color=lerp(color,half3(.82,.91,.85),surf);
    return half4(MixFog(color,i.fog),fringe*.65);
   }
   ENDHLSL
  }
 }
}
