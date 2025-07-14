#ifndef ALLIN13DSHADER_OUTLINE_PASS_INCLUDED
#define ALLIN13DSHADER_OUTLINE_PASS_INCLUDED

#if defined(_OUTLINETYPE_SIMPLE) || defined(_OUTLINETYPE_FADEWITHDISTANCE)
	#define OUTLINE_FACTOR 0.01
#else 
	#define OUTLINE_FACTOR 1.0
#endif

float3 GetObjectScale()
{
    float3 res = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)));
	
    return res;
}

#ifdef OUTLINE_ON
float4 Outline_Constant(float4 vertexPos, float3 normalOS, float3 thicknessScaled)
{
    float3 vertexWS = GetPositionWS(vertexPos);
    float3 vertexToCameraWS = _WorldSpaceCameraPos.xyz - vertexWS;
	
    float distanceToCamera = length(vertexToCameraWS);
	
    float4 res = vertexPos;
	
    float normalizedDistanceToCamera = distanceToCamera / ACCESS_PROP(_MaxCameraDistance);
    float displacement = thicknessScaled.x * normalizedDistanceToCamera;
	res.xyz += normalOS * displacement;

    return res;
}

float4 Outline_Simple(float4 vertexPos, float3 normalOS, float3 thicknessScaled)
{
    float4 res = vertexPos;
	
    res.xyz += normalOS * thicknessScaled;
    return res;
}

float4 Outline_FadeWithDistance(float4 vertexPos, float3 normalOS, float3 thicknessScaled)
{
	float4 res = vertexPos;
	
	float3 vertexWS = GetPositionWS(vertexPos);
	float3 vertexToCameraWS = _WorldSpaceCameraPos.xyz - vertexWS;
	float distanceToCamera = length(vertexToCameraWS);
	
	float normalizedDistanceToCamera = saturate(distanceToCamera / ACCESS_PROP(_MaxFadeDistance));
	float3 correctedThickness = (1 - normalizedDistanceToCamera) * thicknessScaled;
	
	res.xyz += normalOS * correctedThickness;
	res.xyz += normalOS * correctedThickness;
	return res;
}
#endif

FragmentDataOutline OutlinePass_Vertex(VertexData v)
{
	FragmentDataOutline o;

	o.interpolator_01 = 0;
	o.interpolator_02 = 0;
	o.interpolator_03 = 0;

	UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 

#ifdef OUTLINE_ON
    float3 _Object_Scale = GetObjectScale();
    float3 thicknessScaled = (ACCESS_PROP(_OutlineThickness) / _Object_Scale) * OUTLINE_FACTOR;
#endif
	
#ifdef _SPHERIZE_NORMALS_ON
	float3 normalOS = normalize(v.vertex);
#else
	float3 normalOS = v.normal;
#endif

#ifdef _USE_CUSTOM_TIME
	SHADER_TIME(o) = allIn13DShader_globalTime.xyz + ACCESS_PROP(_TimingSeed);
#else
	SHADER_TIME(o) = _Time.xyz + ACCESS_PROP(_TimingSeed);
#endif

	v.vertex = ApplyVertexEffects(v.vertex, normalOS, SHADER_TIME(o));

//#ifdef _OUTLINETYPE_SIMPLE
//	v.vertex = Outline_Simple(v.vertex, v.normal, thicknessScaled);
#ifdef _OUTLINETYPE_SIMPLE
	//float3 smoothNormal = normalize(v.vertexColor.rgb * 2.0 - 1.0); // 디코딩
	//v.vertex = Outline_Simple(v.vertex, smoothNormal, thicknessScaled);
	/*
	float3 smoothNormal = normalize(v.vertexColor.rgb * 2.0 - 1.0); // 디코딩

	float3 center = float3(0, 0, 0); // 필요시 메쉬 중심 좌표로 변경 가능
	float3 localPos = v.vertex.xyz - center;
	float3 blendedNormal = normalize(lerp(v.normal, smoothNormal, 0.5));
	localPos += blendedNormal * thicknessScaled.x;
	v.vertex.xyz = localPos + center;
	*/
	v.vertex = Outline_Simple(v.vertex, v.normal, thicknessScaled);
//#elif _OUTLINETYPE_CONSTANT
//    v.vertex = Outline_Constant(v.vertex, v.normal, thicknessScaled);
#elif _OUTLINETYPE_CONSTANT
	float3 smoothNormal = normalize(v.vertexColor.rgb * 2.0 - 1.0); // 디코딩
    v.vertex = Outline_Constant(v.vertex, smoothNormal, thicknessScaled);
#elif _OUTLINETYPE_FADEWITHDISTANCE
	v.vertex = Outline_FadeWithDistance(v.vertex, v.normal, thicknessScaled);
#endif
	
	o.normalWS = GetNormalWS(v.normal);
	o.mainUV = SIMPLE_CUSTOM_TRANSFORM_TEX(v.uv, _MainTex);
	o.pos = OBJECT_TO_CLIP_SPACE(v);
	o.positionWS = GetPositionWS(v.vertex);

	FOGCOORD(o) = GetFogFactor(o.pos);
	
	return o;
}
float4 OutlinePass_Fragment(FragmentDataOutline i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	float4 col = float4(0, 0, 0, 0); // 기본값, 완전 검정 윤곽선

#ifdef OUTLINE_ON
	col = ACCESS_PROP(_OutlineColor);
#endif

	return col;
}
/*
float4 OutlinePass_Fragment(FragmentDataOutline i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	float4 col = float4(0, 0, 0, 0);
	
#ifdef OUTLINE_ON
	col = ACCESS_PROP(_OutlineColor);
#endif

	float camDistance = distance(i.positionWS, _WorldSpaceCameraPos); 
	float4 screenPos = ComputeScreenPos(i.pos);
	col = ApplyAlphaEffects(col, SCALED_MAIN_UV(i), 1.0, camDistance, screenPos);

#ifdef _ALPHA_CUTOFF_ON
	clip((col.a - ACCESS_PROP(_AlphaCutoffValue)) - 0.001);
#endif	

	col = CustomMixFog(FOGCOORD(i), col);

	col.a *= ACCESS_PROP(_GeneralAlpha);



	return col;
}
*/
#endif