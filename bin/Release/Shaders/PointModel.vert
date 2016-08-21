#version 440

uniform mat4 worldViewProjMatrix;
uniform mat3 rotationMatrix;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_normal;

out VS_OUT
{
	vec3 normal;
} vs_out;

void main(void)
{	
	gl_Position = worldViewProjMatrix * vec4(rotationMatrix * in_position, 1.0f);
	vs_out.normal = rotationMatrix * in_normal;
}