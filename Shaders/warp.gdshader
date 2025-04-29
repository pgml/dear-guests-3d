shader_type spatial;

render_mode blend_mix, cull_back, depth_prepass_alpha;

uniform sampler2D albedo_texture : source_color;

uniform float wave_amplitude : hint_range(0.0, 1.0) = 0.2;
uniform float wave_frequency : hint_range(0.0, 10.0) = 3.0;
uniform float wave_speed : hint_range(0.0, 10.0) = 1.0;

void vertex() {
    float wave = sin(VERTEX.y * wave_frequency + TIME * wave_speed);
    VERTEX.x += wave * wave_amplitude;
}

void fragment() {
    vec4 tex_color = texture(albedo_texture, UV);
    ALBEDO = tex_color.rgb;
    ALPHA = tex_color.a;
}
