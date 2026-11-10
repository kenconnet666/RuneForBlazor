using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Rune.UI;

/// <summary>内置效果交给 JS 场景的参数（data-options 的 JSON，源生成，裁剪友好）；名字是场景里 context.options 的字段。</summary>
[JsonSerializable(typeof(ShaderOptions))]
[JsonSerializable(typeof(MeshGradientOptions))]
[JsonSerializable(typeof(AuroraOptions))]
[JsonSerializable(typeof(ParticleOptions))]
[JsonSerializable(typeof(WaveGridOptions))]
[JsonSerializable(typeof(GlobeOptions))]
[JsonSerializable(typeof(ModelViewerOptions))]
[JsonSerializable(typeof(ConfettiOptions))]
// 字段名驼峰（JS 的写法）
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class ThreeJson : JsonSerializerContext
{
    /// <summary>参数 → JSON 节点（交给 EffectSurface 的 Options）。</summary>
    internal static JsonNode Node<T>(T options, JsonTypeInfo<T> info) => JsonSerializer.SerializeToNode(options, info)!;
}

internal sealed record ShaderOptions(string FragmentShader, IReadOnlyDictionary<string, double>? Uniforms, string[] Palette, double Speed, bool Interactive);

internal sealed record MeshGradientOptions(string[] Palette, double Speed, double Distortion, double Grain, bool Interactive);

internal sealed record AuroraOptions(string[] Palette, double Speed, double Intensity, double Height, bool Interactive);

internal sealed record ParticleOptions(int? Count, string[] Palette, double Size, double Speed, bool Links, double LinkDistance, bool Interactive, double RepelRadius);

internal sealed record WaveGridOptions(string Type, string[] Palette, double Speed, double Amplitude, bool Interactive);

internal sealed record GlobeMarkerOptions(double Lat, double Lng, double? Size, string? Color);

internal sealed record GlobeArcOptions(double[] From, double[] To);

internal sealed record GlobeOptions(
    GlobeMarkerOptions[] Markers, GlobeArcOptions[] Arcs, GeoPoint Center, double RotationSpeed, bool Draggable, double Density,
    string LandColor, string MarkerColor, bool Glow);

internal sealed record ModelViewerOptions(string? Src, string? Object, double RotationSpeed, bool Controls, bool Zoom, ModelViewerOrbit Orbit, double Exposure);

internal sealed record ConfettiBurst(int Id, ConfettiOrigin Origin, double Angle, double Spread, int Count, double Velocity, double Gravity);

internal sealed record ConfettiOptions(ConfettiBurst[] Bursts, string[] Palette);
