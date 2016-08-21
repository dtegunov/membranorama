#version 440

uniform vec4 modelColor;
uniform vec3 cameraDirection;
uniform float isSelected;

in VS_OUT
{
	vec3 normal;
} fs_in;

out vec4 color;

void main(void)
{	
	float modulation = 1.0f;
	if (isSelected == 1.0f)
	{
		uvec2 coords = uvec2(gl_FragCoord.xy);
		coords /= uint(4);
		uint oddline = coords.y % uint(2);
		modulation = coords.x % uint(2) == oddline ? 1.0f : 0.2f;
	}
	
	modulation *= abs(dot(fs_in.normal, cameraDirection));
	
	color = vec4(modelColor.rgb * modulation, modelColor.a);
}