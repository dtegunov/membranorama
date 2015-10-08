#version 420

uniform float useVolume;
uniform vec3 volScale;
uniform vec3 volOffset;
uniform vec3 texSize;
uniform vec2 traceParams;
uniform vec2 normalizeParams;
uniform vec3 lightDirection;
uniform float lightIntensity;

in VS_OUT
{
	vec3 normal;
	vec3 volumePosition;
	vec3 volumeNormal;
	vec4 color;
} fs_in;

layout (binding = 0) uniform sampler3D volTexture;

out vec4 color;

float bspline(float t)
{
	t = abs(t);
	const float a = 2.0f - t;

	if (t < 1.0f) return 2.0f / 3.0f - 0.5f * t * t * a;
	else if (t < 2.0f) return a * a * a / 6.0f;
	else return 0.0f;
}

float cubicTex3DSimple(vec3 pos)
{
	// transform the coordinate from [0,extent] to [-0.5, extent-0.5]
	const vec3 coord_grid = pos - 0.5f;
	vec3 index = floor(coord_grid);
	const vec3 fraction = coord_grid - index;
	index = index + 0.5f;  //move from [-0.5, extent-0.5] to [0, extent]

	float result = 0.0f;
	for (float z = -1; z < 2.5f; z++)  //range [-1, 2]
	{
		float bsplineZ = bspline(z - fraction.z);
		float w = index.z + z;
		for (float y = -1; y < 2.5f; y++)
		{
			float bsplineYZ = bspline(y - fraction.y) * bsplineZ;
			float v = index.y + y;
			for (float x = -1; x < 2.5f; x++)
			{
				float bsplineXYZ = bspline(x - fraction.x) * bsplineYZ;
				float u = index.x + x;
				vec3 coords = vec3(u, v, w) * texSize;
				//coords.yz = 1.0f - coords.yz;
				result += bsplineXYZ * texture(volTexture, vec3(u, v, w) * texSize).r;
			}
		}
	}
	
	return result;
}

void main(void)
{	
	float lum = 0.0f;
	
	if (useVolume != 0.0f)
	{
		vec3 volumePosition = (fs_in.volumePosition - volOffset) * volScale;
		
		uint nsteps = uint(traceParams.y + 0.5f) * 4;
		float traceSum = 0.0f;
		vec3 traceStart = volumePosition + traceParams.x * fs_in.volumeNormal;
		vec3 traceStep = fs_in.volumeNormal * 0.25f;
		
		for (uint i = 0; i <= nsteps; i++)
		{
			vec3 pos = traceStart + float(i) * traceStep;
			//traceSum += texture(volTexture, (pos + 0.5f) * texSize).r;
			traceSum += cubicTex3DSimple(pos + 0.5f);
		}
		traceSum /= float(nsteps + 1);
		traceSum = (traceSum - normalizeParams.x) / normalizeParams.y;
		lum += traceSum;
	}
	
	lum += abs(dot(fs_in.normal, lightDirection)) * lightIntensity;
	
	color = vec4(mix(vec3(lum, lum, lum), fs_in.color.xyz, fs_in.color.w), 1.0f);
}