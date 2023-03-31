// The fragment shader needs to figure out if it is part of a visible hair. The steps to determine this are:
//   - How thick, and what direction is the combing of the fur?
//   - Multiply by the current height and the combing to determine where to look on the hair map
//   - Is there a hair there? Is it tall enough?
//   
// Once all of the above is complete and we've confirmed that we need to render a fragment of a hair, we then
// need to apply all of the various colouring, lighting, etc...
#if !defined (PREPASS)
#include "FastFur-Function-ToonShading.cginc"
#endif


#if defined(SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
fixed4 frag(fragInput i, fixed facing : VFACE) : SV_Target
#else
fixed4 frag(fragInput i) : SV_Target
#endif
{

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	// Get the furshape so we know the thickness and directionality of the fur.
	float zPos = frac(i.ZDATA * 0.1) * 10;// This will actually be the skin if this is the skin layer
	float skinZ = saturate(floor(i.ZDATA * 0.01) * 0.01);// This will actually be 0 if this is the skin layer

#if !defined(SKIN_LAYER)
	if (zPos > 1.05 || zPos <= skinZ + 0.002) discard;
#endif

	// We don't want mip maps for the furShape, because it creates bunch of artifacts. The fur already does a
	// good job of smoothing out any high-frequency noise.
	float4 furShape = _FurShapeMap.SampleLevel(sampler_FurShapeMap, i.uv, 0);

	// Density changes need to be reduced down into 33 discrete steps, otherwise the hairs will not be visible.
	// In the early days of making this shader, I thought this calculation would be faster to use a lookup table,
	// but it isn't. Apparently array lookups use similar GPU resources as texture lookups, and this shader
	// is texture-bound.
	float furDensity = pow(10,round(furShape.a * 32) * 0.125 - 2) * pow(_HairDensity, 3);
	bool densityChange = !(ddx(furDensity) == 0 && ddy(furDensity) == 0);


	// Apply optional height masks
	if (_FurShapeMask1Bits > 0)
	{
		float4 furMask = _FurShapeMask1.SampleLevel(sampler_FurShapeMap, i.uv, 0);
		if (_FurShapeMask1Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask1Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask1Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask1Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask2Bits > 0)
	{
		float4 furMask = _FurShapeMask2.SampleLevel(sampler_FurShapeMap, i.uv, 0);
		if (_FurShapeMask2Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask2Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask2Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask2Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}


	float thicknessSample = furShape.z * (1 - skinZ);
	thicknessSample *= furShape.z < _FurMinHeight ? 0.0 : 1.0;


	// This is one day going to handle fur being a bit shorter when combed, but the equations just don't seem correct yet
	//float furCombingLength = length(.498 - furShape.xy);
	//thicknessSample *= rsqrt(pow(furCombingLength * _FurCombStrength * _FurShellSpacing * _FurCombCompression * 10, 2) + 1);


	// Comb the hairs (ie. shift the uv map points), then check the hair map to see if we're on a hair
	// NOTE: The next line will create visible seams on hard edges if the combing is too strong and the
	// offset goes past the overgrooming in the textures.
#if defined(SKIN_LAYER)
	float2 furUV = i.uv;
#else
	float2 curls = float2(0,0);
	if (_HairCurlsActive > 0)
	{
		curls = float2(_HairCurlXTwists, _HairCurlYTwists) * 6.2832 * zPos;
		curls.y += _HairCurlXYOffset * 0.03491;
		curls = sin(curls) * float2(_HairCurlXWidth, _HairCurlYWidth) / (furDensity * 35);
	}
	float2 furUV = i.uv + curls + (.498 - furShape.xy) * _FurCombStrength * _FurShellSpacing * zPos * 0.1;
#endif
	float2 strandUV = furUV * furDensity;


	//--------------------------------------------------------------------------------
	// Sample the hair maps
	// 
	// This is not a simple task. We can't just take a sample and be done with it, because
	// the hair height will blend into mush by the time the GPU reaches mip map 2, which isn't very
	// far away. To prevent this, the shader counter-acts the effects of mip maps for the height channel.
	//
	// However, the technique to do this is different for the fine and the coarse hair maps. The fine map
	// works best with the old alpha filtering, but the coarse maps work better with the newer
	// mip-level filtering.

	// Calculate the LOD. There is a hardware function for this, but doing it mathematically gives better-looking results.
	float2 dx = ddx(strandUV * _HairMap_TexelSize.z);
	float2 dy = ddy(strandUV * _HairMap_TexelSize.w);
	float hairStrandLOD = max(0, (0.5 * log2(max(dot(dx, dx), dot(dy, dy)))) + (_HairBlur - _HairSharpen));
	//hairStrandLOD = min(hairStrandLOD, _HairMap.CalculateLevelOfDetailUnclamped(my_linear_repeat_sampler, strandUV) + (_HairBlur - _HairSharpen));
	//hairStrandLOD = _TS2;

	// If there was a fur density change, use a default, otherwise there will be a ridge along the density change.
	float hairStrandLODClamped = densityChange ? 4 : hairStrandLOD;


	// Sample the hair maps
	float4 hairStrand = _HairMap.SampleBias(sampler_HairMap, strandUV, _HairBlur - _HairSharpen);
	float4 coarseStrand = _HairMapCoarse.SampleBias(sampler_HairMap, strandUV, _HairBlur - _HairSharpen);


	//--------------------------------------------------------------------------------
	//Sample the fur markings
	float3 furMarkings = 0;
	float visibleEdge = zPos;
#if !defined(SKIN_LAYER)
	float markingsMultiplier = 1;
#endif
	if (_FurMarkingsActive > 0)
	{
		// Find the UV location
		float2x2 rotate = float2x2(_Cos, -_Sin, _Sin, _Cos);
		float2 markingsUV = furUV * (_MarkingsDensity * _MarkingsDensity);
		markingsUV = mul(markingsUV, rotate);

		// Take a raw sample, calculate the height offset, then multiply the colour tint
		furMarkings = tex2D(_MarkingsMap, markingsUV).rgb;
#if !defined(SKIN_LAYER)
		float markingSample = (furMarkings.r * 0.30 + furMarkings.g * 0.59 + furMarkings.b * 0.11);

		furMarkings = furMarkings.rgb * _MarkingsColour;

		// Re-scale the markingSample so that it is always 0-1
		markingSample = (markingSample - _MarkingsMapNegativeCutoff) / (_MarkingsMapPositiveCutoff - _MarkingsMapNegativeCutoff);

		// Flip it if this is a negative offset
		if (_MarkingsHeight > 0) markingSample = 1.0 - markingSample;

		// Calculate the multiplier
		markingsMultiplier = 1.0 - (markingSample * abs(_MarkingsHeight));
#endif
	}



#if !defined(SKIN_LAYER)
	//--------------------------------------------------------------------------------
	// Alpha filtering
	//--------------------------------------------------------------------------------
	// Apply a multiplication to the length, because MipMaps cause hair samples to get blurred with skin samples.
	// The multiplier is calculated by dividing the hair length by the alpha channel, which is always set to 1 for hairs.
	// This isn't a perfect correction, but it still dramatically improves the render quality.
	// 
	// A side-effect of this is that normal trilinear filter will also get multiplied, which will cause the hairs to
	// appear thicker when further away. This is also a benefit, making the hairs more visible further away, even
	// though it's not technically realistic.
	// 
	// Unfortunately, all this scaling sometimes results in errors that causes "fireflies" and other random high-height
	// pixels, which we need to deal with.

	// If the calibration is lower than the length, or there was a density change, that will cause an error, so don't scale it
	hairStrand.a = (hairStrand.a < hairStrand.b || densityChange) ? 1 : hairStrand.a;
	coarseStrand.a = (coarseStrand.a < coarseStrand.b || densityChange) ? 1 : coarseStrand.a;

	// The first filter that catches a lot of these stray pixels is to have a soft half-strength cutoff at higher MipMap levels.
	// The MAX_LOD threshold is higher, up to a maximum of double its value, as we get further away.
#define PIXEL_FILTER_MAX_LOD float2(8, 8)
	float2 filter1 = max(0.5, saturate((PIXEL_FILTER_MAX_LOD * (2 - i.FURFADEIN)) - hairStrandLODClamped));
	// The second filter is to add a slight bias to our equation. The larger the bias, the less "shimmery" pixels will make the cut.
#define PIXEL_FILTER_BIAS float2(0.1, 1.0)
	// Apply the filters. Together they catch about 90% of the glitchy pixels
	float2 alphaRescale = max(1, filter1 * ((1 + PIXEL_FILTER_BIAS) / max(0.1, (float2(hairStrand.a, coarseStrand.a) + PIXEL_FILTER_BIAS))));
	// The filtering makes the hairs a bit shorter, though, so re-scale everything to roughly the same height
#define PIXEL_FILTER_RESCALE float2(0.12, 0.12)
	alphaRescale *= 1.0 + (saturate(0.9 - float2(hairStrand.b, coarseStrand.b)) * PIXEL_FILTER_RESCALE);

	// If we're far away, multiply the length of the hairs. Otherwise they are invisible at far distances.
	alphaRescale *= 2.0 - saturate(i.FURFADEIN);

	float2 alphaRescaleStrength = float2(_HairMapAlphaFilter, _HairMapCoarseAlphaFilter);
	alphaRescale = (alphaRescale * alphaRescaleStrength) + (1 - alphaRescaleStrength);

	//--------------------------------------------------------------------------------
	// Mip level filtering
	//--------------------------------------------------------------------------------
	// The mip maps will cause the hairs to shrink, so we apply a multiplier based on the mip map level.
	// The critical point where this is the most dramatic is between mip maps 3-5.
	//
	// Debugging mip map levels = 0 Red, 1 Yellow, 2 Green, 3 Cyan, 4 Blue, 5 Red, 6 Yellow, 7 Green, 8 Cyan
	#define RESCALE_MIPLEVEL0 float2( 0.0, 0.0)
	#define RESCALE_MIPLEVEL1 float2( 1.5, 4.5)
	#define RESCALE_MIPLEVEL2 float2( 5.0, 6.5)
	#define RESCALE_MIPLEVEL3 float2( 7.0,10.0)
	#define RESCALE_MULTIPLY0 float2( 1.0, 0.0)
	#define RESCALE_MULTIPLY1 float2( 1.0, 1.0)
	#define RESCALE_MULTIPLY2 float2( 3.0, 2.0)
	#define RESCALE_MULTIPLY3 float2( 3.0, 3.0)
	float2 mipRescale = lerp(RESCALE_MULTIPLY0, RESCALE_MULTIPLY1, saturate((hairStrandLODClamped - RESCALE_MIPLEVEL0) / (RESCALE_MIPLEVEL1 - RESCALE_MIPLEVEL0)));
	mipRescale += lerp(RESCALE_MULTIPLY1, RESCALE_MULTIPLY2, saturate((hairStrandLODClamped - RESCALE_MIPLEVEL1) / (RESCALE_MIPLEVEL2 - RESCALE_MIPLEVEL1)));
	mipRescale += lerp(RESCALE_MULTIPLY2, RESCALE_MULTIPLY3, saturate((hairStrandLODClamped - RESCALE_MIPLEVEL2) / (RESCALE_MIPLEVEL3 - RESCALE_MIPLEVEL2)));
	mipRescale -= (RESCALE_MULTIPLY1 + RESCALE_MULTIPLY2);

	float2 mipRescaleStrength = float2(_HairMapMipFilter, _HairMapCoarseMipFilter);
	mipRescale = (mipRescale * mipRescaleStrength) + (saturate(mipRescale) * (1 - mipRescaleStrength));


	//--------------------------------------------------------------------------------
	// Calculate the final hair-length result
	float2 hairLength = float2(hairStrand.b, coarseStrand.b);
	hairLength.g *= _HairMapCoarseStrength;
	hairLength = (hairLength * alphaRescale * mipRescale * (1.0 + _HairClipping + ((1 - i.FURFADEIN) * 0.1)));

	float hairLengthFinal = saturate(max(hairLength.r, hairLength.g));


	// Are we on a visible part of a hair?
	visibleEdge = (skinZ + ((1 - skinZ) * markingsMultiplier)) * hairLengthFinal * thicknessSample;


#if defined(FASTFUR_DEBUGGING) && defined(FORWARD_BASE_PASS)
	if (_FurDebugVerticies > 0.5)
	{
		float vertMaxDist = max(max(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		float vertMinDist = min(min(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		if (vertMaxDist < 0.98 && (vertMinDist > 0.02 || _FurDebugUpperLimit < 0.5 || i.vertDist.a > 1.0)) clip(visibleEdge - zPos);
	}
	else
	{
		float vertMinDist = min(min(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		if (vertMinDist > 0.02 || _FurDebugUpperLimit < 0.5 || i.vertDist.a > 1.0) clip(visibleEdge - zPos);
	}
#else
	clip(visibleEdge - zPos);
#endif
#else
	// This is the skin, so set the hair length to the skin position, for advanced colouring purposes
	float hairLengthFinal = zPos;
#endif


	//--------------------------------------------------------------------------------
	// Beyond this point, we're on a hair (or the skin), so we need to determine what colour it should be
	//--------------------------------------------------------------------------------
	// Just a reminder to myself: if this is the skin layer, then the "skinZ" will actually be 0, while the
	// "zPos" will actually be where the skin is. This is intentional, since we want the skin to be lit like
	// fur if it isn't at position 0.


	// For advanced hair colouring, figure out where we are relative to the tips of the hairs, then determine how strong
	// the root/middle/tip colouring should be.
	float4 albedo = float4(1,1,1,1);
	float albedoMapStrength = 1;
	float markingsMapStrength = 1;
	if(_AdvancedHairColour > 0.5)
	{
		// First, determine if the fur is thick enough to enable the advanced hair colouring. If so, how strong is it?
		float advancedStrength = saturate(((furShape.z - _HairColourMinHeight) / (1.0001 - _HairColourMinHeight)) * (2 - _HairColourMinHeight));
		
		// At far distances, we will need to blend the colour layers together
		float separation = saturate(1 - (hairStrandLOD * 0.125));
		float midLow = (_HairRootPoint + _HairMidLowPoint) * 0.5;
		float midHigh = (_HairMidHighPoint + _HairTipPoint) * 0.5;
		float3 blendLevels = float3(midLow, midHigh - midLow, 1 - midHigh) * (1 - separation);

		// Next, where are we on the length of the hair?
		float hairZ = saturate(saturate((zPos * hairLengthFinal) / (furShape.z + 0.0001)) * (1 + pow(1.25, max(1, hairStrandLOD))) * 0.5);

		// Determine the relative strengths of the root, middle, and tip, based on where we are on the hair
		float3 colourLevels = saturate(float3(1 - ((hairZ - _HairRootPoint) / (_HairMidLowPoint - _HairRootPoint)), 0, (hairZ - _HairMidHighPoint) / (_HairTipPoint - _HairMidHighPoint)));
		colourLevels.y = 1 - (colourLevels.x + colourLevels.z);
		colourLevels = (colourLevels * separation) + blendLevels;

		albedo.rgb = (_HairRootColour * colourLevels.x) + (_HairMidColour * colourLevels.y) + (_HairTipColour * colourLevels.z);
		albedoMapStrength = (dot(float3(_HairRootAlbedo, _HairMidAlbedo, _HairTipAlbedo), colourLevels) * advancedStrength) + (1 - advancedStrength);
		markingsMapStrength = (dot(float3(_HairRootMarkings, _HairMidMarkings, _HairTipMarkings), colourLevels) * advancedStrength) + (1 - advancedStrength);

		albedo.rgb = (albedo.rgb * advancedStrength) + (float3(1,1,1) * (1 - advancedStrength));
	}


#if !defined (PREPASS)
	// We have something to render, start by getting the base colour. We limit the mip map level, otherwise
	// edge fur get sparkles due to the mip map being super-high. 
	float furShapeLOD = _FurShapeMap.CalculateLevelOfDetailUnclamped(sampler_FurShapeMap, _HideSeams ? i.uv : furUV);
#if defined(SKIN_LAYER)
	float4 albedoSample = tex2D(_MainTex, i.uv) * albedoMapStrength * _Color;
#else
	float4 albedoSample = tex2Dlod(_MainTex, float4(_HideSeams ? i.uv : furUV, 0, min(furShapeLOD, 4))) * albedoMapStrength * _Color;
#endif
	albedo = (albedo * albedoMapStrength * albedoSample) + ((1 - albedoMapStrength) * albedo);


	// Test for cutout
#if defined (_ALPHATEST_ON)
	clip(albedo.a - _Cutoff);
#endif



	// Calculate per-hair tinting (not accurate, but speed takes priority)
	if (_ToonShading == 0)
	{
#if defined(SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
		if (_HairColourShift != 0 && facing > 0.5)
#else
		if (_HairColourShift != 0)
#endif
		{
			float colourShift = saturate(furShape.z * 5) * (hairStrand.g - .498) * _HairColourShift;
			float channel1 = saturate(1 - colourShift);
			float channel2 = saturate(colourShift);
			float channel3 = saturate(-colourShift);
			albedo.rgb = albedo.rgb * channel1 + albedo.brg * channel2 + albedo.gbr * channel3;
		}

		// Apply per-hair highlights (not accurate, but speed takes priority)
#if defined(SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
		if (_HairHighlights != 0 && facing > 0.5)
#else
		if (_HairHighlights != 0)
#endif
		{
			float highlight = saturate(furShape.z * 5) * (hairStrand.r - .498) * _HairHighlights;
			float brightness = length(albedo.rgb);
			float highlighting = ((1.1 - brightness) * highlight) + 1;// darker colours are affected more, because otherwise white fur looks dirty
			albedo.rgb *= highlighting * highlighting * max(1, highlighting) * max(1, highlighting);
		}
	}

	// Apply fur markings
	if (_FurMarkingsActive)
	{
		// Apply markings tinting
		float colourStrength = _MarkingsVisibility * markingsMapStrength * furShape.z * max(1, (1.5 - i.FURFADEIN));
		albedo.rgb *= (float3(1, 1, 1) * (1 - colourStrength)) + (furMarkings * colourStrength);
	}



	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Fur occlusion is based on the depth of the fur. It affects all types of light, even direct light, because
	// it's an approximation of every type of light getting partially blocked by other hairs that are in the way.
	float furOcclusion = i.FURFADEIN * saturate(furShape.z - (zPos + _LightPenetrationDepth));
	furOcclusion = pow(1 - (furOcclusion * _DeepFurOcclusionStrength * 0.5), 2);

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Ambient occlusion is based on the occlusion map, further modified by proximity occlusion.
#if defined(SKIN_LAYER)
	float ambientOcclusion = (1 - _OcclusionStrength) + (tex2D(_OcclusionMap, i.uv).r * _OcclusionStrength);
#else
	float ambientOcclusion = 1;
#endif
	if (_ProximityOcclusion > 0)
	{
		float range = length(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
		if (range < _ProximityOcclusionRange)
		{
			float strength = ((_ProximityOcclusionRange - range) / _ProximityOcclusionRange) * _ProximityOcclusion;
			ambientOcclusion *= (1 - strength) * (1 - strength);
		}
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Calculate lighting
	float3 diffuseLight = i.lightData1.rgb * ambientOcclusion;

#if defined(POINT) || defined(SPOT) || (defined(USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE)))
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos.xyz); // This Unity macro also checks for shadows.
#else
	float attenuation = 1;
#endif

	float4 worldLight = min(_LightColor0, _MaxBrightness) * (1.0 - (_SoftenShadows * 0.1));
	float brightness = (worldLight.r * 0.30 + worldLight.g * 0.59 + worldLight.b * 0.11);
	if (brightness > 0.75 && _SoftenBrightness > 0)
	{
		float newBrightness = (brightness * (1 - _SoftenBrightness)) + ((brightness / max(1, brightness * 0.75 + 0.5)) * _SoftenBrightness);
		worldLight *= newBrightness / brightness;
		brightness = newBrightness;
	}

	float3 worldLightDir = _WorldSpaceLightPos0.xyz;
	if (worldLight.a == 0 && _FallbackLightEnable > 0)
	{
		worldLight = float4(_FallbackLightColor.rgb, 1) * _FallbackLightStrength;
		float lightYCos = cos(radians(_FallbackLightAngle));
		worldLightDir = normalize(float3(sin(radians(_FallbackLightDirection)) * lightYCos, -sin(radians(_FallbackLightAngle)), cos(radians(_FallbackLightDirection)) * lightYCos));
	}

	if (_WorldLightReColourStrength > 0)
	{
		worldLight.rgb = (worldLight.rgb * (1 - _WorldLightReColourStrength)) + (_WorldLightReColour.rgb * _WorldLightReColourStrength * brightness);
	}

	float anisotropic1Reflect = 0;
	float anisotropic2Refract = 0;

	if (worldLight.a > 0)
	{
#if defined(POINT) || defined(SPOT)
		worldLightDir = normalize(worldLightDir - i.worldPos.xyz);
#endif

		// Lambertian light
		float wrapScale = _LightWraparound * 0.01;
		float lambertLight = saturate((i.MAINLIGHTDOT * (1 - wrapScale)) + wrapScale);

#if !defined(SKIN_LAYER)
		// Brighten hair tips that are sticking out and catching the light
		float brightenTips = (_LightWraparound * 0.09 + i.MAINLIGHTDOT + pow(zPos, 1.5) - 1);// Brighten tips, leave the rest of the hair shadowed
		brightenTips *= max(0, 0.5 - i.MAINLIGHTDOT) * 0.15 * saturate(i.FURFADEIN + 0.5);// Multiply the brightening by how perpendicular the hair is to the light direction
		brightenTips = saturate(_LightWraparound * brightenTips);

#else
		float brightenTips = 0;
#endif
		// Subsurface scattering
		float scatterStrength = i.SUBSURFACESTRENGTH * (1 - (visibleEdge - zPos));


		// Base diffuse intensity
		float diffuseIntensity = saturate(lambertLight + brightenTips) + scatterStrength;


		float aniosoEnergyConservation = 0;
		if (_FurAnisotropicEnable > 0)
		{
			// Anisotropic
			float anisoBaseStrength = attenuation * (zPos < 0.001 ? _FurAnisoSkin : (saturate((zPos + _FurAnisoDepth * _FurAnisoDepth * _FurAnisoDepth * 0.1) * 10) * ((_FurAnisoDepth * 0.35) + (1 - _FurAnisoDepth) * pow(saturate((furShape.z + 0.5) - ((furShape.z + 0.25) - zPos)), 2)) * 5));
			anisotropic1Reflect = anisoBaseStrength * (1 + _FurAnisoReflectMetallic);
			anisotropic2Refract = anisoBaseStrength * (1 + _FurAnisoRefractMetallic);
			aniosoEnergyConservation = ((anisotropic1Reflect * _FurAnisotropicReflect * (1 + _FurAnisoReflectGloss * 0.5)) + (anisotropic2Refract * _FurAnisotropicRefract * (1 + _FurAnisoRefractGloss * 0.5))) * 0.025;

			anisoBaseStrength *= saturate(i.MAINLIGHTDOT);
			anisotropic1Reflect *= i.ANISOTROPIC1REFLECT * saturate(i.MAINLIGHTDOT);
			anisotropic2Refract *= i.ANISOTROPIC2REFRACT * saturate(i.MAINLIGHTDOT);
		}
		diffuseLight += worldLight * diffuseIntensity * attenuation * (1.0 - aniosoEnergyConservation);
	}
	// Fur Occlusion affects all types of light: ambient, direct, and anisotropic. It is also in addition to ambient occlusion.
	diffuseLight *= furOcclusion;

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Apply Toon Shading
	if (_ToonShading > 0)
	{
		albedo = toonAlbedo(albedo);
		if (_ToonLighting > 0)
		{
			if (_FurAnisotropicEnable > 0)
			{
				float3 aniso1ReflectColour = worldLight * anisotropic1Reflect * _FurAnisotropicReflectColor * ((1 - _FurAnisoReflectMetallic) + (_FurAnisoReflectMetallic * albedo.rgb));
				float3 aniso2RefractColour = worldLight * anisotropic2Refract * _FurAnisotropicRefractColor * ((1 - _FurAnisoRefractMetallic) + (_FurAnisoRefractMetallic * albedo.rgb));
				diffuseLight += (aniso1ReflectColour + aniso2RefractColour) * 3;
			}
			diffuseLight = toonLighting(diffuseLight);
		}
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Skin-layer
#if defined(SKIN_LAYER)
	float3 PBSCol = float3(0,0,0);
	float PBSBlend = 0.0;
	float4 matCapCol = float4(0,0,0,0);
	float matCapMask = 0.0;

	if (_PBSSkin * 1.55 > furShape.z) // 1.55 ensures that this is always calculated when needed, while reducing by ~95% calculating when not needed
	{
		// This fancy looking equation makes more sense when graphed. _PBSSkin 0.0 is always fully off. _PBSSkin 1.0 is always fully on.
		// _PBSSkin 0.59 is fully on for any fur below half-height, and then gradually fades to fully off for full-height fur.
		PBSBlend = saturate((1.0 - (furShape.z / tan(min(i.FURFADEIN + 0.1, _PBSSkin * 0.785398)))) + 1.0);// 0.785398 = PI / 4

		float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);// What direction is the camera?
#if defined(_NORMALMAP)
		float3 worldBinormal = cross(i.worldNormal, i.worldTangent.xyz) * (i.worldTangent.w * unity_WorldTransformParams.w);
		float3 normalMapped = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
		normalMapped = normalize((normalMapped.x * i.worldTangent) + (normalMapped.y * worldBinormal) + (normalMapped.z * i.worldNormal));
#else
		float3 normalMapped = normalize(i.worldNormal);
#endif

#if defined(_METALLICGLOSSMAP)
		float2 metallicMap = tex2D(_MetallicGlossMap, i.uv).ra;
#endif

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// MatCap
		if (_MatcapEnable > 0.0)
		{
			if (_MatcapTextureChannel == 0) { matCapMask = 1.0; }
			else if (_MatcapTextureChannel == 1) { matCapMask = tex2D(_MatcapMask, i.uv).r; }
			else if (_MatcapTextureChannel == 2) { matCapMask = tex2D(_Matcap, i.uv).a; }
			else if (_MatcapTextureChannel == 3) { matCapMask = albedo.a; }
#if defined(_METALLICGLOSSMAP)
			else if (_MatcapTextureChannel == 4 || _MatcapTextureChannel == 5)
			{
				matCapMask = _MatcapTextureChannel == 4 ? metallicMap.y : metallicMap.r;
			}
#endif
			matCapMask *= PBSBlend;

			if (matCapMask > 0.0)
			{
				float3 viewDirUp = normalize(float3(0, 1, 0) - (viewDir * dot(viewDir, float3(0, 1, 0))));
				float3 viewDirRight = normalize(cross(viewDirUp, viewDir));

				float2 matCapUV = float2(dot(viewDirRight, normalMapped), dot(viewDirUp, normalMapped));
				matCapUV = (_MatcapSpecular * matCapUV) + ((1.0 - _MatcapSpecular) * i.worldNormal.xy);
				matCapUV = matCapUV * 0.5 + 0.5;

				matCapCol = tex2D(_Matcap, matCapUV) * _MatcapColor;
				albedo.rgb += matCapCol.rgb * _MatcapAdd * matCapMask;
				albedo.rgb = (albedo.rgb * (1.0 - (_MatcapReplace * matCapMask))) + (matCapCol.rgb * _MatcapReplace * matCapMask);
			}
		}


		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// PBS Skin
#if defined(_METALLICGLOSSMAP)
		float metallic = metallicMap.r;
#else
		float metallic = _Metallic;
#endif

#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
		float smoothness = albedo.a * _GlossMapScale;
#elif defined(_METALLICGLOSSMAP)
		float smoothness = metallicMap.y * _GlossMapScale;
#else
		float smoothness = _Glossiness;
#endif

		float3 specularTint;
		float oneMinusReflectivity;
		float3 PBSAlbedo = DiffuseAndSpecularFromMetallic(albedo.rgb, metallic, specularTint, oneMinusReflectivity);

		UnityLight light;
		light.color = worldLight * attenuation * furOcclusion;
		light.dir = worldLightDir;
		light.ndotl = DotClamped(normalMapped, light.dir);

		UnityIndirect indirectLight;
		indirectLight.diffuse = i.lightData1.rgb * ambientOcclusion * furOcclusion;
#if defined(FORWARD_BASE_PASS) && !defined(_GLOSSYREFLECTIONS_OFF)
		float3 reflectDir = reflect(-viewDir, normalMapped);
		float4 reflectSample = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDir);
		indirectLight.specular = DecodeHDR(reflectSample, unity_SpecCube0_HDR) * ambientOcclusion * furOcclusion;
#else
		indirectLight.specular = 0;
#endif

		PBSCol = UNITY_BRDF_PBS(PBSAlbedo, specularTint, oneMinusReflectivity, smoothness, normalMapped, viewDir, light, indirectLight);
	}
#endif


	// Calculate our 'final' (sort of) colour
	float3 finalCol = albedo.rgb * diffuseLight;
#if defined(SKIN_LAYER)
	finalCol = (finalCol * (1.0 - PBSBlend)) + (PBSCol * PBSBlend);
#endif
	if (_FurAnisotropicEnable > 0 && (_ToonLighting < 0.5 || _ToonShading < 0.5))
	{
		float3 anisoReflectColor = ((1 - _FurAnisoReflectMetallic) + (_FurAnisoReflectMetallic * albedo.rgb)) * _FurAnisotropicReflectColor;
		if (_FurAnisoReflectIridescenceStrength > 0 && anisotropic1Reflect > 0) anisoReflectColor = (anisoReflectColor * (1 - (_FurAnisoReflectIridescenceStrength * 0.05))) + (_FurAnisoReflectIridescenceStrength * 0.05 * (
			(((0.5 + albedo.rgb) * _FurAnisotropicReflectColor) * saturate(1 - abs(i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength))) +
			(((0.5 + albedo.brg) * _FurAnisotropicReflectColorNeg) * saturate(-i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength)) +
			(((0.5 + albedo.gbr) * _FurAnisotropicReflectColorPos) * saturate(i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength))));

		float3 aniso1ReflectColour = (worldLight + (anisotropic1Reflect > 0 ? _FurAnisoReflectEmission : 0)) * anisotropic1Reflect * anisoReflectColor;
		float3 aniso2RefractColour = (worldLight + (anisotropic2Refract > 0 ? _FurAnisoRefractEmission : 0)) * anisotropic2Refract * _FurAnisotropicRefractColor * ((1 - _FurAnisoRefractMetallic) + (_FurAnisoRefractMetallic * albedo.rgb));
		finalCol += aniso1ReflectColour + aniso2RefractColour;
	}


	// Apply emission
#if defined(_EMISSION) && defined(FORWARD_BASE_PASS)
	finalCol += (tex2D(_EmissionMap, _HideSeams ? i.uv : furUV).rgb * _EmissionColor.rgb * _EmissionMapStrength) + (_AlbedoEmission * albedoSample);
#endif
#if defined(SKIN_LAYER) && defined(FORWARD_BASE_PASS)
	finalCol += matCapCol * matCapMask * _MatcapEmission;
#endif


	// Apply fog
	UNITY_APPLY_FOG(i.fogCoord, finalCol.rgb);


	// Apply debugging colours
#if defined(FASTFUR_DEBUGGING)
	// If debugging is on, limit the brightness so that it doesn't wash out the debugging colours
	finalCol = saturate(finalCol);
#endif
#if defined(FASTFUR_DEBUGGING) && defined(FORWARD_BASE_PASS)
	float3 debugColour[12] = {
		float3(1  ,0  ,0),// Editor
		float3(1  ,0.5,0),// VR
		float3(1  ,1  ,0),// Desktop
		float3(0.5,1  ,0),
		float3(0  ,1  ,0),// VR Viewfinder
		float3(0  ,1  ,0.5),
		float3(0  ,1  ,1),// Desktop Viewfinder
		float3(0  ,0.5,1),
		float3(0  ,0  ,1),// Camera Photo
		float3(0.5,0  ,1),
		float3(1  ,0  ,1),// Screenshot
		float3(1  ,0  ,0.5)// Stream Camera
	};
	finalCol = _FurDebugDistance && i.VISIBLELAYERS > 0 ? debugColour[floor(i.VISIBLELAYERS + 1.001) % 12] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugMipMap ? debugColour[(((int)hairStrandLOD) * 2) % 10] * .25 + finalCol * .75 : finalCol;
#if !defined(SKIN_LAYER)
	finalCol = _FurDebugHairMap ? (hairLengthFinal > hairLength.x ? (hairLengthFinal > hairLength.y ? debugColour[8] * .25 + finalCol * .75 : debugColour[4] * .25 + finalCol * .75) : debugColour[0] * .25 + finalCol * .75) : finalCol;
	finalCol = _FurDebugDepth ? debugColour[11 * (i.vertDist.a / i.VISIBLELAYERS)] * .25 + finalCol * .75 : finalCol;
#endif
	finalCol = _FurDebugLength ? debugColour[(uint)round(furShape.z * 64) % (uint)12] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugDensity ? debugColour[(uint)round(furShape.a * 32) % (uint)12] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugCombing ? float3(furShape.rg, 0) * 0.75 + finalCol * 0.25 : finalCol;
	if (_FurDebugQuality)
	{
	#if defined (USING_STEREO_MATRICES)
		float quality = _V4QualityVR;
	#else
		float quality = _VRChatCameraMode < -0.5 ? _V4QualityEditor : _V4Quality2D;
		if (_VRChatMirrorMode > 0.5)
		{
			quality = _VRChatMirrorMode > 1.5 ? _V4Quality2DMirror : _V4QualityVRMirror;
		}
		else
		{
			if (_VRChatCameraMode > 2.5)
			{
				quality = _V4QualityScreenshot;
			}
			else if (_VRChatCameraMode > 0.5)
			{
				quality = (_ScreenParams.y == 720) ? _V4QualityCameraView : (_ScreenParams.y == 1080) ? _V4QualityCameraPhoto : _V4QualityStreamCamera;
			}
		}
	#endif
		if (quality > 9) quality -= 2;
		if (quality > 9) quality -= 2;
		finalCol = (debugColour[quality % 12] * .25) + (finalCol * .75);
	}

#if !defined(SKIN_LAYER)
	if (_FurDebugVerticies)
	{
		float vertDist = max(max(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		finalCol.rgb = (vertDist < 0.98 ? finalCol.rgb : float3(0.5, zPos > 0.98 ? 0 : 0.5, 0.5));
	}
#endif
#endif

#if defined(SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
	if (facing < 0.5)
	{
		finalCol.rgb = (finalCol.rgb * _BackfaceColor.rgb) + _BackfaceEmission.rgb;
	}
#endif

	return(float4(finalCol.rgb, 1));
#endif

	return(float4(0,0,0,0));
}


