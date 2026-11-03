using System;
using UnityEngine;

namespace AgentVFX.Editor
{
    internal static class VfxSpecJson
    {
        public static bool TryParse(string json, out VfxSpec spec, out string error)
        {
            spec = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON 명세가 비어 있습니다.";
                return false;
            }

            try
            {
                spec = JsonUtility.FromJson<VfxSpec>(json);
            }
            catch (Exception exception)
            {
                error = $"JSON 파싱 실패: {exception.Message}";
                return false;
            }

            if (spec == null || string.IsNullOrWhiteSpace(spec.id))
            {
                error = "id는 필수입니다.";
                return false;
            }

            spec.texture ??= new TextureSpec();
            spec.mesh ??= new MeshSpec();
            spec.material ??= new MaterialSpec();
            spec.particle ??= new ParticleSpec();
            spec.prefab ??= new PrefabSpec();

            if (spec.texture.width < 2 || spec.texture.height < 2 || spec.texture.width > 2048 || spec.texture.height > 2048)
            {
                error = "texture.width/height는 2~2048 범위여야 합니다.";
                return false;
            }

            if (spec.mesh.segments < 3 || spec.mesh.segments > 256 || spec.mesh.radius <= 0f || spec.mesh.height <= 0f)
            {
                error = "mesh의 segments/radius/height 값이 유효하지 않습니다.";
                return false;
            }

            if (spec.particle.duration <= 0f || spec.particle.startLifetime <= 0f || spec.particle.maxParticles <= 0 || spec.particle.burstCount < 0)
            {
                error = "particle의 duration/lifetime/maxParticles/burstCount 값이 유효하지 않습니다.";
                return false;
            }

            return true;
        }

        public static string ComputeHash(string json)
        {
            return Hash128.Compute(json).ToString().Substring(0, 12);
        }
    }
}
