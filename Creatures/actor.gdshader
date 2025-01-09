shader_type spatial;

uniform sampler2D texture_albedo : hint_normal;

void fragment() {
    vec4 albedo_tex = texture(texture_albedo, UV);
    ALBEDO = albedo_tex.rgb;
    ALPHA = albedo_tex.a;

    // Enable shadow reception for transparent materials
    if (albedo_tex.a < 0.1) {
        discard;
    }
}