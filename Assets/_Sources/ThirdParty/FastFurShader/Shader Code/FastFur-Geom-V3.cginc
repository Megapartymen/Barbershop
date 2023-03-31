// Super Pipeline
// 
// // The Geometry shader used to be the bottleneck. It used to be responsible for creating all the copies for each
// fur layer, but most of that responsibility has been moved to the Hull + Domain shaders. Now the Geometry shader's
// job is to unpack the data coming from the Domain shader and then render a "chunk" of layers.


[maxvertexcount(PIPELINE3_CHUNKGEOM)]

void geom(triangle hullGeomInput IN[3], inout TriangleStream<fragInput> tristream)
{
	fragInput o[3];
	o[0] = (fragInput)0;

	UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o[0]);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN[0])

		o[1] = o[0];
	o[2] = o[0];

#if defined(FASTFUR_DEBUGGING)
	o[0].vertDist = float4(1, 0, 0, 0);
	o[1].vertDist = float4(0, 1, 0, 0);
	o[2].vertDist = float4(0, 0, 1, 0);
#endif

	hullGeomInput inBuff[3];
	inBuff[0] = IN[0];
	inBuff[1] = IN[1];
	inBuff[2] = IN[2];

	// The centre point will always be IN[0] on NVidia hardware, but on the AMD Vega 64 it isn't. Just
	// to be safe we check all 3 and don't rely on any algorithms that need the inputs to be in a
	// specific order.
	if (inBuff[0].HULLCOORDS.x < 0)
	{
#define RECOVER0(property) inBuff[0].property -= inBuff[1].property + inBuff[2].property;
		RECOVER0(worldPos);
		RECOVER0(worldNormal);
		RECOVER0(uv);
#if !defined(PREPASS)
		RECOVER0(lightData1);
		RECOVER0(lightData2);
#endif
		RECOVER0(furData);
		RECOVER0(windEffect);
		inBuff[0].HULLCOORDS.x = 3 - (inBuff[1].HULLCOORDS.x + inBuff[2].HULLCOORDS.x);
	}

	if (inBuff[1].HULLCOORDS.x < 0)
	{
#define RECOVER1(property) inBuff[1].property -= inBuff[0].property + inBuff[2].property;
		RECOVER1(worldPos);
		RECOVER1(worldNormal);
		RECOVER1(uv);
#if !defined(PREPASS)
		RECOVER1(lightData1);
		RECOVER1(lightData2);
#endif
		RECOVER1(furData);
		RECOVER1(windEffect);
		inBuff[1].HULLCOORDS.x = 3 - (inBuff[0].HULLCOORDS.x + inBuff[2].HULLCOORDS.x);
	}

	if (inBuff[2].HULLCOORDS.x < 0)
	{
#define RECOVER2(property) inBuff[2].property -= inBuff[1].property + inBuff[0].property;
		RECOVER2(worldPos);
		RECOVER2(worldNormal);
		RECOVER2(uv);
#if !defined(PREPASS)
		RECOVER2(lightData1);
		RECOVER2(lightData2);
#endif
		RECOVER2(furData);
		RECOVER2(windEffect);
		inBuff[2].HULLCOORDS.x = 3 - (inBuff[1].HULLCOORDS.x + inBuff[0].HULLCOORDS.x);
	}

	// Prepare the 3 outputs. We don't know what order they are in, but it doesn't matter because we treat them all the same.
	for (int z = 0; z < 3; z++)
	{
#if defined(SKIN_LAYER) && defined(_NORMALMAP)
		o[z].worldTangent = inBuff[z].worldTangent;
#endif
		o[z].uv = inBuff[z].uv;
#if !defined(PREPASS)
		o[z].lightData1 = inBuff[z].lightData1;
		o[z].lightData2 = inBuff[z].lightData2;
#endif
#if defined(USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
		o[z]._ShadowCoord = inBuff[z]._ShadowCoord;
#endif
	}


	// How many layers are being rendered?
	float visibleLayers = max(max(inBuff[0].VISIBLELAYERS, inBuff[1].VISIBLELAYERS), inBuff[2].VISIBLELAYERS);
	float totalTriangles = ceil(floor(visibleLayers) * PIPELINE3_CHUNKSCALE);

	// This calculates the index of the current triangle if there are 3 or less triangles.
	float triangleIndex = round(inBuff[0].HULLCOORDS.y + inBuff[1].HULLCOORDS.y + inBuff[2].HULLCOORDS.y - 2.0);

	// Tf there are more than 3 triangles, the calculation is a bit trickier.
	if (floor(visibleLayers) > 3 * PIPELINE3_CHUNKSIZE && triangleIndex > 2.5)
	{
		float maxIndex = round(max(max(inBuff[0].HULLCOORDS.y, inBuff[1].HULLCOORDS.y), inBuff[2].HULLCOORDS.y));
		// The maxIndex can be used to determine the correct layer for all but the last 2 triangles. For the
		// last 2 we need an additional check to see if it's the very last triangle or not.
		if (maxIndex > (floor(totalTriangles) + 0.5))
		{
			triangleIndex = maxIndex - ((triangleIndex - maxIndex) == 2 ? 1.0 : 2.0);
		}
		else triangleIndex = maxIndex - 2.0;
	}


	// What's the furthest layer distance?
	float maxMaxZFur = max(max(inBuff[0].MAX_Z, inBuff[1].MAX_Z), inBuff[2].MAX_Z);


	// Face-contact detection
	float movementRange = inBuff[0].WORLDTHICKNESS * (0.10 + maxMaxZFur + (_CameraProximityTouch * 0.5));
	float faceDistance = max((inBuff[0].VIEWDISTANCE + inBuff[1].VIEWDISTANCE + inBuff[2].VIEWDISTANCE) * 0.333333333333, movementRange * 0.5);


	// View direction    
	float3 viewDirection = float3(0, 0, 0);
