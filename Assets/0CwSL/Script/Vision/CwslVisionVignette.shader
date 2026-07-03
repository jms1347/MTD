Shader "CwSL/VisionVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Dark Color", Color) = (0.01, 0.012, 0.018, 1)
        _Center ("Center (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Inner Radius", Float) = 0.12
        _OuterRadius ("Outer Radius", Float) = 0.28
        _Aspect ("Aspect", Float) = 1.777
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "Default"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _Center;
            float _InnerRadius;
            float _OuterRadius;
            float _Aspect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UI/RawImage 호환용 (실제 텍스처는 쓰지 않음)
                fixed4 tex = tex2D(_MainTex, i.uv);

                float2 delta = i.uv - _Center.xy;
                delta.x *= _Aspect;
                float dist = length(delta);

                // smoothstep: 안쪽 투명, 바깥 어둠
                float darkness = smoothstep(_InnerRadius, _OuterRadius, dist);
                fixed4 col = _Color;
                col.a = darkness * _Color.a * i.color.a * tex.a;
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
