shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(0.3, 1.0, 0.4, 1.0);
uniform vec4 line_color_attack : source_color = vec4(1.0, 0.2, 0.2, 1.0);
uniform float line_thickness : hint_range(0, 10) = 1.0;
uniform bool enabled = false;
uniform bool attack = false;

const vec2 OFFSETS[8] = {
    vec2(-1, -1), vec2(-1, 0), vec2(-1, 1),
    vec2(0, -1), vec2(0, 1),
    vec2(1, -1), vec2(1, 0), vec2(1, 1)
};

void fragment() {
    // COLOR на входе = texture * modulate * self_modulate
    // Поэтому "чистая" текстура без modulate:
    vec4 tex = COLOR; // это уже текстура с modulate, используем как основу

    if (enabled || attack) {
        // Считаем обводку по альфе соседей в текстуре
        vec2 size = TEXTURE_PIXEL_SIZE * line_thickness;
        float outline = 0.0;
        for (int i = 0; i < OFFSETS.length(); i++) {
            outline += texture(TEXTURE, UV + size * OFFSETS[i]).a;
        }
        outline = min(outline, 1.0);

        // ВАЖНО: используем альфу "чистой" текстуры для маски, а не COLOR.a
        float tex_alpha = texture(TEXTURE, UV).a;
        float mask = max(0.0, outline - tex_alpha);

        vec4 selected_color = attack ? line_color_attack : line_color;

        // Смешиваем: основа (с modulate) + цвет обводки (без modulate, чтобы обводка была видна)
        // Но чтобы modulate влиял и на обводку, можно умножить selected_color на modulate.a или modulate.rgb
        COLOR = mix(tex, selected_color, mask);
    }
    // Если выключено — COLOR уже правильный (текстура * modulate), ничего не делаем
}