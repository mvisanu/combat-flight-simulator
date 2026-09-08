Shader "PacificCombat/Cloud"
{
 Properties { _BaseMap("Density",2D)="white"{} _BaseColor("Cloud",Color)=(1,.99,.95,1) }
 SubShader
 {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float2 size:TEXCOORD1; };
   struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float fog:TEXCOORD1; float distance:TEXCOORD2; float3 view:TEXCOORD3; };
   V vert(A a)
   {
    V o; float3 center=TransformObjectToWorld(a.p.xyz);
    float3 right=UNITY_MATRIX_I_V._m00_m10_m20, up=UNITY_MATRIX_I_V._m01_m11_m21;
    float3 world=center+right*(a.uv.x-.5)*a.size.x+up*(a.uv.y-.5)*a.size.y;
    o.p=TransformWorldToHClip(world); o.uv=a.uv; o.fog=ComputeFogFactor(o.p.z);
    o.distance=distance(center,_WorldSpaceCameraPos); o.view=GetWorldSpaceNormalizeViewDir(center); return o;
   }
   half4 frag(V i):SV_Target
   {
    half4 density=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    float alpha=density.a*.68*smoothstep(70,350,i.distance);
    Light sun=GetMainLight();
    float lining=pow(saturate(dot(normalize(i.view),-sun.direction)),5)*(1-density.a)*.7;
    half3 color=lerp(half3(.49,.60,.68),_BaseColor.rgb,saturate(i.uv.y*.6+density.r*.6));
    color+=sun.color*lining;
    return half4(MixFog(color,i.fog),alpha);
   }
   ENDHLSL
  }
 }
}