#if defined (USING_STEREO_MATRICES)
	viewDirection = normalize((unity_StereoWorldSpaceCameraPos[0].xyz + unity_StereoWorldSpaceCameraPos[1].xyz) * 0.5 - (0.333333333333 * (inBuff[0].worldPos.xyz + inBuff[1].worldPos.xyz + inBuff[2].worldPos.xyz)));
#else
	viewDirection = normalize(_WorldSpaceCameraPos.xyz - (0.333333333333 * (inBuff[0].worldPos.xyz + inBuff[1].worldPos.xyz + inBuff[2].worldPos.xyz)));
#endif


	// Calculate layer spacing
	float skinZ = min(min(inBuff[0].SKIN_Z, inBuff[1].SKIN_Z), inBuff[2].SKIN_Z);
	float3 maxZFur = { inBuff[0].MAX_Z, inBuff[1].MAX_Z, inBuff[2].MAX_Z };
	float3 fadeIn = { IN[0].FURFADEIN, IN[1].FURFADEIN, IN[2].FURFADEIN };
	// Enforce a minimum of 25% of the max. This prevents the fur layers from intersecting each other when they touch skin.
	maxZFur = saturate(max(maxZFur, maxMaxZFur * 0.25));


	// Check for extreme angles. The fur needs a reasonable mip map level, otherwise it wont know where to put the hairs. If a triangle is
	// being viewed at an angle that is too sharp, then tilt the layers slightly to reduce the mip map level.
	float3 surfaceNormal = normalize(cross(inBuff[0].worldPos.xyz - inBuff[1].worldPos.xyz, inBuff[1].worldPos.xyz - inBuff[2].worldPos.xyz));
	// Because the Super Pipeline input verticies are in an unknown order, we need to check to make sure the surface normal is pointing the correct direction
	if (dot(surfaceNormal, inBuff[0].worldNormal) < 0) surfaceNormal = -surfaceNormal;
	// If we are directly facing the triangle, viewAngle will be -1. If we are viewing it directly sideways, viewAngle will be 0. If we are behind it, viewAngle will be postive.
	float viewAngle = dot(viewDirection, surfaceNormal);
	float3 tilt = float3(0, 0, 0);
	float tiltFactor = (0.65 - abs(viewAngle)) * _TiltEdges;
	float tiltThreshold = visibleLayers;
	if (tiltFactor > 0)
	{
		float maxDistance = max(max(inBuff[0].VIEWDISTANCE, inBuff[1].VIEWDISTANCE), inBuff[2].VIEWDISTANCE);
		float minDistance = min(min(inBuff[0].VIEWDISTANCE, inBuff[1].VIEWDISTANCE), inBuff[2].VIEWDISTANCE);
		float3 viewDistance = float3(inBuff[0].VIEWDISTANCE, inBuff[1].VIEWDISTANCE, inBuff[2].VIEWDISTANCE);
		// The furthest vertex will have a tilt of 1, and the closest will have a tilt of 0
		tilt = saturate((viewDistance - minDistance) / (maxDistance - minDistance));
		// Offset the tilt, so that the back and front move in opposite directions (this will also divide it in half)
		tilt -= 0.5;
		// Scale it to the correct thickness
		tilt *= tiltFactor * (maxZFur - skinZ) * 1;

		// Flipping instantly between front/back tilting looks bad, so we want to blend layers between the two instead
		tiltThreshold = saturate((viewAngle * 25) + 0.5) * visibleLayers;
		if (tiltThreshold < visibleLayers) tiltThreshold = floor(tiltThreshold);
	}


	// This is another example of, "this should be faster, but it's slower". Based on my experience with V2, I would think it would be faster
	// for the geometry shaders to interleave their layers with each other (improved parallelization?), but in V3 it's faster to let each
	// instance handle a sequential chunk of layers.

	for (int subLayer = 0; subLayer < PIPELINE3_CHUNKSIZE; subLayer++)
	{
		// NOTE: Since 4.0.0, the layers are actually ordered in reverse, since that makes it easier to figure
		// out the top layer. Also, there is no layer 0, the top layer is layer 1, so we need to be aware of that.

		float layer = subLayer + ((triangleIndex - 1) * PIPELINE3_CHUNKSIZE) + 1;
		if (layer > visibleLayers) return;

		// The 0.75 exponent makes it so that the layer spacing is a bit more spread out near the skin and a bit more compressed further away.
		float portion = min(0.998, pow(1 - (layer <= tiltThreshold ? layer / tiltThreshold : (layer - tiltThreshold) / max(1, floor(visibleLayers) - tiltThreshold)), saturate(skinZ + 0.75)));

		// If we are behind the triangle, tilt it the opposite direction so we can see more of the backside. The tiltThreshold gradually blends the layers between the two.
		float3 posZ = skinZ + (max(0, (layer < tiltThreshold ? tilt : -tilt) * saturate((portion - 0.07) * 3)) + ((maxZFur - skinZ) * portion * (1 - (abs(tilt) * portion * 0.75)))) * saturate((fadeIn + 0.05) * 2.0);

		float3 zData = posZ + round(skinZ * 100) * 100;
		float3 bend = pow(_HairStiffness * 0.85 + ((1 - _HairStiffness) * min(1, posZ)), 2);

		float posZArray[3] = { posZ.x, posZ.y, posZ.z };
		float bendArray[3] = { bend.x, bend.y, bend.z };
		float zDataArray[3] = { zData.x, zData.y, zData.z };

		// Pass all the data to the fragment shader. (Note: I've tried unrolling the loop to see if there is a speed increase. There is none.)
		for (int y = 0; y < 3; y++)
		{
			float3 adjustedNormal = inBuff[y].worldNormal;

			if (faceDistance < movementRange)
			{
				float3 crossVector = cross(viewDirection, adjustedNormal);
				crossVector = normalize(cross(crossVector, -adjustedNormal));
				adjustedNormal = normalize((adjustedNormal * faceDistance * 0.5) - (_CameraProximityTouch * crossVector * posZArray[y] * (movementRange - faceDistance)));
			}

			adjustedNormal = normalize(adjustedNormal - (((saturate(inBuff[y].FURFADEIN * 5.0) * inBuff[y].windEffect.xyz) + float3(0, _FurGravitySlider, 0)) * bendArray[y]));

#if defined(FASTFUR_DEBUGGING)
			o[y].worldPos = float4(inBuff[y].worldPos.xyz + adjustedNormal * inBuff[y].WORLDTHICKNESS * posZArray[y], inBuff[y].VISIBLELAYERS);
			o[y].vertDist.a = layer;
#else
			o[y].worldPos = inBuff[y].worldPos.xyz + adjustedNormal * inBuff[y].WORLDTHICKNESS * posZArray[y];
#endif

			o[y].pos = UnityWorldToClipPos(o[y].worldPos.xyz);
			o[y].ZDATA = zDataArray[y];

			UNITY_TRANSFER_FOG(o[y], o[y].pos);
		}

#if defined(FASTFUR_DEBUGGING)
		if (_FurDebugTopLayer < 0.5 || layer <= 1.0)
		{
#endif
			if (inBuff[0].HULLCOORDS.x == 0) { tristream.Append(o[0]); }
			else if (inBuff[1].HULLCOORDS.x == 0) { tristream.Append(o[1]); }
			else tristream.Append(o[2]);

			if (inBuff[0].HULLCOORDS.x == 1) { tristream.Append(o[0]); }
			else if (inBuff[1].HULLCOORDS.x == 1) { tristream.Append(o[1]); }
			else tristream.Append(o[2]);

			if (inBuff[0].HULLCOORDS.x == 2) { tristream.Append(o[0]); }
			else if (inBuff[1].HULLCOORDS.x == 2) { tristream.Append(o[1]); }
			else tristream.Append(o[2]);

			tristream.RestartStrip();
#if defined(FASTFUR_DEBUGGING)
		}
#endif
	}
}
