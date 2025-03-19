shader_type spatial;
render_mode unshaded, depth_draw_always, cull_disabled, blend_mix;

uniform sampler2D depth_texture : hint_depth_texture;
uniform sampler2D decal_texture : source_color;

uniform float decal_radius = 1.0;
uniform float intersection_thickness = 0.1;
uniform float decal_scale = 1.0;
uniform float time_scale = 0.1;
uniform vec4 decal_color : source_color = vec4(1.0, 1.0, 1.0, 0.5);

void fragment() {
    // Get the depth and reconstruct world position
    float depth = textureLod(depth_texture, SCREEN_UV, 0.0).r;
    vec4 upos = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth, 1.0);
    vec3 world_pos = (INV_VIEW_MATRIX * (upos / upos.w)).xyz;

    // Transform the world position to object space
    vec3 obj_pos = (inverse(MODEL_MATRIX) * vec4(world_pos, 1.0)).xyz;

    // Calculate distance from decal center
    float dist_from_center = length(obj_pos);

    // Calculate distance from decal surface
    float dist_from_surface = abs(dist_from_center - decal_radius);

    // Determine if we should render this fragment
    bool should_render = dist_from_surface <= intersection_thickness;

    // Calculate decal UV based on object space position
    vec2 decal_uv = (obj_pos.xz / decal_radius * 0.5 + 0.5 + vec2(TIME * time_scale)) * decal_scale;

    // Sample decal texture
    vec4 decal = texture(decal_texture, decal_uv);

    // Calculate fade based on distance from surface
    float fade = 1.0 - clamp(dist_from_surface / intersection_thickness, 0.0, 1.0);

    // Combine color and decal
    vec4 final_color = decal_color * decal;
    final_color.a *= fade;

    if (should_render) {
        ALBEDO = final_color.rgb;
        ALPHA = final_color.a;
    } else {
        // Discard fragments outside the decal area
        discard;
    }
}