Shader "PacificCombat/Atmosphere"
{
 Properties
 {
  _Zenith("Zenith",Color)=(.20,.39,.66,1)
  _Horizon("Maritime haze",Color)=(.55,.71,.77,1)
  _SunColor("Sun",Color)=(1,.94,.83,1)
  _SunDirection("Sun direction",Vector)=(0,1,0,0)
 }
 SubShader
 {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Zenith, _Horizon, _SunColor;
   float4 _SunDirection;
   CBUFFER_END
   struct A { float4 p:POSITION; };
   struct V { float4 p:SV_POSITION; float3 direction:TEXCOORD0; };
   V vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); o.direction=i.p.xyz; return o; }
   half4 frag(V i):SV_Target
   {
    float3 direction=normalize(i.direction);
    float haze=exp(-max(0,direction.y)*4.5);
    half3 color=lerp(_Zenith.rgb,_Horizon.rgb,haze);
    float alignment=saturate(dot(direction,normalize(_SunDirection.xyz)));
    float glow=pow(alignment,96)*.10;
    float disk=smoothstep(.999965,.999985,alignment);
    color+=_SunColor.rgb*(glow+disk*2.5);
    // The lower hemisphere is the same haze color as distant water, avoiding an
    // artificial dark ground band where the finite ocean meets the skybox.
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
