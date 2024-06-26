shader_type spatial;
render_mode shadows_disabled;

global uniform float time;

uniform sampler2D reflection_texture : source_color, hint_default_transparent;
uniform sampler2D base : hint_normal;
uniform sampler2D detail : hint_normal;
uniform vec4 base_color : source_color;
uniform float opacity : hint_range(0, 1, .1) = 1;

uniform float base_scroll_speed = .05;
uniform float detail_scroll_speed = .1;

const float DISTORTION_STRENGTH = 0.1;
const float REFLECTION_STRENGTH = 0.5;
const float NORMAL_FADE_START = 500.0;
const float NORMAL_FADE_END = 0.0;

void fragment()
{
	vec4 normal = texture(base, UV + time * base_scroll_speed) + texture(detail, UV - time * detail_scroll_speed);
	
	vec2 uv = SCREEN_UV;
	uv.y *= -1.0;
	//Distort reflection slightly
	uv.x += (0.5 - normal.r) * DISTORTION_STRENGTH;
	vec4 reflection_col = texture(reflection_texture, uv) * REFLECTION_STRENGTH;
	vec4 col = reflection_col + base_color * base_color.a;
	col = clamp(col, 0, 1) * COLOR;
	
	//Fade normal map over distance
	float normal_fade = clamp(smoothstep(NORMAL_FADE_START, NORMAL_FADE_END, -VERTEX.z), 0.0, 1.0);
	NORMAL = normal.rgb * .5;
	ROUGHNESS = 0.0;
	
	ALBEDO = col.rgb;
	ALPHA = clamp(opacity * COLOR.a, 0.0, 1.0);
}
