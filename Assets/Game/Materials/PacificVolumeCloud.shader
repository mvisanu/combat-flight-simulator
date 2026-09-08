Shader "PacificCombat/VolumeCloud"
{
 Properties { _Density("Density", Float)=.009 _Steps("Steps",Float)=24 _Tint("Tint",Color)=(.94,.96,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend One OneMinusSrcAlpha ZWrite Off Cull Front
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   float _Density, _Steps; half4 _Tint;
   struct A { float4 positionOS:POSITION; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; };
   V vert(A v) { V o; o.world=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); return o; }
   float hash(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
   float noise(float3 p) { float3 i=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z); }
   half4 frag(V v):SV_Target
   {
    float3 origin=TransformWorldToObject(GetCameraPositionWS());
    float3 direction=TransformWorldToObject(v.world)-origin;
    float3 inv=1/(direction+1e-8);
    float3 t0=(-.5-origin)*inv,t1=(.5-origin)*inv;
    float3 lo=min(t0,t1),hi=max(t0,t1);
    float enter=max(0,max(lo.x,max(lo.y,lo.z))), leave=min(hi.x,min(hi.y,hi.z));
    float2 uv=v.positionCS.xy/_ScaledScreenParams.xy;
    float depth=SampleSceneDepth(uv);
    #if !UNITY_REVERSED_Z
     depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
    #endif
    float3 scene=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
    leave=min(leave,distance(scene,GetCameraPositionWS())/max(.01,distance(v.world,GetCameraPositionWS())));
    if(leave<=enter) return 0;
    int steps=(int)_Steps;
    float stepSize=(leave-enter)/steps;
    float meters=length(v.world-GetCameraPositionWS())*stepSize;
    float alpha=0; float3 color=0;
    for(int i=0;i<48;i++)
    {
     if(i>=steps || alpha>.985) break;
     float3 p=origin+direction*(enter+(i+.5)*stepSize);
     float edge=saturate((1-length(p*2))*3);
     float density=edge*saturate((noise(p*8)+.45*noise(p*19)-.38)*1.7);
     float a=1-exp(-density*_Density*meters);
     float light=saturate(.64+p.y*.5+noise(p*5)*.22);
     color+=(1-alpha)*a*_Tint.rgb*light;
     alpha+=(1-alpha)*a;
    }
    return half4(color,alpha);
   }
   ENDHLSL
  }
 }
}
