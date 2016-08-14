#version 440

in GS_OUT
{
	vec4 color;
	flat bool selected;
} fs_in;

out vec4 color;

void main(void)
{	
	float modulation = 1.0f;
	if (fs_in.selected)
	{
		uvec2 coords = uvec2(gl_FragCoord.xy);
		coords /= uint(4);
		uint oddline = coords.y % uint(2);
		modulation = coords.x % uint(2) == oddline ? 1.0f : 0.2f;
	}
	
	color = vec4(fs_in.color.rgb * modulation, fs_in.color.a);
}