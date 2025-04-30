shader_type spatial;

render_mode blend_mix, cull_back, depth_prepass_alpha;

uniform sampler2D albedo_texture : source_color;

uniform float wave_amplitude : hint_range(0.0, 1.0) = 0.2;
uniform float wave_frequency : hint_range(0.0, 10.0) = 3.0;
uniform float wave_speed : hint_range(0.0, 10.0) = 1.0;

void vertex() {
    float wave = sin(VERTEX.y * wave_frequency + TIME * wave_speed);

    // Scale the effect based on height: 0 at y=0, 1 at max height
    float height_factor = clamp(VERTEX.y, 0.0, 1.0); // assumes mesh height is in range [0,1]
    VERTEX.x += wave * wave_amplitude * height_factor;
}

void fragment() {
    vec4 tex_color = texture(albedo_texture, UV);
    ALBEDO = tex_color.rgb;
    ALPHA = tex_color.a;
}
