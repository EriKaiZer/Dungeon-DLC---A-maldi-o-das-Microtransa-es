Shader "DungeonDLC/MenuAtmosphere"
{
 Properties { [PerRendererData] _MainTex("Background",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Phase("Time",Float)=0 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   sampler2D _MainTex;fixed4 _Color;float _Phase;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float glow(float2 uv,float2 p){float2 d=(uv-p)*float2(1,0.5625);return exp(-dot(d,d)*2600);}
   fixed4 frag(v2f i):SV_Target
   {
    float2 uv=i.uv;
    // A one-pixel displacement gives the two hanging banners a restrained sway.
    float flag=step(.625,uv.x)*step(uv.x,.665)*step(.50,uv.y)*step(uv.y,.76);
    flag+=step(.845,uv.x)*step(uv.x,.88)*step(.52,uv.y)*step(uv.y,.78);
    uv.x+=flag*floor(sin(uv.y*32+_Phase*1.4)*1.5)/1672;
    fixed4 c=tex2D(_MainTex,uv)*i.color;
    float flicker=.55+.22*sin(_Phase*8.3)+.13*sin(_Phase*17.9);
    float light=glow(i.uv,float2(.654,.40))+glow(i.uv,float2(.873,.35))+glow(i.uv,float2(.80,.315));
    c.rgb+=float3(.16,.065,.005)*light*flicker;
    float2 pixel=floor(i.uv*float2(320,180))/float2(320,180);
    float fog=(sin(pixel.x*23+_Phase*.17+sin(pixel.y*17))*sin(pixel.x*9-_Phase*.12)+1)*.5;
    float distance=(pixel.y-.23)*9;float band=exp(-distance*distance);float darkness=1-saturate(dot(c.rgb,float3(.3,.5,.2))*2);
    c.rgb=lerp(c.rgb,float3(.20,.27,.37),fog*band*darkness*.13);
    return c;
   }
   ENDCG
  }
 }
}
