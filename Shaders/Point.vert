#version 420

uniform mat4 worldViewProjMatrix;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_color;

out VS_OUT
{
	vec3 color;
} vs_out;

void main(void)
{
	vs_out.color = in_color;

	gl_Position = worldViewProjMatrix * vec4(in_position, 1.0f);
}