shader_type spatial;
render_mode world_vertex_coords, depth_prepass_alpha, blend_mix;

uniform vec4 water_color : source_color = vec4(0, 0.41, 0.56, 0.85);

group_uniforms caustic;
uniform sampler2D caustic_texture : source_color, filter_nearest, repeat_enable;
uniform float caustic_texture_scale = 1;
uniform float caustic_intensity = 1;
uniform vec4 caustic_highlight_color : source_color = vec4(1, 1, 1, 1);

group_uniforms waves;
uniform float wave_speed = 1.0;      // Speed of the wave movement
uniform float wave_amplitude = 0.5;  // Height of the wave movement
uniform float wave_frequency = 2.0;  // Number of waves per unit distance


group_uniforms foam;
uniform sampler2D foam_texture : filter_nearest, repeat_enable;
uniform float foam_tex_scale = 1;
uniform vec4 edge_color: source_color;
uniform float edge_scale = 0.1;
uniform float near = 0.1;
uniform float far = 100.0;

uniform float beer_law_factor = 2.0;
uniform float _distance = 50.0;

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

vec2 round_to_pixel(vec2 i, int width){
	float denom = 1.0 / float(width);
	float _x = i.x + abs(mod(i.x, denom) - denom);
	float _y = i.y + abs(mod(i.y , denom) - denom);
	return vec2(_x, _y);
}

float calc_depth_fade(float depth, mat4 projection_matrix, 
						vec4 fragcoord, float beer_factor, float __distance, vec3 vertex) {
	float scene_depth = depth;

	scene_depth = scene_depth * 2.0 - 1.0;
	scene_depth = projection_matrix[3][2] / (scene_depth + projection_matrix[2][2]);
	scene_depth = scene_depth + vertex.z; // z is negative
	
	// application of beers law
	scene_depth = exp(-scene_depth * beer_factor);
	
	float screen_depth = fragcoord.z;
	
	float depth_fade = (scene_depth - screen_depth) / __distance;
	
	depth_fade = clamp(depth_fade, 0.0, 1.0);
	
	return depth_fade;
}

float rim(float depth) {
	depth = 2.0 * depth - 1.0;
	return near * far / (far + depth * (near - far));
}

void vertex() {
	float wave1 = sin(VERTEX.x * wave_frequency + TIME * wave_speed) * wave_amplitude;
	float wave2 = cos(VERTEX.z * wave_frequency * 0.8 + TIME * wave_speed * 1.2) * wave_amplitude * 0.8;
	VERTEX.y += wave1 + wave2;
}

void fragment() {
	vec2 uv = UV * caustic_texture_scale;
	vec2 animated_uv = uv + vec2(TIME * 0.01, TIME * 0.01);
	vec4 tex_color = texture(caustic_texture, animated_uv);

	vec3 final_color = mix(water_color.rgb, tex_color.rgb, tex_color.a * caustic_intensity);
	
	float z_depth = rim(texture(DEPTH_TEXTURE, SCREEN_UV).x);
	float z_pos = rim(FRAGCOORD.z);
	float diff = z_depth - z_pos;
	
	// depth-fade
	float z_depth_fade = calc_depth_fade(texture(DEPTH_TEXTURE, SCREEN_UV).x, PROJECTION_MATRIX, FRAGCOORD, beer_law_factor, _distance, VERTEX);
	vec4 gradientcolor = mix(water_color, water_color, z_depth_fade);
	vec4 depth_color_adj = mix(edge_color, gradientcolor, step(edge_scale, diff));

	ALBEDO = final_color;
	ALPHA = water_color.a;
	
	// Depth-based edge detection
	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r;
	vec4 upos = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth, 1.0);
	vec3 pixel_position = upos.xyz / upos.w;
	
	vec2 foam_uv = round_to_pixel(uv, 200);
	vec4 foam_tex = texture(foam_texture, foam_uv * foam_tex_scale);
	vec3 foam_c = mix(edge_color.rgb, foam_tex.rgb, edge_color.a);
	
	// Adjust the edge foam calculation to include edge_scale
	if (VERTEX.z < pixel_position.z + 0.05 + edge_scale) {
		ALBEDO = foam_c.rgb;
		ALPHA = edge_color.a;
	}
}