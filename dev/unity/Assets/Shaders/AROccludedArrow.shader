// PATH B — Built-in CG shader as instructed.
// NOTE: this project's manifest.json includes com.unity.render-pipelines.universal 17.4.0,
// so URP is likely the active render pipeline. URP can render Built-in CG shaders, but the
// AR Foundation occlusion auto-bindings are most reliable when the shader is
// authored against the active pipeline. If arrows render unoccluded after this is
// applied, switch to a Shader Graph using the AR Foundation occlusion subgraph
// (PATH A in the prompt). See AROcclusionSetup.cs class-doc for context.
Shader "Custom/AROccludedArrow"
{
    Properties
    {
        _Color         ("Color",            Color) = (0, 1, 1, 1)
        _MainTex       ("Texture",          2D)    = "white" {}
        _OcclusionBias ("Occlusion Bias (m)", Float) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            float     _OcclusionBias;

            // AR Foundation auto-binds these when an AROcclusionManager is active.
            // ARCore-side names in AR Foundation 6.x use "_EnvironmentDepth" for the
            // depth texture and "_EnvironmentDepthDisplayMatrix" for the screen-to-depth
            // mapping. If your build silently skips occlusion, log AR Foundation's
            // active material keywords and try alternates like
            // "_EnvironmentDepthTexture" / "_CameraDepthTexture" listed in
            // Library/PackageCache/com.unity.xr.arfoundation@*/Runtime/Occlusion/.
            sampler2D _EnvironmentDepth;
            float4x4  _EnvironmentDepthDisplayMatrix;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float  eyeDepth   : TEXCOORD2;
                float4 vertex     : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex    = UnityObjectToClipPos(v.vertex);
                o.uv        = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);

                // Eye-space depth: positive distance from the camera in meters.
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.eyeDepth = -mul(UNITY_MATRIX_V, worldPos).z;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Screen-space UV in [0,1] for the current fragment.
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // AR's display matrix re-maps screen space onto the depth image,
                // accounting for device rotation, aspect ratio, and the depth
                // texture's coordinate frame.
                float4 transformed = mul(_EnvironmentDepthDisplayMatrix, float4(screenUV, 0, 1));
                float2 depthUV = transformed.xy;

                // Sample environment depth in meters.
                float envDepth = tex2D(_EnvironmentDepth, depthUV).r;

                // Discard if this fragment is behind real geometry. envDepth of 0
                // means "no depth available for this pixel" — leave it visible
                // rather than clipping the entire arrow.
                if (envDepth > 0 && i.eyeDepth > envDepth + _OcclusionBias)
                    discard;

                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }

    // Fallback for devices/pipelines that fail to compile the CG pass. Renders
    // the arrow without occlusion so the navigation visual still works.
    FallBack "Transparent/Diffuse"
}
