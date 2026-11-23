Shader "DungeonDLC/Armas Pixel UI"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        _Cutoff("Recorte",Range(0,1))=.48
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            sampler2D _MainTex; fixed4 _Color; float _Cutoff;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*i.color;clip(c.a-_Cutoff);return fixed4(c.rgb,1);}
            ENDCG
        }
    }
}
