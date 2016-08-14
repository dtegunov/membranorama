#version 420

uniform mat4 worldViewProjMatrix;
uniform float surfaceOffset;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_normal;
layout (location = 2) in vec3 in_volumePosition;
layout (location = 3) in vec3 in_volumeNormal;
layout (location = 4) in vec4 in_color;
layout (location = 5) in float in_selection;

out VS_OUT
{
	vec3 normal;
	vec3 volumePosition;
	vec3 volumeNormal;
	vec4 color;
	float selection;
} vs_out;

void main(void)
{
	vs_out.normal = in_normal;

	vs_out.volumePosition = in_volumePosition + in_volumeNormal * surfaceOffset;
	vs_out.volumeNormal = in_volumeNormal;
	
	vs_out.color = in_color;
	vs_out.selection = in_selection;

	gl_Position = worldViewProjMatrix * vec4(in_position + in_normal * surfaceOffset, 1.0f);
}