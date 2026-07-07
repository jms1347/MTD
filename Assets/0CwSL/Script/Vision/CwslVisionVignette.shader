Shader "CwSL/VisionVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Dark Color", Color) = (0.01, 0.012, 0.018, 1)
        _Center ("Center (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Inner Radius (Viewport)", Float) = 0.12
        _OuterRadius ("Outer Radius (Viewport)", Float) = 0.28
        _Aspect ("Aspect Correction", Float) = 1.77
        _ScryActive ("Scry Active", Float) = 0
        _ScryCenter ("Scry Center (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _ScryInnerRadius ("Scry Inner Radius", Float) = 0.08
        _ScryOuterRadius ("Scry Outer Radius", Float) = 0.18
        _TeamVisionCount ("Team Vision Count", Int) = 0
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
            float _ScryActive;
            float4 _ScryCenter;
            float _ScryInnerRadius;
            float _ScryOuterRadius;
            int _TeamVisionCount;

            #define MAX_TEAM_VISION 6
            float4 _TeamCenters[MAX_TEAM_VISION];
            float _TeamInnerRadii[MAX_TEAM_VISION];
            float _TeamOuterRadii[MAX_TEAM_VISION];

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

            float CircleDarkness(float2 delta, float innerRadius, float outerRadius)
            {
                delta.x *= _Aspect;
                float dist = length(delta);
                return smoothstep(innerRadius, outerRadius, dist);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                float darkness = 1.0;
                if (_TeamVisionCount > 0)
                {
                    for (int idx = 0; idx < _TeamVisionCount; idx++)
                    {
                        float2 teamDelta = i.uv - _TeamCenters[idx].xy;
                        float teamDarkness = CircleDarkness(
                            teamDelta,
                            _TeamInnerRadii[idx],
                            _TeamOuterRadii[idx]);
                        darkness = min(darkness, teamDarkness);
                    }
                }
                else
                {
                    float2 delta = i.uv - _Center.xy;
                    darkness = CircleDarkness(delta, _InnerRadius, _OuterRadius);
                }

                if (_ScryActive > 0.5)
                {
                    float2 scryDelta = i.uv - _ScryCenter.xy;
                    float scryDarkness = CircleDarkness(scryDelta, _ScryInnerRadius, _ScryOuterRadius);
                    darkness = min(darkness, scryDarkness);
                }

                fixed4 col = _Color;
                col.a = darkness * _Color.a * i.color.a * tex.a;
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
