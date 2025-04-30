shader_type spatial;
render_mode unshaded, cull_back, depth_draw_opaque;

uniform vec3 hit_position;
uniform float max_radius = 0.5;
uniform float pixelation = 50.0;
uniform bool effect_enabled = false;
uniform float activation_range = 3.0;
uniform vec2 subviewport_size;  // Pass SubViewport size as a uniform

void fragment() {
    vec3 color = vec3(1.0);  // Default color to white (or any color you want)
    float alpha = 0.0;

    if (effect_enabled) {
        // Adjust screen position for SubViewport scaling using passed size
        vec2 screen_pos = FRAGCOORD.xy / subviewport_size;  // Normalize coordinates by SubViewport size
        vec4 screen_pos_homogenous = vec4(screen_pos, 0.0, 1.0);

        // Inverse of the projection matrix to convert screen space to view space
        mat4 inv_proj = inverse(PROJECTION_MATRIX);
        vec4 view_pos = inv_proj * screen_pos_homogenous;

        // Now convert from view space to world space using the view matrix
        mat4 inv_view = inverse(VIEW_MATRIX);
        vec4 world_pos = inv_view * view_pos;

        // Calculate the distance from the hit position
        float dist_to_hit = distance(world_pos.xyz, hit_position);
        float t = clamp(1.0 - (dist_to_hit / activation_range), 0.0, 1.0);
        float dynamic_radius = t * max_radius;

        // Ensure we only apply the effect within a valid range
        if (dist_to_hit < activation_range) {
            vec2 uv = UV * pixelation;
            uv = floor(uv) / pixelation;

            // Check the distance threshold for the circle effect
            float circle = smoothstep(dynamic_radius, dynamic_radius * 0.95, dist_to_hit);
            color = mix(vec3(1.0), vec3(0.0), circle);  // Mix white and black based on the circle
            alpha = circle;  // Set alpha based on the circle effect
        }
    }

    ALBEDO = color;
    ALPHA = alpha;
}
